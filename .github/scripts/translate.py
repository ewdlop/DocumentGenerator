import os
import requests
import json
from google.cloud import translate_v2 as translate

# GitHub API & Google Translate Client
GH_TOKEN = os.getenv("GH_TOKEN")
GOOGLE_TRANSLATE_API_KEY = os.getenv("GOOGLE_TRANSLATE_API_KEY")
GITHUB_REPO = os.getenv("GITHUB_REPOSITORY")
GITHUB_EVENT_PATH = os.getenv("GITHUB_EVENT_PATH")

translate_client = translate.Client.from_service_account_json("google-credentials.json")

# Read GitHub Event Data
with open(GITHUB_EVENT_PATH, "r") as f:
    event_data = json.load(f)

# Get Issue/PR Content
if "issue" in event_data:
    issue_number = event_data["issue"]["number"]
    issue_body = event_data["issue"]["body"]
elif "pull_request" in event_data:
    issue_number = event_data["pull_request"]["number"]
    issue_body = event_data["pull_request"]["body"]
else:
    issue_number, issue_body = None, None

# Function to translate text
def translate_text(text, target_language):
    translation = translate_client.translate(text, target_language=target_language)
    return translation["translatedText"]

if issue_number and issue_body:
    translated_zh_cn = translate_text(issue_body, "zh-CN")
    translated_zh_tw = translate_text(issue_body, "zh-TW")

    comment_body = f"""
    **🌐 Auto-Translated Versions**
    
    **🇨🇳 Simplified Chinese (简体中文):**  
    {translated_zh_cn}
    
    **🇹🇼 Traditional Chinese (繁體中文):**  
    {translated_zh_tw}
    """

    # Post Comment to GitHub
    comment_url = f"https://api.github.com/repos/{GITHUB_REPO}/issues/{issue_number}/comments"
    headers = {"Authorization": f"token {GH_TOKEN}", "Accept": "application/vnd.github.v3+json"}
    requests.post(comment_url, headers=headers, json={"body": comment_body})
