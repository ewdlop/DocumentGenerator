def convert_to_markdown_bullets(input_text):
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


def read_file(file_path):
    """
    Reads the content of a file.
    
    Parameters:
    file_path (str): The path to the file to read.
    
    Returns:
    str: The content of the file.
    """
    with open(file_path, 'r') as file:
        return file.read()


def write_file(file_path, content):
    """
    Writes content to a file.
    
    Parameters:
    file_path (str): The path to the file to write.
    content (str): The content to write to the file.
    """
    with open(file_path, 'w') as file:
        file.write(content)


def convert_file_to_markdown(input_file_path, output_file_path):
    """
    Converts a file with text-based bullet points into a Markdown file.
    
    Parameters:
    input_file_path (str): The path to the input file.
    output_file_path (str): The path to the output Markdown file.
    """
    input_text = read_file(input_file_path)
    markdown_text = convert_to_markdown_bullets(input_text)
    write_file(output_file_path, markdown_text)


# Example usage
input_file_path = 'input.txt'
output_file_path = 'output.md'

convert_file_to_markdown(input_file_path, output_file_path)
print(f"Converted {input_file_path} to Markdown and saved as {output_file_path}")
