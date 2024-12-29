# DocumentGenerator

https://www.markdownguide.org/tools/dawin/

https://guide.dawin.io/basic-syntax#%D8%A7%D9%84%D9%82%D9%88%D8%A7%D8%A6%D9%85-%D8%A7%D9%84%D9%85%D9%86%D9%82%D9%91%D8%B7%D8%A9-%D8%BA%D9%8A%D8%B1-%D8%A7%D9%84%D9%85%D8%B1%D8%AA%D9%91%D8%A8%D8%A9


# GitBook

https://www.gitbook.com/?utm_source=legacy&utm_medium=redirect&utm_campaign=close_legacy

# RAG

https://aws.amazon.com/what-is/retrieval-augmented-generation/

# Doxygen + Graphviz
https://stackoverflow.com/questions/9484879/graphviz-doxygen-to-generate-uml-class-diagrams


# Citaiton Machine

https://www.scribbr.com/citation/generator/folders/2SW3rWl9aNisXzPZg9dKY9/lists/2LRvxShvLPGS5BKYE6nno4/

# MergeHtmlDocs Function

The `MergeHtmlDocs` function in the `HtmlDocHelper` class allows you to merge two HTML documents without duplicating any nodes. This function ensures that the resulting merged document contains all unique nodes from both input documents.

## Usage

To use the `MergeHtmlDocs` function, follow these steps:

1. Ensure you have the `HtmlDocHelper` class available in your project.
2. Call the `MergeHtmlDocs` function with two HTML document strings as parameters.
3. The function will return the merged HTML document as a string.

## Example

```csharp
string htmlDoc1 = "<html><body><div class='container'><p id='paragraph1'>Hello</p></div></body></html>";
string htmlDoc2 = "<html><body><div class='container'><p id='paragraph2'>World</p></div><footer>Footer content</footer></body></html>";

string mergedHtml = HtmlDocHelper.MergeHtmlDocs(htmlDoc1, htmlDoc2);

Console.WriteLine(mergedHtml);
```

In this example, the `MergeHtmlDocs` function merges the two input HTML documents and returns the merged result. The resulting merged HTML document will contain all unique nodes from both input documents.
