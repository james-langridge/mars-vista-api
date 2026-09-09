using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace MarsVista.Api.Middleware;

/// <summary>
/// Restores the client address behind Railway's edge. The edge reports the
/// client in X-Real-IP (no X-Forwarded-For chain) and connects from the
/// 100.64.0.0/10 range as observed in production logs; only connections from
/// that range are trusted to assert a client address, so private-network
/// callers and anything else keep their connection address.
/// </summary>
internal static class ForwardedHeadersPolicy
{
    internal static void Configure(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        options.ForwardedForHeaderName = "X-Real-IP";
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("100.64.0.0"), 10));
    }
}
