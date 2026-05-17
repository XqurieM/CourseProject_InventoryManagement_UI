using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CourseProject_InventoryManagement.Application.DTOs;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.AuthCommands;
using InventoryManagement.UI.Models;
using InventoryManagement.UI.Options;
using Microsoft.Extensions.Options;

namespace InventoryManagement.UI.Services;

public sealed class BackendApiClient
{
    public const string HttpClientName = "backend-api";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserSessionService _userSessionService;
    private readonly string _baseUrl;

    public BackendApiClient(
        IHttpClientFactory httpClientFactory,
        IUserSessionService userSessionService,
        IOptions<BackendApiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _userSessionService = userSessionService;
        _baseUrl = options.Value.BaseUrl.TrimEnd('/');
    }

    public Task<ApiCallResult<T>> GetAsync<T>(string path, bool requiresAuth = true, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, requiresAuth, cancellationToken);

    public Task<ApiCallResult<TResponse>> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        bool requiresAuth = true,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Post, path, request, requiresAuth, cancellationToken);

    public async Task<ApiCallResult<TResponse>> PostMultipartAsync<TResponse>(
        string path,
        MultipartFormDataContent content,
        bool requiresAuth = true,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, path);

        if (requiresAuth)
        {
            var tokens = _userSessionService.GetTokens();
            if (!string.IsNullOrWhiteSpace(tokens?.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            }
        }

        request.Content = content;

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, HttpMethod.Post, path, cancellationToken);
            return ApiCallResult<TResponse>.Failure((int)response.StatusCode, error);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var value = await JsonSerializer.DeserializeAsync<TResponse>(stream, JsonOptions, cancellationToken);
        if (value is null)
        {
            return ApiCallResult<TResponse>.Failure((int)response.StatusCode, "The API returned an empty response.");
        }

        return ApiCallResult<TResponse>.Success(value, (int)response.StatusCode);
    }

    public Task<ApiCallResult> PostAsync<TRequest>(
        string path,
        TRequest request,
        bool requiresAuth = true,
        CancellationToken cancellationToken = default) =>
        SendWithoutResponseAsync(HttpMethod.Post, path, request, requiresAuth, cancellationToken);

    private async Task<ApiCallResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        var response = await SendCoreAsync(method, path, body, requiresAuth, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, method, path, cancellationToken);
            return ApiCallResult<T>.Failure((int)response.StatusCode, error);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        if (value is null)
        {
            return ApiCallResult<T>.Failure((int)response.StatusCode, "The API returned an empty response.");
        }

        return ApiCallResult<T>.Success(value, (int)response.StatusCode);
    }

    private async Task<ApiCallResult> SendWithoutResponseAsync(
        HttpMethod method,
        string path,
        object? body,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        var response = await SendCoreAsync(method, path, body, requiresAuth, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return ApiCallResult.Success((int)response.StatusCode);
        }

        var error = await ReadErrorAsync(response, method, path, cancellationToken);
        return ApiCallResult.Failure((int)response.StatusCode, error);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method,
        string path,
        object? body,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync(method, path, body, requiresAuth, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || !requiresAuth)
        {
            return response;
        }

        var refreshed = await TryRefreshTokenAsync(cancellationToken);
        if (!refreshed)
        {
            _userSessionService.Clear();
            return response;
        }

        response.Dispose();
        return await SendRequestAsync(method, path, body, requiresAuth, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpMethod method,
        string path,
        object? body,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(method, path);

        if (requiresAuth)
        {
            var tokens = _userSessionService.GetTokens();
            if (!string.IsNullOrWhiteSpace(tokens?.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            }
        }

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        var tokens = _userSessionService.GetTokens();
        if (string.IsNullOrWhiteSpace(tokens?.RefreshToken))
        {
            return false;
        }

        var client = CreateClient();
        var request = new RefreshAccessTokenCommand
        {
            RefreshToken = tokens.RefreshToken
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var response = await client.PostAsync(
            "Auth/Refresh",
            new StringContent(json, Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var refreshedTokens = await JsonSerializer.DeserializeAsync<AuthTokenDto>(stream, JsonOptions, cancellationToken);
        if (refreshedTokens is null)
        {
            return false;
        }

        _userSessionService.SetTokens(refreshedTokens);
        return true;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.BaseAddress = new Uri($"{_baseUrl}/");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, HttpMethod method, string path, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(raw)
            ? $"{method} {path} failed with status code {(int)response.StatusCode}."
            : raw;
    }
}
