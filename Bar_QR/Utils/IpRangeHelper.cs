using System;
using System.Net;

namespace Bar_QR.Utils;

public static class IpRangeHelper
{
    // Comprueba si una entrada (IP simple o CIDR) contiene la ip dada
    public static bool IsInRange(string cidrOrIp, string ip)
    {
        if (string.IsNullOrWhiteSpace(cidrOrIp) || string.IsNullOrWhiteSpace(ip)) return false;

        cidrOrIp = cidrOrIp.Trim();
        ip = ip.Trim();

        if (!IPAddress.TryParse(ip, out var address)) return false;

        if (!cidrOrIp.Contains('/'))
        {
            // comparación simple
            if (IPAddress.TryParse(cidrOrIp, out var single))
            {
                return single.Equals(address);
            }
            return false;
        }

        var parts = cidrOrIp.Split('/');
        if (parts.Length != 2) return false;

        if (!IPAddress.TryParse(parts[0], out var network)) return false;
        if (!int.TryParse(parts[1], out var prefixLength)) return false;

        var addrBytes = address.GetAddressBytes();
        var netBytes = network.GetAddressBytes();

        // IPv4 vs IPv6 mismatch
        if (addrBytes.Length != netBytes.Length) return false;

        var bits = addrBytes.Length * 8;
        if (prefixLength < 0 || prefixLength > bits) return false;

        var maskBytes = new byte[addrBytes.Length];
        for (int i = 0; i < maskBytes.Length; i++)
        {
            int remain = prefixLength - i * 8;
            if (remain >= 8) maskBytes[i] = 0xFF;
            else if (remain <= 0) maskBytes[i] = 0x00;
            else maskBytes[i] = (byte)(0xFF << (8 - remain));
        }

        for (int i = 0; i < addrBytes.Length; i++)
        {
            if ((addrBytes[i] & maskBytes[i]) != (netBytes[i] & maskBytes[i])) return false;
        }

        return true;
    }
}
