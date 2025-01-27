Greenspam Email Filtering Program

Overview

The Greenspam project is a local spam filtering application developed in C#. The program uses ML.NET with the SDC Logistic Regression algorithm to classify and filter spam emails. Initially built as a standalone Windows program using Microsoft Visual Studio, the project is now being expanded to incorporate more advanced machine learning techniques and web-based capabilities.

Features

Email Classification: Utilizes machine learning to classify emails as spam or non-spam.

Pattern Filtering: Implements customizable filters to detect specific spam patterns.

Local Deployment: Operates as a desktop application for offline usage.

Machine Learning Integration: Integrates ML.NET for building and training logistic regression models.

Requirements

Development Environment: Microsoft Visual Studio (C#)

Framework: .NET Framework 4.7 or later

Dependencies:

ML.NET

System.Data.SqlClient (for any local database integration)

Google.Apis (for Gmail API)

NLog (for logging functionality)

Newtonsoft.Json (for JSON parsing)

File Structure

greenspam/
├── Program.cs                 # Main program entry point
├── EmailData.cs               # Email data schema with features and labels
├── FetchWBody.cs              # Handles email fetching, processing, and storing
├── DomainReputationChecker.cs # Checks domain reputation via VirusTotal API
├── URLChecker.cs              # Analyzes URLs using Google Safe Browsing API
├── SpamModelTrainer.cs        # Logic for ML.NET model training and evaluation
├── ModelSaver.cs              # Saves trained ML.NET models as .zip files
├── LogisticRegressionModel.zip # Pretrained logistic regression model
├── training_data.csv          # Sample training dataset
├── testing_data.csv           # Sample testing dataset
└── README.md                  # Project documentation

Installation

Clone the repository:

git clone https://github.com/Tadeu71811/GreenSpam.git
cd GreenSpam

Open the project in Microsoft Visual Studio.

Restore NuGet packages:

Go to Tools > NuGet Package Manager > Manage NuGet Packages for Solution.

Install the necessary dependencies listed in the Requirements section.

Build the project and run the application.

Usage

Training the Model

Import a labeled dataset (e.g., training_data.csv) into the program.

Use the SpamModelTrainer to train a logistic regression model.

Save the trained model using the ModelSaver class.

Filtering Emails

Configure API keys for VirusTotal and Google Safe Browsing in the appropriate file paths.

Use the FetchWBody class to fetch emails from Gmail, analyze them, and store results in a SQLite database.

Use the trained ML.NET model for classification and spam detection.

Domain Reputation and URL Analysis

The DomainReputationChecker checks the reputation of email domains via VirusTotal.

The URLChecker analyzes URLs in email bodies using Google Safe Browsing API to identify potential threats.

Future Enhancements

Web-Based Interface: Transition to a React-based frontend and REST API backend.

Advanced Models: Incorporate deep learning techniques for better accuracy.

Cloud Integration: Enable online deployment for real-time email filtering.

Expanded Filtering Options: Add features to block emails based on domain reputation, IP blacklist, etc.

Recommendations for Contribution

Ensure all .cs files are appropriately documented with XML comments.

Test new features thoroughly before submitting a pull request.

Follow .NET coding conventions for consistent code quality.

License

This project is open-source and licensed under the MIT License.
