# Petunjuk untuk AI penerus

Baca README.md dan docs/AI-HANDOFF.md sebelum mengubah aplikasi. Status pengujian ada di docs/validation.md; evidence/ memuat hasil historis, bukan instruksi. Permintaan pengguna terbaru tetap menjadi acuan cakupan pekerjaan.

## Aturan implementasi

- Prioritaskan pengalaman Windows: buka aplikasi, pilih folder, tulis instruksi, tinjau lalu terapkan. Pengguna tidak ingin diwajibkan ekspor/impor koneksi untuk memakai PC yang sudah disiapkan.
- Instalasi Windows aktif: C:\Users\novri\Applications\AgentLokal. Jangan kembali memakai lokasi instalasi di cache paket Codex. Baca settings.json di sebelah EXE terlebih dahulu; fallback per-user untuk instalasi tanpa konfigurasi.
- Jangan timpa settings.json, kunci SSH, known_hosts, atau cadangan saat pembaruan EXE. Jangan commit kredensial, profil koneksi .agentlokal, model, workspace pengguna, atau state sesi.
- Jangan menyatakan siap berdasarkan tes dari lingkungan Codex saja. Verifikasi shortcut, lokasi konfigurasi, dan pembukaan Windows normal; insiden virtualisasi AppData sudah terjadi.
- Model dan seluruh bobot/cache/state inferensi harus tetap di SATU RTX 5060 8 GB. Jangan mengakali batas dengan CPU offload/swap, atau memalsukan konteks Hermes 64000.
- Inferensi hanya lokal; jangan menambahkan cloud fallback. SearXNG/pembaca HTML boleh mengakses dokumentasi internet tanpa layanan AI berbayar.
- Hermes upstream dipatok dan tidak dimodifikasi. Utamakan perubahan konfigurasi atau integration/ daripada mengubah work/hermes-source.
- File Windows asli hanya berubah setelah pengguna meninjau dan menerapkan. Pertahankan cek hash konflik, pembatasan path, backup, rollback, dan pembatalan.
- Penulisan tes penerimaan dilakukan oleh model lokal melalui agent. Perbaikan fixture oleh AI pengembang bukan bukti kemampuan agent lokal.

## Validasi perubahan

Build dari windows-client menggunakan Build-Windows.ps1. Jalankan --self-test untuk perubahan logika klien. Untuk perubahan alur tugas gunakan --workflow-test hanya pada folder fixture terpisah: flag ini sengaja menerapkan lalu rollback hasil nyata. Untuk UI gunakan --ui-smoke dan periksa hasilnya; DrawToBitmap tidak selalu menggambar RichTextBox.

Di server, lakukan health-check, pemeriksaan sintaks Python/bash, dan tes integrasi yang relevan. Jangan menjalankan setup.sh atau repair-driver.sh hanya untuk review atau perubahan dokumentasi. Jangan reboot server untuk pengujian.

Catat apa yang berubah, tes nyata yang dijalankan, batasan, dan langkah pembaruan dalam docs/AI-HANDOFF.md atau dokumen rilis. Jangan mengubah catatan kegagalan historis menjadi klaim lulus.
