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
            foreach ((string htmlDoc, bool containsTextNode) in HtmlDocHelper.HtmlDocsWithLabel)
            {
                Assert.That(HtmlDocHelper.ContainsTextNode(htmlDoc), Is.EqualTo(containsTextNode));
            }
        });
    }

    [Test]
    public void Test_ContainsTextNodeViaRegex()
    {
        Assert.Multiple(() =>
        {
            foreach ((string htmlDoc, bool containsTextNode) in HtmlDocHelper.HtmlDocsWithLabel)
            {
                Assert.That(HtmlDocHelper.ContainsTextNodeViaRegex(htmlDoc), Is.EqualTo(containsTextNode));
            }
        });
    }

    [Test]
    public void Test_ConatinsTextNodePredicted()
    {
        //need to tune and reevaluate the prediction model
        Assert.Multiple(() =>
        {
            foreach ((string htmlDoc, bool containsTextNode) in HtmlDocHelper.HtmlDocsWithLabel)
            {
                bool actual = HtmlDocHelper.ContainsTextNodePredicted(htmlDoc);
                if(actual != containsTextNode)
                {
                    Console.WriteLine(htmlDoc);
                }
                Assert.That(HtmlDocHelper.ContainsTextNodePredicted(htmlDoc), Is.EqualTo(containsTextNode));
            }
        });
        string test = "<div><li></li></ci><raw></raw><xml></xml></div>";
        Assert.That(HtmlDocHelper.ContainsTextNodePredicted(test), Is.False);
    }
}