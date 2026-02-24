using PPQ.Singleton;
using System.Diagnostics;

namespace PPQ.Views.UserShell;

public partial class UserMenuPage : ContentPage
{
    private readonly MultiplayerService _multiplayer;
    private CancellationTokenSource _searchCts;
    private bool _navigated;

    public UserMenuPage(MultiplayerService multiplayer)
    {
        InitializeComponent();
        _multiplayer = GlobalData.Instance.multiplayer;
    }

    private async void GoToQuizMenu(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("QuizMenu");
    }

    private async void GoToAccount(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Views.AccountPage());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _navigated = false;

        multiplayerButton.IsEnabled = true;

        if (_multiplayer != null)
        {
            _multiplayer.WaitingForOpponent -= OnWaitingForOpponent;
            _multiplayer.MatchFound -= OnMatchFound;
        }
    }

    private async void OnMultiplayerClicked(object sender, EventArgs e)
    {
        multiplayerButton.IsEnabled = false;
        _navigated = false;

        if (!_multiplayer.IsConnected)
            await _multiplayer.ConnectAsync();

        _multiplayer.WaitingForOpponent -= OnWaitingForOpponent;
        _multiplayer.MatchFound -= OnMatchFound;
        _multiplayer.WaitingForOpponent += OnWaitingForOpponent;
        _multiplayer.MatchFound += OnMatchFound;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        _ = StartSearchTimeout();

        await _multiplayer.FindMatch(
            GlobalData.Instance.user.email,
            GlobalData.Instance.user.preferences["language"],
            GlobalData.Instance.user.username
        );
    }

    private void OnWaitingForOpponent()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert(
                (string)Application.Current.Resources["ButtonLabelMultiplayer"],
                (string)Application.Current.Resources["LabelWaintgRival"],
                (string)Application.Current.Resources["DAAcept"]
            );
        });
    }

    private void OnMatchFound(string roomId, string category, string topic)
    {
        _navigated = true;
        _searchCts?.Cancel();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                string route = $"MultiplayerQuizPage?RoomId={roomId}&Category={Uri.EscapeDataString(category)}&Topic={Uri.EscapeDataString(topic)}";
                Debug.WriteLine($"[NAVEGACION] Intentando ir a: {route}");
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR CRÍTICO] Fallo al navegar: {ex.Message}");
                await Shell.Current.GoToAsync("MultiplayerQuizPage");
            }
        });
    }

    private async Task StartSearchTimeout()
    {
        try
        {
            await Task.Delay(15000, _searchCts.Token);

            if (_navigated) return;

            if (_multiplayer.IsConnected)
                await _multiplayer.CancelSearch();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Multiplayer", "No rival found", "OK");
                multiplayerButton.IsEnabled = true;
            });
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("Busqueda cancelada porque se encontró partida.");
        }
    }
}