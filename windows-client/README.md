# Agent Lokal untuk Windows

Buka AgentLokal.exe, pilih folder proyek, tulis instruksi, lalu klik Jalankan.
Setelah agent selesai, buka Tinjau perubahan. Klik Terapkan perubahan untuk
menulis hasil ke folder Windows. Rollback terakhir mengembalikan file sebelumnya;
rollback ditahan bila file telah diedit lagi.

## Pindah laptop atau PC

1. Di PC yang sudah terhubung, buka Koneksi / pindah PC > Ekspor koneksi.
2. Buat password minimal 12 karakter dan simpan file .agentlokal.
3. Salin ZIP aplikasi ini dan profil ke PC baru. Ekstrak ZIP, buka AgentLokal.exe.
4. Pilih Koneksi / pindah PC > Impor koneksi dan masukkan password ekspor.
5. Klik Tes koneksi. Salin folder proyek Anda secara terpisah dan pilih folder itu.

Tidak perlu mengunduh ulang model. Model dan memori agent berada di server.
Profil berisi kunci koneksi yang dienkripsi; password ekspor tidak disimpan.
Simpan profil dan password secara pribadi. Aplikasi memakai data konfigurasi
per pengguna di %LOCALAPPDATA%\AgentLokal. Kedua PC harus dapat mengakses server
192.168.201.238 melalui jaringan lokal atau VPN yang sudah tersedia.
Aplikasi ini belum memiliki sinkronisasi folder otomatis antar-PC.

## Persyaratan

Windows 10/11, .NET Framework 4.8 dan OpenSSH Client bawaan Windows.
Tidak membutuhkan Python, Node.js, atau aplikasi Codex di PC klien.
Untuk membuat shortcut Desktop, jalankan Install-AgentLokal.ps1 dari PowerShell:

    powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-AgentLokal.ps1

Atau cukup jalankan EXE langsung setelah mengekstrak ZIP.

## Batas versi awal

Satu tugas aktif di server. Proyek dikirim sebagai snapshot melalui SSH.
Batas 2.000 file, total 100 MB, maksimal 5 MB per file. Folder dependensi, .git,
file .env dan jenis kunci umum dikecualikan. Pilih folder proyek yang sesuai;
pengecualian nama file tidak bisa mengenali semua rahasia di dalam source code.
Agent menjalankan perintah proyek dalam sandbox Docker tanpa jaringan.
Dependensi proyek di luar image bawaan mungkin perlu disiapkan di server.
Model kecil dapat membuat kesalahan: tinjau perubahan dan hasil tes.
Hentikan menghentikan pekerjaan; hasil parsial dapat tetap muncul untuk ditinjau.

Log dan cadangan lokal: %LOCALAPPDATA%\AgentLokal\state
Proyek server: /home/kevin/projects/windows
Memori bersama: /home/kevin/local-coding-agent/state/experience.sqlite
