using System.Net;
using System.Net.Sockets;

namespace SkillProof.Api.Services;

public class UrlSafetyValidator : IUrlSafetyValidator
{
    private static readonly HashSet<string> DisallowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "file", "ftp", "javascript", "data", "gopher", "ldap", "dict", "jar", "telnet"
    };

    public UrlSafetyCheckResult ValidateUrl(string? rawUrl, bool allowHttpInDev = false)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return new UrlSafetyCheckResult(false, null, "URL cannot be empty.");
        }

        var trimmed = rawUrl.Trim();

        // Scheme prefix basic validation
        if (trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return new UrlSafetyCheckResult(false, null, $"Disallowed scheme detected: '{trimmed.Split(':')[0]}'.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return new UrlSafetyCheckResult(false, null, "Malformed absolute URL format.");
        }

        var scheme = uri.Scheme.ToLowerInvariant();
        if (DisallowedSchemes.Contains(scheme))
        {
            return new UrlSafetyCheckResult(false, null, $"Disallowed URL scheme '{scheme}'. Only HTTPS is permitted.");
        }

        if (scheme == "http" && !allowHttpInDev)
        {
            return new UrlSafetyCheckResult(false, null, "Insecure HTTP scheme is rejected. Production verification requires HTTPS.");
        }

        if (scheme != "http" && scheme != "https")
        {
            return new UrlSafetyCheckResult(false, null, $"Unsupported scheme '{scheme}'. Only HTTPS is permitted.");
        }

        var host = uri.DnsSafeHost.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(host))
        {
            return new UrlSafetyCheckResult(false, null, "URL host cannot be empty.");
        }

        // Hostname safety checks
        if (host == "localhost" || host.EndsWith(".localhost") || host.EndsWith(".local") || host.EndsWith(".internal") || host.EndsWith(".lan"))
        {
            return new UrlSafetyCheckResult(false, null, $"Private or local hostname '{host}' is prohibited.");
        }

        // Direct IP check
        if (IPAddress.TryParse(host, out var directIp))
        {
            if (IsPrivateOrRestrictedIp(directIp))
            {
                return new UrlSafetyCheckResult(false, null, $"Prohibited private, loopback, or metadata IP address: {directIp}.");
            }
        }

        return new UrlSafetyCheckResult(true, uri.ToString(), null);
    }

    public async Task<UrlSafetyCheckResult> ValidateUrlAsync(string? rawUrl, bool allowHttpInDev = false, CancellationToken ct = default)
    {
        var syncResult = ValidateUrl(rawUrl, allowHttpInDev);
        if (!syncResult.IsSafe)
        {
            return syncResult;
        }

        var uri = new Uri(syncResult.NormalizedUrl!);
        var host = uri.DnsSafeHost;

        // If direct IP, sync result already checked
        if (IPAddress.TryParse(host, out _))
        {
            return syncResult;
        }

        // Resolve DNS and ensure no resolved address points to private or restricted networks
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, ct);
            if (addresses == null || addresses.Length == 0)
            {
                return new UrlSafetyCheckResult(false, null, $"Unable to resolve host '{host}'.");
            }

            foreach (var addr in addresses)
            {
                if (IsPrivateOrRestrictedIp(addr))
                {
                    return new UrlSafetyCheckResult(false, null, $"Host '{host}' resolves to restricted or private IP address {addr}.");
                }
            }

            return syncResult;
        }
        catch (SocketException ex)
        {
            return new UrlSafetyCheckResult(false, null, $"DNS resolution failed for '{host}': {ex.Message}");
        }
    }

    public static bool IsPrivateOrRestrictedIp(IPAddress ip)
    {
        // Handle IPv4-mapped IPv6
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();

            // 0.0.0.0/8 (Current network)
            if (bytes[0] == 0) return true;

            // 10.0.0.0/8 (Private)
            if (bytes[0] == 10) return true;

            // 127.0.0.0/8 (Loopback)
            if (bytes[0] == 127) return true;

            // 169.254.0.0/16 (Link-local & AWS/GCP/Azure Metadata 169.254.169.254)
            if (bytes[0] == 169 && bytes[1] == 254) return true;

            // 172.16.0.0/12 (Private 172.16.0.0 - 172.31.255.255)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;

            // 192.168.0.0/16 (Private)
            if (bytes[0] == 192 && bytes[1] == 168) return true;

            // 224.0.0.0/4 (Multicast) & 240.0.0.0/4 (Reserved)
            if (bytes[0] >= 224) return true;
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return true;

            var bytes = ip.GetAddressBytes();
            // fc00::/7 (Unique local)
            if ((bytes[0] & 0xfe) == 0xfc) return true;
            // ::1 (Loopback)
            if (IPAddress.IPv6Loopback.Equals(ip) || IPAddress.IPv6None.Equals(ip)) return true;
        }

        return false;
    }
}
