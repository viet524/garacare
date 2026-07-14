using GaraCare.Domain.Enums;

namespace GaraCare.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool EmailSentSuccessfully { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
