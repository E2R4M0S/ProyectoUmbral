using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Infrastructure.Messaging.RabbitMQ;

public class TypedTriviaAnswerSubmittedConsumer : BackgroundService
{
    private readonly ILogger<TypedTriviaAnswerSubmittedConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private global::RabbitMQ.Client.IConnection? _connection;
    private global::RabbitMQ.Client.IModel? _channel;
    private EventingBasicConsumer? _consumer;

    public TypedTriviaAnswerSubmittedConsumer(ILogger<TypedTriviaAnswerSubmittedConsumer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
        try
        {
            var factory = new ConnectionFactory();
            factory.HostName = host;
            factory.AutomaticRecoveryEnabled = true;

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Limit prefetch so we process one message at a time and avoid unacked buildup
            try
            {
                _channel.BasicQos(0, 1, false);
            }
            catch { }

            // Use the IModel interface to avoid runtime binder issues with dynamic across different
            // RabbitMQ.Client assembly versions.
            _channel.ExchangeDeclare("trivia", "topic", true);
            _channel.QueueDeclare("trivia.answer.submitted", true, false, false, null);
            _channel.QueueBind("trivia.answer.submitted", "trivia", "answer.submitted", null);

            // Use EventingBasicConsumer for reliable behavior across RabbitMQ.Client versions
            _consumer = new EventingBasicConsumer(_channel);
            _consumer.Registered += (sender, ea) => _logger.LogInformation("EventingBasicConsumer registered");
            _consumer.Unregistered += (sender, ea) => _logger.LogInformation("EventingBasicConsumer unregistered");
            _consumer.ConsumerCancelled += (sender, ea) => _logger.LogWarning("EventingBasicConsumer cancelled");
            _consumer.Shutdown += (sender, ea) => _logger.LogWarning("EventingBasicConsumer shutdown");

            _consumer.Received += (model, ea) =>
            {
                try { _logger.LogInformation("EventingBasicConsumer callback invoked for deliveryTag={Tag}", ea.DeliveryTag); } catch { }
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await OnMessageReceivedAsync(model, ea);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception while processing RabbitMQ message (deliveryTag={Tag})", ea.DeliveryTag);
                    }
                });
            };

            string consumerTag = string.Empty;
            try
            {
                consumerTag = _channel.BasicConsume("trivia.answer.submitted", false, _consumer);
                _logger.LogInformation("BasicConsume called for queue trivia.answer.submitted; consumerTag={Tag}", consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BasicConsume call failed");
            }
            _logger.LogInformation("Started typed RabbitMQ consumer for trivia.answer.submitted against host {Host}", host);

            // No polling fallback: this service is pure event-driven. Do not add BasicGet workarounds.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start typed RabbitMQ consumer; it will not run");
            // if startup fails, ensure connection/channel are cleaned
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
        }

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel == null)
        {
            return;
        }
        ulong deliveryTag = ea.DeliveryTag;
        bool acked = false;
        try
        {
            _logger.LogDebug("OnMessageReceivedAsync invoked (deliveryTag={Tag})", deliveryTag);
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received TriviaAnswerSubmittedEvent (typed): {msg}", message);

            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning("Message payload is not an object");
                return;
            }

            // Scoring for this answer already happened synchronously in SubmitAnswerCommandHandler
            // (correct, time-based delta via IScoringStrategy) and was already broadcast via
            // LeaderboardUpdated. This consumer used to ALSO add a flat +10 on top of that for every
            // correct answer, silently doubling every team's score — that's why a good answer could
            // show up as, say, 80 points instead of the 40 actually earned. It must not touch the
            // leaderboard; it only exists to drain the queue for whatever else may still publish here.
            _logger.LogDebug("TriviaAnswerSubmittedEvent consumed (no-op — scoring is synchronous)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing RabbitMQ message; will nack");
            try
            {
                _channel.BasicNack(deliveryTag, false, false);
                acked = true;
            }
            catch (Exception ex2)
            {
                _logger.LogWarning(ex2, "Failed to BasicNack message (deliveryTag={Tag})", deliveryTag);
            }
        }
        finally
        {
            if (!acked)
            {
                try
                {
                    _channel.BasicAck(deliveryTag, false);
                    _logger.LogDebug("Acknowledged message (deliveryTag={Tag})", deliveryTag);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to ack message in finally (deliveryTag={Tag})", deliveryTag);
                }
            }
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Actual work happens on consumer callbacks
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try { /* AsyncEventingBasicConsumer unsubscribes automatically on channel close */ } catch { }
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
        return base.StopAsync(cancellationToken);
    }
}
