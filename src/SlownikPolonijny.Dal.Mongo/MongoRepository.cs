using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;

namespace SlownikPolonijny.Dal;

public class MongoRepositorySettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
    public string CollectionName { get; set; }

    public string DeletedEntriesCollectionName { get; set; } = "DeletedEntries";
}

public class MongoRepository : IRepository
{
    readonly IMongoCollection<Entry> _col;
    readonly IMongoCollection<Entry> _deletedEntriesCol;
    readonly static FindOptions _findOptions;
    readonly static SortDefinition<Entry> _sortAsc;
    readonly static SortDefinition<Entry> _sortDateDesc;

    static MongoRepository()
    {
        createClassMappings();
        _findOptions = new FindOptions()
        {
            Collation = new Collation("pl")
        };
        _sortAsc = Builders<Entry>.Sort.Ascending(e => e.Name);
        _sortDateDesc = Builders<Entry>.Sort.Descending(e => e.TimeAdded);
    }

    public MongoRepository(MongoRepositorySettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);

        _col = database.GetCollection<Entry>(settings.CollectionName);
        _deletedEntriesCol = database.GetCollection<Entry>(settings.DeletedEntriesCollectionName);
    }

    public IMongoCollection<Entry> Collection => _col;

    static void createClassMappings()
    {
        BsonClassMap.RegisterClassMap<Entry>(cm => {
            cm.MapIdProperty(c => c.Id).SetIdGenerator(ObjectIdGenerator.Instance);
            cm.MapProperty(c => c.Name).SetElementName("entry");
            cm.MapProperty(c => c.Meanings).SetElementName("meanings");
            cm.MapProperty(c => c.EnglishMeanings).SetElementName("english_meanings");
            cm.MapProperty(c => c.Examples).SetElementName("examples");
            cm.MapProperty(c => c.SeeAlso).SetElementName("see_also");
            cm.MapProperty(c => c.NameLowerCase).SetElementName("entry_lower_case");
            cm.MapProperty(c => c.Letter).SetElementName("letter");
            cm.MapProperty(c => c.ApprovedBy).SetElementName("approved_by");
            cm.MapProperty(c => c.IPAddress).SetElementName("ip");
            cm.MapProperty(c => c.FromInternet)
                .SetElementName("from_internet")
                .SetDefaultValue(false);

            cm.MapProperty(c => c.TimeAdded)
                .SetElementName("utc_stamp")
                .SetSerializer(new CustomDateTimeOffsetSerializer())
                .SetDefaultValue(DateTimeOffset.MinValue);

            cm.MapProperty(c => c.LastModified)
                .SetElementName("last_modified_utc_stamp")
                .SetSerializer(new CustomDateTimeOffsetSerializer())
                .SetDefaultValue(DateTimeOffset.MinValue);
        });
    }

    public IEnumerable<Entry> GetAllEntries()
    {
        return _col.Find(e => true).ToEnumerable();
    }

    public Entry GetEntryById(string entryId)
    {
        var objId = MongoDB.Bson.ObjectId.Parse(entryId);
        var filter = Builders<Entry>.Filter.Eq("_id", objId);
        return _col.Find(filter).FirstOrDefault();
    }

    public IReadOnlyList<Entry> GetEntriesByName(string name)
    {
        string lowerName = name.ToLower(Entry.Culture);
        return _col
            .Find(e => e.NameLowerCase == lowerName)
            .ToList<Entry>();
    }

    public IReadOnlyList<Entry> GetRandomEntries()
    {
        return _col.AsQueryable().Sample(1).ToList();
    }

    public Entry GetRandomExtryWithExample()
    {
        return _col.AsQueryable()
            .Where(e => e.Examples.Count > 0)
            .Sample(1)
            .SingleOrDefault();
    }

    public IReadOnlyList<Entry> GetLatestEntries()
    {
        return _col.Find(e => true).Sort(_sortDateDesc).Limit(2*21).ToList();
    }

    public IReadOnlyList<Entry> GetEntriesForLetter(char letter)
    {
        string lowerLetter = char.ToLower(letter, Entry.Culture).ToString();

        return _col
            .Find(e => e.Letter == lowerLetter, _findOptions)
            .Sort(_sortAsc)
            .ToList();
    }

    public IReadOnlyList<Entry> Search(string prefix)
    {
        string lowerPrefix = prefix.ToLower(Entry.Culture);

        return _col
            .Find(e => e.NameLowerCase.StartsWith(lowerPrefix), _findOptions)
            .Sort(_sortAsc)
            .ToList();
    }

    public void AddEntry(Entry entry)
    {
        entry.LastModified = DateTimeOffset.UtcNow;
        _col.InsertOne(entry);
    }

    public void UpdateEntry(Entry entry)
    {
        entry.LastModified = DateTimeOffset.UtcNow;

        var objId = (MongoDB.Bson.ObjectId)entry.Id;
        var filter = Builders<Entry>.Filter.Eq("_id", objId);
        var update = Builders<Entry>.Update
            .Set(e => e.Name, entry.Name)
            .Set(e => e.Meanings, entry.Meanings)
            .Set(e => e.EnglishMeanings, entry.EnglishMeanings)
            .Set(e => e.Examples, entry.Examples)
            .Set(e => e.SeeAlso, entry.SeeAlso)
            .Set(e => e.NameLowerCase, entry.NameLowerCase)
            .Set(e => e.Letter, entry.Letter)
            .Set(e => e.ApprovedBy, entry.ApprovedBy)
            .Set(e => e.IPAddress, entry.IPAddress)
            .Set(e => e.FromInternet, entry.FromInternet)
            .Set(e => e.LastModified, entry.LastModified)
            ;
        _col.UpdateOne(filter, update);
    }

    public void RemoveEntry(string entryId, string userDoingDeletion)
    {
        Entry entry = this.GetEntryById(entryId);
        if (entry != null)
        {
            entry.LastModified = DateTimeOffset.UtcNow;
            entry.ApprovedBy = userDoingDeletion;
            _deletedEntriesCol.InsertOne(entry);
        }
        var objId = MongoDB.Bson.ObjectId.Parse(entryId);
        var filter = Builders<Entry>.Filter.Eq("_id", objId);
        _col.DeleteOne(filter);
    }

    public void RestoreEntry(string entryId, string userDoingRestore)
    {
        var objId = MongoDB.Bson.ObjectId.Parse(entryId);
        var filter = Builders<Entry>.Filter.Eq("_id", objId);
        var deletedEntry = _deletedEntriesCol.Find(filter).FirstOrDefault();
        if (deletedEntry == null)
        {
            throw new KeyNotFoundException("Nie znaleziono hasła");
        }

        deletedEntry.ApprovedBy = userDoingRestore;
        AddEntry(deletedEntry);

        _deletedEntriesCol.DeleteOne(filter);
    }
}

public class CustomDateTimeOffsetSerializer : IBsonSerializer<DateTimeOffset>
{
    static readonly string Format = "yyyy-MM-dd HH:mm:ss.ffffff";
    public Type ValueType => typeof(DateTimeOffset);

    public DateTimeOffset Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var str = context.Reader.ReadString();
        if (!string.IsNullOrEmpty(str))
        {
            return DateTimeOffset.ParseExact(str, Format, System.Globalization.CultureInfo.InvariantCulture);
        }
        return DateTimeOffset.MinValue;
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTimeOffset value)
    {
        var str = value.ToString(Format);
        context.Writer.WriteString(str);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        var dto = (DateTimeOffset)value;
        var me = (CustomDateTimeOffsetSerializer)this;
        me.Serialize(context, args, dto);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var me = (CustomDateTimeOffsetSerializer)this;
        return me.Deserialize(context, args);
    }
}
