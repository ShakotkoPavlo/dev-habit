using System.Linq.Expressions;
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
