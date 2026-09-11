#!/usr/bin/env bash
set -euo pipefail
ROOT=/home/kevin/local-coding-agent
cd "$ROOT"
test "$(id -u)" -ne 0 || { echo 'Jalankan sebagai kevin, bukan root.'; exit 1; }
nvidia-smi >/dev/null || { echo 'Driver GPU belum siap. Jalankan audit/perbaikan driver dahulu.'; exit 1; }
command -v docker >/dev/null
if ! python3 -m venv --help >/dev/null; then echo 'Pasang python3-venv terlebih dahulu.'; exit 1; fi
mkdir -p work state/hermes evidence backups
if [ ! -e state/hermes/config.yaml ]; then ln -s ../../config/hermes.yaml state/hermes/config.yaml; fi
if [ ! -e state/hermes/SOUL.md ]; then cp config/SOUL.md state/hermes/SOUL.md; fi
chmod 700 state backups
REV=67764dc0863349a384c16425e73ee8571f3a94b7
if [ ! -d work/hermes-source/.git ]; then
  git clone https://github.com/NousResearch/hermes-agent.git work/hermes-source
  git -C work/hermes-source checkout "$REV"
fi
test "$(git -C work/hermes-source rev-parse HEAD)" = "$REV" || { echo 'Revisi Hermes berbeda; tidak menimpa checkout yang ada.'; exit 1; }
if [ ! -x .venv/bin/python ]; then python3 -m venv .venv; fi
.venv/bin/pip install -c requirements.lock.txt -e './work/hermes-source[mcp]'
docker image inspect local-coding-sandbox:1 >/dev/null 2>&1 || docker build -f Dockerfile.sandbox -t local-coding-sandbox:1 .
docker compose -f /home/kevin/ai-stack/docker-compose.yml up -d --no-deps ollama
docker exec ollama ollama show qwen3.5:4b >/dev/null 2>&1 || docker exec ollama ollama pull qwen3.5:4b
# Config/state are retained. Download the pinned compatible GGUF when absent.
./scripts/download-model.sh
docker compose up -d
for i in $(seq 1 60); do
  if ./health-check >work/setup-health.json; then cat work/setup-health.json; exit 0; fi
  sleep 5
done
cat work/setup-health.json
echo 'Layanan belum sehat; baca docker compose logs.' >&2
exit 1

