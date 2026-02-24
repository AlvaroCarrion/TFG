using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PPQ.Models
{

    // Esta clase sirve de modelo para las preguntas rescatadas de la base de datos MongoDB.
    public class QuestionsModel
    {
        public ObjectId Id { get; set; }

        [BsonElement("category")]
        public string Category { get; set; }

        [BsonElement("topic")]
        public string Topic { get; set; }

        [BsonElement("language")]
        public LanguageModel Language { get; set; }

        [BsonElement("difficulty")]
        public int Difficulty { get; set; }

        [BsonElement("tags")]
        public List<string> Tags { get; set; }

        [BsonElement("image")]
        public string Image { get; set; }
    }

    // Un diccionario dinámico para manejar múltiples idiomas y manejar propiedades fijas
    [BsonNoId]
    public class LanguageModel : Dictionary<string, LanguageContent>
    {
    }

    // Clase para estructurar el contenido de cada idioma.
    public class LanguageContent
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

