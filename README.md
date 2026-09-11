# Agent coding lokal

Proyek: `/home/kevin/local-coding-agent`. Aplikasi Windows: shortcut **Agent Lokal** di Desktop. Sistem ini tidak membutuhkan Codex/ChatGPT atau API AI berbayar untuk penggunaan harian. Server Ubuntu harus menyala dan terjangkau di jaringan lokal. Pencarian dokumentasi memerlukan internet.

## Memakai dari Windows

1. Buka **Agent Lokal**, pilih satu folder proyek, lalu tulis instruksi dalam bahasa Indonesia.
2. Tekan **Jalankan**. Snapshot proyek dikirim melalui SSH ke workspace terisolasi di Ubuntu. Keluaran alat dan jawaban muncul bertahap.
3. Buka **Tinjau perubahan**. Periksa daftar file dan diff, lalu tekan **Terapkan perubahan** untuk menulis hasil ke folder Windows. Versi lama dicadangkan. Jika file berubah di Windows selama agent bekerja, aplikasi menolak penerapan agar perubahan Anda tidak tertimpa.
4. **Rollback terakhir** mengembalikan penerapan terakhir, selama file belum diedit lagi. **Hentikan** membatalkan tugas aktif.

Snapshot mengecualikan `.git`, dependensi/cache umum, `.env`, beberapa nama/ekstensi kredensial umum, file symlink, dan file di atas 5 MB. Maksimum 2.000 file/100 MB. Ini bukan pendeteksi semua rahasia: pilih folder proyek yang memang boleh diproses. Riwayat Git asli tetap berada di Windows. Setiap pengiriman adalah sesi baru; pengalaman teruji tetap tersimpan di server.

Client Windows adalah aplikasi .NET Framework dengan OpenSSH bawaan Windows. Tidak memerlukan Python maupun aplikasi Codex. Konfigurasi dan kunci SSH khusus ada di `%LOCALAPPDATA%\AgentLokal`; password tidak disimpan. Source client disertakan untuk pemeriksaan dan pembangunan ulang.

## Memakai melalui SSH

```bash
ssh kevin@192.168.201.238
mkdir -p /home/kevin/projects/contoh
/home/kevin/local-coding-agent/agent /home/kevin/projects/contoh
```

Untuk satu tugas dari file:

```bash
/home/kevin/local-coding-agent/agent /home/kevin/projects/contoh --oneshot --query-file /path/tugas.txt
```

Contoh instruksi: “Buat modul Python dan unittest untuk konversi suhu, jalankan tes, lalu simpan pelajaran yang benar-benar teruji.” Untuk proyek yang sudah ada: “Baca implementasi dan tes, cari pengalaman relevan, perbaiki bug tanpa mengubah persyaratan tes, lalu verifikasi.”

Gunakan `Ctrl+C` untuk menghentikan CLI. Batas default adalah 20 iterasi dan 600 detik per tugas. Sesi utama diantrekan dengan file lock; runtime hanya memiliki satu slot generasi. Terminal/file tools bekerja dalam Docker sebagai UID 1000, dengan workspace yang dipilih, tanpa GPU atau Docker socket. Lingkungan awal menyediakan Python, unittest, pytest, Git, curl, dan ripgrep. Tambahkan toolchain lain melalui `Dockerfile.sandbox` bila diperlukan; tes Node/Windows-native belum menjadi cakupan validasi awal.

## Inferensi dan batas hardware

Hermes resmi digunakan dengan endpoint OpenAI-compatible **lokal** `http://127.0.0.1:11435/v1`. Proses inferensi Hermes membatasi koneksi Python ke loopback. Semua auxiliary diarahkan ke endpoint utama; fallback cloud dan review latar otomatis dinonaktifkan. Kompresi konteks, bila dibutuhkan, memakai model lokal yang sama. MCP tidak boleh meminta sampling AI.

Ollama yang sudah ada dipertahankan sebagai penyimpanan/pengelola model dan layanan lokal. Runtime pendamping llama.cpp diperlukan karena versi Ollama yang diaudit masih menempatkan embedding masukan di CPU, walaupun menampilkan “100% GPU”. Paket GGUF Ollama Qwen tidak kompatibel dengan loader llama.cpp; runtime pendamping menggunakan Qwen3.5-4B Q4_K_M dari unsloth, revisi dan checksum dicatat di evidence/qwen-gguf-provenance.json. Runtime, memaksa seluruh tensor ke CUDA, mematikan penyesuaian/offload otomatis, dan menempatkan cache KV di GPU. CPU tetap dipakai untuk OS, tokenisasi, penjadwalan, buffer input/output, agent, dan alat. Tidak ada swap untuk memuat model.

Jangan menjalankan model lain di GPU yang sama bersamaan dengan tugas agent. Image, model blob, konteks 64.000, satu slot, dan opsi GPU dipatok di `compose.yaml`. Angka performa serta bukti alokasi tersedia di `docs/validation.md` dan `evidence/`. Startup setelah reboot host tidak diasumsikan telah diuji.

