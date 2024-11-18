using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LanguageController : ControllerBase
{
    private readonly TextAnalyticsClient _textAnalyticsClient;

    public LanguageController(IConfiguration configuration)
    {
        var endpoint = new Uri(configuration["AzureLanguageService:Endpoint"]);
        var apiKey = configuration["AzureLanguageService:ApiKey"];
        _textAnalyticsClient = new TextAnalyticsClient(endpoint, new AzureKeyCredential(apiKey));
    }

    [HttpPost("detect-language")]
    public IActionResult DetectLanguage([FromBody] string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest("Text cannot be empty");

        try
        {
            DetectedLanguage detectedLanguage = _textAnalyticsClient.DetectLanguage(text);

            return Ok(new
            {
                Language = detectedLanguage.Name,
                ISOCode = detectedLanguage.Iso6391Name,
                ConfidenceScore = detectedLanguage.ConfidenceScore
            });
        }
        catch (RequestFailedException ex)
        {
            return StatusCode((int)ex.Status, ex.Message);
        }
    }
}
