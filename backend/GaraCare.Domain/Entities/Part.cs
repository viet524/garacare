namespace GaraCare.Domain.Entities;

public class Part
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }

    public ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();
}
