using Kickstarter.Application.Contracts.Users;
using Kickstarter.Domain.Entities;

namespace Kickstarter.Application.Mapping;

public static class UserMappings
{
    extension(ApplicationUser user)
    {
        public UserResponse ToResponse()
        {
            return new UserResponse(user.Id, user.UserName, user.Email);
        }
    }
}
