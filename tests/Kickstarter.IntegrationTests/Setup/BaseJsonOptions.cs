using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kickstarter.IntegrationTests.Setup;

public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false) },
    };
}
