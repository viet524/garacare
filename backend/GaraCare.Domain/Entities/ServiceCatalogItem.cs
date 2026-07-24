using GaraCare.Domain.Enums;

namespace GaraCare.Domain.Entities;

public class ServiceCatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public BayType? RequiredBayType { get; set; }
    public bool IsMasterTechRequired { get; set; }
}
