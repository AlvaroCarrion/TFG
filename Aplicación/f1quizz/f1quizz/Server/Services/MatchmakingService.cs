using MongoDB.Driver;
using MongoDB.Driver.Core.Connections;
using Server.Models;
using System.Collections.Concurrent;

namespace Server.Services
{
    public class MatchmakingService
    {
        private readonly Dictionary<string, PlayerConnection> _waiting = new();
        private readonly ConcurrentDictionary<string, MatchRoom> _rooms = new();
        private readonly IMongoDatabase _database;
        private static readonly Random _random = new();

        public MatchmakingService(IMongoClient mongoClient)
        {
            _database = mongoClient.GetDatabase("PPQDB");
        }

        // Empareja o encola jugadores.
        public MatchRoom AddPlayer(PlayerConnection player)
        {
            lock (_waiting)
            {
                if (_waiting.ContainsKey(player.ConnectionId))
                    return null;
                if (_waiting.Count > 0)
                {
                    var opponent = _waiting.Values.First();
                    _waiting.Remove(opponent.ConnectionId);

                    string category = GetRandomCategory();
                    string topic = GetRandomTopic(category);
                    var questions = LoadQuestions(category, topic);

                    var room = new MatchRoom(opponent, player)
                    {
                        Category = category,
                        Topic = topic,
                        Questions = questions
                    };

                    _rooms[room.Id] = room;
                    return room;
                }
                _waiting[player.ConnectionId] = player;
                return null;
            }
        }

        public MatchRoom GetRoom(string roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        public string? GetRoomIdByConnectionId(string connectionId)
        {
            var room = _rooms.Values.FirstOrDefault(r => r.Players.Any(p => p.ConnectionId == connectionId));
            return room?.Id;
        }

        public MatchRoom RemovePlayer(string connectionId)
        {
            var room = _rooms.Values.FirstOrDefault(
                r => r.Players.Any(p => p.ConnectionId == connectionId)
            );

            if (room != null)
            {
                _rooms.TryRemove(room.Id, out _);
            }

            lock (_waiting)
            {
                _waiting.Remove(connectionId);
            }

            return room;
        }

        public void RemoveRoom(string roomId)
        {
            _rooms.TryRemove(roomId, out _);
        }

        public void RemovePlayerFromWaiting(string connectionId)
        {
            lock (_waiting)
            {
                if (_waiting.Remove(connectionId))
                {
                    Console.WriteLine($"[Matchmaking] Jugador {connectionId} eliminado de la lista de espera.");
                }
            }
        }

        private static readonly List<string> Categories = new()
        {
            "circuits",
            "drivers",
            "teams"
        };

        private string GetRandomCategory()
        {
            return Categories[_random.Next(Categories.Count)];
        }

        private static readonly Dictionary<string, List<string>> TopicsByCategory = new()
        {
            {
                "circuits", new List<string>
                {
                    "monaco",
                    "montmelo",
                    "silverstone",
                    "suzuka"
                }
            },
            {
                "drivers", new List<string>
                {
                    "ayrtonSenna",
                    "fernandoAlonso",
                    "lewisHamilton",
                    "michaelSchumacher"
                }
            },
            {
                "teams", new List<string>
                {
                    "ferrari",
                    "mclaren",
                    "redbull",
                    "mercedes"
                }
            }
        };

        private string GetRandomTopic(string category)
        {
            if (!TopicsByCategory.ContainsKey(category))
                throw new ArgumentException("Categoría no válida.");

            var topics = TopicsByCategory[category];
            return topics[_random.Next(topics.Count)];
        }

        private List<MongoQuestion> LoadQuestions(string category, string topic)
        {
            Console.WriteLine($"[DB] Buscando preguntas para: {category} - {topic}");

            IMongoCollection<MongoQuestion> collection = category switch
            {
                "circuits" => _database.GetCollection<MongoQuestion>("questionsCircuits"),
                "drivers" => _database.GetCollection<MongoQuestion>("questionsDrivers"),
                "teams" => _database.GetCollection<MongoQuestion>("questionsTeams"),
                _ => throw new ArgumentException("Categoría no válida.")
            };

            var filter = Builders<MongoQuestion>.Filter.And(
                Builders<MongoQuestion>.Filter.Eq(q => q.Category, category),
                Builders<MongoQuestion>.Filter.Eq(q => q.Topic, topic)
            );

            var mongoQuestions = collection.Find(filter).ToList();

            if (mongoQuestions.Count == 0)
            {
                Console.WriteLine($"[DB] ERROR: No se encontraron preguntas para {category}/{topic}");
                mongoQuestions = collection.Find(Builders<MongoQuestion>.Filter.Eq(q => q.Category, category)).ToList();
                throw new InvalidOperationException($"No hay preguntas para {category}/{topic}");
            }
                

            return mongoQuestions
                .OrderBy(_ => Guid.NewGuid())
                .Take(15)
                .ToList();
        }
    }
}