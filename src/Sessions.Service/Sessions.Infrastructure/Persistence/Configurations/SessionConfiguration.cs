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

        // Stages stored as JSONB owned collection — ALL scalar properties must be listed
        // so EF Core includes them in JSON serialization/deserialization.
        builder.OwnsMany(s => s.Stages, stage =>
        {
            stage.ToJson("Stages");
            stage.Property(st => st.MissionId).IsRequired();
            stage.Property(st => st.MissionStageId).IsRequired();
            stage.Property(st => st.MissionTitle).HasMaxLength(200).IsRequired();
            stage.Property(st => st.StageName).HasMaxLength(200).IsRequired();
            stage.Property(st => st.MissionType).HasMaxLength(50).IsRequired();
            stage.Property(st => st.Order).IsRequired();
            stage.Property(st => st.QrToken).HasMaxLength(500).IsRequired();
            stage.Property(st => st.TimeMinutes);
            stage.Property(st => st.Latitude);
            stage.Property(st => st.Longitude);
        });

        builder.HasIndex(s => s.Pin).IsUnique();
    }
}
