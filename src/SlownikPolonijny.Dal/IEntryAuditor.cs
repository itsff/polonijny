using System.Collections.Generic;

namespace SlownikPolonijny.Dal
{
    public interface IEntryAuditor
    {
        IList<string> PerformEntryAudit(string entryId);
        IList<string> PerformEntryAudit(Entry entry);
    }
}
