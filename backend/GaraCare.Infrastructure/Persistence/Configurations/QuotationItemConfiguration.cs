using GaraCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaraCare.Infrastructure.Persistence.Configurations;

public class QuotationItemConfiguration : IEntityTypeConfiguration<QuotationItem>
{
    public void Configure(EntityTypeBuilder<QuotationItem> builder)
    {
        builder.Property(q => q.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(q => q.Description).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(q => q.UnitPrice).HasColumnType("decimal(18,2)");

        builder.HasOne(q => q.WorkOrder)
            .WithMany(w => w.QuotationItems)
            .HasForeignKey(q => q.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.Part)
            .WithMany(p => p.QuotationItems)
            .HasForeignKey(q => q.PartId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
