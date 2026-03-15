using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Driver;
using SlownikPolonijny.Dal;

// Usage: MongoToJson <output-file> [--connection-string <cs>] [--database <db>] [--collection <col>]
//
// Connects to MongoDB and exports all entries (active + deleted) to the
// JSON format used by JsonRepository.
//
// Connection settings are read from environment variables by default:
//   Mongo__ConnectionString, Mongo__DatabaseName, Mongo__CollectionName
// They can be overridden with command-line flags.

string outputPath = null;
string connectionString = Environment.GetEnvironmentVariable("Mongo__ConnectionString");
string databaseName = Environment.GetEnvironmentVariable("Mongo__DatabaseName");
string collectionName = Environment.GetEnvironmentVariable("Mongo__CollectionName");

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--connection-string" when i + 1 < args.Length:
            connectionString = args[++i];
            break;
        case "--database" when i + 1 < args.Length:
            databaseName = args[++i];
            break;
        case "--collection" when i + 1 < args.Length:
            collectionName = args[++i];
            break;
        default:
            if (outputPath == null && !args[i].StartsWith("--"))
                outputPath = args[i];
            else
            {
                Console.Error.WriteLine($"Unknown argument: {args[i]}");
                return 1;
            }
            break;
    }
}

if (outputPath == null)
{
    Console.Error.WriteLine("Usage: MongoToJson <output.json> [--connection-string <cs>] [--database <db>] [--collection <col>]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Connects to MongoDB and exports entries to the JSON format used by JsonRepository.");
    Console.Error.WriteLine("Connection settings default to Mongo__* environment variables.");
    return 1;
}

if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(databaseName) || string.IsNullOrEmpty(collectionName))
{
    Console.Error.WriteLine("Missing MongoDB connection settings.");
    Console.Error.WriteLine("Set Mongo__ConnectionString, Mongo__DatabaseName, Mongo__CollectionName env vars or use flags.");
    return 2;
}

Console.WriteLine($"Connecting to {databaseName}/{collectionName}...");

var settings = new MongoRepositorySettings
{
    ConnectionString = connectionString,
    DatabaseName = databaseName,
    CollectionName = collectionName,
};

var repo = new MongoRepository(settings);

Console.WriteLine("Fetching entries...");
var entries = repo.GetAllEntries().Select(JsonEntry.FromEntry).ToList();
Console.WriteLine($"  {entries.Count} active entries");

Console.WriteLine("Fetching deleted entries...");
var deletedEntries = repo.Collection.Database
    .GetCollection<Entry>(settings.DeletedEntriesCollectionName)
    .Find(FilterDefinition<Entry>.Empty)
    .ToList()
    .Select(JsonEntry.FromEntry)
    .ToList();
Console.WriteLine($"  {deletedEntries.Count} deleted entries");

var store = new OutputStore
{
    Entries = entries,
    DeletedEntries = deletedEntries,
};

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

string json = JsonSerializer.Serialize(store, jsonOptions);
File.WriteAllText(outputPath, json);

Console.WriteLine($"Written to {outputPath}");
return 0;

class OutputStore
{
    public List<JsonEntry> Entries { get; set; } = [];
    public List<JsonEntry> DeletedEntries { get; set; } = [];
}
