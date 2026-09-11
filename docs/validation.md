# Validasi Agent Lokal — Windows dahulu

Tanggal: 11 September 2026. Aplikasi Windows telah terpasang di
C:\Users\novri\Applications\AgentLokal\AgentLokal.exe, dengan shortcut Agent Lokal di Desktop.

## Hasil

| Kriteria | Status | Bukti |
|---|---|---|
| Aplikasi Windows native | Lulus | Kompilasi C#/.NET; UI smoke; Windows build 26200 |
| Kirim proyek lewat SSH → model lokal → tinjau hasil | Lulus | Dua uji workflow nyata, agent membuat greet.py dan test_greet.py |
| Terapkan hasil dan rollback file Windows | Lulus | Hash/snapshot dipulihkan dalam workflow-test; file asli hanya diubah saat Terapkan |
| Profil koneksi terenkripsi | Lulus pada uji komponen | AES-256-CBC + HMAC-SHA256, PBKDF2 200.000 iterasi; roundtrip dan tamper rejection |
| Pindah ke PC fisik kedua | Belum teruji | Fitur impor tersedia; tidak ada PC kedua dalam pengujian |
| Coding oleh model lokal | Lulus | Proyek salam, dua unittest lulus melalui Hermes |
| Perbaikan bug | Lulus | mean() awalnya 2 gagal/1 lulus; agent memperbaiki implementasi; 3 lulus, file tes tidak diubah |
| MCP nyata | Lulus | Protokol stdio dan pemanggilan find_lessons/save_lesson/search_docs/read_page dari Hermes |
| Pengalaman lintas sesi | Lulus | Sesi baru mengambil lesson id 1, lalu memperbaiki fixture terkait; tes kembali lulus |
| Validasi sebelum verified | Lulus | MCP menjalankan unittest independen; workspace tanpa tes mendapat failed; versi Python/paket dicatat |
| Seluruh bobot/cache inferensi di GPU | Lulus berdasarkan log runtime | CUDA model 2603,50 MiB, KV 1062,50 MiB, recurrent state 50,25 MiB; 33/33 layer; tidak ada model buffer CPU |
| Konteks efektif | Lulus | Server 64.000 token/1 slot; masukan nyata 40.835 token mengembalikan kode awal yang benar |
| Inferensi lokal | Lulus pada konfigurasi dan pemeriksaan jalur | Semua provider/auxiliary lokal; fallback nonaktif; audit hook menolak DNS/koneksi eksternal proses inferensi |
| Restart layanan dan persistensi | Lulus | Inference dan SearXNG restart, health pulih, isi database pelajaran sama |
| Startup setelah reboot host | Belum teruji | Docker restart policy persisten; host tidak direboot |
| Pencarian internet | Lulus dengan keterbatasan | SearXNG mengembalikan dokumentasi; engine DuckDuckGo mengalami error TLS yang dilaporkan |
| Pembacaan dokumentasi dan halaman contoh | Lulus | Agent memanggil read_page, paragraf assertEqual terbaca, kode ANGGREK-5060 ditemukan; URL/waktu tersimpan |
| Browser visual/halaman JavaScript | Bukan implementasi versi ini | Memakai pembaca HTML lokal tanpa GPU |
| Proyek Node atau aplikasi Windows-native | Belum teruji | Sandbox awal Python; toolchain tambahan dapat ditambahkan di server |
| Tombol Hentikan pada semua fase transfer/generasi | Belum diuji menyeluruh | Implementasi pembatalan tersedia; Ctrl+C CLI telah menghentikan uji yang salah arah |

## Konfigurasi terukur

Server Ubuntu 24.04.4, 10 vCPU, RAM 15,61 GiB, swap 0; satu RTX 5060 8151 MiB.
Driver NVIDIA 595.84. Ollama 0.22.1 tetap tersedia, cloud dimatikan dan model lama dipertahankan.
Hermes 0.21.1 revisi 67764dc0863349a384c16425e73ee8571f3a94b7.
Runtime llama.cpp b10884, image CUDA 12.8.1 dipatok digest
3307a6cd90a76127de7dd960c020d5cf55c65c7b202b725f29895a67416c3021.

