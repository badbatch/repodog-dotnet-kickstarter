using Bogus;
using Kickstarter.Application.Contracts.Users;

namespace Kickstarter.TestCommon.Faker.Builders.Dto;

public sealed class FakeUserResponse : Faker<UserResponse>
{
    public FakeUserResponse()
    {
        CustomInstantiator(f => new UserResponse(f.Random.Guid(), f.Internet.UserName(), f.Internet.Email()));
    }

    public FakeUserResponse WithId(Guid id)
    {
        RuleFor(x => x.Id, id);
        return this;
    }

    public FakeUserResponse WithUserName(string? userName)
    {
        RuleFor(x => x.UserName, userName);
        return this;
    }

    public FakeUserResponse WithEmail(string? email)
    {
        RuleFor(x => x.Email, email);
        return this;
    }
}
