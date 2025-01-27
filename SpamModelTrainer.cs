using Microsoft.ML;
using Microsoft.ML.Data;
using System;

public class SpamModelTrainer
{
    public MLContext mlContext = new MLContext();

    public ITransformer TrainModel(IDataView trainingData)
    {
        // Data process pipeline
        var pipeline = mlContext.Transforms.Text.FeaturizeText("EmailFeaturized", nameof(EmailData.EmailAddress))
            .Append(mlContext.Transforms.Text.FeaturizeText("DomainFeaturized", nameof(EmailData.Domain)))
            .Append(mlContext.Transforms.Text.FeaturizeText("SubjectFeaturized", nameof(EmailData.Subject)))
            // Concatenate the text features and convert the IsSpam boolean to a label
            .Append(mlContext.Transforms.Concatenate("Features", "EmailFeaturized", "DomainFeaturized", "SubjectFeaturized"))
            .Append(mlContext.Transforms.Conversion.ConvertType(outputColumnName: "Label", inputColumnName: nameof(EmailData.IsSpam), outputKind: DataKind.Boolean))
            .AppendCacheCheckpoint(mlContext);

        // Choose the algorithm and train the model
        var trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features");
        var trainingPipeline = pipeline.Append(trainer);

        return trainingPipeline.Fit(trainingData);
    }

    public void EvaluateModel(ITransformer model, IDataView testData)
    {
        var predictions = model.Transform(testData);
        var metrics = mlContext.BinaryClassification.Evaluate(predictions, "Label");

        Console.WriteLine("Model Performance Metrics:");
        Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
        Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
        // Add additional metrics as needed
    }
}
