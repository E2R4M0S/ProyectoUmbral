using Microsoft.EntityFrameworkCore;
using Sessions.Domain.Entities;
using Sessions.Infrastructure.Persistence.Configurations;

namespace Sessions.Infrastructure;

public class SessionsDbContext : DbContext
{
    public SessionsDbContext(DbContextOptions<SessionsDbContext> options) : base(options)
    {
    }

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionParticipant> SessionParticipants => Set<SessionParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SessionConfiguration());
        modelBuilder.ApplyConfiguration(new SessionParticipantConfiguration());
    }
}
