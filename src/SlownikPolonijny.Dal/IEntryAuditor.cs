using System.Collections.Generic;

namespace SlownikPolonijny.Dal
{
    public interface IEntryAuditor
    {
        IList<IAuditIssue> PerformEntryAudit(string entryId);
        IList<IAuditIssue> PerformEntryAudit(Entry entry);
    }

    public interface IAuditIssue
    {
        string Description { get; }
        bool CanAutoFix { get; }

        void AutoFix ();
    }

    public class GenericAuditIssue : IAuditIssue
    {
        string _description;

        public GenericAuditIssue(string description)
        {
            _description = description;
        }

        public string Description => _description;

        public bool CanAutoFix => false;

        public void AutoFix()
        {
        }

        public override string ToString()
        {
            return this.Description;
        }
    }
}
