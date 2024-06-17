using Shared;

namespace HtmlDocoumentTest;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test_ContainsTextNode()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlDocHelper.ContainsTextNode(HtmlDocHelper.RawHtmlDoc1), Is.True);
            Assert.That(HtmlDocHelper.ContainsTextNode(HtmlDocHelper.RawHtmlDoc2), Is.False);
            Assert.That(HtmlDocHelper.ContainsTextNode(HtmlDocHelper.RawHtmlDoc3), Is.False);
            Assert.That(HtmlDocHelper.ContainsTextNode(HtmlDocHelper.RawHtmlDoc4), Is.True);
            Assert.That(HtmlDocHelper.ContainsTextNode(HtmlDocHelper.RawHtmlDoc5), Is.False);
            Assert.That(HtmlDocHelper.ContainsTextNode(HtmlDocHelper.RawHtmlDoc6), Is.True);
            Assert.That(HtmlDocHelper.ContainsTextNode(HtmlDocHelper.RawHtmlDoc7), Is.True);
            Assert.That(HtmlDocHelper.ContainsTextNode(HtmlDocHelper.RawHtmlDoc8), Is.False);
            Assert.That(HtmlDocHelper.ContainsTextNode(HtmlDocHelper.RawHtmlDoc9), Is.True);
        });

    }

    [Test]
    public void Test_ContainsTextNodeViaRegex()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlDocHelper.ContainsTextNodeViaRegex(HtmlDocHelper.RawHtmlDoc1), Is.True);
            Assert.That(HtmlDocHelper.ContainsTextNodeViaRegex(HtmlDocHelper.RawHtmlDoc2), Is.False);
            Assert.That(HtmlDocHelper.ContainsTextNodeViaRegex(HtmlDocHelper.RawHtmlDoc3), Is.False);
            Assert.That(HtmlDocHelper.ContainsTextNodeViaRegex(HtmlDocHelper.RawHtmlDoc4), Is.True);
            Assert.That(HtmlDocHelper.ContainsTextNodeViaRegex(HtmlDocHelper.RawHtmlDoc5), Is.False);
            Assert.That(HtmlDocHelper.ContainsTextNodeViaRegex(HtmlDocHelper.RawHtmlDoc6), Is.True);
            Assert.That(HtmlDocHelper.ContainsTextNodeViaRegex(HtmlDocHelper.RawHtmlDoc7), Is.True);
            Assert.That(HtmlDocHelper.ContainsTextNodeViaRegex(HtmlDocHelper.RawHtmlDoc8), Is.False);
            Assert.That(HtmlDocHelper.ContainsTextNodeViaRegex(HtmlDocHelper.RawHtmlDoc9), Is.True);
        });

    }
}