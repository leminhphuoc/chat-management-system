using ChatSupportSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatSupportSystem.Infrastructure.Data.Configurations;

public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.Property(a => a.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.OwnsOne(a => a.ShiftSchedule);
    }
}