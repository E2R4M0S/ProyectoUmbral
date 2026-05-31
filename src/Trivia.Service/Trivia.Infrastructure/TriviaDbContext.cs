using Microsoft.EntityFrameworkCore;

namespace Trivia.Infrastructure;

public class TriviaDbContext : DbContext
{
    public TriviaDbContext(DbContextOptions<TriviaDbContext> options) : base(options)
    {
    }

    // Domain sets
    public DbSet<Domain.Quiz>? Quizzes { get; set; }
    public DbSet<Domain.Question>? Questions { get; set; }
    public DbSet<Domain.Answer>? Answers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Domain.Quiz>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Name).IsUnique();
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.HasMany(x => x.Questions).WithOne(q => q.Quiz).HasForeignKey(q => q.QuizId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Domain.Question>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Text).IsRequired().HasMaxLength(2000);
            b.HasMany(x => x.Answers).WithOne(a => a.Question).HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Domain.Answer>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Text).IsRequired().HasMaxLength(1000);
        });
    }

    // Ensure DB created when starting (for development only)
    public void EnsureDatabaseCreated()
    {
        Database.EnsureCreated();
    }
}
