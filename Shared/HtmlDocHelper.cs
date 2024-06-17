using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Shared
{
    public static partial class HtmlDocHelper
    {
        public static bool TryValidHtml(string html, out HtmlDocument? htmlDocument, out IEnumerable<HtmlParseError>? htmlParseErrors)
        {
            htmlDocument = null;
            htmlParseErrors = null;
            if (string.IsNullOrWhiteSpace(html))
            {
                return false;
            }
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            htmlDocument = doc;
            htmlParseErrors = doc.ParseErrors;
            if (htmlParseErrors is not null && htmlParseErrors.Any())
            {
                return false;
            }
            return true;
        }

        public static bool ContainsTextNode(string html) =>
            TryValidHtml(html, out HtmlDocument? doc, out _) &&
            (doc?.DocumentNode.DescendantsAndSelf()
                .Any(node => node.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(node.InnerText)) ?? false);

        public static bool ContainsTextNodeViaRegex(string html)
        {
            if(string.IsNullOrWhiteSpace(html))
            {
                return false;
            }

            // Regex to match text nodes within HTML tags
            string pattern = @">\s*[^<>\s][^<>]*\s*<";

            Regex regex = new Regex(pattern);
            return regex.IsMatch(html);
        }

        public static bool ContainsTextNodePredicted(string html)
        {
            if (PredictionEngine.Value is null)
            {
                throw new InvalidOperationException("PredictionEngine is not initialized.");
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                return false;
            }

            InputData input = new InputData { Html = html };
            OutputPrediction prediction = PredictionEngine.Value.Predict(input);
            return prediction.Prediction;
        }
    }
}