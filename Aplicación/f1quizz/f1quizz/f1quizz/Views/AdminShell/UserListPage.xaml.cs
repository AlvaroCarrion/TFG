using PPQ.Singleton;

namespace PPQ.Views.AdminShell;

public partial class UserListPage : ContentPage
{
	public UserListPage()
	{
		InitializeComponent();
        Loaded += UserListPage_Loaded;
    }

    private async void UserListPage_Loaded(object sender, EventArgs e)
    {
        await LoadUsers();
    }

    // Método que carga los usuarios llamamando al método de base de datos. Usuarios con rol 3 (normales).
    private async Task LoadUsers()
    {
        var users = await GlobalData.Instance.db.GetUsersWithRole3And0Alphabetic();

        if (users == null || users.Count == 0)
        {
            await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], (string)Application.Current.Resources["DAUsersNotFound"]); 
            return;
        }

        UsersList.ItemsSource = users;

        labelUsers.Text = (string)Application.Current.Resources["LabelUsers"] + ": " + GlobalData.Instance.db.CountUsersRol3And0();
    }

    // Moverse a la página de edición de usuarios, pasando el usuario seleccionado como parámetro.
    private async void GoToEdit(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is User selectedUser)
        {
            await Shell.Current.GoToAsync("///EditUsersPage", true, new Dictionary<string, object> {{ "UserData", selectedUser }});
        }
    }

    private async void BanUserClick(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button == null)
            return;

        var user = button.CommandParameter as User;
        if (user == null)
            return;

        bool confirm = await GlobalData.Instance.messages.ShowConfirm(
            (string)Application.Current.Resources["DAConfirm"],
            (string)Application.Current.Resources["DAConfirmBanUser"] + ": " + user.username
        );

        if (!confirm)
            return;

        // Llamar al método de la base de datos para deshabilitar la cuenta.
        bool disabled = await GlobalData.Instance.db.DisableUserAccount(user.email);

        if (disabled)
        {
            // Recargar los usuarios actualizados desde la BD
            var updatedUsers = await GlobalData.Instance.db.GetUsersWithRole3And0Alphabetic();

            if (updatedUsers == null || updatedUsers.Count == 0)
            {
                UsersList.ItemsSource = new List<User>();
                return;
            }

            // Actualizar visualmente la lista
            UsersList.ItemsSource = null;
            UsersList.ItemsSource = updatedUsers;
        }
    }

    // Borrar el usuario seleccionado.
    private async void DeleteUserClick(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button == null)
            return;

        var user = button.CommandParameter as User;
        if (user == null)
            return;

        // Llamar al método de la base de datos.
        bool deleted = await GlobalData.Instance.db.DeleteUser(user.email, user.username);

        if (deleted)
        {
            // Quitar usuario de la lista visual.
            var list = UsersList.ItemsSource as List<User>;
            if (list != null)
            {
                list.Remove(user);
                UsersList.ItemsSource = null;
                UsersList.ItemsSource = list;
            }
        }

        labelUsers.Text = (string)Application.Current.Resources["LabelUsers"] + ": " + GlobalData.Instance.db.CountUsersRol3And0();
    }
}