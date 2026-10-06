#!/usr/bin/env bash
# Prints the digest of <repository>:<tag> on Docker Hub, or nothing when the tag does not exist.
# Only a registry 404 means "absent": any other failure (authentication, rate limit, network)
# fails the script, so that an unknown state is never mistaken for a missing tag.
# Usage: DOCKERHUB_TOKEN=... registry-digest.sh <repository> <tag>
set -euo pipefail
repository=$1
tag=$2

token=$(curl -fsS -u "tetram76:$DOCKERHUB_TOKEN" \
  "https://auth.docker.io/token?service=registry.docker.io&scope=repository:$repository:pull" | jq -er .token)
headers=$(mktemp)
status=$(curl -sS -I -o /dev/null -D "$headers" -w '%{http_code}' \
  -H "Authorization: Bearer $token" \
  -H "Accept: application/vnd.docker.distribution.manifest.v2+json, application/vnd.oci.image.manifest.v1+json, application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.oci.image.index.v1+json" \
  "https://registry-1.docker.io/v2/$repository/manifests/$tag")
case "$status" in
  200) tr -d '\r' < "$headers" | awk 'tolower($1) == "docker-content-digest:" { print $2 }' ;;
  404) ;;
  *) echo "::error::Cannot tell whether $repository:$tag exists (HTTP $status)." >&2; exit 1 ;;
esac
