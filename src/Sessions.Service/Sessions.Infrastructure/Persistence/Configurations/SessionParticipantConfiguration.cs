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

        builder.Property(sp => sp.JoinedAt)
            .IsRequired();

        builder.HasOne(sp => (Session)null!)
            .WithMany(s => s.Participants)
            .HasForeignKey(sp => sp.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}