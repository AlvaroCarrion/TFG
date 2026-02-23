using Microsoft.Maui.Controls;
using PPQ.Models;
using PPQ.Singleton;
using System.Diagnostics;

namespace PPQ.Views.UserShell;

public partial class QuizPage : ContentPage
{
    private readonly string _collection;
    private readonly string _topic;
    private readonly string _category;
    private readonly string _language;
    private List<QuestionsModel> _questions;

    private int _currentIndex = 0;
    private int questionTimer = 15;

    private int _correctAnswers = 0;
    private int _quizPoints = 0;
    private int _wrongAnswers = 0;

    private List<QuizQuestionResult> _results = new();
    private Stopwatch _quizStopwatch;

    private CancellationTokenSource _timerCTS;

    public QuizPage(string tittle, string collection, string category, string topic, string language)
	{
		InitializeComponent();

        titleLabel.Text = tittle;
        _collection = collection;
        _category = category;
        _topic = topic;
        _language = language;

        quizImage.Source = "trafficlight0.png";

        SetButtonsEnabled(false);
        Loaded += QuizPage_Loaded;
    }

    // Método para cargar la página y las preguntas del cuestionario.
    private async void QuizPage_Loaded(object sender, EventArgs e)
    {
        bool loaded = await LoadQuestions();
        if (!loaded)
            return;

        // Mostrar un mensaje antes de comenzar el cuestionario. Si el usuario cancela, regresar a la página anterior. Si confirma, cargar las preguntas y comenzar el cuestionario.
        bool start = await GlobalData.Instance.messages.ShowConfirm(
            (string)Application.Current.Resources["DAStartQuiz"],
            (string)Application.Current.Resources["DAStartQuizText"]
        );

        if (!start)
        {
            await Navigation.PopAsync();
            return;
        }

        _correctAnswers = 0;
        _currentIndex = 0;

        await LoadQuestions();
        await StartCountdown();
        SetButtonsEnabled(true);

        _results.Clear();
        _quizStopwatch = Stopwatch.StartNew();

        ShowQuestion();
    }

