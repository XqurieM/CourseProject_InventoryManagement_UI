using System.ComponentModel.DataAnnotations;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.AuthCommands;

namespace InventoryManagement.UI.Models;

public sealed class LoginPageViewModel
{
    public LoginUserCommand Form { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public sealed class RegisterPageViewModel
{
    public RegisterUserCommand Form { get; set; } = new();

    [Required]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public sealed class ExternalGoogleLoginRequest
{
    public string Token { get; set; } = string.Empty;
}

public sealed class ExternalMicrosoftLoginRequest
{
    public string Token { get; set; } = string.Empty;
}
