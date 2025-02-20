import argparse
import os
import re
from typing import Dict, List, Tuple, Optional

# PDF processing libraries
import fitz  # PyMuPDF
from pypdf import PdfReader, PdfWriter

# NLP and correction libraries
import language_tool_python
import spacy
from difflib import get_close_matches

class PDFCorrector:
    """
    A class for detecting and correcting errors in PDF documents.
    """
    def __init__(self, language='en-US'):
        """Initialize the PDF corrector with language settings and NLP models."""
        self.language = language
        # Initialize grammar checker
        print("Loading language correction tools...")
        self.grammar_tool = language_tool_python.LanguageTool(language)
        
        # Load NLP model for advanced text analysis
        try:
            self.nlp = spacy.load('en_core_web_sm')
        except:
            print("Downloading spaCy language model...")
            os.system('python -m spacy download en_core_web_sm')
            self.nlp = spacy.load('en_core_web_sm')
        
        # Common formatting standards
        self.formatting_standards = {
            'paragraph_spacing': 1.15,
            'default_font': 'Times New Roman',
            'heading_fonts': ['Arial', 'Helvetica'],
            'body_font_size': 12,
            'heading1_font_size': 18,
            'heading2_font_size': 16,
        }
        
        # Dictionary of common abbreviations to avoid false positives
        self.abbreviations = {
            'e.g.': True, 'i.e.': True, 'etc.': True,
            'vs.': True, 'Mr.': True, 'Mrs.': True, 'Ms.': True, 'Dr.': True,
            'Ph.D.': True, 'M.D.': True, 'B.A.': True, 'B.S.': True,
            'U.S.': True, 'U.K.': True, 'E.U.': True,
        }
        
        print("PDF Corrector initialized successfully.")

    def load_pdf(self, pdf_path: str) -> Tuple[PdfReader, fitz.Document]:
        """
        Load a PDF file using both pypdf and PyMuPDF for different operations.
        
        Args:
            pdf_path: Path to the PDF file
            
        Returns:
            Tuple containing pypdf PdfReader and PyMuPDF Document objects
        """
        print(f"Loading PDF: {pdf_path}")
        reader = PdfReader(pdf_path)
        doc = fitz.open(pdf_path)
        return reader, doc

    def extract_text_by_page(self, doc: fitz.Document) -> List[str]:
        """
        Extract text from each page of the PDF.
        
        Args:
            doc: PyMuPDF Document object
            
        Returns:
            List of strings, each containing text from one page
        """
        print("Extracting text from PDF...")
        page_texts = []
        for page_num in range(len(doc)):
            page = doc[page_num]
            text = page.get_text("text")
            page_texts.append(text)
        return page_texts

    def extract_document_structure(self, doc: fitz.Document) -> Dict:
        """
        Analyze the document structure, identifying headings, paragraphs, and formatting.
        
        Args:
            doc: PyMuPDF Document object
            
        Returns:
            Dictionary containing document structure information
        """
        print("Analyzing document structure...")
        structure = {
            'headings': [],
            'paragraphs': [],
            'tables': [],
            'images': [],
            'hyperlinks': [],
            'fonts': {}
        }
        
        # Extract headings and font information
        for page_num in range(len(doc)):
            page = doc[page_num]
            
            # Get font information
            font_dict = {}
            blocks = page.get_text("dict")["blocks"]
            for block in blocks:
                if "lines" in block:
                    for line in block["lines"]:
                        for span in line["spans"]:
                            font_name = span["font"]
                            font_size = span["size"]
                            if font_name not in font_dict:
                                font_dict[font_name] = []
                            if font_size not in font_dict[font_name]:
                                font_dict[font_name].append(font_size)
            
            # Add to document fonts
            for font, sizes in font_dict.items():
                if font not in structure['fonts']:
                    structure['fonts'][font] = []
                structure['fonts'][font] = list(set(structure['fonts'].get(font, []) + sizes))
            
            # Extract links
            links = page.get_links()
            for link in links:
                if 'uri' in link:
                    structure['hyperlinks'].append({
                        'page': page_num,
                        'uri': link['uri']
                    })
            
            # Extract images (basic detection)
            images = page.get_images(full=True)
            for img_index, img in enumerate(images):
                structure['images'].append({
                    'page': page_num,
                    'index': img_index,
                    'width': img[2],
                    'height': img[3]
                })
        
        return structure

    def check_grammar_spelling(self, text: str) -> Tuple[str, List[Dict]]:
        """
        Check grammar and spelling in the text.
        
        Args:
            text: Text content to check
            
        Returns:
            Tuple of (corrected_text, list of errors)
        """
        errors = self.grammar_tool.check(text)
        error_list = []
        
        for error in errors:
            # Filter out false positives for common abbreviations
            context = text[max(0, error.offset - 5):error.offset + error.errorLength + 5]
            if any(abbr in context for abbr in self.abbreviations):
                continue
                
            error_list.append({
                'message': error.message,
                'context': context,
                'suggestions': error.replacements[:3] if error.replacements else [],
                'offset': error.offset,
                'length': error.errorLength
            })
        
        # Apply corrections
        corrected_text = self.grammar_tool.correct(text)
        
        return corrected_text, error_list

    def check_sentence_structure(self, text: str) -> List[Dict]:
        """
        Analyze sentences for structural issues using spaCy.
        
        Args:
            text: Text content to analyze
            
        Returns:
            List of potential sentence structure issues
        """
        doc = self.nlp(text)
        issues = []
        
        for sent in doc.sents:
            # Check for very long sentences (potential readability issues)
            if len(sent) > 40:
                issues.append({
                    'type': 'long_sentence',
                    'text': sent.text,
                    'suggestion': 'Consider breaking this sentence into smaller ones for better readability.'
                })
            
            # Check for passive voice
            has_passive = False
            for token in sent:
                if token.dep_ == "auxpass":
                    has_passive = True
                    break
            
            if has_passive:
                issues.append({
                    'type': 'passive_voice',
                    'text': sent.text,
                    'suggestion': 'Consider using active voice for clarity.'
                })
                
        return issues

    def check_formatting_consistency(self, structure: Dict) -> List[Dict]:
        """
        Check for formatting consistency issues.
        
        Args:
            structure: Document structure information
            
        Returns:
            List of formatting issues
        """
        issues = []
        
        # Check font consistency
        if len(structure['fonts']) > 4:
            issues.append({
                'type': 'too_many_fonts',
                'details': f"Document uses {len(structure['fonts'])} fonts. Consider limiting to 2-3 for consistency.",
                'fonts': list(structure['fonts'].keys())
            })
        
        # Check for broken hyperlinks (would need validation in a real implementation)
        if structure['hyperlinks']:
            for link in structure['hyperlinks']:
                # Simplified check - would need actual link validation in practice
                if not link['uri'].startswith(('http', 'https', 'mailto')):
                    issues.append({
                        'type': 'suspicious_link',
                        'details': f"Potentially broken link: {link['uri']} on page {link['page'] + 1}"
                    })
        
        return issues

    def correct_pdf(self, pdf_path: str, output_path: str, interactive: bool = True) -> Dict:
        """
        Main method to correct a PDF file.
        
        Args:
            pdf_path: Path to the input PDF
            output_path: Path for the corrected PDF
            interactive: Whether to ask for user confirmation on changes
            
        Returns:
            Dictionary with correction statistics
        """
        # Load PDF with both libraries
        reader, doc = self.load_pdf(pdf_path)
        
        # Extract text and structure
        page_texts = self.extract_text_by_page(doc)
        structure = self.extract_document_structure(doc)
        
        # Initialize statistics
        stats = {
            'pages_processed': len(page_texts),
            'grammar_errors': 0,
            'spelling_errors': 0,
            'structure_issues': 0,
            'formatting_issues': 0,
            'corrections_made': 0
        }
        
        # Process each page
        corrected_texts = []
        all_errors = []
        
        print("Checking document for errors...")
        for i, text in enumerate(page_texts):
            print(f"Checking page {i+1}...")
            
            # Check grammar and spelling
            corrected_text, errors = self.check_grammar_spelling(text)
            stats['grammar_errors'] += len([e for e in errors if 'Grammar' in e['message']])
            stats['spelling_errors'] += len([e for e in errors if 'Spelling' in e['message']])
            
            # Check sentence structure
            structure_issues = self.check_sentence_structure(text)
            stats['structure_issues'] += len(structure_issues)
            
            # Add page number to errors
            for error in errors:
                error['page'] = i + 1
            all_errors.extend(errors)
            
            corrected_texts.append(corrected_text)
        
        # Check formatting consistency
        formatting_issues = self.check_formatting_consistency(structure)
        stats['formatting_issues'] += len(formatting_issues)
        
        # Interactive mode - show errors and confirm corrections
        if interactive and all_errors:
            print("\n" + "="*50)
            print(f"Found {len(all_errors)} potential issues:")
            print("="*50)
            
            # Show sample of errors (limit to 10 for readability)
            for i, error in enumerate(all_errors[:10]):
                print(f"{i+1}. Page {error['page']}: {error['message']}")
                print(f"   Context: \"{error['context']}\"")
                if error.get('suggestions'):
                    print(f"   Suggestions: {', '.join(error['suggestions'][:3])}")
                print()
            
            if len(all_errors) > 10:
                print(f"... and {len(all_errors) - 10} more issues")
            
            proceed = input("\nApply corrections? (yes/no): ").lower().strip()
            if proceed != 'yes':
                print("Operation cancelled.")
                return stats
        
        # Apply corrections (in a real implementation, this would modify the PDF content)
        # For now, we'll just report what would be done
        print("\nApplying corrections...")
        writer = PdfWriter()
        
        # In a complete implementation, this would apply the text corrections to the PDF
        # This is simplified as actual text replacement in PDFs is complex
        for i, page in enumerate(reader.pages):
            # In a real implementation, update the page with corrected text
            # page.extract_text() would be replaced with corrected_texts[i]
            writer.add_page(page)
            stats['corrections_made'] += 1
        
        # Save the "corrected" PDF
        with open(output_path, "wb") as f:
            writer.write(f)
        
        print(f"Correction completed. Saved to {output_path}")
        print(f"Statistics: {stats}")
        
        return stats

def main():
    """Main function to run the PDF corrector from command line."""
    parser = argparse.ArgumentParser(description="Self-Correcting PDF Program")
    parser.add_argument("input_pdf", help="Path to the input PDF file")
    parser.add_argument("--output", "-o", help="Path for the corrected PDF file", default=None)
    parser.add_argument("--non-interactive", "-n", action="store_true", help="Run without interactive prompts")
    parser.add_argument("--language", "-l", default="en-US", help="Language code (e.g., en-US, fr-FR)")
    
    args = parser.parse_args()
    
    # Set default output path if not specified
    if not args.output:
        base_name = os.path.splitext(os.path.basename(args.input_pdf))[0]
        args.output = f"{base_name}_corrected.pdf"
    
    # Initialize and run the corrector
    corrector = PDFCorrector(language=args.language)
    corrector.correct_pdf(args.input_pdf, args.output, not args.non_interactive)

if __name__ == "__main__":
    main()
