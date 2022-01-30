namespace Frontend.Models;

public class Photo
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public string Path { get; set; } = null!;

    public string Name { get; set; } = null!;

    public long Size { get; set; }

    public string? Description { get; set; }

    public virtual Comment[]? Comments { get; set; }
}