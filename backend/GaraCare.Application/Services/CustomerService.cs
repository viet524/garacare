using GaraCare.Application.DTOs.Customers;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using GaraCare.Domain.Entities;

namespace GaraCare.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerResponse?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var matches = await _unitOfWork.Repository<Customer>().FindAsync(c => c.Phone == phone, cancellationToken);
        var customer = matches.FirstOrDefault();
        return customer is null ? null : ToResponse(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customers = _unitOfWork.Repository<Customer>();

        // Chỉ chặn trùng SĐT khi đã gắn tài khoản đăng nhập (UserId != null) — tránh tạo 2 hồ sơ
        // cho cùng 1 khách đã có tài khoản Customer portal. Khách vãng lai (UserId null) trùng SĐT
        // với nhau vẫn cho phép (VD: khách cho số người thân, hoặc đổi số) — quyết định đã thống
        // nhất với người dùng khi lên kế hoạch GARA-18.
        var linkedToAccount = await customers.FindAsync(c => c.Phone == request.Phone && c.UserId != null, cancellationToken);
        if (linkedToAccount.Count > 0)
        {
            throw new DuplicatePhoneException("Số điện thoại này đã gắn với một tài khoản khách hàng khác.");
        }

        var customer = new Customer
        {
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
        };
        await customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _unitOfWork.Repository<Customer>().GetAllAsync(cancellationToken);
        return customers.Select(ToResponse).ToList();
    }

    private static CustomerResponse ToResponse(Customer customer) => new()
    {
        Id = customer.Id,
        FullName = customer.FullName,
        Phone = customer.Phone,
        Email = customer.Email,
        Address = customer.Address,
        UserId = customer.UserId,
    };
}
