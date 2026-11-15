#!/usr/bin/env bash
# SessionStart hook of the repository (.claude/settings.json): gives the agent's commands the identity of the agent's
# own GitHub account, so that the user is neither the author nor a participant of what the agent does on GitHub
# (.speckit: gestion-projet/depot/repository.md). The user's account stays the active one of gh for everything else.
#
# Bash on purpose: only a bash hook receives CLAUDE_ENV_FILE, the file whose assignments Claude Code applies to the
# commands it runs later in the session (Bash and PowerShell tools alike, as long as each line is a plain assignment).
set -u

readonly agent_account='Tetram76-bot'

# git asks gh for its GitHub credentials in this repository, so that a push follows GH_TOKEN like any gh command:
# the agent's token in the agent's commands, the active gh account (the user's) everywhere else. The empty entry
# resets the helpers of lower-priority configuration files (Git Credential Manager), which ignore GH_TOKEN.
helper='!gh auth git-credential'
repo="${CLAUDE_PROJECT_DIR:-.}"
if [[ "$(git -C "$repo" config --local --get-all credential.https://github.com.helper 2>/dev/null)" != $'\n'"$helper" ]]; then
  git -C "$repo" config --local --replace-all credential.https://github.com.helper ''
  git -C "$repo" config --local --add credential.https://github.com.helper "$helper"
fi

[[ -n "${CLAUDE_ENV_FILE:-}" ]] || exit 0

# The token stays in the credential store of the workstation, where `gh auth login` put it; it is written nowhere in
# the repository. Without it, an invalid token makes every GitHub command of the agent fail, rather than silently
# fall back on the user's account.
token=$(gh auth token --hostname github.com --user "$agent_account" 2>/dev/null) || token=''
if [[ -z "$token" ]]; then
  token='missing-agent-account-token'
  # The standard output of a SessionStart hook reaches the agent's context.
  echo "No gh token for $agent_account: the agent's GitHub commands will fail until 'gh auth login' adds it."
fi
echo "export GH_TOKEN=$token" >> "$CLAUDE_ENV_FILE"
