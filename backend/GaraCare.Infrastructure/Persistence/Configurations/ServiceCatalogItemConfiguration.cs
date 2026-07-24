using GaraCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaraCare.Infrastructure.Persistence.Configurations;

public class ServiceCatalogItemConfiguration : IEntityTypeConfiguration<ServiceCatalogItem>
{
    public void Configure(EntityTypeBuilder<ServiceCatalogItem> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasColumnType("nvarchar(max)");
        builder.Property(s => s.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(s => s.RequiredBayType).HasConversion<string>().HasMaxLength(20);
    }
}
