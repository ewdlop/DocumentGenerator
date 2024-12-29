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

        //Merge 2 html docs without duplicating any node
        //dont modify the original html docs
        public static string MergeHtmlDocs(string htmlDoc1, string htmlDoc2)
        {
            if (string.IsNullOrWhiteSpace(htmlDoc1))
            {
                return htmlDoc2;
            }

            if (string.IsNullOrWhiteSpace(htmlDoc2))
            {
                return htmlDoc1;
            }

            if (htmlDoc1 == htmlDoc2)
            {
                return htmlDoc1;
            }

            if (TryValidHtml(htmlDoc1, out HtmlDocument? doc1, out _) && TryValidHtml(htmlDoc2, out HtmlDocument? doc2, out _))
            {
                HtmlDocument mergedDoc = new HtmlDocument();
                mergedDoc.LoadHtml(doc1.DocumentNode.OuterHtml);

                MergeNodes(mergedDoc.DocumentNode, doc2.DocumentNode);

                return mergedDoc.DocumentNode.OuterHtml;
            }
            return string.Empty;
        }

        private static void MergeNodes(HtmlNode node1, HtmlNode node2)
        {
            foreach (var child2 in node2.ChildNodes)
            {
                var matchingChild1 = FindMatchingNode(node1, child2);
                if (matchingChild1 != null)
                {
                    MergeNodes(matchingChild1, child2);
                }
                else
                {
                    var importedChild = HtmlNode.CreateNode(child2.OuterHtml);
                    node1.AppendChild(importedChild);
                }
            }
        }

        private static HtmlNode FindMatchingNode(HtmlNode parent, HtmlNode target)
        {
            return parent.ChildNodes
                .FirstOrDefault(child => NodesAreSimilar(child, target));
        }

        private static bool NodesAreSimilar(HtmlNode node1, HtmlNode node2)
        {
            return node1.Name == node2.Name &&
                   node1.Attributes["class"]?.Value == node2.Attributes["class"]?.Value &&
                   node1.Attributes["id"]?.Value == node2.Attributes["id"]?.Value;
        }
    }
}
