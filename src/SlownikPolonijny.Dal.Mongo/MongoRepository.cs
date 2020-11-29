using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;

namespace SlownikPolonijny.Dal
{
    public class MongoRepositorySettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
    }

    public class MongoRepository : IRepository
    {
        readonly IMongoCollection<Entry> _col;
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
        }

        static void createClassMappings()
        {
            BsonClassMap.RegisterClassMap<Entry>(cm => {
                cm.MapIdProperty(c => c.Id).SetIdGenerator(CombGuidGenerator.Instance);
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
            });
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

        public IReadOnlyList<Entry> GetLatestEntries()
        {
            return _col.Find(e => true).Sort(_sortDateDesc).Limit(21).ToList();
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
            _col.InsertOne(entry);
        }

        public void UpdateEntry(Entry entry)
        {
            //_col.UpdateOne(e => e.Id == entry.Id, entry);
        }

        public void RemoveEntry(Entry entry)
        {
            _col.DeleteOne(e => e.Id == entry.Id);
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
}