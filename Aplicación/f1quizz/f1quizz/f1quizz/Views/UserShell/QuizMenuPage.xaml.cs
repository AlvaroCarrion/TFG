using PPQ.Database;
using PPQ.Singleton;

namespace PPQ.Views.UserShell;

public partial class QuizMenuPage : ContentPage
{
    public CollectionViewItem cvi = new CollectionViewItem();
    public QuizMenuPage()
    {
        InitializeComponent();

        circuitsCollectionView.ItemsSource = cvi.CreateListCircuits();
        driversCollectionView.ItemsSource = cvi.CreateListDrivers();
        teamsCollectionView.ItemsSource = cvi.CreateListTeams();
    }

    private async void GoToAccount(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Views.AccountPage());
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {

        if (e.CurrentSelection.FirstOrDefault() is CollectionViewItem item)
        {
            // Limpia la selección visual.
            ((CollectionView)sender).SelectedItem = null;

            // Navegar a la página correspondiente.
            await Navigation.PushAsync(item.CreatePage());
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        circuitsCollectionView.ItemsSource = cvi.CreateListCircuits();
        driversCollectionView.ItemsSource = cvi.CreateListDrivers();
        teamsCollectionView.ItemsSource = cvi.CreateListTeams();
    }
}

// Clase para manejar los elementos del CollectionView.
public class CollectionViewItem {
	public string Title { get; set; }

	public string Image { get; set; }

    public Func<Page> CreatePage { get; set; }

    public bool IsCompleted { get; set; }

    // Método que devuelve los objetos que aparecerán en el CollectionView (Circuitos).
    public List<CollectionViewItem> CreateListCircuits()
    {
        var completed = GlobalData.Instance.user.completedCircuits;

        return new List<CollectionViewItem>
        {
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelCircuitMonaco"],
                Image = "monaco.png",
                IsCompleted = completed.ContainsKey("monaco") && completed["monaco"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelCircuitMonaco"], "questionsCircuits", "circuits", "monaco", GlobalData.Instance.user.preferences["language"])
            },
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelCircuitMontmeló"],
                Image = "montmelo.png",
                IsCompleted = completed.ContainsKey("montmelo") && completed["montmelo"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelCircuitMontmeló"], "questionsCircuits", "circuits", "montmelo", GlobalData.Instance.user.preferences["language"])
            },
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelCircuitSilverstone"],
                Image = "silverstone.png",
                IsCompleted = completed.ContainsKey("silverstone") && completed["silverstone"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelCircuitSilverstone"], "questionsCircuits", "circuits", "silverstone", GlobalData.Instance.user.preferences["language"])
            },
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelCircuitSuzuka"],
                Image = "suzuka.png",
                IsCompleted = completed.ContainsKey("suzuka") && completed["suzuka"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelCircuitSuzuka"], "questionsCircuits", "circuits", "suzuka", GlobalData.Instance.user.preferences["language"])
            }
        };
    }

    // Método que devuelve los objetos que aparecerán en el CollectionView (Pilotos).
    public List<CollectionViewItem> CreateListDrivers()
    {
        var completed = GlobalData.Instance.user.completedDrivers;

        return new List<CollectionViewItem>
        {
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelDriverAyrtonSenna"],
                Image = "ayrton_senna.png",
                IsCompleted = completed.ContainsKey("ayrtonSenna") && completed["ayrtonSenna"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelDriverAyrtonSenna"], "questionsDrivers", "drivers", "ayrtonSenna", GlobalData.Instance.user.preferences["language"])
            },
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelDriverFernandoAlonso"],
                Image = "fernando_alonso.png",
                IsCompleted = completed.ContainsKey("fernandoAlonso") && completed["fernandoAlonso"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelDriverFernandoAlonso"], "questionsDrivers", "drivers", "fernandoAlonso", GlobalData.Instance.user.preferences["language"])
            },
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelDriverLewisHamilton"],
                Image = "lewis_hamilton.png",
                IsCompleted = completed.ContainsKey("lewisHamilton") && completed["lewisHamilton"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelDriverLewisHamilton"], "questionsDrivers", "drivers", "lewisHamilton", GlobalData.Instance.user.preferences["language"])
            },
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelDriverMichaelSchumacher"],
                Image = "michael_schumacher.png",
                IsCompleted = completed.ContainsKey("michaelSchumacher") && completed["michaelSchumacher"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelDriverMichaelSchumacher"], "questionsDrivers", "drivers", "michaelSchumacher", GlobalData.Instance.user.preferences["language"])
            }
        };
    }

    // Método que devuelve los objetos que aparecerán en el CollectionView (Equipos).
    public List<CollectionViewItem> CreateListTeams()
    {

        var completed = GlobalData.Instance.user.completedTeams;

        return new List<CollectionViewItem>
        {
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelTeamFerrari"],
                Image = "ferrari.png",
                IsCompleted = completed.ContainsKey("ferrari") && completed["ferrari"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelTeamFerrari"], "questionsTeams", "teams", "ferrari", GlobalData.Instance.user.preferences["language"])
            },
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelTeamMcLaren"],
                Image = "mclaren.png",
                IsCompleted = completed.ContainsKey("mclaren") && completed["mclaren"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelTeamMcLaren"], "questionsTeams", "teams", "mclaren", GlobalData.Instance.user.preferences["language"])
            },
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelTeamMercedes"],
                Image = "mercedes.png",
                IsCompleted = completed.ContainsKey("mercedes") && completed["mercedes"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelTeamMercedes"], "questionsTeams", "teams", "mercedes", GlobalData.Instance.user.preferences["language"])
            },
            new CollectionViewItem
            {
                Title = (string)Application.Current.Resources["LabelTeamRedBull"],
                Image = "redbull.png",
                IsCompleted = completed.ContainsKey("redbull") && completed["redbull"],
                CreatePage = () => new QuizPage((string)Application.Current.Resources["LabelTeamRedBull"], "questionsTeams", "teams", "redbull", GlobalData.Instance.user.preferences["language"])
            }
        };
    }
}