#!/usr/bin/env bash
# Puts the binary of docker/scout-action where the action looks for it first, downloaded with the token of the
# workflow (GH_TOKEN). The action downloads it without the token (docker/scout-action#116), so under the anonymous
# limit of the runner's address, which GitHub-hosted runners share with every other user: a 403 once others have spent
# it, that no re-run on the same address clears (gestion-projet/depot/integration-continue-ci.md).
#
# The version is the one of the action the job pinned, read from the copy the runner fetched: nothing here repeats the
# pin, which a version update could then miss.
set -euo pipefail

actions="$(dirname "$RUNNER_WORKSPACE")/_actions/docker/scout-action"
shopt -s nullglob
copies=("$actions"/*/)
if [[ ${#copies[@]} -ne 1 ]]; then
  echo "::error::Expected one copy of docker/scout-action under $actions, found ${#copies[@]}."
  exit 1
fi
action="${copies[0]}"

version=$(grep -oP 'const version = "\Kv[0-9]+\.[0-9]+\.[0-9]+(?=")' "${action}index.js")

gh release download "$version" --repo docker/scout-action --pattern docker-scout-action_linux_amd64 \
  --dir "${action}dist" --clobber
echo "docker/scout-action $version: binary downloaded with the token into ${action}dist"
