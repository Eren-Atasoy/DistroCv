using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Sends emails through a user's own Gmail account using OAuth2 tokens
/// and the Gmail API REST endpoint (v1/users/me/messages/send).
///
/// Flow:
/// 1. Loads user's encrypted RefreshToken from DB
/// 2. Exchanges RefreshToken for a fresh AccessToken via Google OAuth2 token endpoint
/// 3. Builds RFC 2822 plain-text email message
/// 4. Sends via Gmail API → email appears in user's "Sent" folder
///
/// Layer: Infrastructure/Services
/// </summary>
public class GmailDeliveryService : IGmailDeliveryService
{
    private readonly DistroCvDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GmailDeliveryService> _logger;
    private readonly string _googleClientId;
    private readonly string _googleClientSecret;

    private const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GmailSendEndpoint = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

    public GmailDeliveryService(
        DistroCvDbContext context,
        IEncryptionService encryptionService,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GmailDeliveryService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _httpClient = httpClient;
        _logger = logger;
        _googleClientId = configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId not configured");
        _googleClientSecret = configuration["Google:ClientSecret"] ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<GmailDeliveryResult> SendEmailAsync(
        GmailDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending email via Gmail API for user {UserId}: {SenderEmail} → {RecipientEmail}",
            request.UserId, request.SenderEmail, request.RecipientEmail);

        try
        {
            // ── Step 1: Get Access Token ───────────────────────
            var accessToken = await GetAccessTokenAsync(request.UserId, cancellationToken);

            // ── Step 2: Build RFC 2822 message ─────────────────
            var rawMessage = BuildRawMessage(request);

            // ── Step 3: Send via Gmail API ─────────────────────
            var gmailMessageId = await SendViaGmailApiAsync(accessToken, rawMessage, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully via Gmail API. Message ID: {GmailMessageId}",
                gmailMessageId);

            return new GmailDeliveryResult
            {
                IsSuccess = true,
                GmailMessageId = gmailMessageId
            };
        }
        catch (GmailDeliveryException)
        {
            throw; // Re-throw typed exceptions as-is
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(
                "Gmail API rate limit (429) for user {UserId}: {Error}",
                request.UserId, ex.Message);

            return new GmailDeliveryResult
            {
                IsSuccess = false,
                ErrorMessage = $"Gmail API rate limit exceeded: {ex.Message}",
                IsRetryable = true,
                IsRateLimited = true
            };
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode == HttpStatusCode.Unauthorized ||
            ex.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogWarning(
                "Gmail API authentication error for user {UserId}: {Error}",
                request.UserId, ex.Message);

            return new GmailDeliveryResult
            {
                IsSuccess = false,
                ErrorMessage = $"Gmail authentication error: {ex.Message}",
                IsRetryable = true,
                IsTokenError = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error sending email via Gmail API for user {UserId}",
                request.UserId);

            return new GmailDeliveryResult
            {
                IsSuccess = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                IsRetryable = false
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasValidCredentialsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user != null && !string.IsNullOrEmpty(user.GmailRefreshToken);
    }

    // ── Private Helpers ────────────────────────────────────────

    /// <summary>
    /// Retrieves the user's refresh token from DB, decrypts it,
    /// and exchanges it for a fresh access token via Google OAuth2.
    /// </summary>
    private async Task<string> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new GmailDeliveryException(
                $"User {userId} not found",
                isRetryable: false, isTokenError: true);
        }

        if (string.IsNullOrEmpty(user.GmailRefreshToken))
        {
            throw new GmailDeliveryException(
                $"User {userId} has no Gmail refresh token. OAuth2 authorization required.",
                isRetryable: false, isTokenError: true);
        }

        // Decrypt the stored refresh token
        string refreshToken;
        try
        {
            refreshToken = _encryptionService.Decrypt(user.GmailRefreshToken);
        }
        catch (Exception ex)
        {
            throw new GmailDeliveryException(
                $"Failed to decrypt Gmail refresh token for user {userId}",
                ex, isRetryable: false, isTokenError: true);
        }

        // Exchange refresh token for access token
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _googleClientId),
            new KeyValuePair<string, string>("client_secret", _googleClientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("grant_type", "refresh_token")
        });

        var tokenResponse = await _httpClient.PostAsync(GoogleTokenEndpoint, tokenRequest, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Google token exchange failed for user {UserId}: {StatusCode} - {Error}",
                userId, tokenResponse.StatusCode, errorContent);

            var isRetryable = tokenResponse.StatusCode == HttpStatusCode.TooManyRequests
                           || tokenResponse.StatusCode == HttpStatusCode.ServiceUnavailable;

            throw new GmailDeliveryException(
                $"Google token exchange failed: {tokenResponse.StatusCode}",
                isRetryable: isRetryable,
                isRateLimited: tokenResponse.StatusCode == HttpStatusCode.TooManyRequests,
                isTokenError: true);
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        var tokenDoc = JsonDocument.Parse(tokenJson);

        if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
        {
            throw new GmailDeliveryException(
                "Google token response did not contain access_token",
                isRetryable: false, isTokenError: true);
        }

        return accessTokenElement.GetString()
            ?? throw new GmailDeliveryException("access_token was null", isRetryable: false, isTokenError: true);
    }

    /// <summary>
    /// Builds an RFC 2822 formatted plain-text email message and
    /// encodes it as base64url for the Gmail API.
    /// </summary>
    private static string BuildRawMessage(GmailDeliveryRequest request)
    {
        var message = new StringBuilder();
        message.AppendLine($"From: {request.SenderName} <{request.SenderEmail}>");
        message.AppendLine($"To: {request.RecipientName} <{request.RecipientEmail}>");
        message.AppendLine($"Subject: {request.Subject}");
        message.AppendLine("MIME-Version: 1.0");
        message.AppendLine("Content-Type: text/plain; charset=utf-8");
        message.AppendLine("Content-Transfer-Encoding: 7bit");
        message.AppendLine(); // Empty line separates headers from body
        message.Append(request.PlainTextBody);

        var rawBytes = Encoding.UTF8.GetBytes(message.ToString());
        return Convert.ToBase64String(rawBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Sends the base64url-encoded raw message via Gmail API REST endpoint.
    /// POST https://gmail.googleapis.com/gmail/v1/users/me/messages/send
    /// </summary>
    private async Task<string> SendViaGmailApiAsync(
        string accessToken,
        string rawBase64UrlMessage,
        CancellationToken cancellationToken)
    {
        var requestBody = JsonSerializer.Serialize(new { raw = rawBase64UrlMessage });
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, GmailSendEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = content;

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Gmail API send failed: {StatusCode} - {Error}",
                response.StatusCode, errorContent);

            // Throw with status-specific info so callers can handle appropriately
            throw new HttpRequestException(
                $"Gmail API error: {response.StatusCode} - {errorContent}",
                null,
                response.StatusCode);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseDoc = JsonDocument.Parse(responseJson);

        var messageId = responseDoc.RootElement.TryGetProperty("id", out var idElement)
            ? idElement.GetString() ?? "unknown"
            : "unknown";

        return messageId;
    }
}
