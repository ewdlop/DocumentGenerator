def merge_multiple_markdown_with_extra_hashes(markdown_inputs):
    """
    Merges multiple markdown inputs, adding an extra '#' to each header, and prepending a new top-level header.

    Args:
        markdown_inputs: A list of strings, where each string is a markdown content.

    Returns:
        A string containing the merged markdown content with extra hashes.
    """

    merged_lines = ["# Merged Markdown Content"]

    for markdown_content in markdown_inputs:
        lines = markdown_content.splitlines()
        for line in lines:
            stripped_line = line.lstrip()
            if stripped_line.startswith("#"):
                header_level = len(stripped_line.split(" ")[0])
                new_header = "#" * (header_level + 1) + line[line.find("#")+header_level:]
                merged_lines.append(new_header)
            else:
                merged_lines.append(line)
        merged_lines.append("") #add an empty line between files.

    return "\n".join(merged_lines).rstrip('\n') #remove trailing empty lines.
