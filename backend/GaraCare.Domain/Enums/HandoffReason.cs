namespace GaraCare.Domain.Enums;

// Chỉ có giá trị khi WorkOrderAssignment.Role == Handoff.
public enum HandoffReason
{
    SickLeave,
    ShiftEnd,
    Reassigned
}
