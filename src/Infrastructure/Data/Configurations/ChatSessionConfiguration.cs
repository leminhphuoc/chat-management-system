using ChatSupportSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatSupportSystem.Infrastructure.Data.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.Property(s => s.UserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired();

        builder.Property(s => s.LastPollTime)
            .IsRequired();

        builder.Property(s => s.QueueEntryTime)
            .IsRequired();

        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.QueueEntryTime);
        builder.HasIndex(s => s.AssignedAgentId);

        builder.HasOne(s => s.AssignedAgent)
            .WithMany()
            .HasForeignKey(s => s.AssignedAgentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}