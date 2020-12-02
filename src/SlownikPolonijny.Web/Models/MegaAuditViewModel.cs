using System;
using SlownikPolonijny.Dal;
using System.Collections.Generic;

namespace SlownikPolonijny.Web.Models
{
    public class EntryAuditResultModel
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public bool IsApproved { get; set; }
        public IList<string> Problems { get; set; } = new List<string>();
    }

    public class MegaAuditViewModel
    {
        public IReadOnlyList<EntryAuditResultModel> AuditResults { get; set; }
    }
}
