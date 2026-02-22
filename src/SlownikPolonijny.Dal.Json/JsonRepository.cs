using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace SlownikPolonijny.Dal;

public class JsonRepository : IRepository
{
    readonly string _filePath;
    readonly ReaderWriterLockSlim _lock = new();
    JsonStore _store;

    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public JsonRepository(JsonRepositorySettings settings)
    {
        _filePath = settings.FilePath;
        _store = LoadOrCreate();
    }

    JsonStore LoadOrCreate()
    {
        if (!File.Exists(_filePath))
            return new JsonStore();

        string json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<JsonStore>(json, _jsonOptions) ?? new JsonStore();
    }

    void Save()
    {
        string json = JsonSerializer.Serialize(_store, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public Entry GetEntryById(string entryId)
    {
        _lock.EnterReadLock();
        try
        {
            return _store.Entries.FirstOrDefault(e => e.Id == entryId)?.ToEntry();
        }
        finally { _lock.ExitReadLock(); }
    }

    public IEnumerable<Entry> GetAllEntries()
    {
        _lock.EnterReadLock();
        try
        {
            return _store.Entries.Select(e => e.ToEntry()).ToList();
        }
        finally { _lock.ExitReadLock(); }
    }

    public IReadOnlyList<Entry> GetEntriesByName(string name)
    {
        string lowerName = name.ToLower(Entry.Culture);
        _lock.EnterReadLock();
        try
        {
            return _store.Entries
                .Where(e => e.Name?.ToLower(Entry.Culture) == lowerName)
                .Select(e => e.ToEntry())
                .ToList();
        }
        finally { _lock.ExitReadLock(); }
    }

    public IReadOnlyList<Entry> GetRandomEntries()
    {
        _lock.EnterReadLock();
        try
        {
            if (_store.Entries.Count == 0)
                return [];

            int idx = Random.Shared.Next(_store.Entries.Count);
            return [_store.Entries[idx].ToEntry()];
        }
        finally { _lock.ExitReadLock(); }
    }

    public Entry GetRandomExtryWithExample()
    {
        _lock.EnterReadLock();
        try
        {
            var withExamples = _store.Entries
                .Where(e => e.Examples is { Count: > 0 })
                .ToList();

            if (withExamples.Count == 0)
                return null;

            int idx = Random.Shared.Next(withExamples.Count);
            return withExamples[idx].ToEntry();
        }
        finally { _lock.ExitReadLock(); }
    }

    public IReadOnlyList<Entry> GetLatestEntries()
    {
        _lock.EnterReadLock();
        try
        {
            return _store.Entries
                .OrderByDescending(e => e.TimeAdded)
                .Take(42)
                .Select(e => e.ToEntry())
                .ToList();
        }
        finally { _lock.ExitReadLock(); }
    }

    public IReadOnlyList<Entry> GetEntriesForLetter(char letter)
    {
        string lowerLetter = char.ToLower(letter, Entry.Culture).ToString();
        _lock.EnterReadLock();
        try
        {
            return _store.Entries
                .Where(e => !string.IsNullOrEmpty(e.Name)
                    && e.Name.ToLower(Entry.Culture).StartsWith(lowerLetter))
                .OrderBy(e => e.Name, StringComparer.Create(Entry.Culture, ignoreCase: true))
                .Select(e => e.ToEntry())
                .ToList();
        }
        finally { _lock.ExitReadLock(); }
    }

    public IReadOnlyList<Entry> Search(string prefix)
    {
        string lowerPrefix = prefix.ToLower(Entry.Culture);
        _lock.EnterReadLock();
        try
        {
            return _store.Entries
                .Where(e => !string.IsNullOrEmpty(e.Name)
                    && e.Name.ToLower(Entry.Culture).StartsWith(lowerPrefix))
                .OrderBy(e => e.Name, StringComparer.Create(Entry.Culture, ignoreCase: true))
                .Select(e => e.ToEntry())
                .ToList();
        }
        finally { _lock.ExitReadLock(); }
    }

    public void AddEntry(Entry entry)
    {
        if (string.IsNullOrEmpty(entry.Id?.ToString()))
            entry.Id = Guid.NewGuid().ToString();

        entry.LastModified = DateTimeOffset.UtcNow;

        _lock.EnterWriteLock();
        try
        {
            _store.Entries.Add(JsonEntry.FromEntry(entry));
            Save();
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void UpdateEntry(Entry entry)
    {
        entry.LastModified = DateTimeOffset.UtcNow;
        string id = entry.Id?.ToString();

        _lock.EnterWriteLock();
        try
        {
            int idx = _store.Entries.FindIndex(e => e.Id == id);
            if (idx < 0)
                throw new KeyNotFoundException($"Entry '{id}' not found.");

            _store.Entries[idx] = JsonEntry.FromEntry(entry);
            Save();
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void RemoveEntry(string entryId, string userDoingDeletion)
    {
        _lock.EnterWriteLock();
        try
        {
            int idx = _store.Entries.FindIndex(e => e.Id == entryId);
            if (idx < 0)
                return;

            var toDelete = _store.Entries[idx];
            toDelete.LastModified = DateTimeOffset.UtcNow;
            toDelete.ApprovedBy = userDoingDeletion;

            _store.DeletedEntries.Add(toDelete);
            _store.Entries.RemoveAt(idx);
            Save();
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void RestoreEntry(string entryId, string userDoingRestore)
    {
        _lock.EnterWriteLock();
        try
        {
            int idx = _store.DeletedEntries.FindIndex(e => e.Id == entryId);
            if (idx < 0)
                throw new KeyNotFoundException("Nie znaleziono hasła");

            var toRestore = _store.DeletedEntries[idx];
            toRestore.ApprovedBy = userDoingRestore;
            toRestore.LastModified = DateTimeOffset.UtcNow;

            _store.Entries.Add(toRestore);
            _store.DeletedEntries.RemoveAt(idx);
            Save();
        }
        finally { _lock.ExitWriteLock(); }
    }
}

class JsonStore
{
    public List<JsonEntry> Entries { get; set; } = [];
    public List<JsonEntry> DeletedEntries { get; set; } = [];
}

public class JsonEntry
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> Meanings { get; set; } = [];
    public List<string> EnglishMeanings { get; set; } = [];
    public List<string> SeeAlso { get; set; } = [];
    public List<string> Examples { get; set; } = [];
    public string ApprovedBy { get; set; }
    public DateTimeOffset TimeAdded { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string IpAddress { get; set; }
    public bool FromInternet { get; set; }

    public Entry ToEntry() => new()
    {
        Id = Id,
        Name = Name,
        Meanings = Meanings ?? [],
        EnglishMeanings = EnglishMeanings ?? [],
        SeeAlso = SeeAlso ?? [],
        Examples = Examples ?? [],
        ApprovedBy = ApprovedBy,
        TimeAdded = TimeAdded,
        LastModified = LastModified,
        IPAddress = IpAddress,
        FromInternet = FromInternet,
    };

    public static JsonEntry FromEntry(Entry e) => new()
    {
        Id = e.Id?.ToString(),
        Name = e.Name,
        Meanings = [.. e.Meanings],
        EnglishMeanings = [.. e.EnglishMeanings],
        SeeAlso = [.. e.SeeAlso],
        Examples = [.. e.Examples],
        ApprovedBy = e.ApprovedBy,
        TimeAdded = e.TimeAdded,
        LastModified = e.LastModified,
        IpAddress = e.IPAddress,
        FromInternet = e.FromInternet,
    };
}
