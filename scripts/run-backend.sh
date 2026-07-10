#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

cd "${REPO_ROOT}"

# Disable MSBuild node reuse for this run so stale build hosts are less likely
# to hold locks when the editor's C# tooling is active.
export MSBUILDDISABLENODEREUSE=1

exec dotnet run --project backend/src/SoundLens.Api -nodeReuse:false "$@"
