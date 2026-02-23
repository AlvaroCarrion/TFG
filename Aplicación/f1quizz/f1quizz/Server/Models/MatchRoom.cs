namespace Server.Models
{
    public class MatchRoom
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public List<PlayerConnection> Players { get; } = new();

        public List<MongoQuestion> Questions { get; set; } = new();

        public string Category { get; set; }
        public string Topic { get; set; }

        public int CurrentQuestionIndex { get; private set; } = 0;
        public bool IsFinished { get; private set; } = false;

        public MatchRoom(PlayerConnection p1, PlayerConnection p2)
        {
            Players.Add(p1);
            Players.Add(p2);
        }

        public void RegisterAnswer(
            string connectionId,
            int questionIndex,
            string answer,
            int remainingSeconds)
        {
            if (questionIndex != CurrentQuestionIndex)
                return;

            var player = Players.First(p => p.ConnectionId == connectionId);

            if (player.HasAnsweredCurrent)
                return;

            var question = Questions[questionIndex];

            if (!question.Language.TryGetValue(player.Language, out var lang))
                lang = question.Language["sp"];

            bool correct = answer == lang.Answer;

            player.RegisterAnswer(correct, remainingSeconds);
        }

        public bool AllPlayersAnswered()
            => Players.All(p => p.HasAnsweredCurrent);

        public void AdvanceQuestion()
        {
            foreach (var p in Players)
                p.ResetForNextQuestion();

            CurrentQuestionIndex++;

            if (CurrentQuestionIndex >= Questions.Count)
                IsFinished = true;
        }

        public MongoQuestion GetCurrentQuestion()
        {
            if (CurrentQuestionIndex >= Questions.Count)
                return null;

            return Questions[CurrentQuestionIndex];
        }

        public bool IsLastQuestion()
            => CurrentQuestionIndex >= Questions.Count;

        public Dictionary<string, object> GetResults()
        {
            var dict = new Dictionary<string, object>();

            foreach (var p in Players)
            {
                dict[p.ConnectionId] = new
                {
                    p.Username,
                    p.TotalScore
                };
            }
            return dict;
        }
    }
}