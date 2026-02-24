using MongoDB.Bson;
using MongoDB.Driver;
using PPQ.Models;
using PPQ.Singleton;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PPQ.Database;

internal class DatabaseClass
{
    // Propiedades.
    private MongoClient client;
    private IMongoDatabase database;

    // Métodos.

    #region Métodos relaccionados con la conexión a la base de datos MongoDB.

    // Método "IsConnected". Para saber si estamos conectados a la base de datos.
    public bool IsConnected { get; private set; } = false;

    // Método "Connect". Para conectar con la base de datos.
    public bool Connect()
    {
        try
        {
            // Intentar obtener la variable de entorno.
            string mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");

            // Si no existe la variable, leer el archivo JSON.
            if (string.IsNullOrWhiteSpace(mongoUri))
            {
                // Usar FileSystem de MAUI.
                using var stream = FileSystem.OpenAppPackageFileAsync("dbconfig.json").Result;
                using var reader = new StreamReader(stream);
                string jsonString = reader.ReadToEnd();

                // Deserializar el JSON para obtener la propiedad "MongoUri".
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                if (config != null && config.ContainsKey("MongoUri"))
                {
                    mongoUri = config["MongoUri"];
                }
            }

            if (string.IsNullOrWhiteSpace(mongoUri))
                throw new Exception((string)Application.Current.Resources["DAMongoURIEx"]);

            client = new MongoClient(mongoUri);
            database = client.GetDatabase("PPQDB");

            IsConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine((string)Application.Current.Resources["DANoConnection"] + ": " + ex.Message);
            IsConnected = false;
            return false;
        }
    }

    // Método "Close". Para cerrar la conexión con la base de datos. MongoClient no necesita cerrarse manualmente. Solo liberar las referencias y marcar como desconectado.
    public async Task Close()
    {
        database = null;
        client = null;
        IsConnected = false;
        await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAClosedConection"], (string)Application.Current.Resources["DAClosedConectionText"]);
    }

    #endregion

    #region Métodos relaccionados con operaciones Create.
    // Método "CreateUser". Para crear un nuevo usuario en la base de datos.
    public async Task<bool> CreateUser(string email, string username, string password)
    {
        try
        {
            // Comprobar conexión con la base de datos.
            if (!IsConnected)
            {
                Connect();
            }

            // Validar los campos básicos.
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAEmptyFields"]); 
                return false;
            }

            // Encriptar la contraseña antes de guardarla.
            string encryptedPassword = await EncryptPassword(password);

            if (string.IsNullOrEmpty(encryptedPassword))
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAPasswordEmpty"]); 
                return false;
            }

            // Obtener la colección "users".
            var collection = database.GetCollection<BsonDocument>("users");

            // Crear el nuevo documento del usuario.
            var newUser = new BsonDocument
        {
            { "email", email },
            { "username", username },
            { "password", encryptedPassword },
            { "id_rol", 3 },
            {
                "statistics", new BsonDocument
                {
                    { "level", 0 },
                    { "points", 0 },
                    { "polePositions", 0 },
                    { "correctAnswers", 0 },
                    { "wrongAnswers", 0 },
                    { "totalGames", 0 },
                    { "totalQuestions", 0 }
                }
            },
            {
                "preferences", new BsonDocument
                {
                    { "language", "en" },
                    { "theme", "light" },
                    { "rememberMe", "no" }
                }
            },
            {
                "completedCircuits", new BsonDocument
                {
                    { "monaco", false },
                    { "montmelo", false },
                    { "silverstone", false },
                    { "suzuka", false },
                }
            },
            {
                "completedDrivers", new BsonDocument
                {
                    { "ayrtonSenna", false },
                    { "fernandoAlonso", false },
                    { "lewisHamilton", false },
                    { "michaelSchumacher", false },
                }
            },
            {
                "completedTeams", new BsonDocument
                {
                    { "ferrari", false },
                    { "mclaren", false },
                    { "mercedes", false },
                    { "redbull", false },
                }
            }
        };

            // Insertar el documento en la colección.
            await collection.InsertOneAsync(newUser);

            // Mostrar mensaje de éxito.
            await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DACreatedUser"], (string)Application.Current.Resources["DACreatedUserText"]); 

            return true;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                $"Error al crear el usuario: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateUserAdmin(string email, string username, string password)
    {
        try
        {
            // Comprobar conexión con la base de datos.
            if (!IsConnected)
            {
                Connect();
            }

            // Validar campos obligatorios.
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"],
                    (string)Application.Current.Resources["DAEmptyFields"]
                );
                return false;
            }

