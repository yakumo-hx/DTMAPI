# DTMAPI 音频解码器构建

玩家使用随 Runtime、AudioPrepare 或 SDK 携带的固定 Windows x64 解码器，无需安装 FFmpeg、WSL 或编译工具。只启用 WAV、MP3、FLAC、AAC/M4A 所需的读取、解码、采样转换和 PCM16 管道输出；没有 MP3/FLAC 编码器、网络、视频或自动响度处理。

开发端从仓库运行 `build/build-audio-decoder.ps1`。需要 x64 Ubuntu WSL、make、dpkg-deb、tar 和 sha256sum；脚本把固定 MinGW/NASM 包解压到临时构建目录，不安装系统软件。源码版本 `92611900bb`、工具链 URL 和裁剪参数见 `build/audio-decoder.json` 与 `build-minimal.sh`；所有下载输入由 `inputs.sha256` 固定。输出在 `.tools/audio-decoder/8.1.3-dtmapi-audio1/`，保留 configure、compiler、build 和 PE imports 记录。

独立源码材料包含未经修改的 `ffmpeg-sources.tar.gz`、LGPL 2.1 许可、构建入口、配置及输入哈希。准确源码也可从 https://github.com/FFmpeg/FFmpeg/archive/92611900bb.tar.gz 获取；该完整源码材料与本地二进制一起提供，但不塞入玩家运行 ZIP。构建未启用 GPL、nonfree、version3 或外部编解码库。

不依赖仓库的重建方式：将源码压缩包、`inputs.sha256` 放入一个 cache 目录；按 `audio-decoder.json` 的 URL 下载七个固定 `.deb` 到 cache 下的 `toolchain-packages/`；在上述 Ubuntu 环境执行 `sh build-minimal.sh /absolute/cache /absolute/output`。脚本先验证全部哈希，再使用同一裁剪配置编译。分发准备只复制 ffmpeg.exe、LICENSE.txt、distribution.json；完整源码材料独立提供。
