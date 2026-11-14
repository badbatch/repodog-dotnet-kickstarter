using Kickstarter.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Kickstarter.Api.Startup;

public static class UserBootstrapper
{
    private const string ExampleRole = "ExampleUser";

    public static async Task EnsureUserAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync(ExampleRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(ExampleRole));
            EnsureSucceeded(roleResult, "creating the ExampleUser role");
        }

        var email = configuration["BootstrapUser:Email"]?.Trim();
        var password = configuration["BootstrapUser:Password"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Both BootstrapUser:Email and BootstrapUser:Password must be configured to create the bootstrap example user."
            );
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };

            var userResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(userResult, "creating the bootstrap example user");
        }

        if (!await userManager.IsInRoleAsync(user, ExampleRole))
        {
            var roleResult = await userManager.AddToRoleAsync(user, ExampleRole);
            EnsureSucceeded(roleResult, "assigning the ExampleUser role to the bootstrap example user");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Identity failed while {operation}: {errors}");
        }
    }
}
