#!/usr/bin/env bash
set -euo pipefail
cd /home/kevin/local-coding-agent
mkdir -p state/models
file=state/models/Qwen3.5-4B-Q4_K_M.gguf
if [ ! -f "$file" ]; then
  curl -fL --retry 3 -o "$file.part" https://huggingface.co/unsloth/Qwen3.5-4B-GGUF/resolve/e87f176479d0855a907a41277aca2f8ee7a09523/Qwen3.5-4B-Q4_K_M.gguf
  mv "$file.part" "$file"
fi
python3 - <<'PY'
import hashlib,json,pathlib
p=pathlib.Path('state/models/Qwen3.5-4B-Q4_K_M.gguf')
expected=json.loads(pathlib.Path('evidence/qwen-gguf-provenance.json').read_text())['sha256']
h=hashlib.sha256()
with p.open('rb') as f:
    for chunk in iter(lambda:f.read(8*1024*1024),b''):h.update(chunk)
if h.hexdigest()!=expected: raise SystemExit('Model checksum mismatch')
print('Model verified')
PY
