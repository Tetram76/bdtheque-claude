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
2. If Codex reacted with 👍 (no findings), stop — approved, see "Exit".
3. Otherwise, for each unresolved review thread from Codex:
   a. Cross-check the finding (see "Cross-check" below).
   b. If valid: make the fix in one dedicated commit.
   c. Reply to the thread (fix applied, or justified rejection).
   d. Resolve the thread.
4. Push the fix commits (if any were made).
5. If commits were pushed, go to 1 (Codex re-reviews automatically).
```

**The approval signal is the 👍 reaction Codex leaves when a review has no
findings.** Check for it every cycle (Step 1).

### Step 1 — Wait for the review

Codex reacts with 👀 within seconds and posts a review within a few minutes.
Poll (e.g. every 30-60s, timeout ~15 min) until a review from Codex exists for
the current head SHA:

```bash
gh api "repos/{owner}/{repo}/pulls/<PR_NUMBER>/reviews?per_page=100" \
  --jq '[.[] | select(.user.login | test("codex"; "i"))] | sort_by(.submitted_at) | last'
```

The endpoint defaults to 30 reviews per page, and since Codex adds one on
every push, a long-running PR can exceed that — comparing against a stale
review would loop until timeout. `per_page=100` (the API's own max) avoids
this in practice. `--paginate` would fetch beyond 100, but `gh api` rejects
combining it with `--jq` unless `--slurp`-ing into one array, and `--slurp`
itself is rejected together with `--jq`; if a PR ever exceeds 100 reviews,
drop `--jq` and paginate with `--slurp` into a file, then filter separately.

Check `.commit_id` matches the current head SHA (`git rev-parse HEAD`) and
`.submitted_at` is newer than the last push. If no matching review appears
after the timeout, tell the user Codex review did not trigger and stop (check
`@codex review` may need to be commented manually, or automatic review is
disabled for the repo).

Check for the 👍 approval reaction on the PR:

```bash
gh api repos/{owner}/{repo}/issues/<PR_NUMBER>/reactions \
  --jq '[.[] | select(.content=="+1")] | length'
```

If this is `> 0`, the review found nothing to fix — go to "Exit". Otherwise
continue to Step 2.

### Step 2 — List and process unresolved threads

List review threads and their resolution state:

```bash
gh api graphql -f query='
query{
  repository(owner:"<OWNER>",name:"<REPO>"){
    pullRequest(number:<PR_NUMBER>){
      reviewThreads(first:100){
        pageInfo{ hasNextPage endCursor }
        nodes{
          id
          isResolved
          comments(first:1){
            nodes{ databaseId author{login} body path line startLine diffHunk }
          }
        }
      }
    }
  }
}'
```

(`<OWNER>`/`<REPO>` can be read from `gh repo view --json owner,name`.)
`reviewThreads` is capped at 100 per page. If `pageInfo.hasNextPage` is
`true`, re-run the query with `reviewThreads(first:100, after:"<endCursor>")`
and merge every page's `nodes` before filtering — otherwise threads beyond
the first 100 are silently skipped.

Filter to threads where `isResolved == false` and the first comment's
`author.login` matches Codex (`chatgpt-codex-connector` or any login
containing "codex").

For each such thread, `path`/`line`/`diffHunk` locate the finding in the diff
when the comment `body` doesn't repeat it.

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

If fix commits were made in this cycle, push them, then return to Step 1:
Codex re-reviews automatically on new commits pushed to the PR branch.

If no fix commit was made (every finding was rejected, or all threads were
already resolved) and Step 2 still found unresolved Codex threads to act on,
Codex will **not** re-review on its own — replying to and resolving threads
doesn't trigger it, only a new commit or an explicit request does. Post a PR
comment containing exactly `@codex review` to request a fresh pass, then
return to Step 1. If that still produces no new review and nothing changed,
stop and report the situation to the user instead of looping forever.

### Exit

The Codex gate is satisfied once Codex has reacted 👍 to the PR for the
current head commit. Merging still requires the CI checks to also pass (see
`.speckit/gestion-projet.md`).
