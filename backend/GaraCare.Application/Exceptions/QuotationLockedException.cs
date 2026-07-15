namespace GaraCare.Application.Exceptions;

// Thrown when adding/editing/removing a QuotationItem on a WorkOrder that already has at
// least one approved item. Maps to 400 — see docs/01-business-spec.md §5.
public class QuotationLockedException : BusinessException
{
    public QuotationLockedException(string message) : base(message)
    {
    }
}
