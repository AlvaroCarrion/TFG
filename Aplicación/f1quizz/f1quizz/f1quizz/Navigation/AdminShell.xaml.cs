using PPQ.Views.AdminShell;

namespace PPQ.Navigation;

public partial class AdminShell : Shell
{
	public AdminShell()
	{
		InitializeComponent();
        Routing.RegisterRoute("AdminMenuPage", typeof(AdminMenuPage));
    }
}