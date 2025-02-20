# Self-Correcting PDF Program: Usage Guide

This guide outlines how to use the Self-Correcting PDF Program to automatically detect and correct errors in PDF documents.

## Installation Requirements

Before using the program, ensure you have the following dependencies installed:

```bash
pip install PyMuPDF pypdf language-tool-python spacy
python -m spacy download en_core_web_sm
```

## Basic Usage

### Command Line Interface

The program can be run from the command line:

```bash
python pdf_corrector.py input.pdf -o corrected.pdf
```

### Options

- `input.pdf`: Path to the PDF file you want to correct
- `-o, --output`: Path for the corrected PDF (default: original_name_corrected.pdf)
- `-n, --non-interactive`: Run without interactive prompts
- `-l, --language`: Language code (e.g., en-US, fr-FR) for text correction

## Features

The program performs the following corrections:

### Text-Based Error Detection and Correction

- **Spelling errors**: Identifies and corrects misspelled words
- **Grammar issues**: Detects and fixes grammatical problems
- **Sentence structure**: Analyzes and suggests improvements for awkward phrasing
- **Readability**: Identifies overly complex sentences and passive voice

### Formatting Consistency Checks

- **Font usage**: Detects inconsistent font usage across the document
- **Hyperlink validation**: Identifies potentially broken links
- **Visual elements**: Reports on image placement and quality issues

## Interactive Mode

By default, the program runs in interactive mode, which:

1. Analyzes the entire document
2. Displays a summary of detected issues
3. Allows you to review and approve corrections before they're applied

## Example Output

When running the program, you'll see output similar to:

```
Loading PDF: document.pdf
PDF Corrector initialized successfully.
Extracting text from PDF...
Analyzing document structure...
Checking document for errors...
Checking page 1...
Checking page 2...

==================================================
Found 15 potential issues:
==================================================
1. Page 1: Possible spelling mistake found
   Context: "the documnet contains"
   Suggestions: document, documents, documented

2. Page 1: Grammar error: A comma is missing
   Context: "However if we consider"
   Suggestions: However, if we consider

...

Apply corrections? (yes/no): yes

Applying corrections...
Correction completed. Saved to corrected.pdf
Statistics: {'pages_processed': 2, 'grammar_errors': 8, 'spelling_errors': 7, 'structure_issues': 3, 'formatting_issues': 2, 'corrections_made': 2}
```

## Advanced Usage

### Integration with Other Tools

The `PDFCorrector` class can be imported and used in other Python applications:

```python
from pdf_corrector import PDFCorrector

corrector = PDFCorrector(language='en-US')
stats = corrector.correct_pdf(
    'document.pdf',
    'corrected.pdf',
    interactive=False
)
print(f"Corrections made: {stats['corrections_made']}")
```

### Batch Processing

For processing multiple PDFs:

```python
import os
from pdf_corrector import PDFCorrector

corrector = PDFCorrector()
pdf_dir = 'documents/'
output_dir = 'corrected_documents/'

os.makedirs(output_dir, exist_ok=True)
for filename in os.listdir(pdf_dir):
    if filename.endswith('.pdf'):
        input_path = os.path.join(pdf_dir, filename)
        output_path = os.path.join(output_dir, filename)
        corrector.correct_pdf(input_path, output_path, interactive=False)
```

## Limitations

- Complex PDF modifications like table restructuring are limited
- OCR for scanned documents requires additional setup
- Some layout elements may not be preserved perfectly during correction
- Heavy formatting changes may affect document appearance

## Future Enhancements

Future versions may include:
- AI-driven content suggestions beyond grammar
- Style consistency enforcement
- PDF form field validation
- Multi-language support with improved detection
- Cloud-based processing for larger documents
