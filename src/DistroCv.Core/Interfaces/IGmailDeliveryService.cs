namespace DistroCv.Core.Interfaces;

/// <summary>
/// Sends emails through a user's own Gmail account using OAuth2 refresh tokens
/// and the Gmail API (v1/users/me/messages/send).
/// The email appears in the user's "Sent Items" folder for 100% organic delivery.
/// Layer: Infrastructure
/// </summary>
public interface IGmailDeliveryService
{
    /// <summary>
    /// Sends a plain-text email from the user's Gmail account.
    /// 1. Retrieves the user's encrypted RefreshToken from DB
    /// 2. Exchanges it for a fresh AccessToken via Google OAuth2
    /// 3. Sends via Gmail API so it lands in user's "Sent" folder
    /// </summary>
    /// <param name="request">Delivery request with all email data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Gmail message ID on success</returns>
    /// <exception cref="GmailDeliveryException">On token errors, rate limits, or API failures</exception>
    Task<GmailDeliveryResult> SendEmailAsync(
        GmailDeliveryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a user has a valid Gmail refresh token stored
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has valid Gmail OAuth2 credentials</returns>
    Task<bool> HasValidCredentialsAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to deliver an email via Gmail API
/// </summary>
public class GmailDeliveryRequest
{
    public Guid UserId { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    /// <summary>Plain-text email body (no HTML)</summary>
    public string PlainTextBody { get; set; } = string.Empty;
}

/// <summary>
/// Result of a Gmail delivery attempt
/// </summary>
public class GmailDeliveryResult
{
    public bool IsSuccess { get; set; }
    public string? GmailMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsRetryable { get; set; }

    /// <summary>True if the error was a 429 rate limit from Google</summary>
    public bool IsRateLimited { get; set; }

    /// <summary>True if the error was a token/auth issue</summary>
    public bool IsTokenError { get; set; }
}

/// <summary>
/// Exception thrown when Gmail delivery fails
/// </summary>
public class GmailDeliveryException : Exception
{
    public bool IsRetryable { get; }
    public bool IsRateLimited { get; }
    public bool IsTokenError { get; }

    public GmailDeliveryException(string message, bool isRetryable = false, bool isRateLimited = false, bool isTokenError = false)
        : base(message)
    {
        IsRetryable = isRetryable;
        IsRateLimited = isRateLimited;
        IsTokenError = isTokenError;
    }

    public GmailDeliveryException(string message, Exception innerException, bool isRetryable = false, bool isRateLimited = false, bool isTokenError = false)
        : base(message, innerException)
    {
        IsRetryable = isRetryable;
        IsRateLimited = isRateLimited;
        IsTokenError = isTokenError;
    }
}
