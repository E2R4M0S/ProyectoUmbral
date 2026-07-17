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
    public DbSet<SessionTeam> SessionTeams => Set<SessionTeam>();
    public DbSet<SessionTeamMember> SessionTeamMembers => Set<SessionTeamMember>();
    public DbSet<SessionAuditEvent> SessionAuditEvents => Set<SessionAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SessionConfiguration());
        modelBuilder.ApplyConfiguration(new SessionParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new SessionTeamConfiguration());
        modelBuilder.ApplyConfiguration(new SessionTeamMemberConfiguration());
        modelBuilder.ApplyConfiguration(new SessionAuditEventConfiguration());
    }
}
