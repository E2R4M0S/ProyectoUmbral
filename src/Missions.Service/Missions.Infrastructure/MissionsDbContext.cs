using Microsoft.EntityFrameworkCore;
using Missions.Domain.Entities;
using Missions.Infrastructure.Persistence;
using Missions.Infrastructure.Persistence.Configurations;

namespace Missions.Infrastructure;

public class MissionsDbContext : DbContext
{
    public MissionsDbContext(DbContextOptions<MissionsDbContext> options) : base(options)
    {
    }

    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MissionStage> MissionStages => Set<MissionStage>();
    public DbSet<MissionClue> MissionClues => Set<MissionClue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MissionConfiguration());
        modelBuilder.ApplyConfiguration(new MissionStageConfiguration());
    }
}
