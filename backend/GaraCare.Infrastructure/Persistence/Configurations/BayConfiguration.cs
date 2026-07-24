using GaraCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaraCare.Infrastructure.Persistence.Configurations;

public class BayConfiguration : IEntityTypeConfiguration<Bay>
{
    public void Configure(EntityTypeBuilder<Bay> builder)
    {
        builder.Property(b => b.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(b => b.CurrentWorkOrder)
            .WithMany()
            .HasForeignKey(b => b.CurrentWorkOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
