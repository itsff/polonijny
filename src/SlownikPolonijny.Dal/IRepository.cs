using System.Collections.Generic;

namespace SlownikPolonijny.Dal
{
    public interface IRepository
    {
        Entry GetEntryById(string entryId);

        IEnumerable<Entry> GetAllEntries();

        IReadOnlyList<Entry> GetEntriesByName(string name);

        IReadOnlyList<Entry> GetRandomEntries();

        IReadOnlyList<Entry> GetLatestEntries();

        IReadOnlyList<Entry> GetEntriesForLetter(char letter);

        IReadOnlyList<Entry> GetEntriesForLetter(string letter)
            => this.GetEntriesForLetter(letter[0]);

        IReadOnlyList<Entry> Search(string prefix);

        void AddEntry(Entry entry);

        void UpdateEntry(Entry entry);

        void RemoveEntry(string entryId, string userDoingDeletion);

        void RemoveEntry(string entryId)
            => this.RemoveEntry(entryId, null);

        void RestoreEntry(string entryId, string userDoingRestore);

        void RestoreEntry(string entryId)
            => this.RestoreEntry(entryId, null);
    }
}
