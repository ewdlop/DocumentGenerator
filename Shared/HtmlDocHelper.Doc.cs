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
    public const string RawHtmlDoc11 = "<div><span></span><div>";
    public const string RawHtmlDoc12 = "<li></li>";
    public const string RawHtmlDoc13 = "<h1></h1>";
    public const string RawHtmlDoc14 = "<div><h1></h1></div>";
    public const string RawHtmlDoc15 = "<h2><h1></h1></h2>";
    public const string RawHtmlDoc16 = "<h3></h3>";
    public const string RawHtmlDoc17 = "<head></head>";
    public const string RawHtmlDoc18 = "<body></body>";
    public const string RawHtmlDoc19 = "<footer></footer>";
    public const string RawHtmlDoc20 = "<script></script>";



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
        { RawHtmlDoc10, false},
        { RawHtmlDoc11, false},
        { RawHtmlDoc12, false},
        { RawHtmlDoc13, false},
        { RawHtmlDoc14, false},
        { RawHtmlDoc15, false},
        { RawHtmlDoc16, false},
        { RawHtmlDoc17, false},
        { RawHtmlDoc18, false},
        { RawHtmlDoc19, false},
        { RawHtmlDoc20, false}
    };
}
