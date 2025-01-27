using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using Microsoft.Data.Sqlite;  // Ensure this package is installed via NuGet

class Program
{
    private static Logger logger;

    static async Task Main(string[] args)
    {
        // Configure NLog from the specified file before doing anything else
        var config = new XmlLoggingConfiguration("NLog.config");
        LogManager.Configuration = config;
        logger = LogManager.GetCurrentClassLogger(); // Initialize logger after setting configuration

        var cts = new CancellationTokenSource();

        // Define the path to your API key files
        string virusTotalApiKeyFilePath = @"C:\Secretkey\virustotalapi.txt"; // Path for VirusTotal API key
        string googleSafeBrowsingApiKeyFilePath = @"C:\Secretkey\google_safebrowsing_api.txt"; // Path for Google Safe Browsing API key

        // Define your database connection string
        string connectionString = "Data Source=D:\\Sqlite\\emailv2.db"; // Update with the correct path

        // Initialize your FetchWBody instances (assuming you've updated the constructor to accept connectionString)
        var fetchWBodyForPetegree = new FetchWBody(cts.Token, "enteremail", virusTotalApiKeyFilePath, googleSafeBrowsingApiKeyFilePath, connectionString);
        var fetchWBodyForNotaspamtest = new FetchWBody(cts.Token, "enteremail", virusTotalApiKeyFilePath, googleSafeBrowsingApiKeyFilePath, connectionString);

        logger.Info("Starting email fetching process. Press 'c' to cancel.");  // Log the start

        // Start fetching emails
        var fetchTaskPetegree = fetchWBodyFor"email".FetchAndStoreEmails();
        var fetchTaskNotaspamtest = fetchWBodyFor"email".FetchAndStoreEmails();

        // Listen for cancellation request
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.C)
                {
                    logger.Info("Cancelling the email fetching process...");  // Log the cancellation request
                    cts.Cancel();
                    break;
                }
            }
        }

        // Wait for all tasks to complete or be cancelled
        try
        {
            await Task.WhenAll(fetchTaskPetegree, fetchTaskNotaspamtest);
        }
        catch (OperationCanceledException)
        {
            logger.Warn("Email fetching process was cancelled.");  // Log the cancellation
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred during email fetching");  // Log unexpected errors
        }

        // Clean up or finalize tasks if necessary
    }
}
