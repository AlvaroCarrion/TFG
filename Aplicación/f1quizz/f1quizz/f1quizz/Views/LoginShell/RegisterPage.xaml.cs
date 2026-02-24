using PPQ.Singleton;
using PPQ.Resources.Lenguages;
using PPQ.Resources.Themes;

namespace PPQ.Views.LoginShell;

public partial class RegisterPage : ContentPage
{
    private bool _isProcessing = false; // Evita que se dispare el proceso más de una vez.

    public async void registerButton_Clicked(object sender, EventArgs e)
    {
        // Evitar múltiples ejecuciones simultáneas.
        if (_isProcessing)
            return;

        _isProcessing = true;

        try
        {
            // Validar conexión.
            if (!GlobalData.Instance.db.IsConnected)
            {
                return;
            }

            string email = mailEntry.Text?.Trim();
            string username = usernameEntry.Text?.Trim();
            string password = passwordEntry.Text;
            string password2 = passwordEntry2.Text;

            // Verificar existencia de usuario o correo.
            bool existMail = await GlobalData.Instance.db.EmailExists(email);
            bool existUsername = await GlobalData.Instance.db.UsernameExists(username);

            if (existMail || existUsername)
            {
                // Si ya existen, se muestra mensaje dentro de los propios métodos.
                return;
            }

            // Validar formato de correo y contraseña.
            bool validEmail = await GlobalData.Instance.db.ValidateEmail(email);
            bool validPassword = await GlobalData.Instance.db.ValidatePassword(password, password2);

            if (!validEmail || !validPassword)
            {
                // Mensajes ya mostrados en sus respectivos métodos.
                return;
            }

            // Crear usuario.
            bool userCreated = await GlobalData.Instance.db.CreateUser(email, username, password);

            if (userCreated)
            {
                // Obtener los datos del usuario recién creado.
                bool getUser = await GlobalData.Instance.db.GetUser(username, password, false);
                // Registro exitoso, navegar a la página principal.
                Application.Current.MainPage = new Navigation.UserShell();
            }
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                ex.Message);
        }
        finally
        {
            _isProcessing = false; // Permite volver a intentar el registro.
        }
    }

    public RegisterPage()
	{
		InitializeComponent();

        languagePicker.ItemsSource = new List<string>
        {
            (string)Application.Current.Resources["LabelLanguageEnglish"],
            (string)Application.Current.Resources["LabelLanguageSpanish"]
        };

        themePicker.ItemsSource = new List<string>
        {
            (string)Application.Current.Resources["LabelThemeLight"],
            (string)Application.Current.Resources["LabelThemeDark"]
        };

        languagePicker.SelectedItem = (string)Application.Current.Resources["LabelLanguageEnglish"];
        themePicker.SelectedItem = (string)Application.Current.Resources["LabelThemeLight"];
    }

    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        // Si los entrys tienen texto, habilita el botón.
        bool isReady = !string.IsNullOrEmpty(mailEntry.Text) && !string.IsNullOrEmpty(usernameEntry.Text) && !string.IsNullOrEmpty(passwordEntry.Text) && !string.IsNullOrEmpty(passwordEntry2.Text);

        registerButton.IsEnabled = isReady;
        registerButton.Opacity = isReady ? 1.0 : 0.5;
    }

    public void LanguagePicker(object sender, EventArgs e)
    {
        ResourceDictionary newDiccionary = null;

        switch (languagePicker.SelectedIndex)
        {
            case 0:
                GlobalData.Instance.user.preferences["language"] = "en";
                newDiccionary = new English();
                Application.Current.Resources.MergedDictionaries.Add(newDiccionary);
                break;

            case 1:
                GlobalData.Instance.user.preferences["language"] = "sp";
                newDiccionary = new Spanish();
                Application.Current.Resources.MergedDictionaries.Add(newDiccionary);
                break;
        }
    }

    public void ThemePicker(object sender, EventArgs e)
    {
        ResourceDictionary newDiccionary = null;

        switch (themePicker.SelectedIndex)
        {
            case 0:
                GlobalData.Instance.user.preferences["theme"] = "light";
                newDiccionary = new LightTheme();
                Application.Current.Resources.MergedDictionaries.Add(newDiccionary);
                break;

            case 1:
                GlobalData.Instance.user.preferences["theme"] = "dark";
                newDiccionary = new DarkTheme();
                Application.Current.Resources.MergedDictionaries.Add(newDiccionary);
                break;
        }
    }

    private async void goToLogin(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///LoginPage");
    }
}