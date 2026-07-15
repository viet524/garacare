using GaraCare.Application.DTOs.QuotationItems;

namespace GaraCare.Application.Interfaces;

public interface IQuotationItemService
{
    Task<QuotationItemResponse> AddAsync(AddQuotationItemRequest request, CancellationToken cancellationToken = default);
    Task RemoveAsync(int itemId, CancellationToken cancellationToken = default);
}
