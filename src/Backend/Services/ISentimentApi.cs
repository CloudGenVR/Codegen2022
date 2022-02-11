using Refit;

namespace PhotoGallery.Services;

public interface ISentimentApi
{
    [Post("/predict")]
    Task<ApiResponse<SentimentPrediction>> GetPredictionAsync(SentimentDataRequest sentimentData);
}

public record SentimentDataRequest(string SentimentText);

public record SentimentPrediction(bool Prediction, float Probability, float Score);