using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace OrgTechRepair.Services;

public sealed class TurnstileCaptchaVerifier : ICaptchaVerifier
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TurnstileCaptchaVerifier> _logger;

    public TurnstileCaptchaVerifier(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TurnstileCaptchaVerifier> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var provider = (_configuration["Security:Captcha:Provider"] ?? "turnstile").Trim().ToLowerInvariant();
        if (provider != "turnstile")
            return false;

        var secret = _configuration["Security:Captcha:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning("Captcha secret key is missing.");
            return false;
        }

        var verifyUrl = _configuration["Security:Captcha:VerifyUrl"]
                        ?? "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        var form = new Dictionary<string, string>
        {
            ["secret"] = secret,
            ["response"] = token
        };
        if (!string.IsNullOrWhiteSpace(remoteIp))
            form["remoteip"] = remoteIp;

        using var content = new FormUrlEncodedContent(form);
        using var response = await _httpClient.PostAsync(verifyUrl, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Turnstile verify HTTP status: {StatusCode}", (int)response.StatusCode);
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(cancellationToken: cancellationToken);
        if (payload?.Success == true)
            return true;

        _logger.LogWarning(
            "Turnstile rejected token. Errors={Errors}; Hostname={Hostname}; Action={Action}; CData={CData}",
            payload?.ErrorCodes == null ? "<none>" : string.Join(", ", payload.ErrorCodes),
            payload?.Hostname ?? "<none>",
            payload?.Action ?? "<none>",
            payload?.CData ?? "<none>");
        return false;
    }

    private sealed class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public List<string>? ErrorCodes { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("cdata")]
        public string? CData { get; set; }
    }
}
