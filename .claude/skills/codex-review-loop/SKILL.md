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

Repeat until Codex approves the current head commit (or the
signature-false-positive exit applies, see "Exit"):

```text
1. Wait for a Codex review on the current head commit.
2. If Codex reacted with 👍 for that review (no findings), stop — approved,
   see "Exit".
3. Otherwise, for each unresolved review thread from Codex:
   a. Cross-check the finding (see "Cross-check" below).
   b. If valid: establish the root cause and fix it at the root (never a
      band-aid), in one dedicated commit.
4. If any fix commits were made, push them — do not reply to or resolve any
   thread until the push has succeeded.
5. For each thread processed in step 3 (i.e. steps a/b):
   c. Reply to the thread (fix applied, or justified rejection), rate the
      Codex comment (👍/👎).
   d. Resolve the thread.
6. If commits were pushed, go to 1 (Codex re-reviews automatically).
```

**The approval signal is the 👍 reaction Codex leaves when a review has no
findings.** Check for it every cycle (Step 1).

### Watching (mandatory, applies to every step that waits)

Nothing notifies the session when Codex approves (bare 👍, and the dedicated
comment is not systematic), so waiting is done by a **live watcher**, never by
intention. Rules (policy: `.speckit/gestion-projet.md` § Cycle, « Surveillance
active obligatoire »):

- **One background watcher per PR** (`run_in_background`), never one script
  shared by several PRs: a shared script exits on the first PR's signal and
  silently stops watching the others.
- It exits only on a terminal state of **its** PR: 👍 from
  `chatgpt-codex-connector[bot]` dated after the head commit's push, a new
  review/inline comment/issue comment from Codex, PR merged/closed, or timeout.
  It also prints the final CI conclusions (`gh pr view <PR_NUMBER> --json
  statusCheckRollup`), since the merge needs both.
- Never say "I'm watching" without a running watcher, and never end a turn
  with a pending PR that has none. When a watcher ends: handle the state, then
  **re-arm** one for every PR still pending before handing back. On timeout,
  tell the user.

```bash
# <HEAD_PUSHED_AT>: ISO timestamp of the head commit's push / last @codex review
for i in $(seq 1 60); do
  up=$(gh api "repos/{owner}/{repo}/issues/<PR_NUMBER>/reactions?per_page=100" \n    --jq '[.[]|select(.content=="+1" and .user.login=="chatgpt-codex-connector[bot]" and .created_at>="<HEAD_PUSHED_AT>")]|length')
  rv=$(gh api "repos/{owner}/{repo}/pulls/<PR_NUMBER>/reviews?per_page=100" \n    --jq '[.[]|select(.user.login=="chatgpt-codex-connector[bot]" and .submitted_at>="<HEAD_PUSHED_AT>")]|length')
  cm=$(gh api "repos/{owner}/{repo}/pulls/<PR_NUMBER>/comments?per_page=100" \n    --jq '[.[]|select(.user.login=="chatgpt-codex-connector[bot]" and .created_at>="<HEAD_PUSHED_AT>")]|length')
  st=$(gh pr view <PR_NUMBER> --json state -q .state)
  echo "thumbs=$up reviews=$rv inline=$cm state=$st"
  { [ "$up" != 0 ] || [ "$rv" != 0 ] || [ "$cm" != 0 ] || [ "$st" != OPEN ]; } && break
  sleep 60
done
gh pr view <PR_NUMBER> --json statusCheckRollup -q '[.statusCheckRollup[]|"\(.name) \(.status) \(.conclusion)"]'
```

### Step 1 — Wait for the review

Codex reacts with 👀 within seconds and posts a review within a few minutes.
Poll (e.g. every 30-60s, timeout ~15 min) until a review from Codex exists for
the current head SHA:

```bash
gh api "repos/{owner}/{repo}/pulls/<PR_NUMBER>/reviews?per_page=100" \
  --jq '[.[] | select(.user.login=="chatgpt-codex-connector[bot]")] | sort_by(.submitted_at) | last'
```

