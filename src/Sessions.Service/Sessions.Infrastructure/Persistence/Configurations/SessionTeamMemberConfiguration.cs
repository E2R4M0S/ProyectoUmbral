using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Entities;

namespace Sessions.Infrastructure.Persistence.Configurations;

public class SessionTeamMemberConfiguration : IEntityTypeConfiguration<SessionTeamMember>
{
    public void Configure(EntityTypeBuilder<SessionTeamMember> builder)
    {
        builder.ToTable("SessionTeamMembers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.SessionTeamId).IsRequired();

        builder.Property(m => m.UserId).IsRequired();

        builder.Property(m => m.UserAlias)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.JoinedAt).IsRequired();

        builder.HasIndex(m => new { m.SessionTeamId, m.UserId }).IsUnique();
    }
}
