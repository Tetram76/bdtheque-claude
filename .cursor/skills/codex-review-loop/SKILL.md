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
2. If Codex reacted with 👍 for that review (no findings), stop — approved,
   see "Exit".
3. Otherwise, for each unresolved review thread from Codex:
   a. Cross-check the finding (see "Cross-check" below).
   b. If valid: make the fix in one dedicated commit.
4. If any fix commits were made, push them — do not reply to or resolve any
   thread until the push has succeeded.
5. For each thread processed in step 3 (i.e. steps a/b):
   c. Reply to the thread (fix applied, or justified rejection).
   d. Resolve the thread.
6. If commits were pushed, go to 1 (Codex re-reviews automatically).
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
disabled for the repo). Keep this review's `.submitted_at` as
`<REVIEW_SUBMITTED_AT>` for the next check.

Check for the 👍 approval reaction **from Codex, dated to this review**:

```bash
gh api "repos/{owner}/{repo}/issues/<PR_NUMBER>/reactions?per_page=100" \
  --jq '[.[] | select(.content=="+1" and (.user.login | test("codex"; "i")) and .created_at >= "<REVIEW_SUBMITTED_AT>")] | length'
```

This endpoint also defaults to 30 reactions per page (max 100); `per_page=100`
avoids missing a fresh Codex 👍 on a PR with many prior reactions, for the
same reason as the reviews query above.

A PR can carry a stale 👍 from an earlier head, or a human 👍 unrelated to
Codex's verdict; filtering by author and by `.created_at >= <REVIEW_SUBMITTED_AT>`
ensures the reaction actually approves the current-head review found above.
If this is `> 0`, the review found nothing to fix — go to "Exit". Otherwise
continue to Step 2.

### Step 2 — List threads and make fixes

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
finding (do not batch multiple findings into one commit). Keep track of each
thread's `<COMMENT_ID>` / `<THREAD_ID>` (from Step 2's query) and its outcome
(fixed, with which commit — or rejected, with why) for Step 4. Do **not**
reply to or resolve threads yet.

### Step 3 — Push, before any reply or resolution

If any fix commits were made in Step 2, push them now, and confirm the push
succeeded (e.g. check the command's exit status and that the remote ref
advanced) before moving on. If a dedicated fix commit exists locally but the
push is rejected or interrupted (branch advanced remotely, credentials or
network failure, etc.), replying to and resolving the corresponding thread
next would close the conversation on the remote PR while the fix is not
actually present there. So: resolve the push failure (retry, rebase, etc.)
and confirm success before proceeding to Step 4 — never reply to or resolve a
thread on the strength of a commit that isn't confirmed pushed.

### Step 4 — Reply and resolve

For each thread identified in Step 2:

**c. Reply to the thread** — always, whether accepted or rejected
(`<COMMENT_ID>` is the `databaseId` of the thread's first comment, from
Step 2's query):

```bash
gh api repos/{owner}/{repo}/pulls/<PR_NUMBER>/comments/<COMMENT_ID>/replies \
  -f body="<reply: what was fixed, or why the finding was rejected>"
```

**d. Resolve the thread**, once its reply is posted:

```bash
gh api graphql -f query='
mutation{
  resolveReviewThread(input:{threadId:"<THREAD_ID>"}){ thread{ isResolved } }
}'
```

(`<THREAD_ID>` is the thread's `id` — the opaque GraphQL node id from Step 2,
not the comment's `databaseId`.)

### Step 5 — Loop or request re-review

If fix commits were made (and pushed in Step 3), return to Step 1: Codex
re-reviews automatically on new commits pushed to the PR branch.

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
