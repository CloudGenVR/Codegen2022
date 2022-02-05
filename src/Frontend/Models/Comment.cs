namespace Frontend.Models;

public class Comment
{
    public Guid Id { get; set; }

    public Guid PhotoId { get; set; }

    public DateTime Date { get; set; }

    public string Text { get; set; } = null!;

    public float SentimentScore { get; set; }
}