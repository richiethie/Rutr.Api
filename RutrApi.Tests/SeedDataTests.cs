using FluentAssertions;
using Rutr.Api.Data;

namespace RutrApi.Tests;

public class SeedDataTests
{
    [Fact]
    public void Demo_password_hash_is_stable()
    {
        var a = SeedData.HashPassword(SeedData.DemoPassword);
        var b = SeedData.HashPassword(SeedData.DemoPassword);
        a.Should().Be(b);
        a.Should().HaveLength(64);
    }
}
