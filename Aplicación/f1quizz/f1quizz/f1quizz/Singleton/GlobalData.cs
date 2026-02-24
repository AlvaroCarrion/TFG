using PPQ.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPQ.Singleton
{
    internal class GlobalData
    {
        // Atributo "_instance" de tipo (clase) "GlobalData". Este atributo sólo puede ser accedido dentro de la clase "GlobalData".
        private static GlobalData _instance;

        // Propiedad "Instance".
        public static GlobalData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GlobalData();
                }
                return _instance;
            }
        }

        // Constructor. Con este constructor, nos aseguramos de que NO podemos crear objetos de este tipo fuera de esta clase.
        private GlobalData() {}

        // Atributo "db". Crear la base de datos y utilizar este atributo para acceder de forma global, gracias  a esta clase, a sus métodos y propiedades.
        public DatabaseClass db = new DatabaseClass();

        // Atributo "user".
        public User user = new User();

        // Atributo "messages". Clase propia para mostrar mis propios mensajes.
        public CustomMessages messages = new CustomMessages();

        // Atributo "multiplayer". Clase propia para controlar los servicios del servidor.
        public MultiplayerService multiplayer = new MultiplayerService();
    }
}
