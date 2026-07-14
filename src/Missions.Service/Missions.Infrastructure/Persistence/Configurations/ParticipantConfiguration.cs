using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Missions.Domain.Entities;

namespace Missions.Infrastructure.Persistence.Configurations;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("Participants");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Alias)
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(p => p.Alias).IsUnique();

        builder.Property(p => p.Email)
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(p => p.Email).IsUnique();

        builder.Property(p => p.KeycloakUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();
    }
}
