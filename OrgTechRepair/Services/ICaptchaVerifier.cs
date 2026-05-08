namespace OrgTechRepair.Services;

public interface ICaptchaVerifier
{
    Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken cancellationToken = default);
}
