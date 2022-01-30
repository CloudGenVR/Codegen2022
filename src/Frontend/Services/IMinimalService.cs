using Frontend.Models;

namespace Frontend.Services;

public interface IMinimalService
{
    Task<Photo[]> SearchImagesAsync();
}
