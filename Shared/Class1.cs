using HtmlAgilityPack;

namespace Shared;

public class Class1
{

}

public static class HtmlDocHelper
{
    public static readonly Lazy<HtmlDocument> HtmlDocument = new(() =>
    {
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(GetHtml());
        return doc;
    });

    public static string GetHtml()
    {
        return "<html><body><h1>Hello World</h1></body></html>";
    }

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
}
