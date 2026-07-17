using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Entities;

namespace Sessions.Infrastructure.Persistence.Configurations;

public class SessionAuditEventConfiguration : IEntityTypeConfiguration<SessionAuditEvent>
{
    public void Configure(EntityTypeBuilder<SessionAuditEvent> builder)
    {
        builder.ToTable("SessionAuditEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SessionId)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.OccurredAt)
            .IsRequired();

        builder.HasIndex(e => e.SessionId);
    }
}
