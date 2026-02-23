using PPQ.Views.SuperadminShell;

namespace PPQ.Navigation;

public partial class SuperadminShell : Shell
{
	public SuperadminShell()
	{
		InitializeComponent();
        Routing.RegisterRoute("SuperaMenuPage", typeof(SuperadminMenuPage));
    }
}