using GaraCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaraCare.Infrastructure.Persistence.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(w => w.InitialDescription).HasColumnType("nvarchar(max)");
        builder.Property(w => w.DiagnosisNote).HasColumnType("nvarchar(max)");
        builder.Property(w => w.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(w => w.DiscountPercent).HasColumnType("decimal(5,2)");
        builder.Property(w => w.ApprovalToken).HasMaxLength(200);

        builder.HasOne(w => w.Vehicle)
            .WithMany(v => v.WorkOrders)
            .HasForeignKey(w => w.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.CreatedByUser)
            .WithMany()
            .HasForeignKey(w => w.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
