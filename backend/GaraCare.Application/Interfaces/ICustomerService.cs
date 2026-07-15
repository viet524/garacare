using GaraCare.Application.DTOs.Customers;

namespace GaraCare.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponse?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    // Danh sách đầy đủ cho màn "Quản lý khách hàng" (Staff/Admin) — chưa cần phân trang vì quy mô
    // demo còn nhỏ; bổ sung $filter/phân trang sau nếu dữ liệu lớn lên.
    Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
