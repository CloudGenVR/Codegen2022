using Frontend.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Frontend.Services;

public interface IMinimalService
{
    Task<Photo[]> SearchImagesAsync(string search);
    Task<byte[]> DownloadImageAsync(Guid photoId);
    Task<Photo> UploadPhoto(string description, IBrowserFile imageForm);
}
