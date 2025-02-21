from fuzzywuzzy import fuzz
from fuzzywuzzy import process

doc1 = """Order ID: 12345
Customer: John Doe
Timestamp: 2025-02-21 10:00:00"""

doc2 = """Order ID: 12345
Customer: John Doe
Timestamp: 2025-02-21 10:05:00"""

similarity = fuzz.ratio(doc1, doc2)

print(f"Document Similarity: {similarity}%")
