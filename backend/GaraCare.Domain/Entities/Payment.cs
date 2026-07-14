using GaraCare.Domain.Enums;

namespace GaraCare.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }

    // Chỉ có giá trị nếu Cash/Card do Staff xác nhận.
    public int? ConfirmedByUserId { get; set; }
    public User? ConfirmedByUser { get; set; }

    public string? TransactionRef { get; set; }
    public string? GatewayStatus { get; set; }
    public DateTime PaidDate { get; set; }
}
