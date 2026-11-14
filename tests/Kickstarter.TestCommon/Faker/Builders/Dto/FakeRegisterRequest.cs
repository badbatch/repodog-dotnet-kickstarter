using Bogus;
using Kickstarter.Application.Contracts.Users;

namespace Kickstarter.TestCommon.Faker.Builders.Dto;

public sealed class FakeRegisterRequest : Faker<RegisterRequest>
{
    public FakeRegisterRequest()
    {
        CustomInstantiator(f => new RegisterRequest(
            f.Internet.Email(),
            f.Internet.Password(12),
            f.Internet.UserName()
        ));
    }

    public FakeRegisterRequest WithEmail(string email)
    {
        RuleFor(x => x.Email, email);
        return this;
    }

    public FakeRegisterRequest WithPassword(string password)
    {
        RuleFor(x => x.Password, password);
        return this;
    }

    public FakeRegisterRequest WithUserName(string? userName)
    {
        RuleFor(x => x.UserName, userName);
        return this;
    }
}
