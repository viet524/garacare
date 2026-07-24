using GaraCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaraCare.Infrastructure.Persistence.Configurations;

public class DiagnosisRecordConfiguration : IEntityTypeConfiguration<DiagnosisRecord>
{
    public void Configure(EntityTypeBuilder<DiagnosisRecord> builder)
    {
        builder.Property(d => d.Notes).HasColumnType("nvarchar(max)");
        builder.Property(d => d.EstimatedLaborHours).HasColumnType("decimal(6,2)");

        // 1-1 với WorkOrder — bất biến, mỗi WorkOrder chỉ có đúng 1 lần chẩn đoán được ký xác nhận.
        builder.HasIndex(d => d.WorkOrderId).IsUnique();

        builder.HasOne(d => d.WorkOrder)
            .WithOne(w => w.DiagnosisRecord)
            .HasForeignKey<DiagnosisRecord>(d => d.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Technician)
            .WithMany()
            .HasForeignKey(d => d.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
