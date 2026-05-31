using CourseProject_InventoryManagement.Application.DTOs;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.AuthCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.UserCommands;
using CourseProject_InventoryManagement.Domain.Enums;
using InventoryManagement.UI.Models;

namespace InventoryManagement.UI.Services;

public interface IAuthenticationFacade
{
    bool IsSignedIn { get; }
    UserDto? GetCachedUser();
    Task<ApiCallResult<UserDto>> LoginAsync(LoginUserCommand command, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDto>> ExternalGoogleLoginAsync(string idToken, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDto>> ExternalMicrosoftLoginAsync(string idToken, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDto>> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<ApiCallResult<List<ActiveSessionDto>>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDto>> UpdateLanguageAsync(LanguageType language, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDto>> UpdateThemeAsync(ThemeType theme, CancellationToken cancellationToken = default);
    Task<ApiCallResult<int>> RevokeAllRefreshTokensAsync(CancellationToken cancellationToken = default);
    Task<ApiCallResult<SalesforceIntegrationResultDto>> IntegrateSalesforceAsync(IntegrateSalesforceCommand command, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}
