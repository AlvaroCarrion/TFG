namespace Server.Models
{
    public class PlayerConnection
    {
        public string ConnectionId { get; }
        public string Username { get; }
        public string Email { get; }
        public string Language { get; }

        public int TotalScore { get; private set; } = 0;
        public int TotalTime { get; private set; } = 0;

        public bool HasAnsweredCurrent { get; private set; } = false;

        public PlayerConnection(string connectionId, string username, string email, string language)
        {
            ConnectionId = connectionId;
            Username = username;
            Email = email;
            Language = language;
        }

        public void RegisterAnswer(bool correct, int remainingSeconds)
        {
            HasAnsweredCurrent = true;

            TotalTime += (15 - remainingSeconds);

            TotalScore += CalculatePoints(correct, remainingSeconds);
        }

        public void ResetForNextQuestion()
        {
            HasAnsweredCurrent = false;
        }

        private int CalculatePoints(bool correct, int remainingSeconds)
        {
            if (!correct || remainingSeconds <= 0)
                return -50;

            if (remainingSeconds > 10)
                return 150;

            if (remainingSeconds > 5)
                return 100;

            return 50;
        }
    }
}