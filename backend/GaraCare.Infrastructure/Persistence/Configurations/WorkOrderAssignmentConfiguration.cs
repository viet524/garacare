using GaraCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaraCare.Infrastructure.Persistence.Configurations;

public class WorkOrderAssignmentConfiguration : IEntityTypeConfiguration<WorkOrderAssignment>
{
    public void Configure(EntityTypeBuilder<WorkOrderAssignment> builder)
    {
        builder.Property(a => a.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.StageAtStart).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.StageAtEnd).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.HandoffReason).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.LaborHoursLogged).HasColumnType("decimal(6,2)");
        builder.Property(a => a.CommissionSplitPercent).HasColumnType("decimal(5,2)");

        builder.HasOne(a => a.WorkOrder)
            .WithMany(w => w.Assignments)
            .HasForeignKey(a => a.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Technician)
            .WithMany()
            .HasForeignKey(a => a.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ApprovedByUser)
            .WithMany()
            .HasForeignKey(a => a.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
