using Microsoft.ML;
using System.IO;
using System.IO.Compression;

public class ModelSaver
{
    public void SaveModelAsZip(ITransformer model, DataViewSchema schema, string modelPath, MLContext mlContext)
    {
        string tempModelPath = Path.GetTempFileName();

        // Save the model to a temporary file
        using (var fileStream = new FileStream(tempModelPath, FileMode.Create, FileAccess.Write, FileShare.Write))
        {
            mlContext.Model.Save(model, schema, fileStream);
        }

        // Create a zip archive and add the model file
        using (var archive = ZipFile.Open(modelPath, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(tempModelPath, "model.zip");
        }

        // Delete the temporary file
        File.Delete(tempModelPath);
    }
}
