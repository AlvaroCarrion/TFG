using PPQ.Singleton;

namespace PPQ.Views.AdminShell;

public partial class AdminMenuPage : ContentPage
{
	public AdminMenuPage()
	{
		InitializeComponent();

        labelWelcome.Text += " " + GlobalData.Instance.user.username;

        if (GlobalData.Instance.db.Connect() == true)
        {
            labelState.Text = (string)Application.Current.Resources["LabelDBStateConnected"];
            labelState.TextColor = (Color)Application.Current.Resources["ColorCorrectAns"];
        } else
        {
            labelState.Text = (string)Application.Current.Resources["LabelDBStateDisconnected"];
            labelState.TextColor = (Color)Application.Current.Resources["ColorWrongAns"];
        }
	}

    private async void GoToAccount(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Views.AccountPage());
    }

    private async void GoToUserList(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///UserListPage");
    }
}