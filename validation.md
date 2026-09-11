# Laporan validasi (11 September 2026)

Lulus: SSH, audit Ubuntu 24.04, RTX 5060/8.151 MiB, driver 595.84, Ollama 0.22.1, mode cloud off, SearXNG lokal, sandbox Docker non-root, Hermes 0.21.1 terpasang, Granite 4.1 3B Q4_K_M structured tool call, konteks 64K (48.044 token masukan; kode awal dipulihkan), dan pencarian SearXNG (29 hasil).

Terukur: tool call 138 token/detik setelah pemuatan; pemuatan awal 138 detik. Pada konteks 64K, model weights CUDA 1.998 GiB, KV cache CUDA 2.656 GiB, 41/41 layer GPU; CPU_Mapped input buffer 200.98 MiB. RAM terpakai sekitar 2.6 GiB saat uji; swap 0.

Gagal/belum teruji: driver runtime llama.cpp yang memaksa seluruh tensor (image kompatibel belum selesai), acceptance end-to-end Hermes memperbaiki fixture, MCP melalui loop agent, restart persistence, reboot, Playwright browser MCP, dan SearXNG engine DuckDuckGo (TLS error). Hermes background review dimatikan agar tidak ada inferensi cloud.

Fixture bug baseline: 3 tes, 2 gagal. Tidak diperbaiki oleh evaluator ini. `save_lesson` hanya menandai verified jika unittest nyata berjalan dan lulus.
