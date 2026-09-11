# Panduan penerusan dan pembaruan Agent Lokal

## Status dan arsitektur

Dibangun 11 September 2026. Aplikasi WinForms C#/.NET Framework di Windows mengirim snapshot ZIP proyek melalui OpenSSH ke server Ubuntu. integration/windows_bridge.py membuat workspace tersendiri, menjalankan agent, lalu mengirim result.zip dengan diff dan hash file awal. Windows menampilkan perubahan dan hanya menulisnya setelah tombol Terapkan. Rollback memeriksa bahwa file belum diedit lagi.

Launcher agent menggunakan file lock agar satu tugas berjalan. Ia memulai Hermes resmi pada endpoint llama.cpp 127.0.0.1:11435/v1. Sandbox Docker non-root memiliki mount workspace, tanpa jaringan/GPU/socket Docker. MCP lokal memakai stdio: pencarian SearXNG, pembacaan HTML, dan database pengalaman SQLite/FTS. save_lesson menjalankan unittest independen sebelum status verified; nol tes tidak dianggap berhasil.

## Peta source

- windows-client/AgentLokal.cs: form, SSH/SCP, ZIP, diff, hash, apply/rollback, flags pengujian.
- windows-client/Connection.cs: konfigurasi, profil koneksi terenkripsi dan dialog koneksi.
- windows-client/Build-Windows.ps1 dan Install-AgentLokal.ps1: kompilasi dan instalasi per-user.
- agent: preflight model, lingkungan bersih, antrean flock, pemanggilan Hermes.
- integration/run_hermes.py: audit hook yang menolak koneksi eksternal dari proses inferensi.
- integration/windows_bridge.py: protokol snapshot Windows melalui SSH, jobs dan penghentian.
- integration/mcp_server.py: MCP, pembaca HTML, pencarian, pengalaman dan bukti tes.
- compose.yaml: inference dan SearXNG; Dockerfile.sandbox: lingkungan eksekusi proyek.
- config/hermes.yaml dan config/SOUL.md: konfigurasi dan instruksi runtime.
- scripts/: setup server yang sudah ada, unduh/checksum model, backup.
- docs/validation.md: status pengujian; evidence/: bukti historis termasuk kegagalan.
- local-agent.ps1/.cmd dan validation.md di root: artefak CLI/laporan awal; gunakan Windows native dan docs/validation.md sebagai acuan terbaru.

## Lingkungan terpasang

Server: kevin@192.168.201.238, root proyek /home/kevin/local-coding-agent.
Windows: C:\Users\novri\Applications\AgentLokal\AgentLokal.exe; shortcut Desktop Agent Lokal.lnk.
Instalasi bersifat spesifik server ini: alamat, username, dan beberapa path masih hardcoded. Jangan mengklaim setup sebagai installer universal.

Model Qwen3.5-4B Q4_K_M distribusi unsloth, revisi e87f176479d0855a907a41277aca2f8ee7a09523. Checksum/provenance di evidence/qwen-gguf-provenance.json. llama.cpp b10884, CUDA12.8.1, image dipatok dalam compose. Hermes revisi 67764dc0863349a384c16425e73ee8571f3a94b7; dependency constraints requirements.lock.txt.

Ollama dan Open WebUI lama dipertahankan. Konfigurasi Ollama ada di /home/kevin/ai-stack/docker-compose.yml, di luar repository. Konfigurasi SearXNG berjalan di state/searxng dan termasuk backup privat, bukan source publik. setup.sh ditujukan menjalankan ulang pada server yang telah disiapkan; untuk server baru perlu audit, driver/GPU Docker, konfigurasi SearXNG dan kredensial SSH tersendiri.

## Pembaruan Windows

