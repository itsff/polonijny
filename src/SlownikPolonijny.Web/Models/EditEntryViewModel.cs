using System;
using SlownikPolonijny.Dal;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SlownikPolonijny.Web.Models;

public class EditEntryViewModel
{
    public Entry Entry { get; set; }
}

public class EditEntryModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> Meanings { get; set; } = new List<string>();
    public List<string> EnglishMeanings { get; set; } = new List<string>();
    public List<string> Examples { get; set; } = new List<string>();
    public List<string> SeeAlso { get; set; } = new List<string>();

    List<string> ValidateCollection(IEnumerable<string> incoming)
    {
        var result = new List<string>();

        foreach(string s in incoming)
        {
            if (s == null) { continue; }

            string ss = s.Trim();
            if (ss.Length >= 1)
            {
                result.Add(ss);
            }
        }

        return result;
    }

    public List<string> Validate()
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(Id))
        {
            problems.Add("Czy jesteś robotem?! 🤖🤖🤖");
        }
        else
        {
            Id = Id.Trim();
        }
        
        if (string.IsNullOrWhiteSpace(Name))
        {
            problems.Add("Hasło jest wymagame");
        }
        else
        {
            Name = Name.Trim();
        }

        Meanings = ValidateCollection(this.Meanings);
        EnglishMeanings = ValidateCollection(this.EnglishMeanings);
        Examples = ValidateCollection(this.Examples);
        SeeAlso = ValidateCollection(this.SeeAlso);

        if (Meanings.Count == 0
            && SeeAlso.Count == 0)
        {
            problems.Add("Podaj przynajmniej jedno znaczenie lub hasło pokrewne");    
        }

        return problems;
    }
}
