using GaraCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaraCare.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.TransactionRef).HasMaxLength(200);
        builder.Property(p => p.GatewayStatus).HasMaxLength(50);

        builder.HasOne(p => p.WorkOrder)
            .WithOne(w => w.Payment)
            .HasForeignKey<Payment>(p => p.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(p => p.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
