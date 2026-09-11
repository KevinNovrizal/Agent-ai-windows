# Agent Coding Lokal

Agent berjalan di `ai-machine` melalui SSH, memakai Hermes Agent 0.21.1 dan model Granite 4.1 3B Q4_K_M melalui Ollama. Cloud Ollama dinonaktifkan, antrean inferensi satu per satu, dan workspace dijalankan dalam container non-root tanpa jaringan.

## Windows

Salin `local-agent.ps1` dan `local-agent.cmd` ke folder kerja. Jalankan:

```powershell
.\local-agent.cmd C:\proyek\saya -q "Perbaiki bug dan jalankan tes" --oneshot
```

Launcher mengirim isi workspace ke server, menjalankan Hermes dengan output bertahap melalui SSH, lalu mengambil perubahan kembali. Kunci SSH berada di `work/server_access_user`; jangan dibagikan.

## Server

```bash
cd /home/kevin/local-coding-agent
./health-check
./agent /home/kevin/workspaces/nama-proyek
docker compose ps
```

Pencarian dokumentasi memakai SearXNG `127.0.0.1:8888`. MCP lokal menyediakan `search_docs`, `read_page`, `find_lessons`, `save_lesson`, dan `mark_obsolete`. Memori berada di `state/experience.sqlite`; halaman web disimpan di `state/web` dengan URL dan waktu pengambilan.

## Validasi dan batasan

Granite menghasilkan structured tool call bahasa Indonesia dan memproses konteks 64.000 token. `ollama ps` melaporkan 100% GPU; model weights dan KV cache berada di CUDA. Ollama tetap menyimpan embedding/input buffer CPU sekitar 201 MiB karena runtime ini memang memakainya; syarat absolut seluruh bobot/cache di VRAM belum terpenuhi. Runtime llama.cpp eksperimental belum diaktifkan karena image CUDA terbaru memerlukan driver 13.3, sementara driver server 13.2.

Driver NVIDIA 595.84 terpasang untuk RTX 5060 8 GiB; kernel baru 6.8.0-139 tersedia tetapi server masih berjalan di 6.8.0-138. Jangan reboot otomatis. Backup compose ada di `backups/`. Startup setelah reboot dan acceptance penuh oleh model lokal masih belum teruji.

Jangan menaruh password, token, private key, atau `.env` ke Git. Untuk rollback konfigurasi Ollama, salin kembali file dari `backups/` lalu jalankan `docker compose up -d` di `/home/kevin/ai-stack`.
