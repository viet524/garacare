using GaraCare.Domain.Enums;

namespace GaraCare.Domain.Entities;

public class Appointment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string ScheduledTimeSlot { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsLate { get; set; }
    public DateTime? LateNotifiedStaffAt { get; set; }

    public WorkOrder? WorkOrder { get; set; }
}
