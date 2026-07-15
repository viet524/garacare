namespace GaraCare.Application.Exceptions;

// Thrown when sending a quote for a WorkOrder that has no QuotationItem yet. Maps to 400.
public class EmptyQuotationException : BusinessException
{
    public EmptyQuotationException(string message) : base(message)
    {
    }
}
