using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

public class HtmlMerger
{
    public static void Main()
    {
        string htmlDoc3 = """
        <div><ul><li><strong>D1110</strong></li></ul><div style="padding-left:40px"><div>NCMHX, CC. NONE</div><div>PROPHY, SCALE, POLISH AND FLOSS AND FL2</div><div>OH-FAIR. RECOMMEND COMP EXAM AND FMX/TX. DRY MOUTH NOTED, REC MORE WATER AND NG</div><div>P-L</div><div>HEMO-M</div><div>CALC-L/M</div><div>AAP-I</div><div>OHI- TBI 2x A DAY/ 2MINS. FLOSSIN INST</div><div>NV 1YR</div><div><br></div><div>HSMITH RDH20312/RDHAP996</div></div></div>
        """;

        string htmlDoc4 = """
        <div><ul><li><strong>D1110</strong></li></ul><div style="padding-left:40px"><div>NCMHX, CC. NEEDS TX, NO PAIN</div><div>PROPHY, SCALE, POLISH AND FLOSS</div><div>OH-FAIR. RECOMMEND COMP EXAM AND FMX/TX</div><div>P-L/M</div><div>HEMO-M</div><div>CALC-L/M</div><div>AAP-I</div><div>OHI- TBI 2x A DAY/ 2MINS. FLOSSIN INST</div><div>NV 1YR</div><div><br></div><div>HSMITH RDH20312/RDHAP996</div></div></div>
        """;

        var doc1 = new HtmlDocument();
        doc1.LoadHtml(htmlDoc3);

        var doc2 = new HtmlDocument();
        doc2.LoadHtml(htmlDoc4);

        var mergedNodes = MergeNodes(doc1.DocumentNode, doc2.DocumentNode);
        var mergedHtml = string.Join("", mergedNodes.Select(node => node.OuterHtml));

        Console.WriteLine(mergedHtml);
    }

    private static IEnumerable<HtmlNode> MergeNodes(HtmlNode node1, HtmlNode node2)
    {
        if (node1 == null)
        {
            yield return node2;
            yield break;
        }
        if (node2 == null)
        {
            yield return node1;
            yield break;
        }

        if (!CanMerge(node1, node2))
        {
            yield return node1;
            yield return node2;
            yield break;
        }

        yield return MergeChildNodes(node1, node2);
    }

    private static IEnumerable<HtmlNode> MergeChildGroups(List<HtmlNode> group1, List<HtmlNode> group2)
    {
        List<HtmlNode> cannotMergeNodes = new List<HtmlNode>();
        List<HtmlNode> mergedNodes = new List<HtmlNode>();

        for (int i = 0; i < group1.Count; i++)
        {
            cannotMergeNodes.Add(group1[i]);
            for (int j = 0; j < group2.Count; j++)
            {
                if (mergedNodes.Contains(group2[j]))
                {
                    continue;
                }
                else
                {
                    if (!cannotMergeNodes.Contains(group2[j]))
                    {
                        cannotMergeNodes.Add(group2[j]);
                    }
                }
                if (CanMerge(group1[i], group2[j]))
                {
                    mergedNodes.Add(group1[i]);
                    mergedNodes.Add(group2[j]);
                    cannotMergeNodes.Remove(group1[i]);
                    cannotMergeNodes.Remove(group2[j]);

                    foreach (HtmlNode mergedNode in MergeNodes(group1[i], group2[j]))
                    {
                        yield return mergedNode;
                    }
                }
            }
        }

        foreach (HtmlNode node in cannotMergeNodes)
        {
            yield return node;
        }
    }

    private static HtmlNode MergeChildNodes(HtmlNode node1, HtmlNode node2)
    {
        HtmlNode mergedNode = node1.Clone();
        mergedNode.RemoveAllChildren();

        var childGroups1 = node1.ChildNodes.GroupBy(n => n.Name).ToDictionary(g => g.Key, g => g.ToList());
        var childGroups2 = node2.ChildNodes.GroupBy(n => n.Name).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var key in childGroups1.Keys.Union(childGroups2.Keys))
        {
            var group1 = childGroups1.TryGetValue(key, out var value1) ? value1 : new List<HtmlNode>();
            var group2 = childGroups2.TryGetValue(key, out var value2) ? value2 : new List<HtmlNode>();

            foreach (var child in MergeChildGroups(group1, group2))
            {
                mergedNode.AppendChild(child);
            }
        }

        return mergedNode;
    }

    private static bool CanMerge(HtmlNode node1, HtmlNode node2)
    {
        return AreDocumentNodes(node1, node2)
            || SimilarElementNode(node1, node2)
            || SameTextNode(node1, node2);
    }

    private static bool HasSameAttributes(HtmlNode node1, HtmlNode node2)
    {
        var attributes1 = node1.Attributes.ToDictionary(attr => attr.Name, attr => attr.Value);
        var attributes2 = node2.Attributes.ToDictionary(attr => attr.Name, attr => attr.Value);

        return attributes1.Count == attributes2.Count &&
               !attributes1.Except(attributes2).Any() &&
               !attributes2.Except(attributes1).Any();
    }

    private static bool AreDocumentNodes(HtmlNode node1, HtmlNode node2)
        => node1.Name == node2.Name && node1.Name == "#document";

    private static bool HasSameName(HtmlNode node1, HtmlNode node2)
        => node1.Name == node2.Name;

    private static bool HasSameLeafNode(HtmlNode node1, HtmlNode node2)
        => !HasAnyChild(node1) &&
           !HasAnyChild(node2) &&
           HasSameName(node1, node2) &&
           HasSameInnerText(node1, node2) &&
           HasSameAttributes(node1, node2);

    private static bool HasSameInnerText(HtmlNode node1, HtmlNode node2)
        => !string.IsNullOrEmpty(node1.InnerText) &&
           !string.IsNullOrEmpty(node2.InnerText) &&
           node1.InnerText == node2.InnerText;

    private static bool IsTextNode(HtmlNode node)
        => node.NodeType == HtmlNodeType.Text;

    private static bool HasAnyChild(HtmlNode node)
        => node.ChildNodes.Count > 0;

    private static bool SimilarElementNode(HtmlNode node1, HtmlNode node2)
        => HasSameName(node1, node2) &&
           HasSameAttributes(node1, node2) &&
           !IsTextNode(node1) &&
           !IsTextNode(node2);

    private static bool SameTextNode(HtmlNode node1, HtmlNode node2)
        => HasSameName(node1, node2) &&
           HasSameAttributes(node1, node2) &&
           IsTextNode(node1) &&
           IsTextNode(node2) &&
           HasSameInnerText(node1, node2);
}
