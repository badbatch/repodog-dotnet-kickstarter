using Microsoft.Extensions.Time.Testing;

namespace Kickstarter.TestCommon;

public static class TestTime
{
    public static DateTime UtcNow => new FakeTimeProvider().GetUtcNow().UtcDateTime;

    public static DateTimeOffset UtcNowOffset => new FakeTimeProvider().GetUtcNow();
}
