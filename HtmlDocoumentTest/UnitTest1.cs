using Shared;
using System.Runtime.InteropServices;

namespace HtmlDocoumentTest;

public class Tests
{

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();

    // Import the FreeConsole function from kernel32.dll
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsole();

    [SetUp]
    public void Setup()
    {
        if(!AllocConsole())
        {
            //throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            System.ComponentModel.Win32Exception exception = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            Console.WriteLine($"Failed to allocate console. {exception.Message}");
        }
        else
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
            Console.WriteLine("Console attached");
        }
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
        //accuracy definitely needs to be improved
        Assert.Multiple(() =>
        {
            foreach ((string htmlDoc, bool containsTextNode) in HtmlDocHelper.HtmlDocsWithLabel)
            {
                Assert.That(HtmlDocHelper.ContainsTextNodePredicted(htmlDoc), Is.EqualTo(containsTextNode), "Input: {0}", htmlDoc);
            }
        });
        string test = "<div><li></li></ci><raw></raw><xml></xml></div>";
        Assert.That(HtmlDocHelper.ContainsTextNodePredicted(test), Is.False);
    }

    [Test]
    public void Test_MergeHtmlDocs()
    {
        string htmlDoc1 = "<html><body><div class='container'><p id='paragraph1'>Hello</p></div></body></html>";
        string htmlDoc2 = "<html><body><div class='container'><p id='paragraph2'>World</p></div><footer>Footer content</footer></body></html>";
        string expectedMergedHtml = "<html><body><div class='container'><p id='paragraph1'>Hello</p><p id='paragraph2'>World</p></div><footer>Footer content</footer></body></html>";

        string mergedHtml = HtmlDocHelper.MergeHtmlDocs(htmlDoc1, htmlDoc2);

        Assert.That(mergedHtml, Is.EqualTo(expectedMergedHtml));
    }
}
