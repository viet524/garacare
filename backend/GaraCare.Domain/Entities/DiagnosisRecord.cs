namespace GaraCare.Domain.Entities;

// Bất biến (immutable) sau khi tạo — không có API sửa/xoá. Sinh khi Technician ký xác nhận
// chẩn đoán (UC-03 bước 3, docs/02-use-cases.md).
public class DiagnosisRecord
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public int TechnicianId { get; set; }
    public User? Technician { get; set; }
    public string? Notes { get; set; }
    public decimal EstimatedLaborHours { get; set; }
    public DateTime SignedAt { get; set; }
}
