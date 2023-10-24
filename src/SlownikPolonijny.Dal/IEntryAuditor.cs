using System.Collections.Generic;

namespace SlownikPolonijny.Dal;

public interface IEntryAuditor
{
    IList<IAuditIssue> PerformEntryAudit(string entryId);
    IList<IAuditIssue> PerformEntryAudit(Entry entry);
}

public class AutoFixStrategy
{
    public string Name { get; set; }
    public Dictionary<string, string> Parameters { get; init; } = new();
}

public interface IAuditIssue
{
    string Description { get; }
    bool CanAutoFix { get; }

    IList<AutoFixStrategy> Fixes { get; }
}

public class GenericAuditIssue : IAuditIssue
{
    public GenericAuditIssue(string description)
    {
        Description = description;
    }

    public string Description { get; init; }

    public bool CanAutoFix => this.Fixes.Count > 0;

    public IList<AutoFixStrategy> Fixes { get; init; } = new List<AutoFixStrategy>();

    public override string ToString()
    {
        return this.Description;
    }
}
