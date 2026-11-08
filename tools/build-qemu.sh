#!/usr/bin/env bash
set -euo pipefail
ROOT="$PWD"
OUT="$ROOT/qemu-build-output"
mkdir -p "$OUT/binary" "$OUT/sources" "$OUT/licenses" "$OUT/build-info"
git clone --depth 1 --branch v10.2.0 https://github.com/qemu/qemu.git qemu-source
cd qemu-source
test "$(git rev-parse HEAD)" = '698104725efad4b29079d857dfdebbd804e34c99'
git rev-parse HEAD > "$OUT/build-info/qemu-commit.txt"
mkdir -p python/wheels "$OUT/sources/build-wheels"
curl --fail --location 'https://files.pythonhosted.org/packages/36/f4/77b5a41d53ae3c530b204ed9a35137171b20f8ee0f337e2ec4993d6026a5/pycotap-1.3.1-py3-none-any.whl' -o python/wheels/pycotap-1.3.1-py3-none-any.whl
echo '1c3a25b3ff89e48f4e00f1f71dbbc1642b4f65c65d416524d07e73492fff25ea  python/wheels/pycotap-1.3.1-py3-none-any.whl' | sha256sum --check
cp python/wheels/*.whl "$OUT/sources/build-wheels/"
git archive --format=tar --prefix=qemu-10.2.0/ HEAD | gzip > "$OUT/sources/qemu-10.2.0.tar.gz"
cp COPYING COPYING.LIB LICENSE "$OUT/licenses/"
mkdir build && cd build
../configure --enable-download --without-default-features --disable-system --disable-user --enable-tools --disable-docs --disable-guest-agent --disable-plugins --disable-install-blobs 2>&1 | tee "$OUT/build-info/configure.log"
ninja qemu-img.exe 2>&1 | tee "$OUT/build-info/build.log"
cp qemu-img.exe "$OUT/binary/"
cp config-host.mak config.log meson-info/intro-buildoptions.json "$OUT/build-info/" || true
./qemu-img.exe --version | tee "$OUT/build-info/version.txt"
./qemu-img.exe --help > "$OUT/build-info/formats.txt"
pacman -Q > "$OUT/build-info/all-build-packages.txt"
gcc --version > "$OUT/build-info/compiler.txt"
cd "$ROOT"
cp tools/build-qemu.sh tools/collect-qemu-sources.py "$OUT/build-info/"

