using Azure.AI.TextAnalytics;
using Azure;
using LanguageServiceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

[ApiController]
[Route("api/[controller]")]
public class LanguageController : ControllerBase
{
    private readonly TextAnalyticsClient _textAnalyticsClient;
    private static readonly ConcurrentDictionary<string, RequestStatus> RequestStatuses = new();

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

        // Eski girişleri temizle
        foreach (var key in RequestStatuses.Keys)
        {
            if ((DateTime.UtcNow - RequestStatuses[key].RequestTime).TotalMinutes > 5)
            {
                RequestStatuses.TryRemove(key, out _);
            }
        }

        // Aynı istek için 30 saniye kontrolü
        if (RequestStatuses.ContainsKey(text))
        {
            var existingRequest = RequestStatuses[text];
            if ((DateTime.UtcNow - existingRequest.RequestTime).TotalSeconds < 30)
            {
                return Ok(new
                {
                    Message = "Please wait 30 seconds before sending the same request."
                });
            }
        }

        // Yeni isteği ekle
        var status = new RequestStatus
        {
            Text = text,
            IsProcessed = false,
            RequestTime = DateTime.UtcNow
        };
        if (!RequestStatuses.TryAdd(text, status))
        {
            return BadRequest("An identical request is already being processed.");
        }

        // İşleme başla
        Task.Run(() =>
        {
            try
            {
                DetectedLanguage detectedLanguage = _textAnalyticsClient.DetectLanguage(text);
                status.Language = detectedLanguage.Name;
                status.ISOCode = detectedLanguage.Iso6391Name;
                status.ConfidenceScore = detectedLanguage.ConfidenceScore;
                status.IsProcessed = true;
            }
            catch (Exception ex)
            {
                status.IsProcessed = true;
                status.Language = "Error";
                status.ISOCode = "N/A";
                status.ConfidenceScore = 0;
                Console.WriteLine($"Error processing request: {ex.Message}");
            }
        });

        return Ok(new
        {
            Message = "Request is being processed."
        });
    }

    [HttpGet("get-result")]
    public IActionResult GetResult([FromQuery] string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !RequestStatuses.ContainsKey(text))
            return NotFound("No request found for the provided text.");

        var status = RequestStatuses[text];
        if (!status.IsProcessed)
        {
            return Ok(new
            {
                Message = "Processing is still ongoing. Please try again later."
            });
        }

        return Ok(new
        {
            status.Language,
            status.ISOCode,
            status.ConfidenceScore
        });
    }
}
