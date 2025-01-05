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
## Pumping Lemma Across Language Classes

### Regular Language Pumping Lemma

#### Definition
For any regular language L, there exists a pumping length p > 0 such that any string s ∈ L where |s| ≥ p can be divided into three parts s = xyz where:
1. |y| > 0
2. |xy| ≤ p
3. For all i ≥ 0, xyⁱz ∈ L

##### Example Proof: L = {aⁿbⁿ | n ≥ 0} is not regular
1. Assume L is regular
2. Let p be the pumping length
3. Consider s = aᵖbᵖ ∈ L
4. By the pumping lemma, s = xyz where |y| > 0 and |xy| ≤ p
5. This means y = aᵏ for some k > 0
6. When we pump i = 2: xyyz = aᵖ⁺ᵏbᵖ
7. This string has unequal numbers of a's and b's, so it's not in L
8. Contradiction! Therefore L is not regular

### Context-Free Language Pumping Lemma

#### Definition
For any context-free language L, there exists a pumping length p > 0 such that any string s ∈ L where |s| ≥ p can be divided into five parts s = uvxyz where:
1. |vy| > 0
2. |vxy| ≤ p
3. For all i ≥ 0, uvⁱxyⁱz ∈ L

##### Example Proof: L = {aⁿbⁿcⁿ | n ≥ 0} is not context-free
1. Assume L is context-free
2. Let p be the pumping length
3. Consider s = aᵖbᵖcᵖ ∈ L
4. By the pumping lemma, s = uvxyz where |vy| > 0 and |vxy| ≤ p
5. Due to |vxy| ≤ p, vxy can contain at most two different letters
6. Case 1: If v and y contain only a's and/or b's
   - Pumping will create a string with too many a's and b's
7. Case 2: If v and y contain only b's and/or c's
   - Pumping will create a string with too many b's and c's
8. Case 3: If v and y contain a's and c's
   - Pumping will create a string with unequal numbers
9. All cases lead to contradiction! Therefore L is not context-free

## Recursively Enumerable Languages

### Key Differences
- I don't think it gets it.
- No pumping lemma exists for recursively enumerable languages
- Can't use pumping lemma to prove a language is not recursively enumerable
- Need other techniques like diagonalization or reduction

### Alternative Tools
1. Rice's Theorem
2. Reduction from Halting Problem
3. Mapping Reduction
4. Many-one Reduction

## Practice Problems

### Problem 1: Prove L = {ww | w ∈ {a,b}*} is not regular
1. Assume L is regular with pumping length p
2. Consider s = aᵖbᵖaᵖbᵖ ∈ L
3. By pumping lemma, s = xyz where |y| > 0 and |xy| ≤ p
4. This means y must consist only of a's
5. When pumped (i = 2), we get a string not in form ww
6. Contradiction! L is not regular

### Problem 2: Prove L = {aⁿbᵐcᵏ | n > m > k > 0} is not context-free
1. Assume L is context-free with pumping length p
2. Consider s = aᵖ⁺²bᵖ⁺¹cᵖ ∈ L
3. By pumping lemma, s = uvxyz where |vy| > 0 and |vxy| ≤ p
4. Due to |vxy| ≤ p, v and y together can't contain all three letters
5. Case analysis shows all pumping possibilities violate n > m > k
6. Contradiction! L is not context-free

## Common Patterns in Pumping Lemma Proofs

### For Regular Languages
1. Choose string length ≥ p carefully
2. Focus on y being within first p characters
3. Look for cases where repetition breaks counting requirements

### For Context-Free Languages
1. Choose string where |vxy| ≤ p matters
2. Case analysis based on what letters appear in v and y
3. Look for violations of multiple counting relationships

### Tips for Successful Proofs
1. Start with assumption language is in desired class
2. Choose witness string carefully
3. Use pigeonhole principle for case analysis
4. Show all cases lead to contradiction
5. Conclude language is not in desired class

In this example, the `MergeHtmlDocs` function merges the two input HTML documents and returns the merged result. The resulting merged HTML document will contain all unique nodes from both input documents.
