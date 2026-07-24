namespace GaraCare.Domain.Enums;

// Chỉ có ý nghĩa khi User.Role == Technician. Điều khiển auto-assign (docs/01-business-spec.md
// §11) và bảng nhận việc §10. InRepair khoá cứng không nhận việc mới; các trạng thái khác có
// thể nhận Diagnosing chen thêm tuỳ quy tắc auto-assign.
public enum TechnicianStatus
{
    Free,
    Diagnosing,
    WaitingOnCustomer,
    WaitingParts,
    InRepair
}
