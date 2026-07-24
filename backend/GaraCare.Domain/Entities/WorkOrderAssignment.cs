using GaraCare.Domain.Enums;

namespace GaraCare.Domain.Entities;

// Thay cho WorkOrder.TechnicianId cố định — lưu vết đóng góp của từng Technician trên 1
// WorkOrder, làm cơ sở chia hoa hồng công bằng khi reassign giữa chừng (docs/01-business-spec.md §13).
public class WorkOrderAssignment
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public int TechnicianId { get; set; }
    public User? Technician { get; set; }
    public AssignmentRole Role { get; set; }
    public TechnicianStatus StageAtStart { get; set; }
    public TechnicianStatus? StageAtEnd { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public HandoffReason? HandoffReason { get; set; }
    public decimal LaborHoursLogged { get; set; }
    public decimal CommissionSplitPercent { get; set; }
    public int ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
}
