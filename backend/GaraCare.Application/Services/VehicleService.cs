using GaraCare.Application.DTOs.Vehicles;
using GaraCare.Application.Exceptions;
using GaraCare.Application.Interfaces;
using GaraCare.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GaraCare.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(IUnitOfWork unitOfWork, ILogger<VehicleService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<VehicleResponse> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _unitOfWork.Repository<Customer>().GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new EntityNotFoundException("Không tìm thấy khách hàng.");

        // Biển số trùng giữa 2 khách khác nhau chỉ log cảnh báo, không chặn cứng — xe có thể đổi
        // chủ hoặc Staff gõ nhầm; quyết định đã thống nhất với người dùng khi lên kế hoạch GARA-19.
        var duplicatePlate = await _unitOfWork.Repository<Vehicle>()
            .FindAsync(v => v.LicensePlate == request.LicensePlate && v.CustomerId != request.CustomerId, cancellationToken);
        if (duplicatePlate.Count > 0)
        {
            _logger.LogWarning(
                "Biển số {LicensePlate} đã tồn tại ở khách hàng khác (CustomerId hiện tại: {CustomerId}).",
                request.LicensePlate,
                request.CustomerId);
        }

        var vehicle = new Vehicle
        {
            CustomerId = customer.Id,
            LicensePlate = request.LicensePlate,
            Brand = request.Brand,
            Model = request.Model,
            Year = request.Year,
        };
        await _unitOfWork.Repository<Vehicle>().AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(vehicle);
    }

    public async Task<IReadOnlyList<VehicleResponse>> GetByCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var vehicles = await _unitOfWork.Repository<Vehicle>().FindAsync(v => v.CustomerId == customerId, cancellationToken);
        return vehicles.Select(ToResponse).ToList();
    }

    private static VehicleResponse ToResponse(Vehicle vehicle) => new()
    {
        Id = vehicle.Id,
        CustomerId = vehicle.CustomerId,
        LicensePlate = vehicle.LicensePlate,
        Brand = vehicle.Brand,
        Model = vehicle.Model,
        Year = vehicle.Year,
    };
}
