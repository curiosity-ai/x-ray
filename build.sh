#!/usr/bin/env bash
# Builds the documentation site in docs/ into .build/ with Neko. Used locally; the docs pipeline
# (.devops/build-docs.yml) runs build.bat, which does the same on Windows.
#   Install the tool once with: dotnet tool update --global Neko
set -euo pipefail
export PATH="$PATH:$HOME/.dotnet/tools"
cd "$(dirname "$0")"

# The .NET global tool installs its executable as `Neko`; some environments also expose a
# lowercase `neko`. Resolve whichever exists, so this works on case-sensitive filesystems.
neko="$(command -v neko || command -v Neko || true)"
[ -n "$neko" ] || { echo "Neko CLI not found on PATH (looked for 'neko' and 'Neko'). Install it with: dotnet tool update --global Neko" >&2; exit 1; }

"$neko" build -i docs -o .build "$@"
