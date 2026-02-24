using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Text.Json;

namespace PPQ.Singleton
{
    public class MultiplayerService
    {
        private HubConnection _connection;

        public event Action WaitingForOpponent;
        public event Action<string, string, string> MatchFound;
        public event Action<QuestionDto> QuestionReceived;
        public event Action<MatchResultDto> MatchFinished;
        public event Action OpponentDisconnected;

        public QuestionDto LastReceivedQuestion { get; private set; }

        public bool IsConnected => _connection != null && _connection.State == HubConnectionState.Connected;

        public async Task ConnectAsync()
        {
            if (_connection != null) return;

            _connection = new HubConnectionBuilder()
               .WithUrl("http://127.0.0.1:5000/matchhub")
               .WithAutomaticReconnect()
               .Build();

            RegisterHandlers();

            try
            {
                await _connection.StartAsync();
                Debug.WriteLine($"SignalR conectado. Id={_connection.ConnectionId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al conectar SignalR: {ex.Message}");
                _connection = null;
            }
        }

        private void RegisterHandlers()
        {
            _connection.On("WaitingForOpponent", () => {
                Debug.WriteLine("[SIGNALR] Evento: WaitingForOpponent");
                WaitingForOpponent?.Invoke();
            });

            _connection.On<JsonElement>("MatchFound", (data) =>
            {
                try
                {
                    string roomId = data.TryGetProperty("roomId", out var r) ? r.GetString() : data.GetProperty("RoomId").GetString();
                    string category = data.TryGetProperty("category", out var c) ? c.GetString() : data.GetProperty("Category").GetString();
                    string topic = data.TryGetProperty("topic", out var t) ? t.GetString() : data.GetProperty("Topic").GetString();

                    Debug.WriteLine($"[SIGNALR] Partida Encontrada: {roomId}");
                    MatchFound?.Invoke(roomId, category, topic);
                }
                catch (Exception ex) { Debug.WriteLine($"[ERROR] MatchFound Parse: {ex.Message}"); }
            });

            _connection.On<QuestionDto>("ReceiveQuestion", (question) =>
            {
                Debug.WriteLine($"[SERVICE] Guardando pregunta #{question.QuestionNumber} en el buzón");
                LastReceivedQuestion = question;
                QuestionReceived?.Invoke(question);
            });

            _connection.On<MatchResultDto>("MatchFinished", (result) => MatchFinished?.Invoke(result));
            _connection.On("OpponentDisconnected", () => OpponentDisconnected?.Invoke());
        }

        public async Task FindMatch(string email, string language, string username)
        {
            if (IsConnected) await _connection.InvokeAsync("FindMatch", email, language, username);
        }

        public async Task JoinRoom(string roomId)
        {
            if (IsConnected) await _connection.InvokeAsync("JoinRoom", roomId);
        }

        public async Task SubmitAnswer(string roomId, int questionNumber, string answer, int points)
        {
            if (IsConnected) await _connection.InvokeAsync("SubmitAnswer", roomId, questionNumber, answer, points);
        }

        public async Task CancelSearch()
        {
            if (IsConnected) await _connection.InvokeAsync("CancelSearch");
        }

        public void ClearLastQuestion()
        {
            LastReceivedQuestion = null;
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public async Task AbandonMatch()
        {
            if (IsConnected)
            {
                await _connection.InvokeAsync("AbandonMatch");
            }
        }
    }

    public class QuestionDto
    {
        public int QuestionNumber { get; set; }
        public string Question { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public string Image { get; set; }
        public string Topic { get; set; }
        public string Answer { get; set; }
        public string Fact { get; set; }
    }

    public class MatchResultDto
    {
        public bool IsTie { get; set; }
        public string Winner { get; set; }
        public List<PlayerResultDto> Players { get; set; } = new();
    }

    public class PlayerResultDto
    {
        public string Username { get; set; }
        public int TotalScore { get; set; }
        public int TotalTime { get; set; }
    }
}