1. Edit source di windows-client. Pertahankan kompatibilitas compiler .NET Framework C# lama (tanpa sintaks C# terbaru).
2. Masuk ke direktori windows-client lalu jalankan powershell -NoProfile -ExecutionPolicy Bypass -File .\Build-Windows.ps1. Compiler: C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe.
3. Jalankan AgentLokal.exe --self-test, periksa self-test.txt. Untuk UI, --ui-smoke. Jangan menganggap exit code 0 saja membuktikan koneksi.
4. Untuk perubahan alur tugas, jalankan AgentLokal.exe --workflow-test "PATH_FIXTURE_KOSONG"; ini mengirim proyek ke server, menerapkan dan rollback. Periksa workflow-validation.txt, manifest, transcript, dan tes proyek hasil model.
5. Pastikan aplikasi tidak sedang mengerjakan tugas. Salin EXE baru ke lokasi instalasi; jangan timpa konfigurasi/kunci. Installer tanpa argumen hanya memperbarui EXE/shortcut. Argumen KeyPath/KnownHostsPath untuk penyediaan awal, bukan pembaruan rutin.
6. Periksa target shortcut dan buka aplikasi secara normal dari Desktop, di luar proses pengujian Codex. Pastikan host terisi dan koneksi bekerja.
7. Buat ZIP hanya dengan EXE, installer dan README; jangan sertakan settings.json atau file kunci. Profil terenkripsi tetap kredensial dan tidak boleh masuk repository/rilis.

## Insiden penting

Shortcut awal ternyata mengarah ke LocalCache paket Codex meski skrip memakai AppData. Aplikasi yang dibuka pengguna tidak membaca konfigurasi dan hanya menawarkan impor. Perbaikan: instal ke UserProfile\Applications\AgentLokal dan dahulukan settings.json di sebelah EXE. Tes SSH dari Codex saja gagal mendeteksi insiden ini. Jangan mengulang klaim siap tanpa memeriksa pembukaan biasa.

Granite 3B gagal memperbaiki fixture. Qwen berhasil pada pembuatan proyek, dua perbaikan bug, memori lintas sesi, dan MCP. Tool-search dinonaktifkan karena model salah memilih alat; nama MCP lengkap ditulis dalam SOUL. Schema/nama alat penting bagi model kecil. Instruksi web yang samar pernah menghabiskan batas 20 iterasi; keandalan universal belum terbukti.

Ollama menaruh embedding masukan di CPU walaupun melaporkan 100% GPU. GGUF Qwen dari Ollama juga tidak kompatibel dengan loader llama.cpp. Karena itu digunakan runtime pendamping dan distribusi GGUF yang checksum-nya dipatok. Jangan menimpa blob Ollama atau menghapus model lama.

Docker Hermes tidak digunakan ulang lintas proses untuk mencegah mount workspace tertukar. Reader HTML mengelompokkan paragraf dan konteks tetangga; mengambil fragmen teks saja pernah menghasilkan kata assertEqual berulang tanpa penjelasan.

## Pembaruan server dan pemulihan

Jalankan backup sebelum deployment. Jangan menimpa state/. Commit/push source tidak otomatis menerapkan aplikasi di Windows atau restart server. Baca diff; ubah/restart hanya layanan terkait. .venv/work/hermes-source tidak masuk Git; setup mempertahankan revision pin. Health-check memeriksa endpoint dan GPU, bukan acceptance lengkap.

Uji dengan fixture terpisah di /home/kevin/projects/local-agent-acceptance atau folder baru. Perintah agent membutuhkan workspace dan query-file absolut. Periksa diff dan tes secara independen; jangan mempercayai ringkasan model saja.

Restart layanan pernah diuji, pelajaran bertahan. Host reboot, PC fisik kedua, semua fase tombol stop, serta toolchain Windows/Node belum diuji menyeluruh. Catat status ini dengan jujur.

State penting: state/experience.sqlite, state/hermes, state/windows; workspace Windows di /home/kevin/projects/windows/JOB. Backup mengecualikan model yang dapat diunduh ulang. Pemulihan mengikuti README. Jangan jalankan docker compose down -v. Jangan hapus snapshot, backup, atau sesi milik pengguna secara otomatis.

## Cakupan pengembangan berikutnya

Prioritas pengguna: Windows mudah dipakai dan mudah pindah laptop/PC. Perbaikan bernilai berikutnya adalah pengaturan server LAN langsung dalam UI, tes koneksi saat startup, dan penyediaan koneksi PC baru yang sederhana. Saat ini ekspor/impor ada, tetapi pengguna tidak ingin itu menjadi prasyarat penggunaan biasa. Belum ada sinkronisasi proyek antar-PC otomatis.

Jangan hardcode password atau membagikan private key dalam paket demi mempermudah onboarding. Kredensial harus disediakan lewat mekanisme lokal yang jelas. Library/model pihak ketiga mempertahankan lisensinya; proyek ini belum menetapkan lisensi khusus atas kode integrasi.
