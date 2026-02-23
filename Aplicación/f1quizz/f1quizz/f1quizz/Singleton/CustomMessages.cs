using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Clase propia para mostrar mensajes por pantalla personalizados y así tener un código más limpio y estructurado.s
namespace PPQ.Singleton
{
    class CustomMessages
    {

        // Mensaje simple con un botón.
        public async Task ShowMessage(string title, string message)
        {
            await App.Current.MainPage.DisplayAlert(title, message, (string)Application.Current.Resources["DAAcept"]);
        }

        // Mensaje con dos botones (por ejemplo: Sí / No).
        public async Task<bool> ShowConfirm(string title, string message)
        {
            bool result = await App.Current.MainPage.DisplayAlert(title, message, (string)Application.Current.Resources["DAConfirm"], (string)Application.Current.Resources["DACancel"]);
            return result;
        }
    }
}