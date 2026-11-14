using Asp.Versioning;
using Kickstarter.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Kickstarter.Api.Controllers;

public sealed record ExampleResponse(string Message);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/example")]
[Authorize]
public sealed class ExampleController : ControllerBase
{
    [HttpGet(Name = "getExample")]
    public Ok<ExampleResponse> Get()
    {
        return TypedResults.Ok(new ExampleResponse("Hello from the API."));
    }

    [HttpGet("restricted", Name = "getRestrictedExample")]
    [Authorize(Policy = AuthorizationPolicies.ExampleAccess)]
    public Ok<ExampleResponse> GetRestricted()
    {
        return TypedResults.Ok(new ExampleResponse("You have access to the restricted example."));
    }
}
