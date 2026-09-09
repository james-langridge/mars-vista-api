using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace MarsVista.Api.Middleware;

/// <summary>
/// Restores the client address behind Railway's edge. The edge reports the
/// client in <see cref="ClientAddressHeader"/> with no X-Forwarded-For chain,
/// so the framework's forwarded-for slot is pointed at that header. The edge
/// connects from the 100.64.0.0/10 range as observed in production logs
/// (2026-09-09); only connections from that range are trusted to assert a
/// client address, so private-network callers and anything else keep their
/// connection address.
/// </summary>
internal static class ForwardedHeadersPolicy
{
    internal const string ClientAddressHeader = "X-Real-IP";

    internal static void Configure(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        options.ForwardedForHeaderName = ClientAddressHeader;
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("100.64.0.0"), 10));
    }

    /// <summary>
    /// Runs after the forwarded-headers middleware, which removes the header it
    /// consumed. A header still present means a sender outside the trusted range
    /// asserted a client address: either spoofing, or the edge range has moved
    /// and anonymous callers are silently sharing per-edge rate-limit buckets.
    /// </summary>
    internal static Task WarnOnUnconsumedClientAddress(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Headers.TryGetValue(ClientAddressHeader, out var asserted))
        {
            context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(ForwardedHeadersPolicy).FullName!)
                .LogWarning(
                    "Client address header {Header}={Asserted} not consumed for connection from {RemoteIp}; sender is outside the trusted edge range",
                    ClientAddressHeader, asserted.ToString(), context.Connection.RemoteIpAddress);
        }

        return next(context);
    }
}
