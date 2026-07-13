using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using global::RabbitMQ.Client;
using global::RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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
    private dynamic? _connection;
    private dynamic? _model;
    private readonly IServiceProvider _serviceProvider;

    public TriviaAnswerSubmittedConsumer(ILogger<TriviaAnswerSubmittedConsumer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => RunAsync(stoppingToken), stoppingToken);
    }

    private void RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Use reflection to create the ConnectionFactory and connection to avoid dynamic binder mismatches
            var factoryType = Type.GetType("RabbitMQ.Client.ConnectionFactory, RabbitMQ.Client");
            if (factoryType == null)
            {
                _logger.LogWarning("RabbitMQ client library not found; consumer will not start.");
                return;
            }

            var factory = Activator.CreateInstance(factoryType)!;
            var hostProp = factoryType.GetProperty("HostName");
            hostProp?.SetValue(factory, Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq");

            // Find any CreateConnection method (sync or async) and invoke it, tolerating different signatures
            var createConn = factoryType.GetMethods().FirstOrDefault(m => m.Name.StartsWith("CreateConnection", StringComparison.Ordinal));
            if (createConn == null)
            {
                // Log available methods for debugging reflection mismatches
                try
                {
                    var methods = factoryType.GetMethods();
                    _logger.LogWarning("CreateConnection not found on ConnectionFactory ({Type}). Available methods: {Methods}", factoryType.FullName, string.Join(", ", methods.Select(m => m.ToString())));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed enumerating methods on ConnectionFactory");
                }

                _logger.LogWarning("ConnectionFactory.CreateConnection method not found; cannot start consumer.");
                return;
            }

            object? conn;
            var createConnParams = createConn.GetParameters();
            if (createConnParams.Length == 0)
            {
                conn = createConn.Invoke(factory, null);
            }
            else
            {
                // Build null args for reference types and default for value types
                var args = new object?[createConnParams.Length];
                for (int i = 0; i < createConnParams.Length; i++)
                {
                    var p = createConnParams[i];
                    if (p.ParameterType.IsValueType) args[i] = Activator.CreateInstance(p.ParameterType);
                    else args[i] = null;
                }
                conn = createConn.Invoke(factory, args);
            }

            if (conn == null)
            {
                _logger.LogWarning("CreateConnection invocation returned null; cannot start consumer.");
                return;
            }

            // If the CreateConnection returned a Task (CreateConnectionAsync), wait and extract the Result
            var connObj = conn;
            var taskType = typeof(System.Threading.Tasks.Task);
            var connType = connObj.GetType();
            if (taskType.IsAssignableFrom(connType))
            {
                // Try to get Result property (Task<T>)
                var resultProp = connType.GetProperty("Result");
                if (resultProp != null)
                {
                    connObj = resultProp.GetValue(connObj)!;
                }
                else
                {
                    // Non-generic Task: synchronously wait
                    var getAwaiter = connType.GetMethod("GetAwaiter");
                    var awaiter = getAwaiter!.Invoke(connObj, null)!;
                    var getResult = awaiter.GetType().GetMethod("GetResult");
                    getResult!.Invoke(awaiter, null);
                    _logger.LogWarning("CreateConnection returned non-generic Task with no result; cannot start consumer.");
                    return;
                }

                connType = connObj.GetType();
            }

            dynamic connDyn = connObj;

            // Find any CreateModel method (sync/async or different overloads)
            var createModel = connType.GetMethods().FirstOrDefault(m => m.Name.StartsWith("CreateModel", StringComparison.Ordinal));
            if (createModel == null)
            {
                try
                {
                    var methods = connType.GetMethods();
                    _logger.LogWarning("CreateModel not found on Connection ({Type}). Available methods: {Methods}", connType.FullName, string.Join(", ", methods.Select(m => m.ToString())));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed enumerating methods on Connection");
                }

                _logger.LogWarning("Connection.CreateModel method not found; cannot start consumer.");
                return;
            }

            var modelParams = createModel.GetParameters();
            var modelArgs = modelParams.Length == 0 ? null : new object?[modelParams.Length];
            for (int i = 0; i < (modelArgs?.Length ?? 0); i++)
            {
                var p = modelParams[i];
                modelArgs![i] = p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
            }

            dynamic model = createModel.Invoke(connObj, modelArgs)!;

            // Declare exchange/queue/binding
            model.ExchangeDeclare("trivia", "topic", true);
            model.QueueDeclare("trivia.answer.submitted", true, false, false, null);
            model.QueueBind("trivia.answer.submitted", "trivia", "answer.submitted", null);

            _connection = connObj;
            _model = model;

            _logger.LogInformation("Started RabbitMQ polling consumer for trivia.answer.submitted (dynamic)");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    dynamic result = model.BasicGet("trivia.answer.submitted", false);
                    if (result != null)
                    {
                        byte[] bytes = Array.Empty<byte>();
                        try
                        {
                            var body = result.Body;
                            if (body is byte[] b) bytes = b;
                            else
                            {
                                var toArray = body.GetType().GetMethod("ToArray", Type.EmptyTypes);
                                if (toArray != null) bytes = (byte[])toArray.Invoke(body, null)!;
                            }
                        }
                        catch { }

                        var message = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : string.Empty;
                        _logger.LogInformation("Received TriviaAnswerSubmittedEvent: {msg}", message);

                        try
                        {
                            var doc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonDocument>(message);
                            if (doc?.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var root = doc.RootElement;
                                var quizId = root.GetProperty("quizId").GetGuid();
                                var teamId = root.GetProperty("teamId").GetGuid();
                                var isCorrect = root.GetProperty("isCorrect").GetBoolean();

                                int delta = isCorrect ? 10 : 0;

                                using var scope = _serviceProvider.CreateScope();
                                var leaderboardRepo = scope.ServiceProvider.GetService<Trivia.Application.Common.Interfaces.ILeaderboardRepository>();
                                if (leaderboardRepo != null)
                                {
                                    var existing = leaderboardRepo.GetByTeamAsync(quizId, teamId).GetAwaiter().GetResult();
                                    if (existing == null)
                                    {
                                        var entry = new Trivia.Domain.Entities.LeaderboardEntry { QuizId = quizId, TeamId = teamId, Score = delta };
                                        leaderboardRepo.AddOrUpdateAsync(entry).GetAwaiter().GetResult();
                                    }
                                    else
                                    {
                                        existing.Score += delta;
                                        leaderboardRepo.AddOrUpdateAsync(existing).GetAwaiter().GetResult();
                                    }

                                    var publisher = scope.ServiceProvider.GetService<Trivia.Application.Common.Interfaces.IEventPublisher>();
                                    if (publisher != null)
                                    {
                                        var leaderboard = leaderboardRepo.GetByQuizAsync(quizId).GetAwaiter().GetResult();
                                        publisher.PublishAsync("LeaderboardUpdated", leaderboard).GetAwaiter().GetResult();
                                    }

                                    // quizId == sessionId (see QuestionCard.tsx): sync session participant score
                                    if (delta > 0)
                                    {
                                        try
                                        {
                                            var httpFactory = scope.ServiceProvider.GetService<System.Net.Http.IHttpClientFactory>();
                                            if (httpFactory != null)
                                            {
                                                var sessionsUrl = Environment.GetEnvironmentVariable("SESSIONS_SERVICE_URL") ?? "http://sessions.service:80";
                                                var scoreJson = System.Text.Json.JsonSerializer.Serialize(new { sessionId = quizId, userId = teamId, delta });
                                                var content = new System.Net.Http.StringContent(scoreJson, Encoding.UTF8, "application/json");
                                                var client = httpFactory.CreateClient();
                                                client.PostAsync($"{sessionsUrl}/internal/participants/score", content).GetAwaiter().GetResult();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, "Failed to sync trivia score to Sessions.Service");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to deserialize/handle TriviaAnswerSubmittedEvent");
                        }

                        try
                        {
                            var tag = result.DeliveryTag;
                            model.BasicAck((ulong)tag, false);
                        }
                        catch { }
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
