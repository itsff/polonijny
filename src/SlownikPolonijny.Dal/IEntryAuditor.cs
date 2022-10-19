using System.Collections.Generic;

namespace SlownikPolonijny.Dal;

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
    public GenericAuditIssue(string description)
    {
        Description = description;
    }

    public string Description { get; init; }

    public bool CanAutoFix => false;

    public void AutoFix()
    {
    }

    public override string ToString()
    {
        return this.Description;
    }
}
