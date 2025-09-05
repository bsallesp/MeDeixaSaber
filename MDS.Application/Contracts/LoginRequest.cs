using System.ComponentModel.DataAnnotations;

namespace MDS.Application.Contracts;

public sealed record LoginRequest(
    [Required(ErrorMessage = "Username is required.")]
    string Username,
    [Required(ErrorMessage = "Password is required.")]
    string Password
);