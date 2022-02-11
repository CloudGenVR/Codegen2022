using MachineLearning.DataModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;

namespace MachineLearning.Controllers;
[Route("api/[controller]")]
[ApiController]
public class PredictController : ControllerBase
{
    private readonly PredictionEnginePool<SentimentData, SentimentPrediction> _predictionEnginePool;

    public PredictController(PredictionEnginePool<SentimentData, SentimentPrediction> predictionEnginePool)
    {
        _predictionEnginePool = predictionEnginePool;
    }

    [HttpPost]
    public ActionResult<SentimentDataResult> Post([FromBody] SentimentDataRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        SentimentData input = new SentimentData
        {
            Sentiment = request.Sentiment,
            SentimentText = request.SentimentText
        };

        SentimentPrediction prediction = _predictionEnginePool.Predict(modelName: "MLModels", example: input);

        if (prediction == null)
            throw new ArgumentNullException();

        var result = new SentimentDataResult
        {
            Prediction = prediction.Prediction,
            Probability = prediction.Probability,
            Score = prediction.Score
        };
        return Ok(result);
    }
}