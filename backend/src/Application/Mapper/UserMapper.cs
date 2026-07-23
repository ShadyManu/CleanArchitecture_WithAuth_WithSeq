using Application.Dtos.Auth.Response;
using Domain.Entities.Auth;

namespace Application.Mapper;

public static class UserMapper
{
    public static UserResponse ToDto(this UserEntity entity) =>
        new()
        {
            Email = entity.Email,
            Username = entity.Username,
            Provider = entity.Provider,
            ProviderId = entity.ProviderId
        };
}
