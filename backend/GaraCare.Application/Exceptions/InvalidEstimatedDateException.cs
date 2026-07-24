namespace GaraCare.Application.Exceptions;

// FinalEstimatedDate nhỏ hơn SystemSuggestedDate (docs/01-business-spec.md §5, §12). Maps 400.
public class InvalidEstimatedDateException : BusinessException
{
    public InvalidEstimatedDateException(string message) : base(message)
    {
    }
}
