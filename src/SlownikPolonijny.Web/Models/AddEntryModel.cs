using System;
using System.Text;
using System.Security.Cryptography;
using SlownikPolonijny.Dal;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SlownikPolonijny.Web.Models
{
    public class AddEntryViewModel
    {
        static readonly Random _random = new Random();
        static readonly byte[] _salt = new byte[32];

        static AddEntryViewModel()
        {
            _random.NextBytes(_salt);
        }

        public AddEntryViewModel()
        {
            Num1 = _random.Next(1, 30);
            Num2 = _random.Next(1, 10);

            int sum = Num1 + Num2;
            CaptchaExpectation = ComputeHash(sum.ToString());
        }

        public static bool HashMatches(string test, string expectation)
        {
            var testHashed = ComputeHash(test);
            return testHashed == expectation;
        }

        static string ComputeHash(string rawData)  
        {  
            using (var algo = SHA256.Create())  
            {  
                algo.TransformBlock(_salt, 0, _salt.Length, null, 0);

                var bytes = Encoding.UTF8.GetBytes(rawData);
                algo.TransformFinalBlock(bytes, 0, bytes.Length);

                return Convert.ToBase64String(algo.Hash);
            }
        }  

        public int Num1 { get; private set; }
        public int Num2 { get; private set; }
        public string CaptchaExpectation { get; private set; }
    }

    public class AddEntryModel
    {
        public string Name { get; set; }
        public List<string> Meanings { get; set; } = new List<string>();
        public List<string> EnglishMeanings { get; set; } = new List<string>();
        public List<string> Examples { get; set; } = new List<string>();
        public List<string> SeeAlso { get; set; } = new List<string>();

        public string Captcha { get; set; }
        public string CaptchaExpectation { get; set; }

        static bool ContainsBadCharacters(string s)
        {
            if (s.Contains('<') || s.Contains('>') || s.Contains(';') || s.Contains('{') || s.Contains('\\'))
            {
                return true;
            }
            return false;
        }

        static bool ContainsBadCharacters(IEnumerable<string> incoming)
        {
            foreach(string s in incoming)
            {
                if (ContainsBadCharacters(s))
                {
                    return true;
                }
            }
            return false;
        }

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

            if (string.IsNullOrWhiteSpace(Captcha)
                || string.IsNullOrWhiteSpace(CaptchaExpectation))
            {
                problems.Add("Czy jesteś robotem?! 🤖🤖🤖");
            }
            else
            {
                Captcha = Captcha ?? "";
                Captcha = Captcha.Trim();

                CaptchaExpectation = CaptchaExpectation ?? "";
                CaptchaExpectation = CaptchaExpectation.Trim();

                if (!AddEntryViewModel.HashMatches(Captcha, CaptchaExpectation))
                {
                    problems.Add("Czy jesteś robotem?! 🤖🤖🤖");
                }
            }
            
            if (string.IsNullOrWhiteSpace(Name))
            {
                problems.Add("Hasło jest wymagame");
            }
            else
            {
                Name = Name.Trim();
                if (ContainsBadCharacters(Name))
                {
                    problems.Add("Hasło zawiera niedozwolone znaki.");
                }
            }

            Meanings = ValidateCollection(this.Meanings);
            if (ContainsBadCharacters(this.Meanings))
            {
                problems.Add("Znaczenie zawiera niedozwolone znaki.");
            }

            EnglishMeanings = ValidateCollection(this.EnglishMeanings);
            if (ContainsBadCharacters(this.EnglishMeanings))
            {
                problems.Add("Znaczenie angielskie zawiera niedozwolone znaki.");
            }

            Examples = ValidateCollection(this.Examples);
            if (ContainsBadCharacters(this.Examples))
            {
                problems.Add("Przykład zawiera niedozwolone znaki.");
            }

            SeeAlso = ValidateCollection(this.SeeAlso);
            if (ContainsBadCharacters(this.SeeAlso))
            {
                problems.Add("Hasło pokrewne zawiera niedozwolone znaki.");
            }

            if (Meanings.Count == 0
                && SeeAlso.Count == 0)
            {
                problems.Add("Podaj przynajmniej jedno znaczenie lub hasło pokrewne");    
            }

            return problems;
        }
    }

    public class AddEntryResultModel
    {
        public bool Success => Problems.Count == 0;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public IList<string> Problems { get; set; } = new List<string>();
    }
}
