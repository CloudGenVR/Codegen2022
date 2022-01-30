using Frontend.Models;

namespace Frontend.Services;

public class FakeMinimalService : IMinimalService
{
    private readonly HttpClient client;

    public FakeMinimalService(HttpClient client)
    {
        this.client = client;
    }

    public async Task<Photo[]> SearchImagesAsync()
    {
        return await Task.FromResult(
                new Photo[]
            {
                new Photo
                {
                    Id = Guid.NewGuid(),
                    Name = "First Image",
                    Description = "My Description",
                    Date = DateTime.UtcNow.AddDays(-1),
                    Path = "images/1.png",
                    Size = 1300,
                    Comments = new Comment[]
                    {
                        new Comment
                        {
                            Date = DateTime.UtcNow.AddMinutes(-10),
                            Text = "Wow, Awesome"
                        },
                        new Comment
                        {
                            Date = DateTime.UtcNow.AddMinutes(-5),
                            Text = "Super"
                        },
                        new Comment
                        {
                            Date = DateTime.UtcNow.AddMinutes(-5),
                            Text = "Bleah"
                        },
                    }
                },
                new Photo
                {
                    Id = Guid.NewGuid(),
                    Name = "Second Image",
                    Description = "My Description",
                    Date = DateTime.UtcNow.AddDays(-1),
                    Path = "images/2.png",
                    Size = 500,
                    Comments = new Comment[]
                    {
                        new Comment
                        {
                            Date = DateTime.UtcNow.AddMinutes(-10),
                            Text = "Wow, Awesome"
                        }
                    }
                },
            }
        );
    }

}
