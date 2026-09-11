#!/usr/bin/env bash
set -euo pipefail
ROOT=/home/kevin/local-coding-agent
cd "$ROOT"
exec 9>state/agent.lock
flock 9
stamp=$(date -u +%Y%m%dT%H%M%SZ)
mkdir -p backups
chmod 700 backups
# No inference background review is active; agent lock gives consistent SQLite state.
tar --exclude=state/models -czf "backups/project-$stamp.tar.gz" config state integration agent health-check compose.yaml Dockerfile.sandbox scripts README.md requirements.lock.txt evidence docs windows-client .dockerignore .gitignore
chmod 600 "backups/project-$stamp.tar.gz"
echo "$ROOT/backups/project-$stamp.tar.gz"
