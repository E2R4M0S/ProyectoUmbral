using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Missions.Domain.Entities;
using Missions.Domain.Enums;

namespace Missions.Infrastructure.Persistence.Configurations;

public class MissionConfiguration : IEntityTypeConfiguration<Mission>
{
    public void Configure(EntityTypeBuilder<Mission> builder)
    {
        builder.ToTable("Missions");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title)
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(m => m.Title).IsUnique();

        builder.Property(m => m.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(m => m.Difficulty)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.TimeMinutes)
            .IsRequired();

        builder.Property(m => m.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.HasMany(m => m.Stages)
            .WithOne(s => s.Mission)
            .HasForeignKey(s => s.MissionId);
    }
}