using System;
using SlownikPolonijny.Dal;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SlownikPolonijny.Web.Models;

public class IndexViewModel
{
    public static readonly Regex LinkRegex = Entry.LinkRegex;

    public Entry Entry { get; set; }
    public string Example { get; set; }
}
