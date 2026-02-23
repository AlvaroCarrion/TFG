using static PPQ.Views.UserShell.QuizPage;

namespace PPQ.Views.UserShell;

public partial class QuizSummaryPage : ContentPage
{
    public QuizSummaryPage(List<QuizQuestionResult> results, int totalPoints, TimeSpan totalTime) {
        InitializeComponent();

        ResultsList.ItemsSource = results;
        TotalPointsLabel.Text = totalPoints.ToString();
        TotalTimeLabel.Text = $"{totalTime.Minutes:D2}:{totalTime.Seconds:D2}";
    }

    private async void GoToQuizMenu(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        await Shell.Current.GoToAsync("QuizMenu");
    }
}