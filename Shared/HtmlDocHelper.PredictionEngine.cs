using Microsoft.ML.Calibrators;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;
using Microsoft.ML;
using static Microsoft.ML.DataOperationsCatalog;
using System.Data;
using System.Collections.Immutable;

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

        Random random = new Random(seed);
        bool anyPostiveClass = false;
        bool anyNegativeClass = false;
        IDataView? trainingDataView = null;
        IDataView? testDataView = null;

        do
        {
            int spltDataSeed = random.Next(seed);
            // Split the data into training and test sets
            TrainTestData splitDataView = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.1, seed: spltDataSeed);
            trainingDataView = splitDataView.TrainSet;
            testDataView = splitDataView.TestSet;

            ImmutableArray<DataDebuggerPreview.RowInfo> trainingDataViewRows = trainingDataView.Preview().RowView;
            ImmutableArray<DataDebuggerPreview.RowInfo> testDataViews = testDataView.Preview().RowView;

            anyPostiveClass = testDataViews.Any(item => item.Values[1].Value.Equals(true));
            anyNegativeClass = testDataViews.Any(item => item.Values[1].Value.Equals(false));
        } while (!anyPostiveClass || !anyNegativeClass);

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
        CalibratedBinaryClassificationMetrics metrics = mlContext.BinaryClassification.Evaluate(predictions, Label);

        Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
        Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"F1 Score: {metrics.F1Score:P2}"); //F1 Score is 0

        //true postive is zero and false postive is 1
        //true negative is 2 and false negative is 1

        Console.WriteLine($"Positive Precision: {metrics.PositivePrecision:P2}"); 
        Console.WriteLine($"Positive Recall: {metrics.PositiveRecall:P2}");//True postive is zero
        Console.WriteLine($"Negative Precision: {metrics.NegativePrecision:P2}"); // =2/3 
        Console.WriteLine($"Negative Recall: {metrics.NegativeRecall:P2}"); // = 2/2 

        // Display confusion matrix
        ConfusionMatrix confusionMatrix = metrics.ConfusionMatrix;
        Console.WriteLine("Confusion Matrix:");
        string confusionMatrixTable= confusionMatrix.GetFormattedConfusionTable();
        Console.WriteLine(confusionMatrix.GetFormattedConfusionTable());

        // Use the model for predictions
        PredictionEngine<InputData, OutputPrediction> predictionEngine = mlContext.Model.CreatePredictionEngine<InputData, OutputPrediction>(trainedModel);

        return predictionEngine;
    });
}