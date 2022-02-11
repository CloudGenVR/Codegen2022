using MachineLearning.DataModels;
using Microsoft.Extensions.ML;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddPredictionEnginePool<SentimentData, SentimentPrediction>()
    .FromFile(modelName: "MLModels", filePath: "MLModels/MLModels.zip", watchForChanges: true);

//// Possiamo usare in alternativa anche il metodo uri
//   builder.Services.AddPredictionEnginePool<SentimentData, SentimentPrediction>()
//  .FromUri(
//      modelName: "SentimentAnalysisModel",
//      uri: "https://github.com/dotnet/samples/raw/main/machine-learning/models/sentimentanalysis/sentiment_model.zip",
//      period: TimeSpan.FromMinutes(1));

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