The endpoint defaults to 30 reviews per page, and since Codex adds one on
every push, a long-running PR can exceed that — comparing against a stale
review would loop until timeout. `per_page=100` (the API's own max) avoids
this in practice.

If a PR ever exceeds 100 reviews, `--paginate` does work together with
`--jq` (unlike `--slurp`, which `gh api` always rejects when combined with
`--jq`) — but only for a filter that is safe to run independently on each
page. The `sort_by(...) | last` aggregation above is **not** safe that way:
under `--paginate`, `--jq` runs once per page, so it would print one "last"
per page instead of the true last across the whole PR. Instead, keep `--jq`
to a pure per-page filter and aggregate afterwards with a separate `jq`
process:

```bash
gh api --paginate "repos/{owner}/{repo}/pulls/<PR_NUMBER>/reviews?per_page=100" \
  --jq '.[] | select(.user.login=="chatgpt-codex-connector[bot]")' \
  | jq -s 'sort_by(.submitted_at) | last'
```

Check `.commit_id` matches the PR's actual head SHA — read with
`gh pr view <PR_NUMBER> --json headRefOid -q .headRefOid`, **not**
`git rev-parse HEAD`. The local checkout can be behind if another actor
pushed to the PR branch, or if this workspace hasn't fetched since the last
push; comparing against a locally-stale SHA would make the loop accept or
wait on a review of a commit that is no longer the PR's real head. Also
check `.submitted_at` is newer than `<LAST_TRIGGER_AT>` — the timestamp of
whichever event started this waiting cycle: the push that was just made
(Step 3), or, on a cycle entered from Step 5 without a new push, the
re-review request comment's `created_at`. Without this check, re-entering
Step 1 after a no-push re-review request would immediately match the same
already-seen review again (same commit SHA, unchanged `submitted_at`), and
the loop would spin on a stale review instead of waiting for the fresh one
it just asked for. If no matching review appears after the timeout, tell the
user Codex review did not trigger and stop (check `@codex review` may need
to be commented manually, or automatic review is disabled for the repo).
Keep this review's `.submitted_at` as `<REVIEW_SUBMITTED_AT>` for the next
check.

Check for the 👍 approval reaction **from Codex, dated to this review**:

```bash
gh api "repos/{owner}/{repo}/issues/<PR_NUMBER>/reactions?per_page=100" \
  --jq '[.[] | select(.content=="+1" and .user.login=="chatgpt-codex-connector[bot]" and .created_at >= "<REVIEW_SUBMITTED_AT>")] | length'
```

This endpoint also defaults to 30 reactions per page (max 100); `per_page=100`
avoids missing a fresh Codex 👍 on a PR with many prior reactions, for the
same reason as the reviews query above. If a PR ever exceeds 100 reactions,
apply the same fix as above: `--paginate` with a per-page-safe `--jq` (drop
the aggregating `| length`, keep only the `select(...)`), piped into an
external `jq -s 'length'` to count across all pages.

A PR can carry a stale 👍 from an earlier head, or a 👍/comment/review from a
human or an unrelated bot whose login happens to contain "codex" (a
collaborator called e.g. `codex-fan`, or another integration). Every identity
check in this skill — this reaction gate, the review lookup above, and the
thread filter in Step 2 — therefore matches the **exact** bot login rather
than a substring, so none of them can be satisfied, or have their result
skewed (e.g. `<REVIEW_SUBMITTED_AT>` picking up someone else's later review),
by an unrelated account. The exact string differs by API: REST endpoints
(reviews, reactions — used above) report bot accounts as
`chatgpt-codex-connector[bot]`, the login named in
`.speckit/gestion-projet.md`; GraphQL's `author.login` on review thread
comments (Step 2) reports the same bot without the `[bot]` suffix, as
`chatgpt-codex-connector` — a documented inconsistency between GitHub's REST
and GraphQL representations of App bots, confirmed against this PR's live
data.
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
`author.login` is exactly `chatgpt-codex-connector` (see the login-format
note under Step 1 for why this differs from the REST-reported
`chatgpt-codex-connector[bot]`). An unrelated reviewer or bot whose login
merely contains "codex" must not match here: Step 4 replies to and resolves
every thread this filter selects, so a false match would auto-resolve a
non-Codex reviewer's actual feedback.

For each such thread, `path`/`line`/`diffHunk` locate the finding in the diff
when the comment `body` doesn't repeat it.

**a. Cross-check** — never apply a finding blindly, and never take it at face
value just because it sounds plausible. Codex can go very deep into detail;
depth alone is not validity. The bar is a **user-proof** application, not
code that survives every theoretical or adversarial twist.

- Is it relevant to this PR's actual objective?
- Does it contradict or align with `.speckit/` (functional rules, data
  model, technical constraints, project governance)?
- Is it factually correct (Codex can hallucinate; verify against the real
  diff/code)? **If the finding makes a claim about a command, API, or tool
  behavior, that claim must be tested empirically (run it) or checked against
  its official documentation — never accepted on reasoning alone.** A wrong
  fix to a wrong finding just produces another finding next cycle.
- **Is the scenario possible in this app's real context?** Reject findings
  whose premise requires misuse, impossible inputs, or execution paths that
  users (or this app's legitimate callers) cannot reach. The code is
  application-scoped — not a public or shared library — so do not harden for
  reuse, arbitrary embedding, or abuse outside the app's threat model and UX.
- **Is the scenario user-relevant?** Legitimate user mistakes, confusing
  flows, and real integration boundaries count; hypothetical edge cases that
  only matter if someone deliberately misuses or "breaks" the API do not.
- Does the fix's cost (complexity, readability, time) justify the benefit?

When rejecting a finding on these grounds, state clearly in the Step 4 reply
*why* the scenario is out of scope (not possible, not user-relevant, or
disproportionate) — not merely "won't fix".

Codex reviews the diff, so its findings are naturally scoped to the lines it
looked at — but the same class of mistake often recurs elsewhere in the same
file (e.g. the same command pattern reused with a different endpoint). Before
fixing, scan the whole file/scope for other instances of the same issue, not
just the one flagged. Fixing only the exact spot Codex pointed at, while an
identical problem remains a few lines away, guarantees another round trip.

That scan finds *other instances of the same surface pattern*, which is not
the same question as *why the pattern exists at all*. Before writing the
fix, ask whether the finding is a symptom of a broader design weakness
rather than an isolated mistake — e.g. repeated identity-matching bugs across
a file may mean the design lacks a single source of truth for "is this
Codex", not just several places that each need their string tightened. A
locally-correct patch that leaves that weakness in place is a band-aid: it
satisfies this one finding while leaving the file structurally prone to
producing the next one. When the fix is only a band-aid, prefer addressing
the underlying weakness directly (e.g. factor the repeated check into one
place referenced everywhere, or restructure the flawed step), even if that
means a larger change than the finding alone would suggest.

**b. If the finding is valid**, fix it — and every other instance of the same
underlying issue found by the scan above, and the deeper weakness if one was
identified — in one dedicated commit per underlying finding (do not batch
multiple *unrelated* findings into one commit, but do not split one finding's
holistic fix across several either). Keep track of each thread's
`<COMMENT_ID>` / `<THREAD_ID>` (from Step 2's query) and its outcome (fixed,
with which commit — or rejected, with why) for Step 4. Do **not** reply to or
resolve threads yet.

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

Never write the literal text `@codex` inside this reply. Per Codex's GitHub
integration, any `@codex` mention followed by anything other than exactly
`review` starts a cloud task using the PR as context (i.e. an unwanted fix
attempt) — only the dedicated top-level PR comment in Step 5, containing
exactly `@codex review` and nothing else, should ever contain that mention.

**c'. Rate Codex's comment** — always, as Codex asks in every comment: 👍
(`+1`) if the finding was useful, 👎 (`-1`) if not:

```bash
gh api repos/{owner}/{repo}/pulls/comments/<COMMENT_ID>/reactions -f content="+1"
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

If no fix commit was made in this cycle — every finding was rejected, all
threads were already resolved, or Step 2 found no unresolved Codex thread at
all (yet Step 1 still found no matching 👍) — Codex will **not** re-review on
its own: replying to and resolving threads doesn't trigger it, only a new
commit or an explicit request does. In every one of these cases, post a PR
comment containing exactly `@codex review` to request a fresh pass, record
that comment's `created_at` as the new `<LAST_TRIGGER_AT>` (Step 1 needs it
to recognize the next review as actually new, rather than re-matching the
same review this cycle already found insufficient), then return to Step 1.
If that still produces no new review and nothing changed, stop and report
the situation to the user instead of looping forever.

**Signature false positive.** Codex may report unsigned commits. If GitHub
shows the commits as "Verified", it is a false positive: handle it like any
rejected finding (justified reply, 👎, resolve), consider that Codex has
nothing else to report, and request a fresh pass with `@codex review`. At most
**3** such requests: if Codex still raises only that same false positive, with
no other comment, see "Exit".

### Exit

The Codex gate is satisfied once Codex has reacted 👍 to the PR for the
current head commit. Merging still requires the CI checks to also pass (see
`.speckit/gestion-projet.md`).

Exception: after 3 requests where Codex repeated only a verified signature
false positive (see Step 5) with no other comment, the PR is considered
validated by Codex even without the 👍.
