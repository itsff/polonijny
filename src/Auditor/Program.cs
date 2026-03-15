using System;
using System.Linq;
using System.Collections.Generic;
using SlownikPolonijny.Dal;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Auditor
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = new MongoRepositorySettings()
            {
                ConnectionString = "mongodb+srv://app:tfLsOljmJpWx1clknQS9JRyD8ln0WDnUEGDNCiSWJt6lKDfwyiU06qUsZUtKatNO@cluster0.7yq8a.mongodb.net/slownik_polonijny",
                DatabaseName = "slownik_polonijny",
                CollectionName = "entries"
            };

            var repo = new MongoRepository(settings);
            var auditor = new EntryAuditor(repo);

            var fakeEntry = new Entry();
            fakeEntry.Name = "DupaJasiu1";
            fakeEntry.Meanings.Add("znaczenie1");
            //fakeEntry.Meanings.Add("znaczenie2.");
            //fakeEntry.Meanings.Add("znaczenie.");
            fakeEntry.EnglishMeanings.Add("ang1");
            fakeEntry.EnglishMeanings.Add("ang2!");
            fakeEntry.EnglishMeanings.Add("ang");
            fakeEntry.SeeAlso.Add("daco");
            fakeEntry.SeeAlso.Add("daco.");
            fakeEntry.Examples.Add("Dobry przykład.");
            fakeEntry.Examples.Add("Zły przykład");
            fakeEntry.Examples.Add("Zły [kaara] przykład!");
            fakeEntry.Examples.Add("Zły [kara] przykład.");

            // var problems = auditor.PerformEntryAudit(fakeEntry);
            // foreach(var p in problems)
            // {
            //     System.Console.WriteLine(p);
            // }

            // foreach (Entry entry in repo.Collection.Find(e => true).ToEnumerable())
            // {
            //     var problems = auditor.PerformEntryAudit(entry);
            //     if (problems.Count > 0)
            //     {
            //         System.Console.WriteLine(entry.Name);
            //         foreach(var p in problems)
            //         {
            //             System.Console.Write("\t");
            //             System.Console.WriteLine(p);
            //         }
            //         System.Console.WriteLine();
            //     }
            // }

            foreach (Entry entry in repo.Collection.Find(e => true).ToEnumerable())
            {
                var seeAlsoLinks = EntryAuditor.GetSeeAlsoLinks(entry);
                foreach (string link in seeAlsoLinks)
                {
                    var linkedEntries = repo.GetEntriesByName(link);
                    foreach (Entry linkedEntry in linkedEntries)
                    {
                        if (!EntryAuditor.HasBackLink(entry.Name, linkedEntry))
                        {
                            System.Console.WriteLine($"Jednostronny link. Hasło '{link}' nie jest spokrewnione z '{entry.Name}'");

                            var set = new HashSet<string>(linkedEntry.SeeAlso);
                            set.Add(entry.Name);
                            linkedEntry.SeeAlso = set.ToList();

                            repo.UpdateEntry(linkedEntry);
                            System.Console.WriteLine("Added link");
                        }
                    }
                }
            }
        }
    }
}
