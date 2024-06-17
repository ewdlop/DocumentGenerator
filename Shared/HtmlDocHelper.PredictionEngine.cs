using Microsoft.ML.Calibrators;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;
using Microsoft.ML;
using static Microsoft.ML.DataOperationsCatalog;

namespace Shared;

public static partial class HtmlDocHelper
{

    public const string Label = "Label";
    public const string Features = "Features";
    public const string PreditctedLabel = "PredictedLabel";

    public record InputData
    {
        [LoadColumn(0)]
        public string Html { get; set; } = string.Empty;

        [LoadColumn(1), ColumnName(Label)]
        public bool HasText { get; set; }
    }

    public class OutputPrediction
    {
        [ColumnName(PreditctedLabel)]
        public bool Prediction { get; set; }

        public float Probability { get; set; }

        public float Score { get; set; }

    }

    public static readonly Lazy<PredictionEngine<InputData, OutputPrediction>> PredictionEngine = new(() =>
    {
        int seed = 41231; //random seed for better reproducibility

        MLContext mlContext = new MLContext(seed);

        IDataView dataView = mlContext.Data.LoadFromEnumerable(HtmlDocsWithLabel.Select(kvp => new InputData { Html = kvp.Key, HasText = kvp.Value }));

        // Split the data into training and test sets
        TrainTestData splitDataView = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2, samplingKeyColumnName: Label);
        IDataView trainingDataView = splitDataView.TrainSet;
        IDataView testDataView = splitDataView.TestSet;

        // Define the data process pipeline
        TextFeaturizingEstimator dataProcessPipeline = mlContext.Transforms.Text.FeaturizeText(outputColumnName: Features, inputColumnName: nameof(InputData.Html));

        // Set the trainer
        //Stochastic Dual Coordinate Ascent 
        SdcaLogisticRegressionBinaryTrainer trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: Label, featureColumnName: Features);

        // Build the training pipeline
        //https://en.wikipedia.org/wiki/Platt_scaling
        EstimatorChain<BinaryPredictionTransformer<CalibratedModelParametersBase<LinearBinaryModelParameters, PlattCalibrator>>> trainingPipeline = dataProcessPipeline.Append(trainer);

        // Train the model
        TransformerChain<BinaryPredictionTransformer<CalibratedModelParametersBase<LinearBinaryModelParameters, PlattCalibrator>>> trainedModel = trainingPipeline.Fit(trainingDataView);

        // Evaluate the model
        IDataView predictions = trainedModel.Transform(testDataView);

        //AUC is not definied when there is no negative class in the data
        //Need to calibrate the train data spliting to contain both positive and negative classes
        //CalibratedBinaryClassificationMetrics metrics = mlContext.BinaryClassification.Evaluate(predictions, Label);

        //Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
        //Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:P2}");
        //Console.WriteLine($"F1 Score: {metrics.F1Score:P2}");

        // Use the model for predictions
        PredictionEngine<InputData, OutputPrediction> predictionEngine = mlContext.Model.CreatePredictionEngine<InputData, OutputPrediction>(trainedModel);

        return predictionEngine;
    });
}