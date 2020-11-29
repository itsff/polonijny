using System;
using SlownikPolonijny.Dal;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SlownikPolonijny.Web.Models
{
    public class EntryViewModel
    {
        public static readonly Regex LinkRegex = new Regex(@"\[(?<text>(\w?\s?)+)(\|(?<link>(\w?\s?)+))?\]");

        public string Name { get; set; }

        public IReadOnlyList<Entry> Entries { get; set; }
    }
}
