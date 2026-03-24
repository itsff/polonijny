using System;
using System.Collections.Generic;
using SlownikPolonijny.Dal;

namespace SlownikPolonijny.Playground;

class Program
{
    static void PrintCollection(IEnumerable<string> col)
    {
        foreach (string item in col)
        {
            System.Console.Write("\t\t- ");
            System.Console.WriteLine(item);
        }
    }

    static void PrintEntry(Entry entry)
    {
        System.Console.WriteLine($"{entry.Name} ({entry.Id})");
        System.Console.WriteLine($"\tLetter: {entry.Letter}");
        System.Console.WriteLine("\tMeanings:");
        PrintCollection(entry.Meanings);

        System.Console.WriteLine("\tEnglish meanings:");
        PrintCollection(entry.EnglishMeanings);

        System.Console.WriteLine("\tExamples:");
        PrintCollection(entry.Examples);

        System.Console.WriteLine("\tSee also:");
        PrintCollection(entry.SeeAlso);
    }

    static string FindLinks(string text)
    {
        var re = Entry.LinkRegex;

        var matches = re.Matches(text);
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
            var textGroup = m.Groups["text"];
            if (textGroup.Success)
            {
                string link = string.Empty;
                var linkGroup = m.Groups["link"];
                if (linkGroup.Success)
                {
                    link = $"<a href=\"~/haslo/{linkGroup.Value}\">{textGroup.Value}</a>";
                }
                else
                {
                    link = $"<a href=\"~/haslo/{textGroup.Value}\">{textGroup.Value}</a>";
                }

                text = re.Replace(text, link, 1);
            }
        }

        return text;
    }

    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            System.Console.Error.WriteLine("Usage: SlownikPolonijny.Playground <entries.json>");
            System.Environment.Exit(1);
        }

        var settings = new Dal.JsonRepositorySettings()
        {
            FilePath = args[0],
        };

        var repo = new Dal.JsonRepository(settings);

        var ee = repo
            .GetLatestEntries();
            //.GetRandomEntries();
        foreach (var e in ee)
        {
            PrintEntry(e);
            System.Console.WriteLine();
        }

        System.Console.WriteLine("Done");
    }
}
