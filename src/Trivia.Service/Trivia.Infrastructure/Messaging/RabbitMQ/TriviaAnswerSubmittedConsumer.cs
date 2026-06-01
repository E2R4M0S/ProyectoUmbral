using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Trivia.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Lightweight RabbitMQ consumer that uses BasicGet polling via reflection.
/// This avoids direct compile-time dependency on RabbitMQ client types and keeps
/// the infra project build stable while still supporting runtime consumption
/// when RabbitMQ.Client is available at runtime.
///
/// Behavior: periodically polls the queue "trivia.answer.submitted" with BasicGet,
/// processes messages synchronously and acknowledges them. A production-ready
/// consumer should use the IModel eventing consumer and more robust error handling.
/// </summary>
public class TriviaAnswerSubmittedConsumer : BackgroundService
{
    private readonly ILogger<TriviaAnswerSubmittedConsumer> _logger;
    private object? _connection;
    private object? _model;

    public TriviaAnswerSubmittedConsumer(ILogger<TriviaAnswerSubmittedConsumer> logger)
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => RunAsync(stoppingToken), stoppingToken);
    }

    private void RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Try to locate RabbitMQ's ConnectionFactory type via reflection
            var factoryType = Type.GetType("RabbitMQ.Client.ConnectionFactory, RabbitMQ.Client");
            if (factoryType == null)
            {
                _logger.LogWarning("RabbitMQ client library not found; consumer will not start.");
                return;
            }

            var factory = Activator.CreateInstance(factoryType)!;
            // Set HostName property if present
            var hostProp = factoryType.GetProperty("HostName");
            hostProp?.SetValue(factory, Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq");

            // Create connection and model
            var createConn = factoryType.GetMethod("CreateConnection", Type.EmptyTypes);
            var conn = createConn?.Invoke(factory, null);
            if (conn == null)
            {
                _logger.LogWarning("Unable to create RabbitMQ connection via reflection.");
                return;
            }

            var connType = conn.GetType();
            var createModel = connType.GetMethod("CreateModel", Type.EmptyTypes);
            var model = createModel?.Invoke(conn, null);
            if (model == null)
            {
                _logger.LogWarning("Unable to create RabbitMQ model/channel via reflection.");
                return;
            }

            _connection = conn;
            _model = model;

            // Declare exchange and queue using IModel methods via reflection
            var modelType = model.GetType();
            var exchangeDeclare = modelType.GetMethod("ExchangeDeclare", new Type[] { typeof(string), typeof(string), typeof(bool) });
            exchangeDeclare?.Invoke(model, new object[] { "trivia", "topic", true });

            var queueDeclare = modelType.GetMethod("QueueDeclare", new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(IDictionary<string, object>) });
            queueDeclare?.Invoke(model, new object[] { "trivia.answer.submitted", true, false, false, null });

            var queueBind = modelType.GetMethod("QueueBind", new Type[] { typeof(string), typeof(string), typeof(string), typeof(IDictionary<string, object>) });
            if (queueBind != null)
            {
                queueBind.Invoke(model, new object[] { "trivia.answer.submitted", "trivia", "answer.submitted", null });
            }

            _logger.LogInformation("Started RabbitMQ polling consumer for trivia.answer.submitted");

            // Poll loop using BasicGet to avoid needing IBasicConsumer types at compile time
            var basicGetMethod = modelType.GetMethod("BasicGet", new Type[] { typeof(string), typeof(bool) });
            var basicAckMethod = modelType.GetMethod("BasicAck", new Type[] { typeof(ulong), typeof(bool) });

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = basicGetMethod?.Invoke(model, new object[] { "trivia.answer.submitted", false });
                    if (result != null)
                    {
                        var resultType = result.GetType();
                        // Body may be byte[] or ReadOnlyMemory<byte>
                        var bodyProp = resultType.GetProperty("Body");
                        byte[] bytes = Array.Empty<byte>();
                        if (bodyProp != null)
                        {
                            var bodyVal = bodyProp.GetValue(result);
                            if (bodyVal is byte[] bArr)
                            {
                                bytes = bArr;
                            }
                            else if (bodyVal != null)
                            {
                                var toArray = bodyVal.GetType().GetMethod("ToArray", Type.EmptyTypes);
                                if (toArray != null)
                                {
                                    bytes = (byte[])toArray.Invoke(bodyVal, null)!;
                                }
                            }
                        }

                        string message = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : "";
                        _logger.LogInformation("Received TriviaAnswerSubmittedEvent: {msg}", message);

                        // TODO: deserialize message and update leaderboard and DB here

                        // Acknowledge
                        var deliveryTagProp = resultType.GetProperty("DeliveryTag");
                        if (deliveryTagProp != null && basicAckMethod != null)
                        {
                            var tag = deliveryTagProp.GetValue(result);
                            // DeliveryTag is ulong
                            basicAckMethod.Invoke(model, new object[] { Convert.ToUInt64(tag), false });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while polling RabbitMQ");
                }

                Thread.Sleep(500);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ consumer");
        }
    }

    public override void Dispose()
    {
        try
        {
            var conn = _connection;
            var connType = conn?.GetType();
            var close = connType?.GetMethod("Close", Type.EmptyTypes);
            close?.Invoke(conn, null);
        }
        catch { }

        try
        {
            var model = _model;
            var modelType = model?.GetType();
            var closeModel = modelType?.GetMethod("Close", Type.EmptyTypes);
            closeModel?.Invoke(model, null);
        }
        catch { }

        base.Dispose();
    }
}
