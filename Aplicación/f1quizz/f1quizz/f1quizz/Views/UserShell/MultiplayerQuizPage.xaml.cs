using Microsoft.Maui.Controls;
using PPQ.Singleton;
using System.Collections.Generic;
using System.Diagnostics;

namespace PPQ.Views.UserShell;

[QueryProperty(nameof(RoomId), "RoomId")]
[QueryProperty(nameof(Category), "Category")]
[QueryProperty(nameof(Topic), "Topic")]
[QueryProperty(nameof(Language), "Language")]
public partial class MultiplayerQuizPage : ContentPage
{
    public string RoomId { get; set; }
    public string Category { get; set; }
    public string Topic { get; set; }
    public string Language { get; set; }

    private MultiplayerService MultiplayerService => GlobalData.Instance.multiplayer;

    private bool _initialized = false;
    private bool _isCountdownFinished = false;
    private QuestionDto _currentQuestion;
    private int _questionTimer = 15;
    private CancellationTokenSource _timerCTS;

    private int _quizPoints = 0;
    private List<QuizQuestionResult> _results = new();
    private Stopwatch _quizStopwatch;

    private bool _isMatchFinished = false;

    private QuestionDto _pendingFirstQuestion;

    public MultiplayerQuizPage()
    {
        InitializeComponent();

        quizImage.Source = "trafficlight0.png";
        SetButtonsEnabled(false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_initialized) return;
        _initialized = true;

        MultiplayerService.QuestionReceived += OnQuestionReceived;
        MultiplayerService.MatchFinished += OnMatchFinished;
        MultiplayerService.OpponentDisconnected += OnOpponentDisconnected;

        await StartCountdown();

        _isCountdownFinished = true;
        _quizStopwatch = Stopwatch.StartNew();

        if (MultiplayerService.IsConnected && !string.IsNullOrEmpty(RoomId))
            await MultiplayerService.JoinRoom(RoomId);

        if (_pendingFirstQuestion != null)
        {
            ForceUpdateUI(_pendingFirstQuestion);
            _pendingFirstQuestion = null;
        }
    }

    private async Task StartCountdown()
    {
        int frame = 1;
        for (int i = 5; i > 0; i--)
        {
            quizImage.Source = $"trafficlight{frame}.png";
            frame++;

            countdownLabel.Text = (string)Application.Current.Resources["LabelStarting"] + ": " + i;
            await Task.Delay(1000);
        }

        quizImage.Source = "trafficlight0.png";
        countdownLabel.Text = "0s";
        await Task.Delay(500);
    }

    private void OnQuestionReceived(QuestionDto question)
    {
        if (!_isCountdownFinished)
        {
            _pendingFirstQuestion = question;
            return;
        }

        ForceUpdateUI(question);
    }

