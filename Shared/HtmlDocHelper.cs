using DocumentFormat.OpenXml.ExtendedProperties;
using HtmlAgilityPack;
using Humanizer;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Shared;

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
        if(htmlParseErrors is not null && htmlParseErrors.Any())
        {
            return false;
        }
        return true;
    }

    public static bool ContainsTextNode(string html)
    {
        if(TryValidHtml(html, out var doc, out _))
        {
            return doc?.DocumentNode.DescendantsAndSelf().Any(node => node.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(node.InnerText)) ?? false;
        }
       
        return false;
    }

    public static bool ContainsTextNodeViaRegex(string html)
    {
        // Regex to match text nodes within HTML tags

        //>: Look for the end of an opening HTML tag.
        //\s *: Allow for any whitespace that might appear after the tag.
        //[^<>\s]: Ensure that there is at least one non - whitespace character that is not < or >.This ensures there is actual text content.
        //[^<>] *: Match any number of characters that are not<or>.This captures the rest of the text content between the tags.
        //\s *: Allow for any whitespace that might appear before the next tag.
        //<: Look for the start of a closing HTML tag or the next opening tag.
        string pattern = @">\s*[^<>\s][^<>]*\s*<";
        
        Regex regex = new Regex(pattern);
        return regex.IsMatch(html);
    }
}
