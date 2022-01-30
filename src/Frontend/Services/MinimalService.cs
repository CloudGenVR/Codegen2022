using System.Net.Http.Json;
using System.Text.Json;
using Frontend.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Frontend.Services;

public class MinimalService : IMinimalService
{
    private readonly HttpClient client;

    public MinimalService(HttpClient client)
    {
        this.client = client;
    }

    public Task<Photo[]> SearchImagesAsync(string search) =>
        client.GetFromJsonAsync<Photo[]>($"photos?q={search}")!;

    public Task<byte[]> DownloadImageAsync(Guid photoId) =>
        client.GetByteArrayAsync($"photos/{photoId}/image")!;

    public async Task<Photo> UploadPhoto(string description, IBrowserFile imageForm)
    {
        await using var ms = new MemoryStream();
        await imageForm.OpenReadStream().CopyToAsync(ms);
        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(ms.GetBuffer()), "\"uploadFile\"", imageForm.Name }
        };

        var responseMessage = await client.PostAsync($"photos?description={description}", content);
        responseMessage.EnsureSuccessStatusCode();
        var messageText = await responseMessage.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Photo>(messageText)!;
    }
}
