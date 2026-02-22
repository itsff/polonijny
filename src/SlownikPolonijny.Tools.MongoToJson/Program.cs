using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SlownikPolonijny.Dal;

// Usage: MongoToJson <input-file> <output-file>
// Converts a MongoDB JSON export (array of BSON-extended-JSON documents)
// to the simple JSON format used by JsonRepository.

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: MongoToJson <input.json> <output.json>");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Converts a MongoDB export (mongoexport --jsonArray) to the");
    Console.Error.WriteLine("simple JSON format used by SlownikPolonijny.Dal.Json.");
    return 1;
}

string inputPath = args[0];
string outputPath = args[1];

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    return 2;
}

string inputJson = File.ReadAllText(inputPath);

JsonDocument doc;
try
{
    doc = JsonDocument.Parse(inputJson);
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"Failed to parse input JSON: {ex.Message}");
    return 3;
}

var entries = new List<JsonEntry>();
int skipped = 0;

var root = doc.RootElement;

// Support both array and line-delimited (NDJSON) formats
IEnumerable<JsonElement> documents;
if (root.ValueKind == JsonValueKind.Array)
{
    documents = root.EnumerateArray();
}
else
{
    // Single document at root
    documents = [root];
}

foreach (var item in documents)
{
    var entry = TryConvert(item);
    if (entry != null)
        entries.Add(entry);
    else
        skipped++;
}

var store = new OutputStore { Entries = entries };

var outputOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

string outputJson = JsonSerializer.Serialize(store, outputOptions);
File.WriteAllText(outputPath, outputJson);

Console.WriteLine($"Converted {entries.Count} entries. Skipped {skipped}.");
Console.WriteLine($"Output written to: {outputPath}");
return 0;


static JsonEntry TryConvert(JsonElement item)
{
    // MongoDB field names from MongoRepository class mappings:
    //   Name        → "entry"
    //   Meanings    → "meanings"
    //   EnglishMeanings → "english_meanings"
    //   Examples    → "examples"
    //   SeeAlso     → "see_also"
    //   ApprovedBy  → "approved_by"
    //   IPAddress   → "ip"
    //   FromInternet → "from_internet"
    //   TimeAdded   → "utc_stamp"
    //   LastModified → "last_modified_utc_stamp"
    //   Id          → "_id" (ObjectId: {"$oid": "..."})

    string id = ExtractId(item);
    string name = GetString(item, "entry");

    if (string.IsNullOrWhiteSpace(name))
        return null;

    return new JsonEntry
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Name = name,
        Meanings = GetStringList(item, "meanings"),
        EnglishMeanings = GetStringList(item, "english_meanings"),
        Examples = GetStringList(item, "examples"),
        SeeAlso = GetStringList(item, "see_also"),
        ApprovedBy = GetString(item, "approved_by"),
        IpAddress = GetString(item, "ip"),
        FromInternet = GetBool(item, "from_internet"),
        TimeAdded = ParseMongoDate(item, "utc_stamp"),
        LastModified = ParseMongoDate(item, "last_modified_utc_stamp"),
    };
}

static string ExtractId(JsonElement item)
{
    if (!item.TryGetProperty("_id", out var idProp))
        return null;

    // Extended JSON ObjectId: {"$oid": "hexstring"}
    if (idProp.ValueKind == JsonValueKind.Object
        && idProp.TryGetProperty("$oid", out var oidProp))
    {
        return oidProp.GetString();
    }

    // Plain string id
    if (idProp.ValueKind == JsonValueKind.String)
        return idProp.GetString();

    return null;
}

static string GetString(JsonElement item, string key)
{
    if (item.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
        return prop.GetString();
    return null;
}

static bool GetBool(JsonElement item, string key)
{
    if (item.TryGetProperty(key, out var prop))
    {
        if (prop.ValueKind == JsonValueKind.True) return true;
        if (prop.ValueKind == JsonValueKind.False) return false;
    }
    return false;
}

static List<string> GetStringList(JsonElement item, string key)
{
    var result = new List<string>();
    if (!item.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.Array)
        return result;

    foreach (var element in prop.EnumerateArray())
    {
        if (element.ValueKind == JsonValueKind.String)
            result.Add(element.GetString());
    }
    return result;
}

static DateTimeOffset ParseMongoDate(JsonElement item, string key)
{
    if (!item.TryGetProperty(key, out var prop))
        return DateTimeOffset.MinValue;

    // Extended JSON date: {"$date": "2020-01-01T00:00:00Z"} or {"$date": {"$numberLong": "..."}}
    if (prop.ValueKind == JsonValueKind.Object)
    {
        if (prop.TryGetProperty("$date", out var dateProp))
        {
            if (dateProp.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(dateProp.GetString(), out var dto))
            {
                return dto;
            }
            if (dateProp.ValueKind == JsonValueKind.Object
                && dateProp.TryGetProperty("$numberLong", out var numLong)
                && long.TryParse(numLong.GetString(), out long ms))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(ms);
            }
        }
        return DateTimeOffset.MinValue;
    }

    // Plain string in MongoRepository format: "yyyy-MM-dd HH:mm:ss.ffffff"
    if (prop.ValueKind == JsonValueKind.String)
    {
        string raw = prop.GetString();
        if (DateTimeOffset.TryParseExact(raw, "yyyy-MM-dd HH:mm:ss.ffffff",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            return dto;
        }
        if (DateTimeOffset.TryParse(raw, out var dto2))
            return dto2;
    }

    return DateTimeOffset.MinValue;
}

class OutputStore
{
    public List<JsonEntry> Entries { get; set; } = [];
    public List<JsonEntry> DeletedEntries { get; set; } = [];
}

class JsonEntry
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
}
