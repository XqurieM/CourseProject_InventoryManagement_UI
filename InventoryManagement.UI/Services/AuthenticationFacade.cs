using CourseProject_InventoryManagement.Application.DTOs;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.AuthCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.UserCommands;
using CourseProject_InventoryManagement.Domain.Enums;
using InventoryManagement.UI.Models;

namespace InventoryManagement.UI.Services;

public sealed class AuthenticationFacade : IAuthenticationFacade
{
    private readonly BackendApiClient _backendApiClient;
    private readonly IUserSessionService _userSessionService;

    public AuthenticationFacade(BackendApiClient backendApiClient, IUserSessionService userSessionService)
    {
        _backendApiClient = backendApiClient;
        _userSessionService = userSessionService;
    }

    public bool IsSignedIn => _userSessionService.IsAuthenticated;

    public UserDto? GetCachedUser() => _userSessionService.GetUser();

    public async Task<ApiCallResult<UserDto>> LoginAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        var authResult = await _backendApiClient.PostAsync<LoginUserCommand, AuthTokenDto>(
            "Auth/Login",
            command,
            requiresAuth: false,
            cancellationToken);

        return await FinalizeAuthenticationAsync(authResult, cancellationToken);
    }

    public async Task<ApiCallResult<UserDto>> ExternalGoogleLoginAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return ApiCallResult<UserDto>.Failure(StatusCodes.Status400BadRequest, "Google token is required.");
        }

        var authResult = await _backendApiClient.GetAsync<AuthTokenDto>(
            $"Auth/ExternalLoginGoogle?token={Uri.EscapeDataString(idToken)}",
            requiresAuth: false,
            cancellationToken);

        return await FinalizeAuthenticationAsync(authResult, cancellationToken);
    }

    public async Task<ApiCallResult<UserDto>> ExternalMicrosoftLoginAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return ApiCallResult<UserDto>.Failure(StatusCodes.Status400BadRequest, "Microsoft token is required.");
        }

        var authResult = await _backendApiClient.GetAsync<AuthTokenDto>(
            $"Auth/ExternalLoginMicrosoft?token={Uri.EscapeDataString(idToken)}",
            requiresAuth: false,
            cancellationToken);

        return await FinalizeAuthenticationAsync(authResult, cancellationToken);
    }

    public async Task<ApiCallResult<UserDto>> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        var authResult = await _backendApiClient.PostAsync<RegisterUserCommand, AuthTokenDto>(
            "Auth/Register",
            command,
            requiresAuth: false,
            cancellationToken);

        return await FinalizeAuthenticationAsync(authResult, cancellationToken);
    }

    public async Task<ApiCallResult<UserDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var result = await _backendApiClient.GetAsync<UserDto>("Auth/Me", requiresAuth: true, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            _userSessionService.SetUser(result.Value);
        }

        return result;
    }

    public Task<ApiCallResult<List<ActiveSessionDto>>> GetActiveSessionsAsync(CancellationToken cancellationToken = default) =>
        _backendApiClient.GetAsync<List<ActiveSessionDto>>("Auth/GetActiveSessions", requiresAuth: true, cancellationToken);

    public async Task<ApiCallResult<UserDto>> UpdateLanguageAsync(LanguageType language, CancellationToken cancellationToken = default)
    {
        var result = await _backendApiClient.PostAsync<UpdateUserLanguageCommand, UserDto>(
            "Auth/UpdateLanguage",
            new UpdateUserLanguageCommand { PreferredLanguage = language },
            requiresAuth: true,
            cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            _userSessionService.SetUser(result.Value);
        }

        return result;
    }

    public async Task<ApiCallResult<UserDto>> UpdateThemeAsync(ThemeType theme, CancellationToken cancellationToken = default)
    {
        var result = await _backendApiClient.PostAsync<UpdateUserThemeCommand, UserDto>(
            "Auth/UpdateTheme",
            new UpdateUserThemeCommand { PreferredTheme = theme },
            requiresAuth: true,
            cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            _userSessionService.SetUser(result.Value);
        }

        return result;
    }

    public Task<ApiCallResult<int>> RevokeAllRefreshTokensAsync(CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync<RevokeAllRefreshTokensCommand, int>(
            "Auth/RevokeAllRefreshTokens",
            new RevokeAllRefreshTokensCommand(),
            requiresAuth: true,
            cancellationToken);

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var tokens = _userSessionService.GetTokens();
        if (!string.IsNullOrWhiteSpace(tokens?.RefreshToken))
        {
            await _backendApiClient.PostAsync(
                "Auth/RevokeRefreshToken",
                new RevokeRefreshTokenCommand { RefreshToken = tokens.RefreshToken },
                requiresAuth: true,
                cancellationToken);
        }

        _userSessionService.Clear();
    }

    private async Task<ApiCallResult<UserDto>> FinalizeAuthenticationAsync(
        ApiCallResult<AuthTokenDto> authResult,
        CancellationToken cancellationToken)
    {
        if (!authResult.IsSuccess || authResult.Value is null)
        {
            return ApiCallResult<UserDto>.Failure(authResult.StatusCode, authResult.ErrorMessage);
        }

        _userSessionService.SetTokens(authResult.Value);

        var meResult = await GetCurrentUserAsync(cancellationToken);
        if (meResult.IsSuccess)
        {
            return meResult;
        }

        var fallbackUser = new UserDto
        {
            Id = authResult.Value.UserId,
            UserName = authResult.Value.UserName,
            Email = authResult.Value.Email,
            IsAdmin = authResult.Value.IsAdmin
        };
        _userSessionService.SetUser(fallbackUser);

        return ApiCallResult<UserDto>.Success(fallbackUser);
    }
}
