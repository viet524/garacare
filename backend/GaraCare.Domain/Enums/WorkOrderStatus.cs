namespace GaraCare.Domain.Enums;

// Full state machine documented in docs/01-business-spec.md §3-5.
// Transitions must be validated in the Application (Service) layer only.
public enum WorkOrderStatus
{
    Received,
    Diagnosing,
    DiagnosisConfirmed,
    QuotePending,
    InRepair,
    WaitingParts,
    Completed,
    Delivered,
    Cancelled
}
