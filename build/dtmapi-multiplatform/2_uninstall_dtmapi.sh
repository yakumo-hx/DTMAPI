#!/usr/bin/env bash
set -u

script_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd -P)" || exit 1
host="$script_dir/Content/DTMAPIInstaller/hosts/linux-x64/dtmapi-installer"

if [[ ! -f "$host" ]]; then
    printf '%s\n' "[ERROR] DTMAPI Linux installer is missing: $host" >&2
    exit 127
fi
if [[ ! -x "$host" ]] && ! chmod u+x -- "$host" 2>/dev/null; then
    printf '%s\n' '[ERROR] Cannot mark the DTMAPI Linux installer executable.' >&2
    printf '%s\n' 'If this Workshop location is mounted with noexec, copy the complete package to an executable local folder and try again.' >&2
    exit 126
fi

"$host" uninstall "$@"
exit_code=$?
if [[ $exit_code -eq 126 ]]; then
    printf '%s\n' '[ERROR] The Workshop filesystem appears to be mounted noexec.' >&2
    printf '%s\n' '[错误] Workshop 文件系统可能禁止执行（noexec）。' >&2
    printf '%s\n' 'Copy the complete Workshop package to an executable local folder; do not copy only the host or extract the bundled ZIP.' >&2
    printf '%s\n' '请把整个 Workshop 包复制到允许执行的本地文件夹；不要只复制安装器，也不要解压包内 ZIP。' >&2
fi
exit "$exit_code"
