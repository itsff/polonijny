using System;
using System.Linq;
using System.Collections.Generic;
using SlownikPolonijny.Dal;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: Auditor <entries.json>");
    return 1;
}

string filePath = args[0];
var repo = new JsonRepository(new JsonRepositorySettings { FilePath = filePath });
var auditor = new EntryAuditor(repo);

int totalIssues = 0;

foreach (Entry entry in repo.GetAllEntries())
{
    var problems = auditor.PerformEntryAudit(entry);
    if (problems.Count > 0)
    {
        Console.WriteLine(entry.Name);
        foreach (var p in problems)
        {
            Console.Write("\t");
            Console.WriteLine(p);
        }
        Console.WriteLine();
        totalIssues += problems.Count;
    }
}

Console.WriteLine($"Total: {totalIssues} issues");
return totalIssues > 0 ? 1 : 0;
