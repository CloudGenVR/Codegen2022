using Frontend.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Frontend.Services;

public interface IMinimalService
{
    Task<Photo[]> SearchImagesAsync(string search);
    Task<byte[]> DownloadImageAsync(Guid photoId);
    Task<Photo> UploadPhotoAsync(string description, IBrowserFile imageForm);
    Task AddCommentAsync(Guid photoId, Comment comment);
    Task<Comment[]?> GetCommentAsync(Guid photoId);
}
