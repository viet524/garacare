using GaraCare.Domain.Enums;

namespace GaraCare.Domain.Entities;

public class WorkOrderStatusHistory
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public WorkOrderStatus FromStatus { get; set; }
    public WorkOrderStatus ToStatus { get; set; }

    // Null nếu chuyển do khách duyệt/từ chối qua ApprovalToken.
    public int? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }

    public bool ApprovedViaToken { get; set; }
    public DateTime ChangedAt { get; set; }
}
