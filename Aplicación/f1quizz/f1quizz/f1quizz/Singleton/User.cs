using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PPQ.Resources.Lenguages;
using PPQ.Resources.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPQ.Singleton
{
    // Clase que recoge los datos del usuario.
    [BsonIgnoreExtraElements] // Para recoger solo los campos que me interesen.
    public class User
    {
        public ObjectId Id { get; set; }

        [BsonElement("email")]
        public string email { get; set; }

        [BsonElement("username")]
        public string username { get; set; }

        [BsonElement("id_rol")]
        public int idRol { get; set; }


        public Dictionary<string, int> statistics = new Dictionary<string, int>();

        public Dictionary<string, string> preferences = new Dictionary<string, string>();

        public Dictionary<string, bool> completedCircuits = new Dictionary<string, bool>();

        public Dictionary<string, bool> completedDrivers = new Dictionary<string, bool>();

        public Dictionary<string, bool> completedTeams = new Dictionary<string, bool>();

        // Método "LoadDictionaries" para cargar los diccionarios (preferencias del usuario).
        public void LoadDictionaries()
        {
            ResourceDictionary nuevoDiccionario = null;

            switch (preferences["language"])
            {
                case "en":
                    nuevoDiccionario = new English();
                    Application.Current.Resources.MergedDictionaries.Add(nuevoDiccionario);
                    break;

                case "sp":
                    nuevoDiccionario = new Spanish();
                    Application.Current.Resources.MergedDictionaries.Add(nuevoDiccionario);
                    break;
            }

            switch (preferences["theme"])
            {
                case "light":
                    nuevoDiccionario = new LightTheme();
                    Application.Current.Resources.MergedDictionaries.Add(nuevoDiccionario);
                    break;

                case "dark":
                    nuevoDiccionario = new DarkTheme();
                    Application.Current.Resources.MergedDictionaries.Add(nuevoDiccionario);
                    break;
            }
        }
    }
}
