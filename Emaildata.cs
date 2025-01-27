using Microsoft.ML.Data;
using System.Collections.Generic;

public class EmailData
{
    [LoadColumn(0)]
    public string Date { get; set; }

    [LoadColumn(1)]
    public string Subject { get; set; }

    [LoadColumn(2)]
    public string Domain { get; set; }

    [LoadColumn(3)]
    public string EmailAddress { get; set; }

    [LoadColumn(4)]
    public string Body { get; set; }

    [LoadColumn(5)]
    public bool IsSpam { get; set; }

    // Existing fields for domain reputation
    [LoadColumn(6)]
    public bool? IsDomainBlacklisted { get; set; }

    [LoadColumn(7)]
    public int? DomainReputationScore { get; set; }

    // New field to indicate if the email is considered malicious based on URL analysis
    [LoadColumn(8)]
    public bool? IsMalicious { get; set; }

    // A list of threat types identified in the URLs within the email body
    // This could be serialized as a JSON string or stored in a separate table if using a relational database
    [LoadColumn(9)]
    public string ThreatTypes { get; set; }
}
