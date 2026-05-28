using Alca.MonoGame.Kernel.Weather;

namespace Alca.MonoGame.Kernel.UnitTests.Weather;

public sealed class WeatherTypeIdTests
{
    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var a = new WeatherTypeId("rainy");
        var b = new WeatherTypeId("rainy");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentCase_ReturnsTrue()
    {
        var a = new WeatherTypeId("RAINY");
        var b = new WeatherTypeId("rainy");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var a = new WeatherTypeId("rainy");
        var b = new WeatherTypeId("snowy");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void EqualityOperator_SameValue_ReturnsTrue()
    {
        var a = new WeatherTypeId("fog");
        var b = new WeatherTypeId("fog");
        Assert.True(a == b);
    }

    [Fact]
    public void InequalityOperator_DifferentValue_ReturnsTrue()
    {
        var a = new WeatherTypeId("fog");
        var b = new WeatherTypeId("storm");
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_SameValueDifferentCase_SameHash()
    {
        var a = new WeatherTypeId("Sunny");
        var b = new WeatherTypeId("sunny");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void PredefinedTypes_AreDistinct()
    {
        WeatherTypeId[] all =
        [
            WeatherTypeId.Sunny, WeatherTypeId.HeatWave, WeatherTypeId.Cloudy,
            WeatherTypeId.Fog,   WeatherTypeId.Storm,    WeatherTypeId.Thunderstorm,
            WeatherTypeId.HailStorm, WeatherTypeId.Blizzard,
            WeatherTypeId.ColdSnap, WeatherTypeId.OrangeWind
        ];

        var distinct = all.Distinct().ToArray();
        Assert.Equal(all.Length, distinct.Length);
    }

    [Fact]
    public void CustomTypeId_NotEqualToPredefined()
    {
        var custom = new WeatherTypeId("radioactive_rain");
        Assert.NotEqual(custom, WeatherTypeId.Storm);
        Assert.NotEqual(custom, WeatherTypeId.Thunderstorm);
    }

    [Fact]
    public void Constructor_NullValue_TreatedAsEmpty()
    {
        var a = new WeatherTypeId(null!);
        Assert.Equal(string.Empty, a.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var id = new WeatherTypeId("blizzard");
        Assert.Equal("blizzard", id.ToString());
    }

    [Fact]
    public void Equals_Object_BoxedWeatherTypeId_ReturnsTrue()
    {
        WeatherTypeId a = new("fog");
        object boxed = new WeatherTypeId("fog");
        Assert.True(a.Equals(boxed));
    }

    [Fact]
    public void Equals_Object_WrongType_ReturnsFalse()
    {
        WeatherTypeId a = new("fog");
        Assert.False(a.Equals("fog"));
    }
}
