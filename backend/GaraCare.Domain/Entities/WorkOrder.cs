using GaraCare.Domain.Enums;

namespace GaraCare.Domain.Entities;

public class WorkOrder
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public WorkOrderStatus Status { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string? InitialDescription { get; set; }
    public string? DiagnosisNote { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountPercent { get; set; }

    // Đổi tên từ EstimatedCompletionDate (v5, docs/03-data-model.md): hệ thống tự tính
    // (không sửa tay), công thức ở docs/01-business-spec.md §12.
    public DateTime? SystemSuggestedDate { get; set; }

    // Staff xác nhận/tăng buffer rồi gửi cho khách — guard: FinalEstimatedDate >= SystemSuggestedDate.
    public DateTime? FinalEstimatedDate { get; set; }

    // True nếu DiagnosisRecord.EstimatedLaborHours > 2 giờ (Heavy Repair, docs/01-business-spec.md §3 bước 4).
    public bool IsHeavyRepair { get; set; }

    public bool IsDelayed { get; set; }
    public DateTime? QuoteSentAt { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public bool NeedsFollowUpCall { get; set; }
    public DateTime? CompletedDate { get; set; }

    // Chuỗi ngẫu nhiên dùng để khách vãng lai duyệt/từ chối báo giá qua link không cần đăng nhập.
    public string? ApprovalToken { get; set; }
    public DateTime? ApprovalTokenExpiresAt { get; set; }
    public DateTime? ApprovalTokenUsedAt { get; set; }

    public ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();
    public Payment? Payment { get; set; }
    public ICollection<WorkOrderStatusHistory> StatusHistory { get; set; } = new List<WorkOrderStatusHistory>();
    public DiagnosisRecord? DiagnosisRecord { get; set; }
    public ICollection<WorkOrderAssignment> Assignments { get; set; } = new List<WorkOrderAssignment>();
}