Model Qwen3.5-4B Q4_K_M, Apache-2.0, distribusi GGUF unsloth revisi
e87f176479d0855a907a41277aca2f8ee7a09523. SHA256 file:
00fe7986ff5f6b463e62455821146049db6f9313603938a70800d1fb69ef11a4.
Paket Ollama model yang sama tidak dapat dimuat oleh loader llama.cpp karena perbedaan metadata;
file Ollama asli tidak dimodifikasi. [Catatan kompatibilitas upstream](https://github.com/ggml-org/llama.cpp/pull/25334).

Opsi: semua tensor CUDA, fit off, load-mode none, lazy-mode off, Flash Attention on,
cache q8_0, cache-ram 0, reasoning off, context 64000, parallel 1.
Buffer host untuk output/komputasi normal tetap ada; bukan bobot atau cache KV/recurrent.
Pada uji panjang, nvidia-smi mencatat 4242 MiB GPU terpakai; RAM sistem used 2.529.124.352 byte,
RAM available 14.233.055.232 byte, swap 0. Pembacaan file model awal memakai RAM/page cache normal.

Uji structured tool call: 0,762 detik total, 44 token output, 88,19 token/detik decode.
Uji panjang: 14,529 detik total; 40.835 token prompt; 16 token output; recall MERPATI-742 benar.
Angka ini adalah pengukuran contoh tertentu, bukan janji kecepatan semua tugas.

## Keterbatasan yang ditemukan

Granite 3B gagal pada acceptance perbaikan awal; bukti kegagalan dipertahankan. Qwen berhasil
pada dua fixture perbaikan dan pembuatan proyek. Pemilihan alat web sempat berulang salah dan
mencapai batas 20 iterasi. Konfigurasi akhir menampilkan schema alat langsung dan SOUL memakai
nama MCP lengkap. Penggunaan nama alat lengkap pada instruksi web berhasil memanggil alatnya.
Ini belum membuktikan keandalan untuk semua proyek atau semua instruksi bebas.

Pembaca HTML menyimpan URL/waktu dan memilih paragraf beserta konteks tetangga. Tidak menjalankan
JavaScript. Jejak alat, bukan ringkasan model saja, dipakai sebagai bukti. Tidak ada fine-tuning.

Aplikasi Windows memiliki batas snapshot 2.000 file / 100 MB, maksimal 5 MB per file.
Tidak menyinkronkan folder antar-PC otomatis. Ekspor koneksi memindahkan akses server,
sedangkan model/memori tetap di server. Folder proyek disalin terpisah.

## Lokasi bukti dan operasi

Di server: /home/kevin/local-coding-agent/evidence, config, scripts, integration, windows-client.
Jejak Windows: state/windows/JOB/transcript.txt dan result.zip. Memori: state/experience.sqlite.
Log lokal/cadangan Windows: %LOCALAPPDATA%\AgentLokal\state.

Jalankan health-check untuk kondisi terkini. scripts/setup.sh mempertahankan state dan menggunakan
versi dependensi yang dikunci. scripts/backup.sh mengarsip konfigurasi/state tanpa model yang bisa
diunduh ulang dan diverifikasi checksum. Backup telah berhasil dibuat. Prosedur rollback di README server.
Repository hanya lokal, tidak dipublikasikan. Tidak ada password SSH/sudo dalam paket Windows atau source.

Paket portabel: AgentLokal-Windows.zip. Source: AgentLokal-Windows-Source.zip.
Panduan pengguna: PANDUAN-WINDOWS.md.

Perbaikan instalasi Windows: shortcut sebelumnya mengarah ke cache Codex dan konfigurasi tidak terbaca pada pembukaan biasa. Instalasi dipindahkan ke folder Applications pengguna; aplikasi mendahulukan settings.json di sebelah EXE. UI smoke menunjukkan host terisi dan SSH memakai kunci lokasi baru berhasil.

