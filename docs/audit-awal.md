# Audit awal local coding agent

Tanggal audit: 10 September 2026. Status: TERHALANG DRIVER/SUDO; sistem agent belum dibuat dan belum siap dipakai.

## Temuan terukur
- SSH ke kevin@192.168.201.238 berhasil dengan kunci khusus; hostname ai-machine.
- Ubuntu 24.04.4 LTS, kernel aktif 6.8.0-138-generic.
- 10 vCPU QEMU, RAM 15 GiB, swap 0, disk / dan /home tersedia 74 GiB (hasil df -h).
- PCI NVIDIA 10de:2d05 terdeteksi. Nama GPU/VRAM belum dapat diverifikasi dengan nvidia-smi.
- nvidia-smi gagal berkomunikasi dengan driver; modinfo nvidia: Module nvidia not found.
- Paket driver pengguna 595.71.05 terpasang. Modul NVIDIA tersedia untuk beberapa kernel lama hingga 6.8.0-136, tetapi tidak untuk kernel aktif 6.8.0-138.
- Docker container ollama memakai ollama/ollama:latest; image digest sha256:3ca37ec2b9cb6341b62554074205c616778fe98abcf9e4fc50361b79a07407ae. Binary dalam container menunjukkan client version 0.22.1.
- Container ollama berhenti dengan error nvidia-container-cli: initialization error: nvml error: driver not loaded. Restart policy always; port host 11434; volume ai-stack_ollama ke /root/.ollama.
- Container open-webui aktif dan healthy pada port 3000; tidak diubah.
- Compose yang ada: /home/kevin/ai-stack/docker-compose.yml. Isinya belum dibaca; dapat berisi rahasia.
- Port listening lain: 22, 43009 loopback, 10248 loopback, 53 loopback, 10250.
- sudo -n true gagal: a password is required.

## Perbaikan yang perlu dilakukan
Pasang modul NVIDIA untuk kernel yang sedang dipakai. Rujukan resmi: https://ubuntu.com/server/docs/nvidia-drivers-installation/

Simulasi `apt-get -s install linux-modules-nvidia-595-open-6.8.0-138-generic` pada saat audit merencanakan 16 paket diperbarui, 2 dipasang, dan 1 dihapus. Paket yang dihapus adalah linux-modules-nvidia-595-open-6.8.0-124-generic. Versi driver baru dalam transaksi adalah 595.84. Ini belum dijalankan. Rencana APT dapat berubah setelah indeks paket berubah.

Perlu password sudo melalui terminal pengguna dan keputusan pengguna terhadap penghapusan modul lama sebelum eksekusi. Jangan menggunakan Docker privileged sebagai pengganti akses sudo. Jangan reboot atau mengubah akses SSH.

Setelah instalasi berhasil, coba `sudo modprobe nvidia` dan `nvidia-smi` tanpa reboot. Kemudian Codex dapat memeriksa resource dan memulihkan container Ollama yang ada, dengan backup sebelum perubahan konfigurasi.

## Status kriteria penerimaan
| Kriteria | Status |
|---|---|
| Koneksi SSH dan identifikasi OS/resource | Lulus |
| GPU tersedia untuk inferensi | Gagal |
| Ollama berjalan dan endpoint bisa digunakan | Gagal |
| Model, kuantisasi, lisensi, tool calling, konteks panjang | Belum teruji |
| Semua bobot dan cache inferensi di VRAM | Belum teruji |
| Pemilihan Hermes atau agent alternatif | Belum teruji |
| CLI, alat file/terminal, pembatasan workspace | Belum teruji |
| SearXNG, pembaca web, MCP nyata | Belum teruji |
| Memori persisten lintas sesi | Belum teruji |
| Acceptance coding dan perbaikan bug oleh model lokal | Belum teruji |
| Larangan inferensi cloud dan pemulihan layanan | Belum teruji |
| Startup setelah reboot | Belum teruji |

## Perubahan yang telah dilakukan
Izin kunci SSH lokal diperbaiki. Binary Ollama disalin ke /tmp/local-agent-ollama-version-check untuk memeriksa versinya; tidak menjalankan inferensi. Laporan ini disimpan dalam proyek Git lokal server. Tidak ada paket, konfigurasi layanan, model, atau data lama yang dihapus/diubah.

## Langkah lanjutan
Selesaikan penghalang driver terlebih dahulu, lalu lanjutkan instruksi lengkap prompt-codex-agent-lokal.md dari uji kelayakan model sebelum membangun integrasi besar. Pertahankan Ollama dan layanan lain yang sudah ada. Tidak ada angka performa model yang diukur pada audit ini.
