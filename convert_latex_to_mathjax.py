import re

def convert_latex_to_mathjax(markdown_text):
    """
    Convert LaTeX-style math expressions in markdown to MathJax format.
    Handles both inline and display math modes.
    
    Args:
        markdown_text (str): Input markdown text with LaTeX math expressions
        
    Returns:
        str: Converted text with MathJax delimiters
    """
    # First, handle display math mode \[ ... \]
    def replace_display_math(match):
        content = match.group(1).strip()
        return f"$${content}$$"
    
    # Handle inline math mode \( ... \)
    def replace_inline_math(match):
        content = match.group(1).strip()
        return f"$${content}$$"
    
    # Handle single $ delimiters
    def replace_single_dollar(match):
        content = match.group(1).strip()
        return f"$${content}$$"
        
    # Convert display math mode
    text = re.sub(r'\\\[(.*?)\\\]', replace_display_math, markdown_text, flags=re.DOTALL)
    
    # Convert inline math mode
    text = re.sub(r'\\\((.*?)\\\)', replace_inline_math, text, flags=re.DOTALL)
    
    # Convert single $ delimiters
    text = re.sub(r'\$(.*?)\$', replace_single_dollar, text, flags=re.DOTALL)
    
    return text

# Example usage
if __name__ == "__main__":
    sample_text = """### 1. **Symplectic Manifold**  
A symplectic manifold \\((M, \\omega)\\) is a smooth, even-dimensional space \\(M\\) equipped with a closed, non-degenerate 2-form \\(\\omega\\). It provides the foundational geometric structure for classical mechanics, where:
- Points on \\(M\\) represent states in phase space.
- The 2-form \\(\\omega\\) encodes the relationship between position \\(q_i\\) and momentum \\(p_i\\).

\\[H: M \\to \\mathbb{R}\\]

With single dollars:
$H: M \\to \\mathbb{R}$
"""
    
    converted_text = convert_latex_to_mathjax(sample_text)
    print("Converted text:")
    print(converted_text)
