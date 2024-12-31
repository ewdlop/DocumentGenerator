# DocumentGenerator

#
Here's how to run LaTeX locally with YAML configuration:

To use this:

```yaml
# latex-config.yaml
latex:
  compiler: pdflatex
  output_dir: ./build
  templates_dir: ./templates
  packages:
    - amsmath
    - amssymb
    - mathtools
    
document:
  title: "Hamiltonian Systems"
  author: "Author Name"
  template: article

# main.tex
\documentclass{article}
\usepackage{amsmath}
\usepackage{amssymb}
\usepackage{mathtools}

\begin{document}
\title{Hamiltonian Systems}
\author{Author Name}
\maketitle

\section{Definitions}
A Hamiltonian system is represented by a function $H: M \to \mathbb{R}$, with evolution dictated by Hamilton's equations.

Canonical coordinates consist of position $q_i$ and momentum $p_i$ pairs.

\end{document}

# build.py
import yaml
import subprocess

def build_latex():
    with open('latex-config.yaml', 'r') as f:
        config = yaml.safe_load(f)
    
    cmd = [
        config['latex']['compiler'],
        '-output-directory',
        config['latex']['output_dir'],
        'main.tex'
    ]
    subprocess.run(cmd)

if __name__ == '__main__':
    build_latex()
```

1. Install TeXLive or MiKTeX

2. Create directory structure with config files

3. Run: python build.py

Requirements:

```

pip install pyyaml

```

The script will compile the LaTeX document according to the YAML configuration.

# dawin

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
