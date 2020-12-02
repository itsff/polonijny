using System;
using SlownikPolonijny.Dal;
using System.Collections.Generic;

namespace SlownikPolonijny.Web.Models
{
    public class EntryListViewModel
    {
        public IReadOnlyList<Entry> Entries { get; set; }
        public bool ShowEdit { get; set; }
    }
}
