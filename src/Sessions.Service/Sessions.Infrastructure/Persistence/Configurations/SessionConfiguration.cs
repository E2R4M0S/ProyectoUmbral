using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Pin)
            .HasMaxLength(6)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.StartedAt);

        builder.Property(s => s.EndedAt);

        builder.Property(s => s.CurrentStageOrder)
            .IsRequired()
            .HasDefaultValue(0);

        // Stages stored as JSONB owned collection
        builder.OwnsMany(s => s.Stages, stage =>
        {
            stage.ToJson("Stages");
            stage.Property(st => st.MissionId).IsRequired();
            stage.Property(st => st.MissionTitle).HasMaxLength(200).IsRequired();
            stage.Property(st => st.MissionType).HasMaxLength(50).IsRequired();
            stage.Property(st => st.Order).IsRequired();
        });

        builder.HasIndex(s => s.Pin).IsUnique();
    }
}
