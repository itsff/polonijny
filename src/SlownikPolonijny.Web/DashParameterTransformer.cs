using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;

namespace SlownikPolonijny.Web;

public class DashedParameterTransformer : IOutboundParameterTransformer
{
    public string TransformOutbound(object value)
    {
        if (value == null) { return null; }
        return ToDashed(value.ToString());
    }

    public static string ToDashed(string val)
    {
        val = val.ToLower(SlownikPolonijny.Dal.Entry.Culture);
        // First escape any dashes
        val = val.Replace('-', '\x01');

        // Then replace spaces with dashes
        val = val.Replace(' ', '-');

        // Finally, bring back original dashes
        val = val.Replace("\x01", "--");

        return val;
    }

    public static string FromDashed(string val)
    {
        val = val.Replace("--", "\x01");
        val = val.Replace('-', ' ');
        val = val.Replace('\x01', '-');
        return val;
    }
}
