using System;
using System.Globalization;
using System.Net;

namespace Server.Utils;

public static class IpParser
{
    public static IPEndPoint ToIpEndPoint(string endPoint)
    {
        var ep = endPoint.Split(':');
        if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
        IPAddress? ip;
        if (ep.Length > 2)
        {
            if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                throw new FormatException("Invalid ip-address");
        }
        else
        {
            if (!IPAddress.TryParse(ep[0], out ip)) throw new FormatException("Invalid ip-address");
        }

        if (!int.TryParse(ep[^1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out var port))
            throw new FormatException("Invalid port");
        return new IPEndPoint(ip, port);
    }
}