    // Método que genera una cuenta atrás antes de comenzar el cuestionario.
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
    }

    // Método para cargar y preparar las preguntas del cuestionario.
    private async Task<bool> LoadQuestions()
    {
        _questions = await GlobalData.Instance.db.GetQuizQuestionsAsync(
            _collection, _category, _topic, _language);

        if (_questions == null || _questions.Count == 0)
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAError"],
                (string)Application.Current.Resources["DAQuestionsNotFound"]
            );

            await Navigation.PopAsync();
            return false;
        }

        return true;
    }

    // Método que muestra las preguntas.
    private void ShowQuestion()
    {
        if (_questions == null || _questions.Count == 0 || _currentIndex >= _questions.Count)
        {
            EndQuiz();
            return;
        }

        SetButtonsEnabled(true);

        var q = _questions[_currentIndex];

        // Obtener idioma dinámico
        LanguageContent lang = null;

        if (q.Language.ContainsKey(_language))
            lang = q.Language[_language];
        else
            lang = q.Language.Values.First();

        question.Text = lang.Question ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(q.Image))
            quizImage.Source = q.Image;

        var options = lang.Options ?? new List<string>();
        while (options.Count < 4) options.Add("");

        var shuffled = options.OrderBy(x => Guid.NewGuid()).ToList();

        answerA.Text = shuffled[0];
        answerB.Text = shuffled[1];
        answerC.Text = shuffled[2];
        answerD.Text = shuffled[3];

        ResetButtonsStyle();
        questionTimer = 15;
        MoveCarToColumn(_currentIndex);
        StartTimer();
    }

    // Método que muestra el temporizador de la pregunta.
    private void StartTimer()
    {
        // Cancelar temporizador anterior.
        _timerCTS?.Cancel();
        _timerCTS = new CancellationTokenSource();
        var ct = _timerCTS.Token;

        // Reiniciar contador.
        questionTimer = 15;
        countdownLabel.Text = "15s";

        // Ejecutar el bucle en background.
        _ = Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested && questionTimer > 0)
                {
                    await Task.Delay(1000, ct); // Espera 1s o cancela.
                    if (ct.IsCancellationRequested) break;

                    questionTimer--;

                    // Actualizar UI en hilo principal.
                    await MainThread.InvokeOnMainThreadAsync(() =>{countdownLabel.Text = $"{questionTimer}s";});
                }

                if (!ct.IsCancellationRequested && questionTimer == 0)
                {
                    // Tiempo expirado: invocar en hilo UI.
                    await MainThread.InvokeOnMainThreadAsync(() =>{HandleTimeExpired();});
                }
            }
            catch (TaskCanceledException)
            {
                // temporizador cancelado, no hacer nada.
            }
            catch (Exception ex)
            {
                // Si algo falla, mostrarlo (opcional).
                await GlobalData.Instance.messages.ShowMessage((string)Application.Current.Resources["DAError"], ex.Message);
            }
        }, ct);
    }

    // Método que procesa la respuesta del usuario.
    private async void OnOptionClicked(object sender, EventArgs e)
    {
        SetButtonsEnabled(false);
        _timerCTS?.Cancel();

        var button = (Button)sender;
        var selected = button.Text;

        string answer;

        var q = _questions[_currentIndex];
        var lang = q.Language.ContainsKey(_language)
            ? q.Language[_language]
            : q.Language.Values.First();

        bool correct = selected == lang.Answer;

        if (correct)
        {
            _correctAnswers++;

            int points = CalculatePointsByTime(questionTimer);
            _quizPoints += points;

            answer = (string)Application.Current.Resources["DACorrect"];

            button.BackgroundColor = (Color)App.Current.Resources["ColorCorrectAns"];
        }
        else
        {
            _wrongAnswers++;
            _quizPoints -= 50;

            answer = (string)Application.Current.Resources["DAIncorrect"];

            button.BackgroundColor = (Color)App.Current.Resources["ColorWrongAns"];
            MarkCorrectAnswer(lang.Answer);
        }

        await Task.Delay(1000);

        if (!correct)
        {
            UpdateCircuitColor(_currentIndex, 0);
        }
        else
        {
            //  Si es correcta marcar según el tiempo restante
            UpdateCircuitColor(_currentIndex, questionTimer);
        }

        int timeUsed = 15 - questionTimer;

        _results.Add(new QuizQuestionResult
        {
            QuestionNumber = _currentIndex + 1,
            Correct = answer,
            Points = correct ? CalculatePointsByTime(questionTimer) : -50,
            TimeUsed = timeUsed
        });

        await ShowResultMessage(correct, lang.Fact);
    }

    // Método para bloquear o desbloquear los botones de respuesta.
    private void SetButtonsEnabled(bool enabled)
    {
        answerA.IsEnabled = enabled;
        answerB.IsEnabled = enabled;
        answerC.IsEnabled = enabled;
        answerD.IsEnabled = enabled;
    }

    // Mostrar el resultado de la respuesta.
    private async Task ShowResultMessage(bool correct, string fact)
    {
        string title = correct ? (string)Application.Current.Resources["DACorrect"] : (string)Application.Current.Resources["DAIncorrect"];
        string msg = fact;

        await DisplayAlert(title, msg, (string)Application.Current.Resources["DANextQuestion"]); 

        _currentIndex++;
        ShowQuestion();
    }

    // Método para marcar la respuesta correcta.
    private void MarkCorrectAnswer(string correct)
    {
        foreach (var btn in new[] { answerA, answerB, answerC, answerD })
        {
            if (btn.Text == correct)
            {
                btn.BackgroundColor = (Color)App.Current.Resources["ColorCorrectAns"];
            }
        }
    }

    // Método para reiniciar el estilo de los botones de respuesta.
    private void ResetButtonsStyle()
    {
        foreach (var btn in new[] { answerA, answerB, answerC, answerD })
        {
            btn.BackgroundColor = (Color)Application.Current.Resources["Principal"];
        }
    }

    // Muestra la respuesta correcta cuando se acaba el tiempo.
    private void HandleTimeExpired()
    {
        var q = _questions[_currentIndex];

        var lang = q.Language.ContainsKey(_language)
            ? q.Language[_language]
            : q.Language.Values.First();

        MarkCorrectAnswer(lang.Answer);

        UpdateCircuitColor(_currentIndex, 0);

        _wrongAnswers++;
        _quizPoints -= 50;

        _results.Add(new QuizQuestionResult
        {
            QuestionNumber = _currentIndex + 1,
            Correct = (string)Application.Current.Resources["DAIncorrect"],
            Points = -50,
            TimeUsed = 15
        });

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(2000);
            await ShowResultMessage(false, lang.Fact);
        });
    }

    // Muestra un mensaje y regresa a la pantalla anterior.
    private async void EndQuiz()
    {
        _quizStopwatch.Stop();

        // Si ha acertado todas las preguntas
        if (_correctAnswers == 15)
        {
            try
            {
                // Determinar campo a actualizar según la categoría
                string dictName = _category switch
                {
                    "circuits" => "completedCircuits",
                    "drivers" => "completedDrivers",
                    "teams" => "completedTeams",
                    _ => null
                };

                if (!string.IsNullOrEmpty(dictName))
                {
                    // Actualizar en memoria del usuario (asegurarse de que exista la key)
                    switch (dictName)
                    {
                        case "completedCircuits":
                            if (!GlobalData.Instance.user.completedCircuits.ContainsKey(_topic))
                                GlobalData.Instance.user.completedCircuits.Add(_topic, true);
                            else
                                GlobalData.Instance.user.completedCircuits[_topic] = true;
                            break;

                        case "completedDrivers":
                            if (!GlobalData.Instance.user.completedDrivers.ContainsKey(_topic))
                                GlobalData.Instance.user.completedDrivers.Add(_topic, true);
                            else
                                GlobalData.Instance.user.completedDrivers[_topic] = true;
                            break;

                        case "completedTeams":
                            if (!GlobalData.Instance.user.completedTeams.ContainsKey(_topic))
                                GlobalData.Instance.user.completedTeams.Add(_topic, true);
                            else
                                GlobalData.Instance.user.completedTeams[_topic] = true;
                            break;
                    }

                    // Llamar al método de BD.
                    bool updated = await GlobalData.Instance.db.UpdateCompletedItem(
                        GlobalData.Instance.user.email,
                        dictName,
                        _topic
                    );

                    if (updated)
                    {
                        await GlobalData.Instance.messages.ShowMessage(
                            (string)Application.Current.Resources["DACongrats"],
                            (string)Application.Current.Resources["DAAllCorrect"]
                        );
                    }
                    else
                    {
                        // Fallback: informar que no se pudo actualizar en BD.
                        await GlobalData.Instance.messages.ShowMessage(
                            (string)Application.Current.Resources["DAError"],
                            (string)Application.Current.Resources["DAUpdateFailed"]
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                await GlobalData.Instance.messages.ShowMessage(
                    (string)Application.Current.Resources["DAError"],
                    ex.Message
                );
            }
        }
        else
        {
            await GlobalData.Instance.messages.ShowMessage(
                (string)Application.Current.Resources["DAEnd"],
                (string)Application.Current.Resources["DAEndText"]
            );
        }

        await UpdateUserStatisticsAsync();

        await Navigation.PushModalAsync(new QuizSummaryPage(
            _results,
            _quizPoints,
            _quizStopwatch.Elapsed
        ));
    }

    // Calcular la puntuación basada en el tiempo restante.
    private int CalculatePointsByTime(int remainingSeconds)
    {
        if (remainingSeconds > 10)
            return 150;
        if (remainingSeconds > 5)
            return 100;
        if (remainingSeconds > 0)
            return 50;

        return -50;
    }

    // Actualiza las estadísticas del usuario en la base de datos.
    private async Task UpdateUserStatisticsAsync()
    {
        bool perfectQuiz = _correctAnswers == 15;

        await GlobalData.Instance.db.UpdateUserStatistics(
            GlobalData.Instance.user.email,
            _quizPoints,
            _correctAnswers,
            _wrongAnswers,
            perfectQuiz
        );
    }

    // Método para actualizar el color del cuadro de circuito según el tiempo restante.
    private void UpdateCircuitColor(int questionIndex, int time)
    {
        var box = GetCircuitBox(questionIndex);
        if (box == null) return;

        Color color;

        if (time <= 0)
        {
            color = (Color)Application.Current.Resources["ColorWrongAns"];
        }
        else if (time > 10)
        {
            color = (Color)Application.Current.Resources["ColorPurpleSector"];
        }
        else if (time > 5)
        {
            color = (Color)Application.Current.Resources["ColorCorrectAns"];
        }
        else
        {
            color = (Color)Application.Current.Resources["ColorYellowSector"];
        }

        box.BackgroundColor = color;
    }

    // Método para obtener la label del cuadro de circuito según el índice.
    private Label GetCircuitBox(int index)
    {
        return index switch
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
    }

    // Mueve la imagen del coche a la casilla correspondiente.
    private void MoveCarToColumn(int index)
    {
        if (index < 0 || index > 14)
            return;

        Grid.SetColumn(carImage, index);
    }
}

// Clase modelo para guardar información de cada pregunta respondida en el cuestionario.
public class QuizQuestionResult
{
    public int QuestionNumber { get; set; }
    public string Correct { get; set; }
    public int Points { get; set; }
    public int TimeUsed { get; set; }
}