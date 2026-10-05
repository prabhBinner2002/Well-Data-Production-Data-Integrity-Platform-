using WellData.Infrastructure.Auditing;

namespace WellData.Api.Auth;

/// <summary>The signed-in user of the current request, or "system" for background work.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string Name => httpContextAccessor.HttpContext?.User.Identity?.Name ?? SystemUser.SystemName;
}
