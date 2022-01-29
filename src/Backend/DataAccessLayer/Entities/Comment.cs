namespace PhotoGallery.DataAccessLayer.Entities;

public class Comment
{
    public Guid Id { get; set; }

    public Guid PhotoId { get; set; }

    public DateTime Date { get; set; }

    public string Text { get; set; } = null!;

    public virtual Photo Photo { get; set; } = null!;
}