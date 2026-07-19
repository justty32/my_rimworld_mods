#!/usr/bin/env python3
import os, sys, json, urllib.request

key = os.environ.get("DEEPSEEK_API_KEY")
if not key:
    print("NO_KEY", file=sys.stderr); sys.exit(2)
prompt = open(sys.argv[1], encoding="utf-8").read()
body = json.dumps({
    "model": "deepseek-chat",
    "messages": [{"role": "user", "content": prompt}],
    "stream": False,
    "temperature": 1.0,
}).encode("utf-8")
req = urllib.request.Request(
    "https://api.deepseek.com/chat/completions",
    data=body,
    headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"},
)
with urllib.request.urlopen(req, timeout=180) as r:
    resp = json.load(r)
print(resp["choices"][0]["message"]["content"])
