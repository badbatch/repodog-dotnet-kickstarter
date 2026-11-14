using Bogus;
using Kickstarter.Domain.Entities;

namespace Kickstarter.TestCommon.Faker.Builders.Entity;

public sealed class FakeApplicationUser : Faker<ApplicationUser>
{
    public FakeApplicationUser()
    {
        RuleFor(x => x.Id, f => f.Random.Guid());
        RuleFor(x => x.UserName, f => f.Internet.UserName());
        RuleFor(x => x.Email, f => f.Internet.Email());
        RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber());
    }

    public FakeApplicationUser WithId(Guid id)
    {
        RuleFor(x => x.Id, id);
        return this;
    }

    public FakeApplicationUser WithUserName(string? value)
    {
        RuleFor(x => x.UserName, value);
        return this;
    }

    public FakeApplicationUser WithEmail(string? value)
    {
        RuleFor(x => x.Email, value);
        return this;
    }

    public FakeApplicationUser WithPhoneNumber(string? value)
    {
        RuleFor(x => x.PhoneNumber, value);
        return this;
    }
}
