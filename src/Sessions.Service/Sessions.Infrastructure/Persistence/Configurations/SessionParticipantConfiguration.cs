using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Entities;

namespace Sessions.Infrastructure.Persistence.Configurations;

public class SessionParticipantConfiguration : IEntityTypeConfiguration<SessionParticipant>
{
    public void Configure(EntityTypeBuilder<SessionParticipant> builder)
    {
        builder.ToTable("SessionParticipants");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.SessionId)
            .IsRequired();

        builder.Property(sp => sp.UserId)
            .IsRequired();

        builder.Property(sp => sp.UserAlias)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(sp => sp.JoinedAt)
            .IsRequired();

        builder.HasOne<Session>()
            .WithMany(s => s.Participants)
            .HasForeignKey(sp => sp.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}