using System.Linq.Expressions;
using DevHabit.Contracts.Auth;
using DevHabit.Contracts.User;

namespace DevHabit.Api.Mappers;

public static class UserMapper
{
    public static User ToContract(this Domain.Entities.User user)
    {
        return new User
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            IdentityId = user.IdentityId
        };
    }

    public static Domain.Entities.User ToEntity(this RegisterUser registerUser)
    {
        return new Domain.Entities.User()
        {
            Id = $"u_{Guid.CreateVersion7()}",
            Name = registerUser.Name,
            Email = registerUser.Email,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}

public static class UserQueries
{
    public static Expression<Func<Domain.Entities.User, User>> ProjectToContract()
    {
        return user => new User
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            IdentityId = user.IdentityId
        };
    }
}
