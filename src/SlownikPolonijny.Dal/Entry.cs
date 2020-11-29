using System;
using System.Collections.Generic;

namespace SlownikPolonijny.Dal
{
    public class Entry
    {
        public static System.Globalization.CultureInfo Culture 
            => System.Globalization.CultureInfo.GetCultureInfo("pl-PL");

        public object Id { get; set; }
        public string Name { get; set; }

        public string NameLowerCase
        {
            get
            {
                if (!String.IsNullOrEmpty(this.Name))
                {
                    return this.Name.ToLower(Entry.Culture);
                }
                return null;
            }
        }

        public string Letter
        {
            get
            {
                if (!String.IsNullOrEmpty(this.NameLowerCase))
                {
                    return this.NameLowerCase.Substring(0, 1);
                }
                return "!";
            }
        }

        public IList<string> Meanings { get; set; } = new List<string>();
        public IList<string> EnglishMeanings { get; set; } = new List<string>();
        public IList<string> SeeAlso { get; set; } = new List<string>();
        public IList<string> Examples { get; set; } = new List<string>();


        public string ApprovedBy { get; set; }
        public DateTimeOffset TimeAdded { get; set; }
        public string IPAddress { get; set; }
        public bool FromInternet { get; set; } = false;

        public override bool Equals(object obj)
        {
            return obj is Entry entry &&
                   EqualityComparer<object>.Default.Equals(Id, entry.Id) &&
                   Name == entry.Name;
        }

        public override int GetHashCode()
        {
            int hashCode = 18232891;
            hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }
    }
}
