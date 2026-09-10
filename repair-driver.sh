#!/usr/bin/env bash
set -euo pipefail
if [ "$(id -u)" -ne 0 ]; then
  exec sudo bash "$0" "$@"
fi
if [ "$(hostname)" != ai-machine ] || [ "$(uname -r)" != 6.8.0-138-generic ]; then
  echo 'Identitas mesin atau kernel berubah. Hentikan dan lakukan audit ulang.' >&2
  exit 1
fi
package=linux-modules-nvidia-595-open-6.8.0-138-generic
backup_dir=/var/backups/local-coding-agent/driver-$(date -u +%Y%m%dT%H%M%SZ)
install -d -m 700 "$backup_dir"
dpkg-query -W > "$backup_dir/packages-before.txt"
apt-mark showmanual > "$backup_dir/manual-packages-before.txt"
cp -a /etc/modprobe.d /etc/modules-load.d "$backup_dir/"
if [ -f /etc/default/grub ]; then cp -a /etc/default/grub "$backup_dir/grub-before"; fi
apt-get -s install "$package" > "$backup_dir/apt-plan.txt"
cat "$backup_dir/apt-plan.txt"
# Refuse any removal other than the obsolete NVIDIA module identified in audit.
if awk '/^Remv / && $2 != "linux-modules-nvidia-595-open-6.8.0-124-generic" {bad=1} END {exit !bad}' "$backup_dir/apt-plan.txt"; then
  echo 'Rencana APT menghapus paket lain. Tidak ada instalasi dijalankan.' >&2
  exit 1
fi
printf '\nCadangan: %s\n' "$backup_dir"
# Keep APT confirmation interactive. Do not restart unrelated services or reboot.
NEEDRESTART_MODE=l apt-get install "$package"
modprobe nvidia
nvidia-smi | tee "$backup_dir/nvidia-smi-after.txt"
dpkg-query -W > "$backup_dir/packages-after.txt"
printf '\nDriver berhasil dimuat. Kembali ke Codex agar implementasi dilanjutkan.\n'
