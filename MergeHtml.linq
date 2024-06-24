<Query Kind="Statements">
  <NuGetReference>HtmlAgilityPack</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>HtmlAgilityPack</Namespace>
</Query>

using HtmlAgilityPack;


var htmlDoc1 = """
<html>
    <body>
        <div class='container'>
            <p id='paragraph1'>Hello</p>
        </div>
    </body>
</html>
""";

var htmlDoc2 = """
<html>
    <body>
        <div class='container'>
            <p id='paragraph2'>World</p>
        </div>
        <footer>Footer content</footer>
    </body>
</html>
""";


var htmlDoc3 = """
<html><body><div>456<div><div class="123"></div></body><footer>1</footer></html>
""";

var htmlDoc4 = """
<html><body><div>456<div><div class="123"></div><div>test</div></body><footer id="123"></footer></html>
""";

//HtmlDocument doc = new HtmlDocument();
//doc.LoadHtml(htmlDoc1);
//
//HtmlDocument doc2 = new HtmlDocument();
//doc2.LoadHtml(htmlDoc2);
//
//var list = doc.DocumentNode
//.Descendants().ToList();
//list.AddRange(doc2.DocumentNode
//	.Descendants());
//list = list.DistinctBy(n => n.InnerHtml).ToList();
//HtmlDocument doc3 = new HtmlDocument();
//list.ForEach(n => doc3.DocumentNode.AppendChild(n));
//GetLeafNodes(doc2.DocumentNode).Dump();

MergeHtml(htmlDoc1, htmlDoc2);


static void MergeHtml(string html1, string html2)
{
	HtmlDocument doc = new HtmlDocument();
	doc.LoadHtml(html1);

	var htmlParseErrors = doc.ParseErrors;
	if (htmlParseErrors is not null && htmlParseErrors.Any())
	{
		return;
	}
	doc.Dump();
	
	HtmlDocument doc2 = new HtmlDocument();
	doc2.LoadHtml(html2);

	var htmlParseErrors2 = doc.ParseErrors;
	if (htmlParseErrors2 is not null && htmlParseErrors2.Any())
	{
		return;
	}
	MergeHtmlDocuments(doc, doc2).DocumentNode.OuterHtml.Dump();

}

static HtmlDocument MergeHtmlDocuments(HtmlDocument doc1, HtmlDocument doc2)
{
    var mergedDoc = new HtmlDocument();
    mergedDoc.LoadHtml(doc1.DocumentNode.OuterHtml);

    MergeNodes(mergedDoc.DocumentNode, doc2.DocumentNode);

    return mergedDoc;
}

static void MergeNodes(HtmlNode node1, HtmlNode node2)
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

static HtmlNode FindMatchingNode(HtmlNode parent, HtmlNode target)
{
    return parent.ChildNodes
        .FirstOrDefault(child => NodesAreSimilar(child, target));
}

static bool NodesAreSimilar(HtmlNode node1, HtmlNode node2)
{
    return node1.Name == node2.Name &&
           node1.Attributes["class"]?.Value == node2.Attributes["class"]?.Value &&
		   node1.Attributes["id"]?.Value == node2.Attributes["id"]?.Value;
}


static HtmlNode MergeNodes2(HtmlNode? node1, HtmlNode? node2)
{
	if (node1 is null) return node2;
	if (node2 is null) return node1;
	return node1;
}


static List<HtmlNode> GetLeafNodes(HtmlNode node)
{
	var leafNodes = new List<HtmlNode>();

	foreach (var child in node.ChildNodes)
	{
		if (child.NodeType == HtmlNodeType.Element)
		{
			if (IsLeafNode(child))
			{
				leafNodes.Add(child);
			}
			else
			{
				leafNodes.AddRange(GetLeafNodes(child));
			}
		}
	}

	return leafNodes;
}

static bool IsLeafNode(HtmlNode node)
{
	foreach (var child in node.ChildNodes)
	{
		if (child.NodeType == HtmlNodeType.Element)
		{
			return false;
		}
	}
	return true;
}