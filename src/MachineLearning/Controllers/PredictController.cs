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
    public ActionResult<SentimentDataResult> Post([FromBody] SentimentData input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        SentimentPrediction prediction = _predictionEnginePool.Predict(modelName: "SentimentAnalysisModel", example: input);

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