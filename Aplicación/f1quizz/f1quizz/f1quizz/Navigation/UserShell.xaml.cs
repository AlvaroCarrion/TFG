using PPQ.Views.UserShell;

namespace PPQ.Navigation;

public partial class UserShell : Shell
{
	public UserShell()
	{
		InitializeComponent();
        Routing.RegisterRoute("QuizMenu", typeof(QuizMenuPage));
        Routing.RegisterRoute("MultiplayerQuizPage", typeof(PPQ.Views.UserShell.MultiplayerQuizPage));
    }
}