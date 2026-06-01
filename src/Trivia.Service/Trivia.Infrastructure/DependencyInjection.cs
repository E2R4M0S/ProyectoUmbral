using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trivia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Use Postgres provider per project skills
        services.AddDbContext<TriviaDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<Trivia.Application.Common.Interfaces.IQuizRepository, Trivia.Infrastructure.Persistence.QuizRepository>();
        services.AddScoped<Trivia.Application.Common.Interfaces.IParticipantAnswerRepository, Trivia.Infrastructure.Persistence.ParticipantAnswerRepository>();

        // Http-based event publisher to RealTimeHub (keeps microservice decoupling)
        services.AddHttpClient<Trivia.Application.Common.Interfaces.IEventPublisher, Trivia.Infrastructure.Messaging.HttpEventPublisher>(client =>
        {
            client.BaseAddress = new Uri(configuration["RealTimeHub:Url"] ?? "http://localhost:5005");
        });

        // RabbitMQ consumer for leaderboard updates
        services.AddHostedService<Trivia.Infrastructure.Messaging.RabbitMQ.TriviaAnswerSubmittedConsumer>();

        return services;
    }
}
