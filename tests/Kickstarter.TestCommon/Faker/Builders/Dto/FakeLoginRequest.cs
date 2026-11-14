using Bogus;
using Kickstarter.Application.Contracts.Users;

namespace Kickstarter.TestCommon.Faker.Builders.Dto;

public sealed class FakeLoginRequest : Faker<LoginRequest>
{
    public FakeLoginRequest()
    {
        CustomInstantiator(f => new LoginRequest(f.Internet.Email(), f.Internet.Password(12)));
    }

    public FakeLoginRequest WithEmail(string email)
    {
        RuleFor(x => x.Email, email);
        return this;
    }

    public FakeLoginRequest WithPassword(string password)
    {
        RuleFor(x => x.Password, password);
        return this;
    }
}
