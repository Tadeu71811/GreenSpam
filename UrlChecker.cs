using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class URLChecker
{
    private string apiKey;

    public URLChecker(string apiKeyFilePath)
    {
        apiKey = File.ReadAllText(apiKeyFilePath);
    }

    public async Task<(bool IsMalicious, string ThreatType)> CheckUrlAsync(string url)
    {
        // Construct the request to Google Safe Browsing API
        var requestUrl = $"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={apiKey}";
        var requestBody = ConstructRequestBody(url);

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.PostAsync(requestUrl, new StringContent(requestBody, Encoding.UTF8, "application/json"));
            var responseBody = await response.Content.ReadAsStringAsync();

            // Parse the response from Google Safe Browsing
            // You'll need to implement the logic based on the actual structure of the response
            // This is a placeholder for demonstration
            bool isMalicious = responseBody.Contains("maliciousContent");
            string threatType = isMalicious ? "MALWARE/SOCIAL_ENGINEERING" : string.Empty; // Simplified

            return (isMalicious, threatType);
        }
    }

    private string ConstructRequestBody(string url)
    {
        // Construct the JSON request body with the URL to be checked
        // Adjust based on the actual API specification
        return $@"
        {{
            ""client"": {{
                ""clientId"":      ""Greenspam Desktop Client"",
                ""clientVersion"": "".8""
            }},
            ""threatInfo"": {{
                ""threatTypes"":      [""MALWARE"", ""SOCIAL_ENGINEERING""],
                ""platformTypes"":    [""WINDOWS""],
                ""threatEntryTypes"": [""URL""],
                ""threatEntries"": [
                    {{""url"": ""{url}""}}
                ]
            }}
        }}";
    }
}
