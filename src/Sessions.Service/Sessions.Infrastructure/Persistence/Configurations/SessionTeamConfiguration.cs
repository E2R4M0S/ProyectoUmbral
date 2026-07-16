using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Entities;

namespace Sessions.Infrastructure.Persistence.Configurations;

public class SessionTeamConfiguration : IEntityTypeConfiguration<SessionTeam>
{
    public void Configure(EntityTypeBuilder<SessionTeam> builder)
    {
        builder.ToTable("SessionTeams");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.SessionId).IsRequired();

        builder.Property(t => t.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.MaxMembers)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(t => t.Score)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => new { t.SessionId, t.Name }).IsUnique();

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(t => t.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Members)
            .WithOne()
            .HasForeignKey(m => m.SessionTeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
