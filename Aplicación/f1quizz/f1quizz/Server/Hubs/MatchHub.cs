using Microsoft.AspNetCore.SignalR;
using Server.Models;
using Server.Services;
using System.Diagnostics;

namespace Server.Hubs
{
    public class MatchHub : Hub
    {
        private readonly MatchmakingService _matchmaking;

        public MatchHub(MatchmakingService matchmaking)
        {
            _matchmaking = matchmaking;
        }

        public async Task FindMatch(string userEmail, string language, string username)
        {
            try
            {
                Console.WriteLine($"[FindMatch] {username} ({userEmail}) lang={language}");

                var player = new PlayerConnection(
                    Context.ConnectionId,
                    username,
                    userEmail,
                    language
                );

                MatchRoom? room = _matchmaking.AddPlayer(player);

                if (room == null)
                {
                    await Clients.Caller.SendAsync("WaitingForOpponent");
                    return;
                }

                Console.WriteLine($"[MATCH] Sala creada: {room.Id} con {room.Players.Count} jugadores");

                foreach (var p in room.Players)
                {
                    await Clients.Client(p.ConnectionId).SendAsync("MatchFound", new
                    {
                        roomId = room.Id,
                        category = room.Category,
                        topic = room.Topic
                    });
                }

                await Task.Delay(5000);
                await SendQuestion(room, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR FindMatch] {ex.Message}");
            }
        }

        public async Task JoinRoom(string roomId)
        {
            var room = _matchmaking.GetRoom(roomId);
            if (room != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                Debug.WriteLine($"[HUB] Cliente {Context.ConnectionId} unido a grupo {roomId}");

                var question = room.GetCurrentQuestion();
                var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

                if (question != null && player != null)
                {
                    if (!question.Language.TryGetValue(player.Language, out var lang))
                        lang = question.Language["sp"];

                    var dto = new QuestionDto
                    {
                        QuestionNumber = room.CurrentQuestionIndex + 1,
                        Question = lang.Question,
                        Options = lang.Options,
                        Topic = room.Topic,
                        Image = question.Image,
                        Answer = lang.Answer,
                        Fact = lang.Fact
                    };

                    await Clients.Caller.SendAsync("ReceiveQuestion", dto);
                }
            }
        }

        private async Task SendQuestion(MatchRoom room, int index)
        {
            if (index < 0 || index >= room.Questions.Count) return;

            var mongoQuestion = room.Questions[index];

            foreach (var player in room.Players)
            {
                if (!mongoQuestion.Language.TryGetValue(player.Language, out var lang))
                    lang = mongoQuestion.Language["sp"];

                var dto = new QuestionDto
                {
                    QuestionNumber = index + 1,
                    Question = lang.Question,
                    Options = lang.Options,
                    Image = mongoQuestion.Image,
                    Answer = lang.Answer,
                    Fact = lang.Fact
                };

                await Clients.Client(player.ConnectionId).SendAsync("ReceiveQuestion", dto);
            }
        }

        public async Task SubmitAnswer(string roomId, int questionNumber, string answer, int remainingSeconds)
        {
            var room = _matchmaking.GetRoom(roomId);
            if (room == null) return;

            room.RegisterAnswer(Context.ConnectionId, questionNumber - 1, answer, remainingSeconds);

            Console.WriteLine($"[DEBUG] Jugador {Context.ConnectionId} respondió Q:{questionNumber}. ¿Todos listos? {room.AllPlayersAnswered()}");

            if (room.AllPlayersAnswered())
            {
                if (room.CurrentQuestionIndex >= room.Questions.Count - 1)
                {
                    await FinalizeMatch(room);
                }
                else
                {
                    room.AdvanceQuestion();
                    Console.WriteLine($"[DEBUG] Avanzando a pregunta índice: {room.CurrentQuestionIndex}");

                    await SendQuestion(room, room.CurrentQuestionIndex);
                }
            }
        }

        private async Task FinalizeMatch(MatchRoom room)
        {
            var players = room.Players
                .OrderByDescending(p => p.TotalScore)
                .ThenBy(p => p.TotalTime)
                .ToList();

            bool isTie = players[0].TotalScore == players[1].TotalScore &&
                         players[0].TotalTime == players[1].TotalTime;

            var result = new MatchResultDto
            {
                IsTie = isTie,
                Winner = isTie ? null : players[0].Username,
                Players = players.Select(p => new PlayerResultDto
                {
                    Username = p.Username,
                    TotalScore = p.TotalScore,
                    TotalTime = p.TotalTime
                }).ToList()
            };

            Console.WriteLine($"\n[PARTIDA FINALIZADA]");
            Console.WriteLine($" Sala ID: {room.Id}");
            Console.WriteLine($" Ganador: {players[0].Username} ({players[0].TotalScore} pts)");
            Console.WriteLine($" Perdedor: {players[1].Username} ({players[1].TotalScore} pts)");

            await Clients.Group(room.Id).SendAsync("MatchFinished", result);
            _matchmaking.RemoveRoom(room.Id);
            Console.WriteLine($"[SALA DESTRUIDA] La sala {room.Id} ha sido eliminada de memoria.\n");
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"[SignalR] Connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public async Task AbandonMatch()
        {
            var room = _matchmaking.RemovePlayer(Context.ConnectionId);

            if (room != null)
            {
                var remaining = room.Players.FirstOrDefault(p => p.ConnectionId != Context.ConnectionId);
                if (remaining != null)
                {
                    Console.WriteLine($"\n[ABANDONO] Un jugador ha abandonado la sala {room.Id}.");
                    Console.WriteLine($"[VICTORIA AUTOMÁTICA] GANADOR: {remaining.Username}\n");

                    await Clients.Client(remaining.ConnectionId).SendAsync("OpponentDisconnected");
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"[CONEXIÓN PERDIDA] Cliente: {Context.ConnectionId}");

            var room = _matchmaking.RemovePlayer(Context.ConnectionId);
            _matchmaking.RemovePlayerFromWaiting(Context.ConnectionId);

            if (room != null)
            {
                Console.WriteLine($"[INFO] El jugador pertenecía a la sala activa: {room.Id}. Notificando al rival...");

                var remaining = room.Players.FirstOrDefault(p => p.ConnectionId != Context.ConnectionId);
                if (remaining != null)
                {
                    Console.WriteLine($"\n[DESCONEXIÓN] Se ha perdido la conexión con un jugador.");
                    Console.WriteLine($"[VICTORIA AUTOMÁTICA] GANADOR: {remaining.Username}\n");

                    await Clients.Client(remaining.ConnectionId).SendAsync("OpponentDisconnected");
                }

                _matchmaking.RemoveRoom(room.Id);
                Console.WriteLine($"[SALA DESTRUIDA] Sala {room.Id} eliminada por desconexión.");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public void CancelSearch()
        {
            _matchmaking.RemovePlayerFromWaiting(Context.ConnectionId);
        }
    }
}