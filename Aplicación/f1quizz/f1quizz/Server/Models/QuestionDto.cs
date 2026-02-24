namespace Server.Models
{
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
        public string? Winner { get; set; }

        public List<PlayerResultDto> Players { get; set; } = new();
    }

    public class PlayerResultDto
    {
        public string Username { get; set; }
        public int TotalScore { get; set; }
        public int TotalTime { get; set; }
    }
}