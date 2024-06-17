using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Shared;

public static partial class HtmlDocHelper
{
    public static readonly Lazy<HtmlDocument> HtmlDocument = new(() =>
    {
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(RawHtmlDoc1);
        return doc;
    });

    //random html docs
    public const string RawHtmlDoc1 = "<html><body><h1>Hello World</h1></body></html>";
    public const string RawHtmlDoc2 = "<html></html>";
    public const string RawHtmlDoc3 = "<div><br></div>";
    public const string RawHtmlDoc4 = "<div>...</div>";
    public const string RawHtmlDoc5 = "<div>";
    public const string RawHtmlDoc6 = "<div>...</div><div>...</div>";
    public const string RawHtmlDoc7 = "<div>...</div><div>...</div><div>...</div>";
    public const string RawHtmlDoc8 = "<div><li></li></div>";
    public const string RawHtmlDoc9 = "<div>123</div>";
    public const string RawHtmlDoc10 = "<span></span>";

    //whether the html doc contains text node
    public static readonly Dictionary<string, bool> HtmlDocsWithLabel = new()
    {
        { RawHtmlDoc1, true },
        { RawHtmlDoc2, false },
        { RawHtmlDoc3, false },
        { RawHtmlDoc4, true },
        { RawHtmlDoc5, false },
        { RawHtmlDoc6, true },
        { RawHtmlDoc7, true },
        { RawHtmlDoc8, false },
        { RawHtmlDoc9, true },
        { RawHtmlDoc10, false}
    };
}
