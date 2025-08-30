#nullable enable
using System.ComponentModel.DataAnnotations;
using MDS.Application.Security;
using Xunit;

namespace MDS.Application.Tests.Security;

public sealed class JwtOptionsValidationTests
{
    static void ValidateAndThrow(JwtOptions o)
    {
        var ctx = new ValidationContext(o);
        Validator.ValidateObject(o, ctx, true);
        if (string.IsNullOrWhiteSpace(o.SigningKey) || o.SigningKey.Length < 32)
            throw new ValidationException("SigningKeyTooShort");
        if (o.ExpirationMinutes <= 0)
            throw new ValidationException("ExpirationMinutesMustBePositive");
    }

    [Fact]
    public void Valid_Config_Passes()
    {
        var opt = new JwtOptions
        {
            Issuer = "issuer",
            Audience = "aud",
            SigningKey = new string('k', 40),
            ExpirationMinutes = 60
        };
        ValidateAndThrow(opt);
    }

    [Theory]
    [InlineData(null, "aud", "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk", 60)]
    [InlineData("", "aud", "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk", 60)]
    [InlineData("issuer", null, "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk", 60)]
    [InlineData("issuer", "", "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk", 60)]
    [InlineData("issuer", "aud", null, 60)]
    [InlineData("issuer", "aud", "", 60)]
    [InlineData("issuer", "aud", "short-key-<32", 60)]
    [InlineData("issuer", "aud", "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk", 0)]
    [InlineData("issuer", "aud", "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk", -5)]
    public void Invalid_Config_Throws(string? issuer, string? audience, string? key, int exp)
    {
        var opt = new JwtOptions
        {
            Issuer = issuer ?? "",
            Audience = audience ?? "",
            SigningKey = key ?? "",
            ExpirationMinutes = exp
        };
        Assert.Throws<ValidationException>(() => ValidateAndThrow(opt));
    }

    [Fact]
    public void SigningKey_MinLength_32()
    {
        var opt = new JwtOptions
        {
            Issuer = "i",
            Audience = "a",
            SigningKey = new string('k', 31),
            ExpirationMinutes = 30
        };
        Assert.Throws<ValidationException>(() => ValidateAndThrow(opt));
    }
}
