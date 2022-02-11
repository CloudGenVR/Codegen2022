using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MimeMapping;
using PhotoGallery.DataAccessLayer;
using PhotoGallery.DataAccessLayer.Entities;
using PhotoGallery.Filters;
using PhotoGallery.Models;
using PhotoGallery.Services;
using Refit;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureAppConfiguration(app =>
{
    app.AddJsonFile("appsettings.local.json", optional: true);
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

builder.Services.AddSqlServer<PhotoGalleryDbContext>(builder.Configuration.GetConnectionString("SqlConnection"));
builder.Services.AddScoped<AzureStorageService>();

builder.Services.AddRefitClient<ISentimentApi>()
.ConfigureHttpClient(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("AppSettings:SentimentServiceUrl"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.OperationFilter<FormFileOperationFilter>());
builder.Services.AddFluentValidationRulesToSwagger();

builder.Services.AddCors(o => o.AddDefaultPolicy(builder =>
{
    builder
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders(HeaderNames.ContentDisposition);
}));

var app = builder.Build();
app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = string.Empty;
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PhotoGallery API v1");
});

app.UseCors();

app.MapGet("/api/photos", async (PhotoGalleryDbContext db, [FromQuery(Name = "q")] string? searchText) =>
{
    var query = db.Photos.AsNoTracking().OrderBy(p => p.Name).AsQueryable();

    if (!string.IsNullOrWhiteSpace(searchText))
    {
        query = query.Where(p => p.Name.Contains(searchText) || p.Description!.Contains(searchText));
    }

    var photos = await query.ToListAsync();
    return photos;
})
.Produces(StatusCodes.Status200OK, typeof(IEnumerable<Photo>))
.WithName("GetPhotos");

app.MapGet("/api/photos/{id:guid}", async (Guid id, PhotoGalleryDbContext db) =>
{
    var photo = await db.Photos.FindAsync(id);
    if (photo is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(photo);
})
.Produces(StatusCodes.Status200OK, typeof(Photo))
.Produces(StatusCodes.Status404NotFound)
.WithName("GetPhoto");

app.MapGet("/api/photos/{id:guid}/image", async (Guid id, AzureStorageService azureStorageService, PhotoGalleryDbContext db) =>
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
.WithName("GetPhotoStream");

app.MapGet("/api/photos/{id:guid}/comments", async (Guid id, PhotoGalleryDbContext db) =>
{
    var photoExists = await db.Photos.AnyAsync(p => p.Id == id);
    if (!photoExists)
    {
        return Results.NotFound();
    }

    var comments = await db.Comments.AsNoTracking().Where(c => c.PhotoId == id).OrderBy(c => c.Date).ToListAsync();
    return Results.Ok(comments);
})
.Produces(StatusCodes.Status200OK, typeof(IEnumerable<Comment>))
.Produces(StatusCodes.Status404NotFound)
.WithName("GetPhotoComments");

app.MapGet("/api/photos/{photoId:guid}/comments/{commentId:guid}", async (Guid photoId, Guid commentId, PhotoGalleryDbContext db) =>
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

app.MapPost("/api/photos", async (FormFileContent file, string? description, AzureStorageService storageService, PhotoGalleryDbContext db) =>
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
.Accepts<FormFileContent>("multipart/form-data")
.Produces(StatusCodes.Status201Created, typeof(Photo))
.Produces(StatusCodes.Status400BadRequest)
.WithName("UploadPhoto");

app.MapPost("/api/photos/{id:guid}/comments", async (Guid id, NewComment comment, PhotoGalleryDbContext db, IValidator<NewComment> validator,
    ISentimentApi sentimentApi) =>
{
    var validationResult = validator.Validate(comment);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
            .ToDictionary(k => k.Key, v => v.Select(e => e.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }

    var photoExists = await db.Photos.AnyAsync(p => p.Id == id);
    if (!photoExists)
    {
        return Results.NotFound();
    }

    using var sentimentResponse = await sentimentApi.GetPredictionAsync(new SentimentDataRequest(comment.Text));
    await sentimentResponse.EnsureSuccessStatusCodeAsync();

    var dbComment = new Comment
    {
        PhotoId = id,
        Date = DateTime.UtcNow,
        Text = comment.Text,
        SentimentScore = sentimentResponse.Content!.Probability
    };

    db.Comments.Add(dbComment);
    await db.SaveChangesAsync();

    return Results.CreatedAtRoute("GetPhotoComment", new { PhotoId = id, CommentId = dbComment.Id }, dbComment);
})
.Produces(StatusCodes.Status201Created, typeof(Comment))
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem()
.WithName("AddPhotoComment");

app.MapDelete("/api/photos/{id:guid}", async (Guid id, AzureStorageService storageService, PhotoGalleryDbContext db) =>
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
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.WithName("DeletePhoto");

app.MapDelete("/api/photos/{photoId:guid}/comments/{commentId:guid}", async (Guid photoId, Guid commentId, PhotoGalleryDbContext db) =>
{
    var comment = await db.Comments.FirstOrDefaultAsync(c => c.PhotoId == photoId && c.Id == commentId);
    if (comment is null)
    {
        return Results.NotFound();
    }

    db.Comments.Remove(comment);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.WithName("DeleteComment");

app.Run();