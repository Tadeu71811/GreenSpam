using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Data.Sqlite;
using NLog; // Added for NLog integration
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class FetchWBody
{
    static string[] Scopes = { GmailService.Scope.GmailModify };
    static string ApplicationName = "Greenspam Desktop Client";
    private string connectionString = "Data Source=D:\\Sqlite\\emailv2.db";
    private GmailService service;
    private CancellationToken cancellationToken;
    private string userEmail;
    private string apiKeyFilePath;
    private DomainReputationChecker domainReputationChecker;
    private URLChecker urlChecker;
    private static Logger logger = LogManager.GetCurrentClassLogger(); // Initialize NLog Logger

    public FetchWBody(CancellationToken token, string userEmail, string apiKeyFilePath, string googleSafeBrowsingApiKeyFilePath, string connectionString)
    {
        this.cancellationToken = token;
        this.userEmail = userEmail;
        this.apiKeyFilePath = apiKeyFilePath;
        this.connectionString = connectionString; // Set the database connection string using the correct parameter name

        try
        {
            using (var stream = new FileStream("SECRETKEY", FileMode.Open, FileAccess.Read))
            {
                string userTokenDirectory = $"tokens_{userEmail}";
                UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    userEmail,
                    CancellationToken.None,
                    new FileDataStore(userTokenDirectory, true)).Result;
                Console.WriteLine($"Credential file saved to: {userTokenDirectory}");

                service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                domainReputationChecker = new DomainReputationChecker(apiKeyFilePath);
                urlChecker = new URLChecker(googleSafeBrowsingApiKeyFilePath);
                logger.Info($"Initialized FetchWBody for {userEmail}"); // Log initialization
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Gmail service for {userEmail}: {ex.Message}\nStack Trace: {ex.StackTrace}");
            logger.Error(ex, $"Error initializing Gmail service for {userEmail}"); // Log error
        }
    }

    public async Task FetchAndStoreEmails()
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine($"Attempting to fetch new emails at {DateTime.Now}...");
                logger.Info($"Attempting to fetch new emails for {userEmail} at {DateTime.Now}"); // Log attempt

                var emails = await GetEmails();
                if (emails.Count == 0)
                {
                    Console.WriteLine("No new emails to process.");
                    logger.Info("No new emails to process."); // Log no new emails
                }
                else
                {
                    InsertEmailData(emails);
                    Console.WriteLine($"Fetched and stored {emails.Count} emails at {DateTime.Now}.");
                    logger.Info($"Fetched and stored {emails.Count} emails for {userEmail} at {DateTime.Now}."); // Log result
                }
                await Task.Delay(TimeSpan.FromHours(2), cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during FetchAndStoreEmails operation: " + ex.Message);
                logger.Error(ex, "Error during FetchAndStoreEmails operation"); // Log error
            }
        }
    }

    private async Task<List<EmailData>> GetEmails()
    {
        List<EmailData> emails = new List<EmailData>();
        List<string> categories = new List<string> { "INBOX" };

        foreach (var category in categories)
        {
            Console.WriteLine($"Checking category: {category}");
            var request = service.Users.Messages.List("me");
            request.Q = $"is:unread in:{category}";
            request.MaxResults = 10;
            var response = await request.ExecuteAsync(cancellationToken);

            if (response != null && response.Messages != null && response.Messages.Count > 0)
            {
                foreach (var messageItem in response.Messages)
                {
                    try
                    {
                        var email = await service.Users.Messages.Get("me", messageItem.Id).ExecuteAsync(cancellationToken);
                        var emailData = new EmailData
                        {
                            Date = email.Payload.Headers.FirstOrDefault(h => h.Name == "Date")?.Value ?? "Unknown Date",
                            Subject = email.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "No Subject",
                            Domain = ExtractDomain(email.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value) ?? "Unknown Domain",
                            EmailAddress = email.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value ?? "Unknown Email Address",
                            Body = GetEmailBody(email.Payload) ?? "No Body",
                            IsSpam = false // This will be set based on your spam detection logic
                        };

                        var (isValid, reason) = IsValidEmailData(emailData);
                        if (isValid)
                        {
                            // Existing Domain Reputation Check
                            var domainReputationResult = await domainReputationChecker.CheckDomainReputationAsync(emailData.Domain);
                            if (domainReputationResult != null)
                            {
                                emailData.IsDomainBlacklisted = domainReputationResult.IsBlacklisted;
                                emailData.DomainReputationScore = domainReputationResult.ReputationScore;
                                var domainStatus = domainReputationResult.IsBlacklisted ? "Unsafe" : "Appears Safe";
                                Console.WriteLine($"Domain: {emailData.Domain}, Status: {domainStatus}, Score: {emailData.DomainReputationScore}");
                            }
                            else
                            {
                                Console.WriteLine($"No reputation info for domain: {emailData.Domain}. Status: Unknown");
                            }

                            // New URL Checking in the email body
                            var urls = ExtractUrls(emailData.Body);
                            bool foundMaliciousUrl = false;
                            foreach (var url in urls)
                            {
                                try
                                {
                                    var urlCheckResult = await urlChecker.CheckUrlAsync(url);
                                    if (urlCheckResult.IsMalicious)
                                    {
                                        // Handle the malicious URL accordingly
                                        emailData.IsMalicious = true;
                                        emailData.ThreatTypes = urlCheckResult.ThreatType; // Corrected to ThreatType
                                        Console.WriteLine($"Malicious URL Detected: {url}, ThreatType: {urlCheckResult.ThreatType}");
                                        foundMaliciousUrl = true;
                                        break; // Stop checking more URLs if one is found malicious
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to check URL {url}: {ex.Message}");
                                }
                            }

                            // If no URLs are detected as malicious:
                            if (!foundMaliciousUrl)
                            {
                                Console.WriteLine("All URLs in the email appear safe.");
                                emailData.IsMalicious = false;  // explicitly set as false if no malicious URLs found
                            }

                            emails.Add(emailData);
                            Console.WriteLine($"Processed email: {emailData.Subject}");
                            MarkEmailAsRead(messageItem.Id);
                        }
                        else
                        {
                            // Log details for skipped email
                            Console.WriteLine($"Skipping email (ID: {messageItem.Id}). Reason: {reason}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing email {messageItem.Id}: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"No unread emails found in category: {category}");
            }
        }

        return emails;
    }

    private (bool IsValid, string Reason) IsValidEmailData(EmailData emailData)
    {
        var issues = new List<string>();
        if (string.IsNullOrEmpty(emailData.Date)) issues.Add("Missing Date");
        if (string.IsNullOrEmpty(emailData.Subject)) issues.Add("Missing Subject");
        // Add similar checks for other fields...

        var isValid = issues.Count == 0;
        var reason = isValid ? "Valid" : $"Skipped due to: {string.Join(", ", issues)}";
        return (isValid, reason);
    }

    private void MarkEmailAsRead(string messageId)
    {
        var modifyRequest = new ModifyMessageRequest();
        modifyRequest.RemoveLabelIds = new List<string> { "UNREAD" };
        service.Users.Messages.Modify(modifyRequest, "me", messageId).Execute();
    }

    private void InsertEmailData(List<EmailData> emails)
    {
        using var connection = new SqliteConnection(connectionString);
        try
        {
            connection.Open();

            foreach (var email in emails)
            {
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText =
                @"
                INSERT INTO Emails (Date, Subject, Domain, EmailAddress, Body, IsSpam, IsDomainBlacklisted, DomainReputationScore, IsMalicious, ThreatTypes)
                VALUES ($date, $subject, $domain, $emailAddress, $body, $isSpam, $isBlacklisted, $reputationScore, $isMalicious, $threatTypes)
                ";

                insertCommand.Parameters.AddWithValue("$date", email.Date);
                insertCommand.Parameters.AddWithValue("$subject", email.Subject);
                insertCommand.Parameters.AddWithValue("$domain", email.Domain);
                insertCommand.Parameters.AddWithValue("$emailAddress", email.EmailAddress);
                insertCommand.Parameters.AddWithValue("$body", email.Body);
                insertCommand.Parameters.AddWithValue("$isSpam", email.IsSpam ? 1 : 0);
                insertCommand.Parameters.AddWithValue("$isBlacklisted", email.IsDomainBlacklisted.HasValue ? (email.IsDomainBlacklisted.Value ? 1 : 0) : DBNull.Value);
                insertCommand.Parameters.AddWithValue("$reputationScore", email.DomainReputationScore.HasValue ? Math.Clamp(email.DomainReputationScore.Value, -100, 100) : (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("$isMalicious", email.IsMalicious.HasValue ? (email.IsMalicious.Value ? 1 : 0) : (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("$threatTypes", email.ThreatTypes ?? "");  // Assuming ThreatTypes is a string and can be null

                insertCommand.ExecuteNonQuery();
            }
            Console.WriteLine("Emails successfully written to the database.");
            logger.Info($"Emails successfully written to the database for {userEmail}."); // Log success
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error writing to the database: " + ex.Message);
            logger.Error(ex, "Error writing to the database"); // Log error
        }
    }

    private string ExtractDomain(string emailAddress)
    {
        if (string.IsNullOrEmpty(emailAddress))
            return string.Empty;

        var start = emailAddress.IndexOf('@');
        if (start > -1 && start < emailAddress.Length - 1)
            return emailAddress.Substring(start + 1).Split('>')[0].Trim();

        return string.Empty;
    }

    private string GetEmailBody(MessagePart payload)
    {
        string DecodeBase64Url(string base64UrlString)
        {
            string base64Normalized = base64UrlString.Replace('-', '+').Replace('_', '/');
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64Normalized));
        }

        if (payload.Parts == null || !payload.Parts.Any())
        {
            return payload.Body.Data != null ? DecodeBase64Url(payload.Body.Data) : null;
        }

        foreach (var part in payload.Parts)
        {
            if (part.MimeType == "text/plain")
            {
                return part.Body.Data != null ? DecodeBase64Url(part.Body.Data) : null;
            }
            else if (part.Parts != null)
            {
                foreach (var subPart in part.Parts)
                {
                    if (subPart.MimeType == "text/plain")
                    {
                        return subPart.Body.Data != null ? DecodeBase64Url(subPart.Body.Data) : null;
                    }
                }
            }
        }

        return null;
    }

    private List<string> ExtractUrls(string text)
    {
        var urls = new List<string>();
        var regex = new Regex(@"http[s]?://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var matches = regex.Matches(text);
        foreach (Match match in matches)
        {
            urls.Add(match.Value);
        }
        return urls;
    }
}
