using PPQ.Singleton;

namespace PPQ.Views.UserShell;

public partial class MultiplayerQuizSummaryPage : ContentPage
{
    public MultiplayerQuizSummaryPage(List<QuizQuestionResult> results, int totalPoints, TimeSpan totalTime, MatchResultDto matchResult)
    {
        InitializeComponent();

        ResultsList.ItemsSource = results;
        TotalPointsLabel.Text = totalPoints.ToString();
        TotalTimeLabel.Text = $"{totalTime.Minutes:D2}:{totalTime.Seconds:D2}";

        Result(matchResult);
    }

    private void Result(MatchResultDto matchResult)
    {
        string currentUsername = GlobalData.Instance.user.username;

        if (matchResult.IsTie)
        {
            result.SetDynamicResource(Label.TextProperty, "LabelDraw");
        }
        else if (matchResult.Winner == currentUsername)
        {
            result.SetDynamicResource(Label.TextProperty, "LabelWinner");
            result.TextColor = Colors.Green;
        }
        else
        {
            result.SetDynamicResource(Label.TextProperty, "LabelLoser");
            result.TextColor = Colors.Red;
        }
    }

    private async void GoToQuizMenu(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        await Shell.Current.GoToAsync("///UserMenuPage");
    }
}