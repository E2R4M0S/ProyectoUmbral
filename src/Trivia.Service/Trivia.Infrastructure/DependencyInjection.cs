using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Common.Strategies;

namespace Trivia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Use PostgreSQL exclusively
        services.AddDbContext<TriviaDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<Trivia.Application.Common.Interfaces.IQuizRepository, Trivia.Infrastructure.Persistence.QuizRepository>();
        services.AddScoped<Trivia.Application.Common.Interfaces.IQuestionRepository, Trivia.Infrastructure.Persistence.QuestionRepository>();
        services.AddScoped<Trivia.Application.Common.Interfaces.IParticipantAnswerRepository, Trivia.Infrastructure.Persistence.ParticipantAnswerRepository>();
        services.AddScoped<Trivia.Application.Common.Interfaces.IAnswerRepository, Trivia.Infrastructure.Persistence.AnswerRepository>();
        services.AddScoped<Trivia.Application.Common.Interfaces.ILeaderboardRepository, Trivia.Infrastructure.Persistence.LeaderboardRepository>();
        services.AddScoped<IScoringStrategy, TimeBasedScoringStrategy>();
        // Application command handlers (register MediatR handlers in DI container via assemblies elsewhere; if manual registration needed, add here)

        // Configure an HTTP client that can be used as a fallback publisher to RealTimeHub
        services.AddHttpClient("RealTimeHub", client =>
        {
            client.BaseAddress = new Uri(configuration["RealTimeHub:Url"] ?? "http://localhost:5005");
        });

        // Prefer RabbitMQ publisher when RABBITMQ_HOST is present. If construction fails at startup
        // (library/version mismatch, connectivity), fall back to HTTP publisher so the service remains usable.
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_HOST")))
        {
            services.AddSingleton<Trivia.Application.Common.Interfaces.IEventPublisher>(sp =>
            {
                try
                {
                    // Try to construct RabbitMqEventPublisher; it may throw if RabbitMQ client isn't compatible
                    return ActivatorUtilities.CreateInstance<Trivia.Infrastructure.Messaging.RabbitMQ.RabbitMqEventPublisher>(sp, configuration);
                }
                catch (Exception ex)
                {
                    // Log and fallback to HTTP-based publisher
                    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Trivia.Infrastructure.Messaging.HttpEventPublisher>>();
                    logger.LogWarning(ex, "Falling back to HttpEventPublisher because RabbitMQ publisher failed to initialize");
                    var clientFactory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
                    var httpClient = clientFactory.CreateClient("RealTimeHub");
                    return new Trivia.Infrastructure.Messaging.HttpEventPublisher(httpClient, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Trivia.Infrastructure.Messaging.HttpEventPublisher>>());
                }
            });
        }
        else
        {
            // No RabbitMQ requested; use HTTP publisher
            services.AddSingleton<Trivia.Application.Common.Interfaces.IEventPublisher>(sp =>
            {
                var clientFactory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
                var httpClient = clientFactory.CreateClient("RealTimeHub");
                return new Trivia.Infrastructure.Messaging.HttpEventPublisher(httpClient, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Trivia.Infrastructure.Messaging.HttpEventPublisher>>());
            });
        }

        // RabbitMQ consumer for leaderboard updates: only register if RabbitMQ is configured
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_HOST")))
        {
            services.AddHostedService<Trivia.Infrastructure.Messaging.RabbitMQ.TypedTriviaAnswerSubmittedConsumer>();
        }

        services.AddHttpClient("realTimeHub", client =>
        {
            client.BaseAddress = new Uri(configuration["RealTimeHub:Url"] ?? "http://localhost:5005");
        });

        services.AddHttpClient("sessionsService", client =>
        {
            client.BaseAddress = new Uri(configuration["SessionsService:Url"] ?? "http://sessions.service:80");
        });

        return services;
    }
}
