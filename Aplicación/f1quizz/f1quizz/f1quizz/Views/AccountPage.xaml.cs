using PPQ.Singleton;
using PPQ.Resources.Lenguages;
using PPQ.Resources.Themes;
using System.Threading.Tasks;

namespace PPQ.Views;

public partial class AccountPage : ContentPage
{
    private bool _pageLoaded = false;

    public AccountPage()
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

        passwordEntry.Unfocus();

        FillInfo();

        if (GlobalData.Instance.user.idRol == 1 || GlobalData.Instance.user.idRol == 2)
        {
            statsBox.IsVisible = false;
            statsLabel.IsVisible = false;
            statsGrid.IsVisible = false;
        }
    }

	public void FillInfo()
	{
        GlobalData.Instance.db.GetUserInfo(GlobalData.Instance.user.email);
        emailLabel.Text = GlobalData.Instance.user.email;
        usernameLabel.Text = GlobalData.Instance.user.username;

        switch (GlobalData.Instance.user.preferences.GetValueOrDefault("language"))
        {
            case "en":
                languagePicker.SelectedItem = (string)Application.Current.Resources["LabelLanguageEnglish"];
                break;

            case "sp":
                languagePicker.SelectedItem = (string)Application.Current.Resources["LabelLanguageSpanish"];
                break;
        }

        switch (GlobalData.Instance.user.preferences.GetValueOrDefault("theme"))
        {
            case "light":
                themePicker.SelectedItem = (string)Application.Current.Resources["LabelThemeLight"];
                break;

            case "dark":
                themePicker.SelectedItem = (string)Application.Current.Resources["LabelThemeDark"];
                break;
        }

        switch (GlobalData.Instance.user.preferences.GetValueOrDefault("rememberMe"))
        {
            case "yes":
                rememberMeCheckBox.IsChecked = true;
                break;

            case "no":
                rememberMeCheckBox.IsChecked = false;
                break;
        }

        levelLabel.Text = GlobalData.Instance.user.statistics.GetValueOrDefault("level").ToString();
        pointsLabel.Text = GlobalData.Instance.user.statistics.GetValueOrDefault("points").ToString();
        polepositionsLabel.Text = GlobalData.Instance.user.statistics.GetValueOrDefault("polePositions").ToString();

        correctAnsLabel.Text = GlobalData.Instance.user.statistics.GetValueOrDefault("correctAnswers").ToString();
        wrongAnsLabel.Text = GlobalData.Instance.user.statistics.GetValueOrDefault("wrongAnswers").ToString();
        totalAnsLabel.Text = GlobalData.Instance.user.statistics.GetValueOrDefault("totalQuestions").ToString();

        totalQuizesLabel.Text = GlobalData.Instance.user.statistics.GetValueOrDefault("totalGames").ToString();

        _pageLoaded = true;
    }

    public async void LanguagePicker(object sender, EventArgs e)
    {
        ResourceDictionary newDiccionary = null;

        switch (languagePicker.SelectedIndex)
        {
            case 0:
                await GlobalData.Instance.db.UpdateUserLanguage(GlobalData.Instance.user.email, "en");
                newDiccionary = new English();
                Application.Current.Resources.MergedDictionaries.Add(newDiccionary);
                break;

            case 1:
                await GlobalData.Instance.db.UpdateUserLanguage(GlobalData.Instance.user.email, "sp");
                newDiccionary = new Spanish();
                Application.Current.Resources.MergedDictionaries.Add(newDiccionary);
                break;
        }
    }

    public async void ThemePicker(object sender, EventArgs e)
    {
        ResourceDictionary newDiccionary = null;

        switch (themePicker.SelectedIndex)
        {
            case 0:
                GlobalData.Instance.user.preferences["theme"] = "light";
                await GlobalData.Instance.db.UpdateUserTheme(GlobalData.Instance.user.email, "light");
                newDiccionary = new LightTheme();
                Application.Current.Resources.MergedDictionaries.Add(newDiccionary);
                break;

            case 1:
                GlobalData.Instance.user.preferences["theme"] = "dark";
                await GlobalData.Instance.db.UpdateUserTheme(GlobalData.Instance.user.email, "dark");
                newDiccionary = new DarkTheme();
                Application.Current.Resources.MergedDictionaries.Add(newDiccionary);
                break;
        }
    }

    private async void RememberMeCheckBoxChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!_pageLoaded)
            return;

        if (string.IsNullOrWhiteSpace(GlobalData.Instance.user.username))
            return;

        bool updated = await GlobalData.Instance.db.UpdateRememberMeByUsername(
            GlobalData.Instance.user.email,
            e.Value
        );

        if (!updated)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                (string)Application.Current.Resources["DAErrorUpdate"]
            );
        }
    }

    private async void ChangePassword(object sender, EventArgs e)
    {
        string newPassword = passwordEntry.Text;

        bool updated = await GlobalData.Instance.db.UpdateUserPassword(GlobalData.Instance.user.email, newPassword);

        if (updated)
        {
            // Limpiar campos tras éxito
            passwordEntry.Text = string.Empty;
        }
    }

    private async void CerrarSesion(object sender, EventArgs e)
    {
        await GlobalData.Instance.db.ResetRememberMe(GlobalData.Instance.user.email);
        Application.Current.MainPage = new Navigation.LoginShell();
    }
}