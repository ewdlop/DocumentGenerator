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

}
