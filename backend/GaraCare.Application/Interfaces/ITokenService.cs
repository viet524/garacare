using GaraCare.Domain.Entities;

namespace GaraCare.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
