using Microsoft.EntityFrameworkCore;
using MimeMapping;
using PhotoGallery.DataAccessLayer;
using PhotoGallery.DataAccessLayer.Entities;
using PhotoGallery.Filters;
using PhotoGallery.Models;
using PhotoGallery.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureAppConfiguration(app =>
{
    app.AddJsonFile("appsettings.local.json", optional: true);
});

builder.Services.AddSqlServer<PhotoGalleryDbContext>(builder.Configuration.GetConnectionString("SqlConnection"));
builder.Services.AddScoped<AzureStorageService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.OperationFilter<UploadOperationFilter>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/photos", async (PhotoGalleryDbContext db) =>
{
    var photos = await db.Photos.OrderBy(p => p.Name).ToListAsync();
    return photos;
})
.WithName("GetPhotos")
.Produces(StatusCodes.Status200OK, typeof(IEnumerable<Photo>));

app.MapGet("/photos/{id:guid}", async (Guid id, AzureStorageService azureStorageService, PhotoGalleryDbContext db) =>
{
    var photo = await db.Photos.FindAsync(id);
    if (photo is null)
    {
        return Results.NotFound();
    }

    var stream = await azureStorageService.ReadAsync(photo.Path);
    if (stream is null)
    {
        return Results.NotFound();
    }

    return Results.Stream(stream, MimeUtility.GetMimeMapping(photo.Name));
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithName("GetPhoto");

app.MapPost("photos", async (FormFileContent file, string? description, AzureStorageService storageService, PhotoGalleryDbContext db) =>
{
    using var stream = file.Content.OpenReadStream();

    var id = Guid.NewGuid();
    var newFileName = $"{id}{Path.GetExtension(file.Content.FileName)}".ToLowerInvariant();

    await storageService.SaveAsync(newFileName, stream);

    var photo = new Photo
    {
        Id = id,
        Name = file.Content.FileName,
        Path = newFileName,
        Size = file.Content.Length,
        Description = description,
        Date = DateTime.UtcNow
    };

    db.Photos.Add(photo);
    await db.SaveChangesAsync();

    return Results.CreatedAtRoute("GetPhoto", new { id }, photo);
})
.WithName("UploadPhoto")
.WithMetadata(UploadOperationFilter.UploadOperation)
.Produces(StatusCodes.Status201Created, typeof(Photo))
.Produces(StatusCodes.Status400BadRequest);

app.MapDelete("/photos/{id:guid}", async (Guid id, AzureStorageService storageService, PhotoGalleryDbContext db) =>
{
    var photo = await db.Photos.FindAsync(id);
    if (photo is null)
    {
        return Results.NotFound();
    }

    await storageService.DeleteAsync(photo.Path);

    db.Photos.Remove(photo);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeletePhoto")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.Run();