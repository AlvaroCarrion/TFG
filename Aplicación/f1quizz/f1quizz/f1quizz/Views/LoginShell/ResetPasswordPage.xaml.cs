using PPQ.Singleton;
using PPQ.Resources.Lenguages;
using PPQ.Resources.Themes;
using PPQ.Navigation;

namespace PPQ.Views.LoginShell;

public partial class ResetPasswordPage : ContentPage
{
	public ResetPasswordPage()
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
        bool isReady = !string.IsNullOrEmpty(tokenEntry.Text) && !string.IsNullOrEmpty(resetEntry.Text);

        resetPassButton.IsEnabled = isReady;
        resetPassButton.Opacity = isReady ? 1.0 : 0.5;
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

    public async void resetPassButton_Clicked(object sender, EventArgs e)
    {
        resetPassButton.IsEnabled = false;
        resetPassButton.Opacity = 0.5;

        try
        {
            string token = tokenEntry.Text?.Trim();
            string newPassword = resetEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"],
                    "Por favor, rellena todos los campos.");
                return;
            }

            bool isSuccess = await GlobalData.Instance.db.ResetPasswordWithToken(token, newPassword);

            if (isSuccess)
            {
                tokenEntry.Text = string.Empty;
                resetEntry.Text = string.Empty;

                await Shell.Current.GoToAsync("///LoginPage");
            }
        }
        catch (Exception ex)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"], ex.Message);
        }
        finally
        {
            resetPassButton.IsEnabled = true;
            resetPassButton.Opacity = 1.0;
        }
    }
}