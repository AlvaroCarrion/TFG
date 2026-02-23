using PPQ.Navigation;
using PPQ.Singleton;

namespace PPQ
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            if (GlobalData.Instance.db.HasRememberedUser() == true)
            {
                switch (GlobalData.Instance.user.idRol)
                {
                    case 1: // Superadministrador.
                        return new Window(new SuperadminShell());
                    case 2: // Administrador.
                        return new Window(new AdminShell());
                    case 3: // Usuario.
                        return new Window(new UserShell());
                }

                return new Window(new LoginShell());
            }
            else {
                return new Window(new LoginShell());
            }
        }
    }
}