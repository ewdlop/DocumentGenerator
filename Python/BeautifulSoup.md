# BeautifulSoup

You can use **Natural Language Processing (NLP)** to tag parts of speech in an HTML document and apply different colors accordingly. Below is a **Python script** that processes an HTML file, tags words with their parts of speech (POS) using `spaCy`, and wraps them in `<span>` tags with corresponding colors.

---

### Steps:
1. **Extract text** from the HTML document.
2. **Tag each word** with its part of speech.
3. **Wrap words** with `<span>` elements styled based on their POS.
4. **Reinsert** the styled text back into the HTML.

---

### Python Script:
```python
import spacy
from bs4 import BeautifulSoup

# Load spaCy's English model
nlp = spacy.load("en_core_web_sm")

# Define a color mapping for parts of speech
POS_COLORS = {
    "NOUN": "blue",
    "VERB": "red",
    "ADJ": "green",
    "ADV": "purple",
    "PRON": "brown",
    "DET": "orange",
    "ADP": "gray",
    "CONJ": "pink",
    "NUM": "gold",
    "PART": "cyan",
    "PUNCT": "black",
    "SYM": "black",
    "X": "black"
}

def colorize_text(text):
    """Apply POS-based coloring to text"""
    doc = nlp(text)
    colored_text = ""
    
    for token in doc:
        color = POS_COLORS.get(token.pos_, "black")  # Default to black
        colored_text += f'<span style="color: {color};">{token.text}</span> '
    
    return colored_text.strip()

def process_html(file_path):
    """Process an HTML file, colorizing words based on their POS"""
    with open(file_path, "r", encoding="utf-8") as file:
        soup = BeautifulSoup(file, "html.parser")
    
    # Process text within <p> tags (can be extended to other elements)
    for tag in soup.find_all(["p", "span", "div"]):
        if tag.string:
            tag.string.replace_with(BeautifulSoup(colorize_text(tag.string), "html.parser"))

    # Save the modified HTML
    output_path = "colored_output.html"
    with open(output_path, "w", encoding="utf-8") as file:
        file.write(str(soup))

    print(f"Processed HTML saved as {output_path}")

# Run the script on an HTML file
process_html("example.html")
```

---

### Expected Output:
This script **modifies the input HTML file** by wrapping words with `<span>` tags and applying **color styles** based on their **part of speech**.

For example, a sentence like:

```html
<p>The quick brown fox jumps over the lazy dog.</p>
```
will be transformed into:
```html
<p>
    <span style="color: orange;">The</span>
    <span style="color: green;">quick</span>
    <span style="color: brown;">brown</span>
    <span style="color: blue;">fox</span>
    <span style="color: red;">jumps</span>
    <span style="color: gray;">over</span>
    <span style="color: orange;">the</span>
    <span style="color: green;">lazy</span>
    <span style="color: blue;">dog</span>.
</p>
```

---

### Improvements & Enhancements:
1. **Support More Elements:** Extend beyond `<p>` tags to **headings, lists, and tables**.
2. **Customizable Colors:** Allow users to configure their own **POS-color mapping**.
3. **Live Webpage Processing:** Convert this script into a **browser extension** or **Flask web app** for real-time HTML processing.

Would you like me to refine this further or integrate it into a **GitHub Actions workflow**? 🚀
