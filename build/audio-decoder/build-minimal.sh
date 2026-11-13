#!/bin/sh
# Developer-only Windows x64 cross-build; no system package installation.
set -eu
cache=$1
output=$2
work=/tmp/dtmapi-audio-decoder-92611900bb-audio1
toolchain="$work/toolchain"
source="$work/source"
build="$work/build"
mkdir -p "$toolchain" "$source" "$build" "$output"
(cd "$cache" && sha256sum -c inputs.sha256)
if [ ! -f "$toolchain/.extracted" ]; then
    while read -r checksum package; do
        case "$package" in toolchain-packages/*.deb) dpkg-deb -x "$cache/$package" "$toolchain" ;; esac
    done < "$cache/inputs.sha256"
    touch "$toolchain/.extracted"
fi
if [ ! -f "$source/configure" ]; then
    tar -xzf "$cache/ffmpeg-sources.tar.gz" -C "$source" --strip-components=1
fi
export PATH="$toolchain/usr/bin:$PATH"
compiler="$toolchain/usr/bin/x86_64-w64-mingw32-gcc-posix"
"$compiler" --version > "$output/compiler.txt"
nasm -v >> "$output/compiler.txt"
cd "$build"
sh "$source/configure" \
    --target-os=mingw32 --arch=x86_64 --enable-cross-compile \
    --cross-prefix="$toolchain/usr/bin/x86_64-w64-mingw32-" \
    --cc="$compiler" --extra-ldflags=-static \
    --disable-autodetect --disable-everything \
    --disable-gpl --disable-nonfree --disable-version3 \
    --disable-network --disable-avdevice --disable-swscale \
    --disable-doc --disable-debug --disable-ffplay --disable-ffprobe \
    --disable-pthreads --enable-w32threads --enable-small \
    --enable-ffmpeg --enable-protocol=file,pipe \
    --enable-demuxer=wav,mp3,flac,aac,mov \
    '--enable-decoder=pcm_*,adpcm_*,aac,aac_fixed,aac_latm,mp1,mp1float,mp2,mp2float,mp3,mp3float,mp3adu,mp3adufloat,mp3on4,mp3on4float,flac,alac' \
    --enable-parser=aac,aac_latm,flac,mpegaudio \
    --enable-encoder=pcm_s16le --enable-muxer=pcm_s16le \
    --enable-filter=aresample --extra-version=dtmapi-audio1 \
    > "$output/configure.log" 2>&1
make -j8 ffmpeg.exe > "$output/build.log" 2>&1
cp ffmpeg.exe "$output/ffmpeg.exe"
cp "$source/COPYING.LGPLv2.1" "$output/LICENSE.txt"
"$toolchain/usr/bin/x86_64-w64-mingw32-objdump" -p ffmpeg.exe > "$output/pe-imports.txt"
