using Microsoft.EntityFrameworkCore;

namespace Trivia.Infrastructure;

public class TriviaDbContext : DbContext
{
    public TriviaDbContext(DbContextOptions<TriviaDbContext> options) : base(options)
    {
    }

    public DbSet<Trivia.Domain.Entities.Question> Questions => Set<Trivia.Domain.Entities.Question>();
    public DbSet<Trivia.Domain.Entities.ParticipantAnswer> ParticipantAnswers => Set<Trivia.Domain.Entities.ParticipantAnswer>();
    public DbSet<Trivia.Domain.Entities.Answer> Answers => Set<Trivia.Domain.Entities.Answer>();
}
