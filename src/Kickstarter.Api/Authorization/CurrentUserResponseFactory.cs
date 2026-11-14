using System.Security.Claims;
using Kickstarter.Application.Contracts.Users;
using Microsoft.AspNetCore.Authorization;

namespace Kickstarter.Api.Authorization;

public sealed class CurrentUserResponseFactory(IAuthorizationService authorizationService)
{
    public async Task<CurrentUserResponse> CreateAsync(UserResponse user, ClaimsPrincipal principal)
    {
        var roles = principal
            .Identities.Where(identity => identity.IsAuthenticated)
            .SelectMany(identity => identity.FindAll(identity.RoleClaimType))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var policies = new List<string>();

        foreach (var policy in AuthorizationPolicies.CurrentUserPolicies)
        {
            if ((await authorizationService.AuthorizeAsync(principal, null, policy)).Succeeded)
            {
                policies.Add(policy);
            }
        }

        return new CurrentUserResponse(user.Id, user.UserName, user.Email, roles, policies);
    }
}
