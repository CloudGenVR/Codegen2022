using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
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

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddSqlServer<PhotoGalleryDbContext>(builder.Configuration.GetConnectionString("SqlConnection"));
builder.Services.AddScoped<AzureStorageService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.OperationFilter<FormFileOperationFilter>());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/photos", async (PhotoGalleryDbContext db, [FromQuery(Name = "q")] string? searchText) =>
{
    var query = db.Photos.OrderBy(p => p.Name).AsQueryable();

    if (!string.IsNullOrWhiteSpace(searchText))
    {
        query = query.Where(p => p.Description!.Contains(searchText));
    }

    var photos = await query.ToListAsync();
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

app.MapGet("/photos/{id:guid}/comments", async (Guid id, PhotoGalleryDbContext db) =>
{
    var photoExists = await db.Photos.AnyAsync(p => p.Id == id);
    if (!photoExists)
    {
        return Results.NotFound();
    }

    var comments = await db.Comments.Where(c => c.PhotoId == id).OrderBy(c => c.Date).ToListAsync();
    return Results.Ok(comments);
})
.Produces(StatusCodes.Status200OK, typeof(IEnumerable<Comment>))
.Produces(StatusCodes.Status404NotFound)
.WithName("GetPhotoComments");

app.MapGet("/photos/{photoId:guid}/comments/{commentId:guid}", async (Guid photoId, Guid commentId, PhotoGalleryDbContext db) =>
{
    var comment = await db.Comments.FirstOrDefaultAsync(c => c.PhotoId == photoId && c.Id == commentId);
    if (comment is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(comment);
})
.Produces(StatusCodes.Status200OK, typeof(Comment))
.Produces(StatusCodes.Status404NotFound)
.WithName("GetPhotoComment");

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
.Accepts<FormFileContent>("multipart/form-data")
.Produces(StatusCodes.Status201Created, typeof(Photo))
.Produces(StatusCodes.Status400BadRequest);

app.MapPut("/photos/{id:guid}/comments", async (Guid id, NewComment comment, PhotoGalleryDbContext db) =>
{
    var photoExists = await db.Photos.AnyAsync(p => p.Id == id);
    if (!photoExists)
    {
        return Results.NotFound();
    }

    var dbComment = new Comment
    {
        PhotoId = id,
        Date = DateTime.UtcNow,
        Text = comment.Text,
        SentimentScore = Random.Shared.NextDouble()
    };

    db.Comments.Add(dbComment);
    await db.SaveChangesAsync();

    return Results.CreatedAtRoute("GetPhotoComment", new { PhotoId = id, CommentId = dbComment.Id }, dbComment);
})
.Produces(StatusCodes.Status201Created, typeof(Comment))
.Produces(StatusCodes.Status404NotFound)
.WithName("AddPhotoComment");

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