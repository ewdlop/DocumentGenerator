def convert_to_markdown_bullets(text):
    """
    Converts text-based bullet points into Markdown bullet points.
    
    Parameters:
    text (str): The input text containing bullet points.
    
    Returns:
    str: The converted text with Markdown bullet points.
    """
    lines = text.split('\n')
    markdown_lines = []
    
    for line in lines:
        stripped_line = line.strip()
        if stripped_line and not stripped_line.startswith('- '):
            markdown_lines.append(f"- {stripped_line}")
        else:
            markdown_lines.append(line)
    
    return '\n'.join(markdown_lines)


def convert_text_to_markdown(input_text):
    """
    Converts text-based bullet points into Markdown bullet points.
    
    Parameters:
    input_text (str): The input text containing bullet points.
    
    Returns:
    str: The converted text with Markdown bullet points.
    """
    lines = input_text.split('\n')
    markdown_lines = []

    for line in lines:
        stripped_line = line.strip()
        if stripped_line and not stripped_line.startswith('- '):
            markdown_lines.append(f"- {stripped_line}")
        else:
            markdown_lines.append(line)
    
    return '\n'.join(markdown_lines)


# Example usage
input_text = """First point
Second point
Third point"""

converted_text = convert_to_markdown_bullets(input_text)
print(converted_text)
