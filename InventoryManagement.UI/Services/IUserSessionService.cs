using CourseProject_InventoryManagement.Application.DTOs;

namespace InventoryManagement.UI.Services;

public interface IUserSessionService
{
    bool IsAuthenticated { get; }
    AuthTokenDto? GetTokens();
    UserDto? GetUser();
    string? GetLanguageCode();
    void SetTokens(AuthTokenDto tokens);
    void SetUser(UserDto user);
    void SetLanguageCode(string languageCode);
    void Clear();
}
