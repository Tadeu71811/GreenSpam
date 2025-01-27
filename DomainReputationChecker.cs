using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class DomainReputationChecker
{
    private readonly HttpClient _httpClient;

    public DomainReputationChecker(string apiKeyFilePath)
    {
        _httpClient = new HttpClient();
        var apiKey = ReadApiKeyFromFile(apiKeyFilePath);
        _httpClient.DefaultRequestHeaders.Add("x-apikey", apiKey);
    }

    private string ReadApiKeyFromFile(string filePath)
    {
        try
        {
            return System.IO.File.ReadAllText(filePath).Trim();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading API key from file: {ex.Message}");
        }
    }

    public async Task<DomainReputationResult> CheckDomainReputationAsync(string domainName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://www.virustotal.com/api/v3/domains/{domainName}");
            response.EnsureSuccessStatusCode();
            string apiResponse = await response.Content.ReadAsStringAsync();

            return ParseResponse(apiResponse, domainName);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"API Request Failed: {ex.Message}, Response Content: {await ex.GetResponseContentAsync()}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Request Failed: {ex.Message}");
            return null;
        }
    }

    private DomainReputationResult ParseResponse(string apiResponse, string domainName)
    {
        var responseObj = JsonConvert.DeserializeObject<VirusTotalApiResponse>(apiResponse);

        var lastAnalysisStats = responseObj.Data?.Attributes?.LastAnalysisStats;
        var isBlacklisted = lastAnalysisStats != null && lastAnalysisStats.Malicious > 0;
        var reputationScore = responseObj.Data?.Attributes?.Reputation ?? 0; // Default to 0 if null

        Console.WriteLine($"Domain: {domainName}, Reputation Score: {reputationScore}");

        return new DomainReputationResult
        {
            IsBlacklisted = isBlacklisted,
            ReputationScore = reputationScore
        };
    }
}

public class DomainReputationResult
{
    public bool IsBlacklisted { get; set; }
    public int ReputationScore { get; set; }
}

public class VirusTotalApiResponse
{
    public ApiResponseData Data { get; set; }

    public class ApiResponseData
    {
        public ApiResponseAttributes Attributes { get; set; }
    }

    public class ApiResponseAttributes
    {
        public LastAnalysisStats LastAnalysisStats { get; set; }
        public int Reputation { get; set; }
    }

    public class LastAnalysisStats
    {
        public int Malicious { get; set; }
        // Other analysis stats...
    }
}

public static class HttpRequestExceptionExtensions
{
    public static async Task<string> GetResponseContentAsync(this HttpRequestException exception)
    {
        return exception.Data.Contains("ResponseContent") ? exception.Data["ResponseContent"].ToString() : "No response content available";
    }
}