## Web, MCP, dan pengalaman

- `search_docs`: SearXNG lokal pada loopback port 8888. Error mesin pencari/rate limit diteruskan, tanpa fallback vendor berbayar.
- `read_page`: pembaca HTML lokal tanpa GPU. Hasil disimpan dengan URL dan waktu di `state/web/`; hanya bagian terbatas/relevan diberikan ke model. Halaman yang membutuhkan JavaScript kompleks dapat tidak terbaca; ini bukan browser visual penuh.
- `find_lessons`: pencarian SQLite FTS sebelum perbaikan.
- `save_lesson`: menjalankan `python -m unittest discover -v` secara independen dalam container. Hanya hasil sukses dengan jumlah tes lebih dari nol mendapat status `verified`. Kegagalan/timeout mendapat status `failed`. Status ini merupakan bukti tes pada workspace tersebut, bukan jaminan solusi berlaku universal.
- `mark_obsolete`: menandai pelajaran yang tidak berlaku. Koreksi dibuat sebagai pelajaran baru dan diuji kembali.

Database: `state/experience.sqlite`. Metadata mencakup signature, proyek, waktu, uraian percobaan, diff, perintah/hasil tes, dependensi, dan sumber. Ini penyimpanan pengalaman, bukan training/fine-tuning bobot. Memori bawaan Hermes tersedia di `state/hermes/memories/`. Jangan menyimpan rahasia dalam keduanya.

Untuk menambah MCP, tambahkan server **lokal** pada `config/hermes.yaml`, beserta `command`, `args`, `env` eksplisit, timeout, dan `sampling.enabled: false`. Tambahkan `mcp-NAMA` ke toolset CLI lalu mulai sesi baru. Implementasi protokol yang dipakai adalah MCP SDK 2.0.0 (`MCPServer`), bukan fungsi Python yang sekadar diberi nama MCP.

## Operasi, log, backup, dan rollback

```bash
cd /home/kevin/local-coding-agent
./health-check
docker compose logs --tail 100 inference searxng
docker compose restart inference searxng
docker compose stop
docker compose up -d
./scripts/backup.sh
```

`compose.yaml` adalah sumber konfigurasi layanan proyek; `config/hermes.yaml` adalah sumber konfigurasi agent. Konfigurasi layanan Ollama yang sudah ada tetap di `/home/kevin/ai-stack/docker-compose.yml`. Jangan memakai `docker compose down -v`, karena volume model yang ada harus dipertahankan. Open WebUI di port 3000 tidak dikelola oleh proyek ini.

Log Hermes: `state/hermes/logs/` dan database sesi `state/hermes/state.db`. Jejak MCP: `state/mcp-calls.jsonl`. Jejak Windows: `state/windows/JOB/transcript.txt` dan `result.zip`; workspace snapshot di `/home/kevin/projects/windows/JOB`. Hasil/arsip lokal Windows berada di `%LOCALAPPDATA%\AgentLokal\state`. Hapus snapshot lama hanya setelah memastikan tidak dibutuhkan.

`scripts/setup.sh` dapat dijalankan ulang pada server ini tanpa menghapus state/model. Script memeriksa revisi Hermes dan mempertahankan konfigurasi yang sudah ada. Cadangan konfigurasi Ollama sebelum perubahan berada di `backups/`; cadangan paket/driver awal berada di `/var/backups/local-coding-agent/`. Jangan memasukkan direktori state atau cadangan berisi konfigurasi pribadi ke Git.

Rollback proyek: hentikan tugas, buat backup baru, jalankan `docker compose stop`, lalu pulihkan arsip backup proyek yang dipilih ke direktori ini sebagai user kevin. Jalankan `docker compose up -d` dan `./health-check`. Pemulihan konfigurasi Ollama dilakukan terpisah dari salinan sebelum perubahan, lalu recreate hanya layanan `ollama` dengan `--no-deps`. Rollback driver memerlukan pemeriksaan paket/kernel terpasang dan bukan bagian dari rollback file proyek; tidak ada reboot otomatis.

Jika “runtime belum siap”, periksa health/log, versi image CUDA, dan proses GPU. Jika tes gagal, baca hasil alat, bukan hanya ringkasan model. Jika MCP gagal saat startup dingin, timeout koneksi 60 detik telah disediakan; periksa `mcp-stderr.log`. Jika Windows gagal menghubungi server, periksa jaringan LAN dan SSH; tidak ada fallback cloud.


Backup mengecualikan state/models karena model dapat diunduh ulang dengan scripts/download-model.sh dan diverifikasi SHA256. Simpan requirements.lock.txt bersama backup/repository.

Untuk model 4B, tools.tool_search.enabled=off membuat schema alat langsung tersedia dan mengurangi kesalahan pemilihan alat. Docker tidak digunakan ulang lintas proses agar mount workspace tidak tertukar antarproyek.
