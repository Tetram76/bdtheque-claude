---
name: codex-review-loop
description: >-
  Drives a pull request through the mandatory Codex review cycle on this
  repository until Codex approves it. Use whenever a PR on this repo has
  received (or is waiting for) a review from the Codex GitHub bot
  (chatgpt-codex-connector[bot]), before merging, or when the user asks to
  "traiter les retours Codex", "attendre la revue Codex", or similar.
---

# Codex Review Loop

Policy source of truth: `.speckit/gestion-projet.md` → "Revue de code" /
"Revue Codex (bloquante)". This skill is the operational procedure for that
policy: a PR on this repo cannot be merged until the Codex bot
(`chatgpt-codex-connector[bot]`) has approved the current head commit.

Requires `gh` authenticated (`gh auth status`) with access to the repo.

Placeholder convention below: `{owner}` / `{repo}` are literal — `gh api`
auto-expands them from the current repo in REST endpoint paths only. Every
`<ANGLE_BRACKET>` placeholder (`<PR_NUMBER>`, `<COMMENT_ID>`, `<THREAD_ID>`)
is **not** auto-expanded anywhere (REST path, GraphQL body, or `-F`/`-f`
value) and must be substituted with a real value before running the
command.

## Loop

Repeat until Codex approves the current head commit:

```text
1. Wait for a Codex review on the current head commit.
2. For each unresolved review thread from Codex:
   a. Cross-check the finding (see "Cross-check" below).
   b. If valid: make the fix in one dedicated commit.
   c. Reply to the thread (fix applied, or justified rejection).
   d. Resolve the thread.
3. Push the fix commits (if any were made).
4. If commits were pushed, go to 1 (Codex re-reviews automatically).
5. If the latest Codex review state is APPROVED, stop.
```

### Step 1 — Wait for the review

Codex reacts with 👀 within seconds and posts a review within a few minutes.
Poll (e.g. every 30-60s, timeout ~15 min) until a review from Codex exists for
the current head SHA:

```bash
gh api repos/{owner}/{repo}/pulls/<PR_NUMBER>/reviews \
  --jq '[.[] | select(.user.login | test("codex"; "i"))] | sort_by(.submitted_at) | last'
```

Check `.commit_id` matches the current head SHA (`git rev-parse HEAD`) and
`.submitted_at` is newer than the last push. If no matching review appears
after the timeout, tell the user Codex review did not trigger and stop (check
`@codex review` may need to be commented manually, or automatic review is
disabled for the repo).

If `.state == "APPROVED"`, the loop is done — go to "Exit".

### Step 2 — List and process unresolved threads

List review threads and their resolution state:

```bash
gh api graphql -f query='
query{
  repository(owner:"<OWNER>",name:"<REPO>"){
    pullRequest(number:<PR_NUMBER>){
      reviewThreads(first:100){
        nodes{
          id
          isResolved
          comments(first:1){ nodes{ databaseId author{login} body } }
        }
      }
    }
  }
}'
```

(`<OWNER>`/`<REPO>` can be read from `gh repo view --json owner,name`.)

Filter to threads where `isResolved == false` and the first comment's
`author.login` matches Codex (`chatgpt-codex-connector` or any login
containing "codex").

For each such thread:

**a. Cross-check** — never apply a finding blindly:

- Is it relevant to this PR's actual objective?
- Does it contradict or align with `.speckit/` (functional rules, data
  model, technical constraints, project governance)?
- Is it factually correct (Codex can hallucinate; verify against the real
  diff/code)?
- Does the fix's cost (complexity, readability, time) justify the benefit?

**b. If the finding is valid**, fix it in exactly one commit dedicated to that
finding (do not batch multiple findings into one commit).

**c. Reply to the thread** — always, whether accepted or rejected:

```bash
gh api repos/{owner}/{repo}/pulls/<PR_NUMBER>/comments/<COMMENT_ID>/replies \
  -f body="<reply: what was fixed, or why the finding was rejected>"
```

(`<COMMENT_ID>` is the `databaseId` of the thread's first comment, from
Step 2's query.)

**d. Resolve the thread**, once its reply is posted:

```bash
gh api graphql -f query='
mutation{
  resolveReviewThread(input:{threadId:"<THREAD_ID>"}){ thread{ isResolved } }
}'
```

(`<THREAD_ID>` is the thread's `id` — the opaque GraphQL node id from Step 2,
not the comment's `databaseId`.)

### Step 3-4 — Push and loop

Push all fix commits made in this cycle, then return to Step 1: Codex
re-reviews automatically on new commits pushed to the PR branch.

If no threads needed a fix (all already resolved, or no valid findings to
act on) and the last review state is not `APPROVED`, still return to Step 1 —
a comment-only reply on rejected findings can be enough for Codex to approve
next round, but if nothing changed and no new review appears, stop and report
the situation to the user instead of looping forever.

### Exit

Once the latest Codex review on the current head commit is `APPROVED`, the
Codex gate is satisfied. Merging still requires the CI checks to also pass
(see `.speckit/gestion-projet.md`).
