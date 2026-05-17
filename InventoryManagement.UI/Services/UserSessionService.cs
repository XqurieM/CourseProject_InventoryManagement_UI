using System.Text.Json;
using CourseProject_InventoryManagement.Application.DTOs;

namespace InventoryManagement.UI.Services;

public sealed class UserSessionService : IUserSessionService
{
    private const string TokensKey = "auth.tokens";
    private const string UserKey = "auth.user";
    private const string LanguageCodeKey = "ui.language";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserSessionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => GetTokens() is not null && GetUser() is not null;

    public AuthTokenDto? GetTokens() => Read<AuthTokenDto>(TokensKey);

    public UserDto? GetUser() => Read<UserDto>(UserKey);

    public string? GetLanguageCode() => GetSession().GetString(LanguageCodeKey);

    public void SetTokens(AuthTokenDto tokens) => Write(TokensKey, tokens);

    public void SetUser(UserDto user)
    {
        Write(UserKey, user);
        SetLanguageCode(MapLanguageCode(user.PreferredLanguage));
    }

    public void SetLanguageCode(string languageCode) => GetSession().SetString(LanguageCodeKey, languageCode);

    public void Clear()
    {
        var session = GetSession();
        session.Remove(TokensKey);
        session.Remove(UserKey);
        session.Remove(LanguageCodeKey);
    }

    private T? Read<T>(string key)
    {
        var session = GetSession();
        var json = session.GetString(key);
        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private void Write<T>(string key, T value)
    {
        var session = GetSession();
        session.SetString(key, JsonSerializer.Serialize(value, JsonOptions));
    }

    private ISession GetSession() =>
        _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("An active HTTP session is required.");

    private static string MapLanguageCode(CourseProject_InventoryManagement.Domain.Enums.LanguageType languageType) =>
        languageType switch
        {
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.Turkish => "tr",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.English => "en",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.German => "de",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.French => "fr",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.Spanish => "es",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.Italian => "it",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.Russian => "ru",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.Chinese => "zh",
            _ => "en"
        };
}
