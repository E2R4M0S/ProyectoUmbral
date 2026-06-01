using Microsoft.EntityFrameworkCore;

namespace Trivia.Infrastructure;

public class TriviaDbContext : DbContext
{
    public TriviaDbContext(DbContextOptions<TriviaDbContext> options) : base(options)
    {
    }

    public DbSet<Trivia.Domain.Entities.Quiz> Quizzes => Set<Trivia.Domain.Entities.Quiz>();
<<<<<<< HEAD
    public DbSet<Trivia.Domain.Entities.Question> Questions => Set<Trivia.Domain.Entities.Question>();
    public DbSet<Trivia.Domain.Entities.ParticipantAnswer> ParticipantAnswers => Set<Trivia.Domain.Entities.ParticipantAnswer>();
=======
>>>>>>> 61b3cec (feat(hu-39): countdown timer frontend + backend endpoints and skeleton for answer submission and RabbitMQ consumer)
}
