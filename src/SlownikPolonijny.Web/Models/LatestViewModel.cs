using System;
using SlownikPolonijny.Dal;
using System.Collections.Generic;

namespace SlownikPolonijny.Web.Models
{
    public class LatestViewModel
    {
        public IReadOnlyList<Entry> Entries { get; set; }
    }
}
