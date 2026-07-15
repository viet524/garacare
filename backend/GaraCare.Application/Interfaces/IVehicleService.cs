using GaraCare.Application.DTOs.Vehicles;

namespace GaraCare.Application.Interfaces;

public interface IVehicleService
{
    Task<VehicleResponse> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VehicleResponse>> GetByCustomerAsync(int customerId, CancellationToken cancellationToken = default);
}
