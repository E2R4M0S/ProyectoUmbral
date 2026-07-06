using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Missions.Domain.Entities;

namespace Missions.Infrastructure.Persistence.Configurations;

public class MissionStageConfiguration : IEntityTypeConfiguration<MissionStage>
{
    public void Configure(EntityTypeBuilder<MissionStage> builder)
    {
        builder.ToTable("MissionStages");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(s => s.Order)
            .IsRequired();

        builder.Property(s => s.QrToken)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.Latitude)
            .IsRequired(false);

        builder.Property(s => s.Longitude)
            .IsRequired(false);

        builder.HasIndex(s => new { s.MissionId, s.Order })
            .IsUnique();

        builder.HasMany(s => s.Clues)
            .WithOne()
            .HasForeignKey(c => c.StageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
