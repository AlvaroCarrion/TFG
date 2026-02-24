using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Server.Models
{
    public class MongoQuestion
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("category")]
        public string Category { get; set; }

        [BsonElement("topic")]
        public string Topic { get; set; }

        [BsonElement("language")]
        public Dictionary<string, MongoLanguageBlock> Language { get; set; }

        [BsonElement("difficulty")]
        public int Difficulty { get; set; }

        [BsonElement("tags")]
        public List<string> Tags { get; set; }

        [BsonElement("image")]
        public string Image { get; set; }
    }

    public class MongoLanguageBlock
    {
        [BsonElement("question")]
        public string Question { get; set; }

        [BsonElement("options")]
        public List<string> Options { get; set; }

        [BsonElement("answer")]
        public string Answer { get; set; }

        [BsonElement("fact")]
        public string Fact { get; set; }
    }
}
