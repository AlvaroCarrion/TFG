using PPQ.Singleton;
using PPQ.Resources.Lenguages;
using PPQ.Resources.Themes;
using PPQ.Navigation;

namespace PPQ.Views.LoginShell;

public partial class LoginPage : ContentPage
{
    public LoginPage()
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
        // Si ambos entrys tienen texto, habilita el botón.
        bool isReady = !string.IsNullOrEmpty(usernameEntry.Text) && !string.IsNullOrEmpty(passwordEntry.Text);

        loginButton.IsEnabled = isReady;
        loginButton.Opacity = isReady ? 1.0 : 0.5;
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

    public async void loginInButton_Clicked(object sender, EventArgs e)
    {
        usernameEntry.Unfocus();
        passwordEntry.Unfocus();

        if (GlobalData.Instance.db.IsConnected == true) {
            bool result = await GlobalData.Instance.db.GetUser(usernameEntry.Text, passwordEntry.Text, rememberMeCheckBox.IsChecked);

            if (!result)
            {
                // Login incorrecto.
                return;
            }

            if (result == true)
            {
                switch (GlobalData.Instance.user.idRol)
                {
                    case 1: // Superadministrador.
                        Application.Current.MainPage = new Navigation.AdminShell();
                        break;
                    case 2: // Administrador.
                        Application.Current.MainPage = new Navigation.AdminShell();
                        break;
                    case 3: // Usuario.
                        Application.Current.MainPage = new Navigation.UserShell();
                        break;
                }
                
            }
        }
    }

    public async void resetPassButton_Clicked(object sender, EventArgs e)
    {
        usernameEntry.Unfocus();

        string email = usernameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert(
                (string)Application.Current.Resources["DAError"],
                (string)Application.Current.Resources["LabelEnterEmail"],
                "OK");
            return;
        }

        bool confirm = await DisplayAlert(
            (string)Application.Current.Resources["LabelResetPass"],
            (string)Application.Current.Resources["LabelConfirmResetPassword"], 
            "OK",
            (string)Application.Current.Resources["DACancel"]);

        if (!confirm)
            return;

        if (!GlobalData.Instance.db.IsConnected)
            GlobalData.Instance.db.Connect();

        bool result = await GlobalData.Instance.db.RequestPasswordReset(email);

        if (result)
        {
            await DisplayAlert(
                (string)Application.Current.Resources["DASuccess"],
                (string)Application.Current.Resources["LabelResetEmailSent"],
                "OK");

            await Shell.Current.GoToAsync("///ResetPasswordPage");
        }
        else
        {
            await DisplayAlert(
                (string)Application.Current.Resources["DAError"],
                (string)Application.Current.Resources["LabelGenericError"],
                "OK");
        }
    }

    private async void goToRegister(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///RegisterPage");
    }
}