    private void ForceUpdateUI(QuestionDto question)
    {
        if (question == null) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _currentQuestion = question;
            _questionTimer = 15;
            titleLabel.Text = $"Pregunta {question.QuestionNumber}";
            questionLabel.Text = question.Question;

            answerA.Text = question.Options.ElementAtOrDefault(0) ?? "";
            answerB.Text = question.Options.ElementAtOrDefault(1) ?? "";
            answerC.Text = question.Options.ElementAtOrDefault(2) ?? "";
            answerD.Text = question.Options.ElementAtOrDefault(3) ?? "";

            if (!string.IsNullOrEmpty(question.Image)) quizImage.Source = question.Image;

            MoveCarToColumn(question.QuestionNumber - 1);
            ResetButtonsStyle();
            SetButtonsEnabled(true);
            StartTimer();
        });
    }

    private void StartTimer()
    {
        _timerCTS?.Cancel();
        _timerCTS = new CancellationTokenSource();
        var token = _timerCTS.Token;

        Task.Run(async () =>
        {
            try
            {
                while (_questionTimer > 0 && !token.IsCancellationRequested)
                {
                    MainThread.BeginInvokeOnMainThread(() => countdownLabel.Text = $"{_questionTimer}s");
                    await Task.Delay(1000, token);
                    _questionTimer--;
                }
                if (!token.IsCancellationRequested && _questionTimer <= 0)
                    await MainThread.InvokeOnMainThreadAsync(HandleTimeExpired);
            }
            catch { }
        }, token);
    }

    private async void OnOptionClicked(object sender, EventArgs e)
    {
        if (_currentQuestion == null) return;
        var button = (Button)sender;
        string selectedAnswer = button.Text;
        int timeSnapshot = _questionTimer;

        _timerCTS?.Cancel();
        SetButtonsEnabled(false);

        bool isCorrect = selectedAnswer.Equals(_currentQuestion.Answer, StringComparison.Ordinal);

        if (isCorrect)
        {
            button.BackgroundColor = (Color)Application.Current.Resources["ColorCorrectAns"];
            int points = CalculatePoints(isCorrect, timeSnapshot);
            _quizPoints += points;
            ApplyCircuitColor(isCorrect, timeSnapshot);
            _results.Add(new QuizQuestionResult { QuestionNumber = _currentQuestion.QuestionNumber, Correct = "Correcto", Points = points, TimeUsed = 15 - timeSnapshot });
        }
        else
        {
            button.BackgroundColor = (Color)Application.Current.Resources["ColorWrongAns"];
            MarkCorrectAnswerUI(_currentQuestion.Answer);
            _quizPoints -= 50;
            ApplyCircuitColor(false, timeSnapshot);
            _results.Add(new QuizQuestionResult { QuestionNumber = _currentQuestion.QuestionNumber, Correct = "Incorrecto", Points = -50, TimeUsed = 15 - timeSnapshot });
        }

        await MultiplayerService.SubmitAnswer(RoomId, _currentQuestion.QuestionNumber, selectedAnswer, timeSnapshot);
    }

    private async Task HandleTimeExpired()
    {
        SetButtonsEnabled(false);
        ApplyCircuitColor(false, 0);
        MarkCorrectAnswerUI(_currentQuestion.Answer);
        _quizPoints -= 50;
        _results.Add(new QuizQuestionResult { QuestionNumber = _currentQuestion.QuestionNumber, Correct = "Incorrecto", Points = -50, TimeUsed = 15 });
        await MultiplayerService.SubmitAnswer(RoomId, _currentQuestion.QuestionNumber, "TIMEOUT", 0);
    }

    private void OnMatchFinished(MatchResultDto result)
    {
        _isMatchFinished = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _timerCTS?.Cancel();
            _quizStopwatch?.Stop();
            var summaryPage = new MultiplayerQuizSummaryPage(_results, _quizPoints, _quizStopwatch.Elapsed, result);
            await Navigation.PushModalAsync(summaryPage);
        });
    }

    private void SetButtonsEnabled(bool enabled) => answerA.IsEnabled = answerB.IsEnabled = answerC.IsEnabled = answerD.IsEnabled = enabled;

    private void ResetButtonsStyle()
    {
        var color = (Color)Application.Current.Resources["Principal"];
        answerA.BackgroundColor = answerB.BackgroundColor = answerC.BackgroundColor = answerD.BackgroundColor = color;
    }

    private void MarkCorrectAnswerUI(string correct)
    {
        if (answerA.Text == correct) answerA.BackgroundColor = (Color)Application.Current.Resources["ColorCorrectAns"];
        else if (answerB.Text == correct) answerB.BackgroundColor = (Color)Application.Current.Resources["ColorCorrectAns"];
        else if (answerC.Text == correct) answerC.BackgroundColor = (Color)Application.Current.Resources["ColorCorrectAns"];
        else if (answerD.Text == correct) answerD.BackgroundColor = (Color)Application.Current.Resources["ColorCorrectAns"];
    }

    private void MoveCarToColumn(int index) => Grid.SetColumn(carImage, index);

    private int CalculatePoints(bool correct, int remainingSeconds)
    {
        if (!correct || remainingSeconds <= 0) return -50;
        if (remainingSeconds > 10) return 150;
        if (remainingSeconds > 5) return 100;
        return 50;
    }

    private Label GetCircuitBox(int index) => index switch
    {
        0 => box1,
        1 => box2,
        2 => box3,
        3 => box4,
        4 => box5,
        5 => box6,
        6 => box7,
        7 => box8,
        8 => box9,
        9 => box10,
        10 => box11,
        11 => box12,
        12 => box13,
        13 => box14,
        14 => box15,
        _ => null
    };

    private void ApplyCircuitColor(bool isCorrect, int remainingTime)
    {
        var box = GetCircuitBox(_currentQuestion.QuestionNumber - 1);
        if (box == null) return;
        if (!isCorrect) { box.BackgroundColor = (Color)Application.Current.Resources["ColorWrongAns"]; return; }
        int timeUsed = 15 - remainingTime;
        if (timeUsed <= 5) box.BackgroundColor = (Color)Application.Current.Resources["ColorPurpleSector"];
        else if (timeUsed <= 10) box.BackgroundColor = (Color)Application.Current.Resources["ColorCorrectAns"];
        else box.BackgroundColor = (Color)Application.Current.Resources["ColorYellowSector"];
    }

    private void OnOpponentDisconnected()
    {
        _isMatchFinished = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _timerCTS?.Cancel();
            _quizStopwatch?.Stop();
            await DisplayAlert((string)Application.Current.Resources["LabelWinner"], (string)Application.Current.Resources["LabelRivalDisconnected"], "OK");
            await Shell.Current.GoToAsync("///UserMenuPage");
        });
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _timerCTS?.Cancel();
        MultiplayerService.QuestionReceived -= OnQuestionReceived;
        MultiplayerService.MatchFinished -= OnMatchFinished;
        MultiplayerService.OpponentDisconnected -= OnOpponentDisconnected;
        if (!_isMatchFinished) await MultiplayerService.AbandonMatch();
        if (MultiplayerService.IsConnected) await MultiplayerService.DisconnectAsync();
    }
}