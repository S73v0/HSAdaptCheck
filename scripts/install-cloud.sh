#!/usr/bin/env bash
set -euo pipefail
# Portable core only; no Eclipse, clinical data or Varian DLLs are installed here.
export DOTNET_CLI_HOME=/workspace/tools/dotnet-home
export NUGET_PACKAGES=/workspace/tools/nuget
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
mkdir -p /workspace/tools
if [[ ! -x /workspace/tools/dotnet/dotnet ]]; then
  python3 - <<'SDK'
import hashlib, pathlib, subprocess, tarfile, tempfile
url = "https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.426/dotnet-sdk-8.0.426-linux-x64.tar.gz"
expected = "8141d01bdc53f604aed3bb661b604707a64209f8a947cd81bf95b4f518951593d77864069191f54ea5e84362a409da6b0f69074bf893b283d7e0cf93e2bf20a8"
with tempfile.TemporaryDirectory() as work:
    archive = pathlib.Path(work) / "sdk.tar.gz"
    subprocess.run(["curl", "--fail", "--location", "--silent", "--show-error", url, "--output", str(archive)], check=True)
    digest = hashlib.sha512(archive.read_bytes()).hexdigest()
    if digest.lower() != expected.lower():
        raise RuntimeError("Official SDK SHA-512 mismatch; archive will not be used")
    destination = pathlib.Path("/workspace/tools/dotnet")
    destination.mkdir(parents=True, exist_ok=True)
    with tarfile.open(archive) as package:
        package.extractall(destination, filter="data")
SDK
fi
export PATH="/workspace/tools/dotnet:$PATH"
cd /workspace/HSAdaptCheck
dotnet --version
dotnet restore HSAdaptCheck.sln --locked-mode
dotnet build HSAdaptCheck.sln -c Release --no-restore
dotnet run --project tests/HSAdaptCheck.Tests -c Release --no-build
