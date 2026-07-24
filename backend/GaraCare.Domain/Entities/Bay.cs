using GaraCare.Domain.Enums;

namespace GaraCare.Domain.Entities;

public class Bay
{
    public int Id { get; set; }
    public BayType Type { get; set; }
    public BayStatus Status { get; set; }
    public int? CurrentWorkOrderId { get; set; }
    public WorkOrder? CurrentWorkOrder { get; set; }
}
