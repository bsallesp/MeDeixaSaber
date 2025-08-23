using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace MDS.Application.Security;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var ok = Validator.TryValidateObject(options, context, results, true);
        if (!ok) return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage!).ToArray());
        return options.SigningKey.Length < 32 ? ValidateOptionsResult.Fail("SigningKey too short") : ValidateOptionsResult.Success;
    }
}