using System.Collections.Generic;
using MongoDB.Driver;

namespace SlownikPolonijny.Dal;

public class MongoEntryAuditor : IEntryAuditor
{
    readonly IMongoCollection<Entry> _col;
    readonly MongoRepository _repo;

    public MongoEntryAuditor(MongoRepository repo)
        : this(repo.Collection, repo)
    {
    }

    public MongoEntryAuditor(IMongoCollection<Entry> col, MongoRepository repo)
    {
        _col = col;       
        _repo = repo;     
    }

    public IList<IAuditIssue> PerformEntryAudit(string entryId)
    {
        var problems = new List<IAuditIssue>();

        Entry e = _repo.GetEntryById(entryId);
        if (e == null)
        {
            problems.Add(new GenericAuditIssue("Nie ma hasła z takim ID"));
            return problems;
        }

        return PerformEntryAudit(e);
    }

    public IList<IAuditIssue> PerformEntryAudit(Entry e)
    {
        var problems = new List<IAuditIssue>();

        SpotCheck(e, problems);
        CheckPunctuation(e, problems);
        CheckLinks(e, problems);

        return problems;
    }

    void SpotCheck(Entry entry, List<IAuditIssue> problems)
    {
        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            problems.Add(new GenericAuditIssue("Hasło jest puste"));
        }

        if (entry.Meanings.Count == 0 && entry.SeeAlso.Count == 0)
        {
            problems.Add(new GenericAuditIssue("Brak znaczenia i hasła pokrewnego"));
        }
    }

    static bool EndsWithLetter(string s)
    {
        s = s.Trim();
        if (s.Length > 0)
        {
            char last = s[s.Length - 1];
            return (char.IsLetter(last)
                && !char.IsDigit(last))
                || last == ']'
                || last == ')'
                ;
        }
        return false;
    }

    static bool EndsWithPunctuation(string s)
    {
        s = s.Trim();
        if (s.Length > 0)
        {
            char last = s[s.Length - 1];
            return last == '.'
                || last == '!'
                || last == '?'
                || last == ')'
                || last == ']'
                ;
        }
        return false;
    }

    public void CheckPunctuation(Entry entry, List<IAuditIssue> problems)
    {
        if (!EndsWithLetter(entry.Name))
        {
            problems.Add(new GenericAuditIssue("Hasło nie jest zakończone literą"));
        }

        foreach (var m in entry.Meanings)
        {
            if (!EndsWithLetter(m))
            {
                problems.Add(new GenericAuditIssue("Znaczenie nie jest zakończone literą"));
                break;
            }
        }

        foreach (var m in entry.EnglishMeanings)
        {
            if (!EndsWithLetter(m))
            {
                problems.Add(new GenericAuditIssue("Znaczenie angielskie nie jest zakończone literą"));
                break;
            }
        }

        foreach (var m in entry.SeeAlso)
        {
            if (!EndsWithLetter(m))
            {
                problems.Add(new GenericAuditIssue("Hasło pokrewne nie jest zakończone literą"));
                break;
            }
        }

        foreach (var m in entry.Examples)
        {
            if (!EndsWithPunctuation(m))
            {
                problems.Add(new GenericAuditIssue("Przykłady powinny się kończyć znakiem przestankowym"));
                break;
            }
        }
    }

    static ISet<string> GetLinks(string text)
    {
        var links = new HashSet<string>();
        var re = Entry.LinkRegex;

        var matches = re.Matches(text);

        foreach (System.Text.RegularExpressions.Match m in matches)
        {
            var textGroup = m.Groups["text"];
            if (textGroup.Success)
            {
                string link = textGroup.Value;

                var linkGroup = m.Groups["link"];
                if (linkGroup.Success)
                {
                    link = linkGroup.Value;
                }
                
                links.Add(link.ToLower(Entry.Culture));
            }
        }
        return links;
    }

    static ISet<string> GetExamplesLinks(Entry entry)
    {
        var links = new HashSet<string>();

        foreach (var m in entry.Examples)
        {
            links.UnionWith(GetLinks(m));
        }

        return links;
    }

    public static ISet<string> GetSeeAlsoLinks(Entry entry)
    {
        var links = new HashSet<string>();

        foreach (var m in entry.SeeAlso)
        {
            links.Add(m.ToLower(Entry.Culture));
        }

        return links;
    }

    public static bool HasBackLink(string fromEntryName, Entry otherEntry)
    {
        fromEntryName = fromEntryName.ToLower(Entry.Culture);
        if (fromEntryName == otherEntry.NameLowerCase)
        {
            return true;
        }

        ISet<string> links = GetSeeAlsoLinks(otherEntry);
        return links.Contains(fromEntryName);
    }

    public void CheckLinks(Entry entry, List<IAuditIssue> problems)
    {
        var links = GetExamplesLinks(entry);
        foreach (string link in links)
        {
            var linkedEntries = _repo.GetEntriesByName(link);
            if (linkedEntries.Count == 0)
            {
                problems.Add(new GenericAuditIssue($"Przykład ma link do nieistniejącego hasła: '{link}'"));
            }
        }

        links = GetSeeAlsoLinks(entry);
        foreach (string link in links)
        {
            var linkedEntries = _repo.GetEntriesByName(link);
            if (linkedEntries.Count == 0)
            {
                problems.Add(new GenericAuditIssue($"Link do nieistniejącego hasła pokrewnego: '{link}'"));
            }
            else
            {
                foreach (Entry linkedEntry in linkedEntries)
                {
                    if (!HasBackLink(entry.Name, linkedEntry))
                    {
                        var fix = new AutoFixStrategy() { Name = "add-link" };
                        fix.Parameters["from"] = link;
                        fix.Parameters["to"] = entry.Name;

                        var issue = new GenericAuditIssue($"Jednostronny link. Hasło '{link}' nie jest spokrewnione z '{entry.Name}'");
                        issue.Fixes.Add(fix);

                        problems.Add(issue);
                    }
                }
            }
        }
    }
}
