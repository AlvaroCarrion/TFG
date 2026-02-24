using PPQ.Singleton;

namespace PPQ.Views.AdminShell;

[QueryProperty(nameof(UserData), "UserData")]
public partial class EditUsersPage : ContentPage
{
    private User _userData;

    public User UserData
    {
        get => _userData;
        set
        {
            _userData = value;
            LoadUserInfo();
        }
    }

    public EditUsersPage()
	{
		InitializeComponent();
	}

    private void LoadUserInfo()
    {
        if (_userData == null)
            return;

        userEmail.Text = _userData.email;
        username.Text = _userData.username;
    }

    private async void EditUser(object sender, EventArgs e)
    {
        // Hacer el mail obligatorio.
        if (string.IsNullOrWhiteSpace(userEmail.Text))
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                (string)Application.Current.Resources["DAMailEmpty"]);
            return;
        }

        // Validar la contraseña.
        string finalPassword = null;

        bool pass1Empty = string.IsNullOrWhiteSpace(password.Text);
        bool pass2Empty = string.IsNullOrWhiteSpace(password2.Text);

        if ((!pass1Empty && pass2Empty) || (pass1Empty && !pass2Empty))
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                (string)Application.Current.Resources["DAPasswordNoMatch"]);
            return;
        }

        if (!pass1Empty && !pass2Empty)
        {
            bool isValid = await GlobalData.Instance.db.ValidatePassword(password.Text, password2.Text);
            if (!isValid)
            {
                return;
            }
            
            finalPassword = password.Text;
        }

        bool success = await GlobalData.Instance.db.UpdateUserData(userEmail.Text, username.Text, finalPassword, 3);
    }
}