            // Encriptar la contraseña.
            string encryptedPassword = await EncryptPassword(password);

            if (string.IsNullOrEmpty(encryptedPassword))
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"],
                    (string)Application.Current.Resources["DAPasswordEmpty"]
                );
                return false;
            }

            // Obtener la colección.
            var collection = database.GetCollection<BsonDocument>("users");

            // Crear documento del usuario administrador.
            var newAdmin = new BsonDocument
        {
            { "email", email },
            { "username", username },
            { "password", encryptedPassword },
            { "id_rol", 2 },
            {
                "preferences", new BsonDocument
                {
                    { "language", "en" },
                    { "theme", "light" },
                    { "rememberMe", "no" }
                }
            }
        };

            // Insertar en la base de datos.
            await collection.InsertOneAsync(newAdmin);

            // Mensaje de éxito.
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DACreatedUser"],
                (string)Application.Current.Resources["DACreatedUserText"]
            );

            return true;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                $"Error al crear el administrador: {ex.Message}"
            );
            return false;
        }
    }

    #endregion

    #region Métodos relaccionados con operaciones Read.
    // Método "GetUser". Devuelve 1 si encuentra usuario válido, 0 si no.
    public async Task<bool> GetUser(string identifier, string password, bool rememberChecked)
    {
        try
        {
            // Comprobar si hay conexión con la base de datos.
            if (!IsConnected)
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DANoConnection"]);
                return false;
            }

            var collection = database.GetCollection<BsonDocument>("users");

            // Filtro para buscar por email o username.
            var filter = Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Eq("email", identifier),
                Builders<BsonDocument>.Filter.Eq("username", identifier)
            );

            // Buscar el primer usuario que coincida con el identificador.
            var userDoc = collection.Find(filter).FirstOrDefault();

            // Si no se encuentra el usuario, mostrar mensaje y salir.
            if (userDoc == null)
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAUserNotFound"], (string)Application.Current.Resources["DAUserNotFoundText"]);
                return false;
            }

            // Obtener la contraseña del usuario encontrado.
            string storedPassword = userDoc.GetValue("password", "").AsString;

            // Encriptar la contraseña introducida para compararla.
            string encryptedInputPassword = await EncryptPassword(password);

            // Comparar la contraseña almacenada con la introducida.
            if (storedPassword != encryptedInputPassword)
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAWrongPassword"], (string)Application.Current.Resources["DAWrongPasswordText"]);
                return false;
            }

            // Si la contraseña coincide, asignar los valores al singleton "User".
            GlobalData.Instance.user.email = userDoc.GetValue("email", "").AsString;
            GlobalData.Instance.user.username = userDoc.GetValue("username", "").AsString;
            GlobalData.Instance.user.idRol = userDoc.GetValue("id_rol", 0).ToInt32();

            if (GlobalData.Instance.user.idRol == 0)
            {
                await GlobalData.Instance.messages.ShowMessage(
                        (string)Application.Current.Resources["DAError"],
                        (string)Application.Current.Resources["DAUserNotEnabled"]);
                return false;
            }

            // Si el checkbox "recordarme" está marcado, actualizar el campo en MongoDB.
            if (rememberChecked)
            {
                var update = Builders<BsonDocument>.Update.Set("preferences.rememberMe", "yes");
                await collection.UpdateOneAsync(filter, update);
            }

            // Cargar la información adicional del usuario.
            GetUserInfo(identifier);

            // Cargar los diccionarios de preferencias del usuario.
            GlobalData.Instance.user.LoadDictionaries();

            // Retornar 1 si la validación es correcta.
            return true;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], ex.Message);
            return false;
        }
    }

    // Método "GetUserInfo". Para recoger los campos de información del usuario en diccionarios. Con un diccionario (clave-valor) es más fácil trabajar.
    public async void GetUserInfo(string identifier)
    {
        try
        {
            var collection = database.GetCollection<BsonDocument>("users");

            // Buscar por email o username.
            var filter = Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Eq("email", identifier),
                Builders<BsonDocument>.Filter.Eq("username", identifier)
            );

            var userDoc = collection.Find(filter).FirstOrDefault();

            // Crear los diccionarios.
            var statistics = new Dictionary<string, int>();
            var preferences = new Dictionary<string, string>();
            var completedCircuits = new Dictionary<string, bool>();
            var completedDrivers = new Dictionary<string, bool>();
            var completedTeams = new Dictionary<string, bool>();

            // Extraer las subestructuras BSON y convertirlas a diccionarios.
            if (userDoc.Contains("statistics"))
            {
                var statsDoc = userDoc["statistics"].AsBsonDocument;
                foreach (var el in statsDoc.Elements)
                    statistics[el.Name] = el.Value.ToInt32();
            }

            if (userDoc.Contains("preferences"))
            {
                var prefsDoc = userDoc["preferences"].AsBsonDocument;
                foreach (var el in prefsDoc.Elements)
                    preferences[el.Name] = el.Value.AsString;
            }

            if (userDoc.Contains("completedCircuits"))
            {
                var circuitsDoc = userDoc["completedCircuits"].AsBsonDocument;
                foreach (var el in circuitsDoc.Elements)
                    completedCircuits[el.Name] = el.Value.ToBoolean();
            }

            if (userDoc.Contains("completedDrivers"))
            {
                var driversDoc = userDoc["completedDrivers"].AsBsonDocument;
                foreach (var el in driversDoc.Elements)
                    completedDrivers[el.Name] = el.Value.ToBoolean();
            }

            if (userDoc.Contains("completedTeams"))
            {
                var driversDoc = userDoc["completedTeams"].AsBsonDocument;
                foreach (var el in driversDoc.Elements)
                    completedTeams[el.Name] = el.Value.ToBoolean();
            }

            GlobalData.Instance.user.statistics = statistics;
            GlobalData.Instance.user.preferences = preferences;
            GlobalData.Instance.user.completedCircuits = completedCircuits;
            GlobalData.Instance.user.completedDrivers = completedDrivers;
            GlobalData.Instance.user.completedTeams = completedTeams;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], ex.Message);
        }
    }

    // Método para comprobar si un correo ya existe en la base de datos. Devuelve true si existe, false si no.
    public async Task<bool> EmailExists(string email)
    {
        try
        {
            // Comprobar conexión con la base de datos.
            if (!IsConnected)
            {
                Connect();
            }

            // Comprobar que el correo no sea nulo o vacío.
            if (string.IsNullOrWhiteSpace(email))
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAMailEmpty"]);
                return false;
            }

            // Obtener la colección de usuarios.
            var collection = database.GetCollection<BsonDocument>("users");

            // Crear el filtro para buscar el correo.
            var filter = Builders<BsonDocument>.Filter.Eq("email", email);

            // Buscar el primer documento que cumpla la condición.
            var userDoc = collection.Find(filter).FirstOrDefault();

            // Si se encuentra el usuario, mostrar mensaje y devolver true.
            if (userDoc != null)
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"],(string)Application.Current.Resources["DAMailExists"]);
                return true;
            }

            // Si no se encuentra el usuario, devolver false.
            return false;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message);
            return false;
        }
    }

    // Método para comprobar si un nombre de usuario ya existe en la base de datos. Devuelve true si existe, false si no.
    public async Task<bool> UsernameExists(string username)
    {
        try
        {
            // Comprobar conexión con la base de datos.
            if (!IsConnected)
            {
                Connect();
            }

            // Comprobar que el nombre de usuario no sea nulo o vacío.
            if (string.IsNullOrWhiteSpace(username))
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"],
                    "El nombre de usuario no puede estar vacío.");
                return false;
            }

            // Obtener la colección de usuarios.
            var collection = database.GetCollection<BsonDocument>("users");

            // Crear el filtro para buscar por nombre de usuario.
            var filter = Builders<BsonDocument>.Filter.Eq("username", username);

            // Buscar el primer documento que cumpla la condición.
            var userDoc = collection.Find(filter).FirstOrDefault();

            // Si se encuentra el usuario, mostrar mensaje y devolver true.
            if (userDoc != null)
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAUsernameExists"]); 
                return true;
            }

            // Si no se encuentra el usuario, devolver false.
            return false;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message);
            return false;
        }
    }

    // Método "HasRememberedUser". Devuelve true si existe un usuario con rememberMe = "yes".
    public bool HasRememberedUser()
    {
        try
        {
            // Comprobar conexión con la base de datos.
            if (!IsConnected)
            {
                Connect();
            }

            // Obtener la colección "users".
            var collection = database.GetCollection<BsonDocument>("users");

            // Crear el filtro para buscar un usuario con rememberMe = "yes".
            var filter = Builders<BsonDocument>.Filter.Eq("preferences.rememberMe", "yes");

            // Buscar el primer documento que cumpla la condición.
            var userDoc = collection.Find(filter).FirstOrDefault();

            // Si no se encuentra ningún usuario, devolver false.
            if (userDoc == null)
            {
                return false;
            }

            // Si se encuentra un usuario, asignar sus valores al singleton "User".
            GlobalData.Instance.user.email = userDoc.GetValue("email", "").AsString;
            GlobalData.Instance.user.username = userDoc.GetValue("username", "").AsString;
            GlobalData.Instance.user.idRol = userDoc.GetValue("id_rol", 0).ToInt32();

            // Cargar la información adicional del usuario.
            GetUserInfo(GlobalData.Instance.user.email);

            // Cargar los diccionarios de preferencias del usuario.
            GlobalData.Instance.user.LoadDictionaries();

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    // Método para obtener las preguntas del cuestionario según los parámetros dados.
    public async Task<List<QuestionsModel>> GetQuizQuestionsAsync(string collectionName,string category, string topic, string language)
    {
        try
        {
            if (!IsConnected)
                Connect();

            var collection = database.GetCollection<QuestionsModel>(collectionName);

            // Buscar category, topic y que exista el idioma dentro del diccionario.
            var filter = Builders<QuestionsModel>.Filter.And(
                Builders<QuestionsModel>.Filter.Eq(q => q.Category, category),
                Builders<QuestionsModel>.Filter.Eq(q => q.Topic, topic),
                Builders<QuestionsModel>.Filter.Exists($"language.{language}")
            );

            var all = await collection.Find(filter).ToListAsync();

            if (all.Count == 0)
                return new List<QuestionsModel>(); // Si no encuentra nada.

            // Busca 15 preguntas: 5 de cada dificultad (1, 2 y 3).
            // Se agrupan por dificultad.
            var diff1 = all.Where(p => p.Difficulty == 1).ToList();
            var diff2 = all.Where(p => p.Difficulty == 2).ToList();
            var diff3 = all.Where(p => p.Difficulty == 3).ToList();

            // Asegurar 5 por dificultad.
            if (diff1.Count < 5 || diff2.Count < 5 || diff3.Count < 5)
                return new List<QuestionsModel>();

            Random rnd = new Random();

            // Tomandr 5 de cada dificultad.
            var result = new List<QuestionsModel>();
            result.AddRange(diff1.OrderBy(x => rnd.Next()).Take(5));
            result.AddRange(diff2.OrderBy(x => rnd.Next()).Take(5));
            result.AddRange(diff3.OrderBy(x => rnd.Next()).Take(5));

            // Barajar respuestas.
            foreach (var q in result)
            {
                if (q.Language.TryGetValue(language, out var content))
                {
                    content.Options = content.Options.OrderBy(_ => rnd.Next()).ToList();
                }
            }

            // Devolver la lista barajada de preguntas.
            return result.OrderBy(x => rnd.Next()).ToList();
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message);

            return new List<QuestionsModel>();
        }
    }

    // Método para obtener los usuarios con rol 3 y 0 ordenados alfabéticamente (usuarios normales y deshabilitados).
    public async Task<List<User>> GetUsersWithRole3And0Alphabetic()
    {
        try
        {
            if (!IsConnected)
                Connect();

            var collection = database.GetCollection<User>("users");

            // Filtro: rol 3 o rol 0
            var filter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Eq(u => u.idRol, 3),
                Builders<User>.Filter.Eq(u => u.idRol, 0)
            );

            var users = await collection.Find(filter).ToListAsync();

            if (users == null || users.Count == 0)
                return new List<User>();

            // Ordenar:
            // 1º Usuarios con rol 3 ordenados por username.
            // 2º Usuarios con rol 0 ordenados por username.
            var ordered = users
                .OrderByDescending(u => u.idRol == 3)
                .ThenBy(u => u.idRol == 3 ? u.username : null)
                .ThenBy(u => u.idRol == 0 ? u.username : null)
                .ToList();

            return ordered;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message
            );
            return new List<User>();
        }
    }

    // Método para obtener los usuarios con rol 3, 2 y 0 ordenados alfabéticamente.
    public async Task<List<User>> GetUsersWithRole3And2And0Alphabetic()
    {
        try
        {
            if (!IsConnected)
                Connect();

            var collection = database.GetCollection<User>("users");

            // Filtro: rol 3, 2 o 0
            var filter = Builders<User>.Filter.In(u => u.idRol, new[] { 3, 2, 0 });

            var users = await collection.Find(filter).ToListAsync();

            if (users == null || users.Count == 0)
                return new List<User>();

            // Orden:
            // 1º Rol 3
            // 2º Rol 2
            // 3º Rol 0
            // Dentro de cada rol: username alfabético
            var ordered = users
                .OrderByDescending(u => u.idRol) // 3 → 2 → 0
                .ThenBy(u => u.username)
                .ToList();

            return ordered;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message
            );
            return new List<User>();
        }
    }

    // Método para contar todos los usuarios de rol 3 que hay en la base de datos.
    public int CountUsersRol3And0()
    {
        var collection = database.GetCollection<BsonDocument>("users");

        var filter = Builders<BsonDocument>.Filter.In(
            "id_rol",
            new[] { 3, 0 }
        );

        return (int)collection.CountDocuments(filter);
    }

    public int CountUsersRol3And2And0()
    {
        var collection = database.GetCollection<BsonDocument>("users");

        var filter = Builders<BsonDocument>.Filter.In(
            "id_rol",
            new[] { 3, 2, 0 }
        );

        return (int)collection.CountDocuments(filter);
    }
    #endregion

    #region Métodos relaccionados con operaciones Update.
    // Método "ResetRememberMe". Establece el campo rememberMe en "no" para el usuario indicado.
    public async Task ResetRememberMe(string email)
    {
        try
        {
            if (!IsConnected)
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DANoConnection"]);
                return;
            }

            var collection = database.GetCollection<BsonDocument>("users");

            var filter = Builders<BsonDocument>.Filter.Eq("email", email);
            var update = Builders<BsonDocument>.Update.Set("preferences.rememberMe", "no");

            await collection.UpdateOneAsync(filter, update);
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], ex.Message);
        }
    }

    // Método para actualizar el idioma del usuario.
    public async Task<bool> UpdateUserLanguage(string email, string newLanguage)
    {
        if (await UpdateUserField(email, "preferences.language", newLanguage) == true)
        {
            GlobalData.Instance.user.preferences["language"] = newLanguage;
            return true;
        }
        else
        {
            return false;
        }
    }

    // Método para actualizar el tema del usuario.
    public async Task<bool> UpdateUserTheme(string email, string newTheme)
    {
        if (await UpdateUserField(email, "preferences.theme", newTheme) == true)
        {
            GlobalData.Instance.user.preferences["theme"] = newTheme;
            return true;
        }
        else
        {
            return false;
        }
    }

    // Método para actualizar cierto campo del usuario en la base de datos.
    private async Task<bool> UpdateUserField(string email, string fieldPath, object newValue)
    {
        try
        {
            if (IsConnected == false) return false;

            var collection = database.GetCollection<BsonDocument>("users");

            var filter = Builders<BsonDocument>.Filter.Eq("email", email);
            var update = Builders<BsonDocument>.Update.Set(fieldPath, BsonValue.Create(newValue));

            var result = await collection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"], ex.Message);
            return false;
        }
    }

    // Método para actualizar el estado de los quizes.
    public async Task<bool> UpdateCompletedItem(string email, string dictName, string key)
    {
        try
        {
            if (!IsConnected)
                Connect();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dictName) || string.IsNullOrWhiteSpace(key))
                return false;

            var collection = database.GetCollection<BsonDocument>("users");

            string fieldPath = $"{dictName}.{key}";

            var filter = Builders<BsonDocument>.Filter.Eq("email", email);
            var update = Builders<BsonDocument>.Update.Set(fieldPath, true);

            var result = await collection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message
            );
            return false;
        }
    }

    // Método para actualizar el campo rememberME.
    public async Task<bool> UpdateRememberMeByUsername(string mail, bool isChecked)
    {
        try
        {
            if (!IsConnected)
                Connect();

            if (string.IsNullOrWhiteSpace(mail))
                return false;

            var collection = database.GetCollection<BsonDocument>("users");

            string value = isChecked ? "yes" : "no";

            var filter = Builders<BsonDocument>.Filter.Eq("email", mail);
            var update = Builders<BsonDocument>.Update.Set("preferences.rememberMe", value);

            var result = await collection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                // Actualizar también el singleton
                GlobalData.Instance.user.preferences["rememberMe"] = value;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message
            );
            return false;
        }
    }

    // Método para que el usuario pueda actualizar su contraseña.
    public async Task<bool> UpdateUserPassword(string email, string newPassword)
    {
        try
        {
            if (!IsConnected)
                Connect();

            // Validar email.
            if (string.IsNullOrWhiteSpace(email))
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"],
                    (string)Application.Current.Resources["DAMailEmpty"]);
                return false;
            }

            // Validar contraseña.
            bool valid = await ValidatePassword(newPassword, newPassword);
            if (!valid)
                return false;

            // Encriptar contraseña.
            string encryptedPassword = await EncryptPassword(newPassword);

            if (string.IsNullOrWhiteSpace(encryptedPassword))
                return false;

            var collection = database.GetCollection<BsonDocument>("users");

            var filter = Builders<BsonDocument>.Filter.Eq("email", email);
            var update = Builders<BsonDocument>.Update.Set("password", encryptedPassword);

            var result = await collection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DASuccess"],
                    (string)Application.Current.Resources["DAPasswordUpdated"]);
                return true;
            }

            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                (string)Application.Current.Resources["DAUserNotFound"]);

            return false;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message);
            return false;
        }
    }

    // Método para actualizar las estadísticas de n usuario después de un quiz.
    public async Task<bool> UpdateUserStatistics(string email, int pointsEarned, int correctAnswers, int wrongAnswers, bool perfectQuiz)
    {
        try
        {
            if (!IsConnected)
                Connect();

            var collection = database.GetCollection<BsonDocument>("users");

            var update = Builders<BsonDocument>.Update
                .Inc("statistics.points", pointsEarned)
                .Inc("statistics.correctAnswers", correctAnswers)
                .Inc("statistics.wrongAnswers", wrongAnswers)
                .Inc("statistics.totalGames", 1)
                .Inc("statistics.totalQuestions", 15);

            if (perfectQuiz)
            {
                update = update.Inc("statistics.polePositions", 1);
            }

            await collection.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("email", email),
                update
            );

            // Actualizar nivel después de actualizar puntos.
            await UpdateUserLevel(email);

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Método para actualizar el niveldel usuario según sus puntos.
    private async Task UpdateUserLevel(string email)
    {
        var collection = database.GetCollection<BsonDocument>("users");

        var user = await collection.Find(
            Builders<BsonDocument>.Filter.Eq("email", email)
        ).FirstOrDefaultAsync();

        if (user == null) return;

        int points = user["statistics"]["points"].AsInt32;
        int level = CalculateLevel(points);

        await collection.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("email", email),
            Builders<BsonDocument>.Update.Set("statistics.level", level)
        );
    }

    // Método que complementa la actualización del nivel.
    private int CalculateLevel(int points)
    {
        if (points >= 82500) return 10;
        if (points >= 67500) return 9;
        if (points >= 54000) return 8;
        if (points >= 42000) return 7;
        if (points >= 31500) return 6;
        if (points >= 22500) return 5;
        if (points >= 15000) return 4;
        if (points >= 9000) return 3;
        if (points >= 4500) return 2;
        if (points >= 1500) return 1;

        return 0;
    }

    // Método para deshabilitar una cuenta.
    public async Task<bool> DisableUserAccount(string email)
    {
        try
        {
            if (!IsConnected)
                Connect();

            // Validar email vacío.
            if (string.IsNullOrWhiteSpace(email))
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"],
                    (string)Application.Current.Resources["DAMailEmpty"]
                );
                return false;
            }

            var collection = database.GetCollection<BsonDocument>("users");

            // Filtro por correo.
            var filter = Builders<BsonDocument>.Filter.Eq("email", email);

            // Update combinado.
            var update = Builders<BsonDocument>.Update
                .Set("id_rol", 0)
                .Set("preferences.rememberMe", "no");

            // Ejecutar actualización.
            var result = await collection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DASuccess"],
                    (string)Application.Current.Resources["DAUserDisabled"]
                );
                return true;
            }
            else
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"],
                    (string)Application.Current.Resources["DAUserNotFound"]
                );
                return false;
            }
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message
            );
            return false;
        }
    }
    #endregion

    #region Métodos relaccionados con operaciones Delete.
    // Método "DeleteUserAsync". Para eliminar un usuario de la base de datos.
    public async Task<bool> DeleteUser(string email, string username)
    {
        try
        {
            if (!IsConnected)
                Connect();

            // Preguntar confirmación
            bool confirm = await GlobalData.Instance.messages.ShowConfirm(
                (string)Application.Current.Resources["DAConfirm"], 
                (string)Application.Current.Resources["DADeleteUser"] + " " + username
            );

            if (!confirm)
                return false;

            var collection = database.GetCollection<BsonDocument>("users");

            // Filtro doble (email + username)
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("email", email),
                Builders<BsonDocument>.Filter.Eq("username", username)
            );

            var result = await collection.DeleteOneAsync(filter);

            if (result.DeletedCount > 0)
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DASuccess"],
                    (string)Application.Current.Resources["DAUserDeleted"]
                );
                return true;
            }
            else
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"], 
                    (string)Application.Current.Resources["DAUserNotFound"]
                );
                return false;
            }
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"], ex.Message);
            return false;
        }
    }

    // Método para actualizar la información de un usuario.
    public async Task<bool> UpdateUserData(string email, string newUsername, string newPassword, int newRol)
    {
        try
        {
            if (!IsConnected)
                Connect();

            if (string.IsNullOrWhiteSpace(email))
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"],
                    (string)Application.Current.Resources["DAMailEmpty"]);
                return false;
            }

            var collection = database.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.Eq("email", email);

            var updateDefs = new List<UpdateDefinition<BsonDocument>>();

            // Actualizar el username.
            if (!string.IsNullOrWhiteSpace(newUsername))
            {
                // Comprobar que no exista otro usuario con ese username.
                var exists = await UsernameExists(newUsername);
                if (exists)
                {
                    // UsernameExists ya muestra el mensaje de error.
                    return false;
                }

                updateDefs.Add(
                    Builders<BsonDocument>.Update.Set("username", newUsername)
                );
            }

            // Actualizar la contraseña.
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                string encrypted = await EncryptPassword(newPassword);

                if (string.IsNullOrWhiteSpace(encrypted))
                {
                    await GlobalData.Instance.messages.ShowMessage(
                        (string)Application.Current.Resources["DAError"],
                        (string)Application.Current.Resources["DAPasswordEmpty"]);
                    return false;
                }

                updateDefs.Add(
                    Builders<BsonDocument>.Update.Set("password", encrypted)
                );
            }

            // Actualizar el rol.
            if (newRol >= 0)
            {
                updateDefs.Add(
                    Builders<BsonDocument>.Update.Set("id_rol", newRol)
                );
            }

            // Si no hay nada que actualizar.
            if (!updateDefs.Any())
                return false;

            var combinedUpdates =
                Builders<BsonDocument>.Update.Combine(updateDefs);

            var result = await collection.UpdateOneAsync(filter, combinedUpdates);

            if (result.ModifiedCount > 0)
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DASuccess"],
                    (string)Application.Current.Resources["DAUserUpdated"]);
                return true;
            }

            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                (string)Application.Current.Resources["DAUserNotFound"]);
            return false;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message);
            return false;
        }
    }
    #endregion

    #region Métodos relaccionados con la verificación de contraseñas y correos, encriptado de contraseñas.
    // Método "ValidatePassword". Para validar que la contraseña cumple los requisitos mínimos. (Mínimo 8 caracteres, al menos 2 letras y 1 mayúscula).
    public async Task<bool> ValidatePassword(string password, string password2)
    {
        try
        {
            // Comprobar si la contraseña es nula o vacía.
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(password2))
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAPasswordEmpty"]);
                return false;
            }

            // Comprobar que la contraseña y su confirmación coinciden.
            if (password != password2)
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAPasswordNoMatch"]);
                return false;
            }

            // Comprobar longitud mínima.
            if (password.Length < 8 || password2.Length < 8)
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAPasswordMin8Chars"]);
                return false;
            }

            // Comprobar si contiene al menos dos letras.
            int letterCount = password.Count(char.IsLetter);
            if (letterCount < 2)
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAPasswordMin2Letters"]);
                return false;
            }

            // Comprobar si contiene al menos una mayúscula.
            if (!password.Any(char.IsUpper))
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAPasswordMin1CapitalLetter"]);
                return false;
            }

            // Si cumple todos los requisitos, devolver true.
            return true;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message);
            return false;
        }
    }

    // Método "ValidateEmail". Para validar que el correo cumple los requisitos mínimos.
    public async Task<bool> ValidateEmail(string email)
    {
        try
        {
            // Comprobar si el correo es nulo o está vacío.
            if (string.IsNullOrWhiteSpace(email))
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAMailEmpty"]); 
                return false;
            }

            // Expresión regular para validar el formato del correo.
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            // Comprobar si el formato es válido.
            bool isValid = System.Text.RegularExpressions.Regex.IsMatch(email, pattern);

            if (!isValid)
            {
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAMailNoMatchPattern"]);
                return false;
            }

            // Si cumple el formato, devolver true.
            return true;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message);
            return false;
        }
    }

    // Método para encriptar las constraseñas usando SHA256.
    public async Task<string> EncryptPassword(string password) { 
        try {
            //Crear instancia del algoritmo SHA256.
            using (SHA256 sha256 = SHA256.Create()) {
                // Convertir la contraseña a bytes.
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);  
                // Calcular el hash. 
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);  
                // Convertir el hash a un string hexadecimal. 
                StringBuilder sb = new StringBuilder(); 

                foreach (byte b in hashBytes) { 
                    sb.Append(b.ToString("x2")); // Formato hexadecimal.
                }  
                
                return sb.ToString(); // Devolver el hash en forma de string. 
            } 
        } catch (Exception ex) { 
            await GlobalData.Instance.messages.ShowMessage( (string)Application.Current.Resources["DAError"], ex.Message);
            return string.Empty; 
        } 
    }

    // Método para generar un token seguro para reestablecer contraseña.
    private string GenerateSecureToken()
    {
        byte[] bytes = new byte[32];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
    }

    // Método para solicitar el reestablecimiento de la constraseña.
    public async Task<bool> RequestPasswordReset(string email)
    {
        try
        {
            if (!IsConnected)
                Connect();

            if (string.IsNullOrWhiteSpace(email))
                return false;

            var collection = database.GetCollection<BsonDocument>("users");

            var filter = Builders<BsonDocument>.Filter.Eq("email", email);

            var user = await collection.Find(filter).FirstOrDefaultAsync();

            if (user == null)
                return true;

            string token = GenerateSecureToken();
            DateTime expiry = DateTime.UtcNow.AddMinutes(30);

            var update = Builders<BsonDocument>.Update
                .Set("resetToken", token)
                .Set("resetTokenExpiry", expiry);

            await collection.UpdateOneAsync(filter, update);

            await SendResetEmail(email, token);

            return true;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message);
            return false;
        }
    }

    // Método para enviar el correo de reestablecimiento de contraseña.
    private async Task SendResetEmail(string email, string token)
    {
        try
        {
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress("carrionalvaro25@gmail.com", "Tu App");
                mail.To.Add(email);
                mail.Subject = "Restablecer contraseña";
                mail.Body = $"Tu código para restablecer contraseña es:\n\n{token}\n\nCaduca en 30 minutos.";
                mail.IsBodyHtml = false;

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential("carrionalvaro25@gmail.com", "liagudhqpgsvenvw");
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    await smtp.SendMailAsync(mail);
                }
            }
        }
        catch (SmtpException ex)
        {
            // Esto te dirá exactamente por qué falló (ej. error de autenticación)
            await GlobalData.Instance.messages.ShowMessage("Error de envío", $"Código: {ex.StatusCode}\n{ex.Message}");
        }
    }

    // Método para confirmar la nueva contraseña.
    public async Task<bool> ResetPasswordWithToken(string token, string newPassword)
    {
        try
        {
            if (!IsConnected)
                Connect();

            if (string.IsNullOrWhiteSpace(token))
                return false;

            bool valid = await ValidatePassword(newPassword, newPassword);
            if (!valid)
                return false;

            var collection = database.GetCollection<BsonDocument>("users");

            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("resetToken", token),
                Builders<BsonDocument>.Filter.Gt("resetTokenExpiry", DateTime.UtcNow)
            );

            var user = await collection.Find(filter).FirstOrDefaultAsync();

            if (user == null)
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"],
                    "Token inválido o expirado.");
                return false;
            }

            string encrypted = await EncryptPassword(newPassword);

            var update = Builders<BsonDocument>.Update
                .Set("password", encrypted)
                .Unset("resetToken")
                .Unset("resetTokenExpiry");

            await collection.UpdateOneAsync(filter, update);

            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DASuccess"],
                "Contraseña actualizada correctamente.");

            return true;
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message);
            return false;
        }
    }
    #endregion
}