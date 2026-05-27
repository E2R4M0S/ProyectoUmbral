using Microsoft.EntityFrameworkCore;
using Teams.Domain.Entities;
using Teams.Infrastructure.Persistence.Configurations;

namespace Teams.Infrastructure;

public class TeamsDbContext : DbContext
{
    public TeamsDbContext(DbContextOptions<TeamsDbContext> options) : base(options)
    {
    }

    public DbSet<Participant> Participants => Set<Participant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ParticipantConfiguration());
    }
}
