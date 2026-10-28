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

The `.speckit/` is the source of truth for PR management on this repo: it
prevails over this skill and over any other rule, skill or plugin, global
ones included; on any divergence, the speckit applies. Policy:
`.speckit/gestion-projet/revue-tickets.md` → "Revue de code" / "Revue Codex (bloquante)". This skill is the operational procedure for that
policy: a PR on this repo cannot be merged until the Codex bot
(`chatgpt-codex-connector[bot]`) has approved the current head commit.

Codex reviews a PR when it is opened as non-draft, when a draft is marked
ready for review, and on every push to a non-draft PR: always open the PR as a
draft, and mark it ready for review only once the branch content is complete,
the CI checks have passed and the SonarQube Cloud analysis of the head commit
has been handled (skill `sonarcloud-pr-analysis`); until then, the work stays
local or on the draft PR (policy: § « Ouverture de la PR »).
Marking a draft ready triggers a review by itself, so never pair it with a
`@codex review` comment. As a safety net only, the watcher (Step 1) checks
that the review started: only if Codex has put no 👀 on the PR description
2 minutes after marking it ready, request a review with a `@codex review`
comment.

Requires `gh` authenticated (`gh auth status`) with access to the repo.

## Codex budget cooldown — check before any trigger

When Codex runs out of budget it posts "You have reached your Codex usage
limits" on a PR. Any review triggered within **5 hours** of that notice is
refused **and restarts the 5-hour delay**, whichever PR triggers it: the
budget is shared (policy: `.speckit/gestion-projet/revue-tickets.md` § « Budget de Codex
épuisé »). So, before **every** act that could trigger a review — creating a
non-draft PR, marking a draft ready, pushing to a non-draft PR, or posting
any comment with an `@codex` mention (Step 5 included) — run the check below.
It scans the PRs that are open, or were closed less than 5 hours ago — not
every PR: closing or merging the PR that carries the notice does not end the
cooldown. A notice posted within the window bumped its PR's `updated_at`, so
reading the comments of the PRs updated since the cutoff is enough.

```bash
set -o pipefail
window=18000
cutoff=$(date -u -d "@$(( $(date -u +%s) - window ))" +%FT%TZ)
prs=$(gh api --paginate "repos/{owner}/{repo}/pulls?state=all&sort=updated&direction=desc&per_page=100" \
        --jq ".[]|select(.updated_at>=\"$cutoff\")|.number") || { echo "query failed: retry"; exit 1; }
last=""
for n in $prs; do
  t=$(gh api --paginate "repos/{owner}/{repo}/issues/$n/comments?since=$cutoff&per_page=100" \
        --jq ".[]|select(.user.login==\"chatgpt-codex-connector[bot]\" and .created_at>=\"$cutoff\" and (.body|test(\"reached your Codex usage limits\")))|.created_at" \
      | sort | tail -1) || { echo "query failed: retry"; exit 1; }
  [[ "$t" > "$last" ]] && last=$t
done
if [ -n "$last" ]; then
  echo "blocked until $(date -u -d "@$(( $(date -u -d "$last" +%s) + window ))" +%FT%TZ)"
else
  echo "clear (no notice since $cutoff)"
fi
```

`blocked` → do none of those acts: open the PR as a draft (drafts are out of
Codex's scope, so pushing to one is fine), keep commits local or on a draft,
and tell the user when the window ends. A failed query is not `clear`: retry.
This check overrides every other step of this skill that triggers Codex.

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
1. Wait for Codex on the current head commit (watcher, Step 1).
2. If Codex approved it (👍, or an approving receipt naming it) and CI has
   completed, stop — see "Exit".
3. Otherwise, for each unresolved review thread from Codex:
   a. Cross-check the finding (see "Cross-check" below). If valid,
      establish its root cause — which may lie outside the diff and outside
      the PR's objective — then confront the finding as a whole (what it
      raises, its cause and its fix) with the PR's objective (see "Scope"
      below).
   b. If the whole finding is within the PR's objective: fix it at the root (never a
      band-aid), in one dedicated commit. If not: the fix may be deferred
      (ticket, or note in the implementation plan — see "Scope") — never
      patched over in this PR.
4. If any fix commits were made, push them — do not reply to or resolve any
   thread until the push has succeeded.
5. For each thread processed in step 3 (i.e. steps a/b):
   c. Reply to the thread (fix applied, fix deferred with its ticket or plan note, or
      justified rejection), rate the Codex comment (👍/👎).
   d. Resolve the thread.
6. If commits were pushed, go to 1 (Codex re-reviews automatically).
```

**Codex approves the head commit in one of two ways** (policy:
`.speckit/gestion-projet/revue-tickets.md` § Revue Codex), often without posting any review:

- the **👍 reaction** on the PR description — it names no commit, so the
  watcher ties it to the head through the review's course (trigger time, head
  unchanged, no older review still running);
- an **approving receipt**: a top-level comment marked
  `<!-- codex-review-receipt:v1 -->` that reads "Codex Review: Didn't find any
  major issues" and whose `**Reviewed commit:**` (an abbreviated SHA) is a
  prefix of the head SHA. A receipt naming an older commit approves nothing.

The watcher (Step 1) checks for both every cycle.

### Watching (mandatory, applies to every step that waits)

Nothing notifies the session when Codex approves (bare 👍, and the approving
receipt is not systematic), so waiting is done by a **live watcher**, never by
intention. Rules (policy: `.speckit/gestion-projet/revue-tickets.md` § Cycle, « Surveillance
active obligatoire »):

- **One background watcher per PR** (`run_in_background`), never one script
  shared by several PRs: a shared script exits on the first PR's signal and
  silently stops watching the others.
- It exits only on a terminal state of **its** PR: new feedback from Codex
  (review, inline or top-level comment other than an approving receipt for the
  head), PR merged/closed, approval of the head — the 👍 from
  `chatgpt-codex-connector[bot]` dated after `<LAST_TRIGGER_AT>`, or an
  approving receipt naming the head — once every CI check has completed — at least one check registered, an empty rollup being
  CI not started (the merge needs both) —, a change of the PR's head commit,
  the end of a Codex review started before `<LAST_TRIGGER_AT>` (it keeps
  watching while that review runs; its verdict is about an older head, so
  its 👍 never counts as approval), GitHub queries failing 5 times in a
  row, a review not started — no 👀 2 minutes after
  `<LAST_TRIGGER_AT>` —, a stuck Codex — no verdict 10 minutes after the 👀
  that started the review (policy: § Cycle, « Revue non démarrée », « Codex
  bloqué ») —, or timeout (about one hour, e.g. CI never completing after an
  approval). It prints why (`exit: …`) and the final CI conclusions.
- Any other top-level Codex comment (e.g. "You have reached your Codex usage
  limits", which no review follows; the "Codex Review Summary" table; a
  receipt naming an older commit) only hands control back so it gets read; it
  is **never** an approval — only the 👍 and the approving receipt naming the
  head are.
- Never say "I'm watching" without a running watcher, and never end a turn
  with a pending PR that has none. A suspended PR — Codex usage limit, a
  review still not started or stuck after a new review request, timeout, or
  GitHub queries that keep failing (see Step 1) — is not pending:
  it gets no watcher. When a watcher ends: handle the state, then **re-arm**
  one for every PR still pending before handing back, with `<HANDLED_UNTIL>`
  set to the time the event was handled.

```bash
# Two timestamps, because a 👍 is a *state* and a review/comment is an *event*:
# <LAST_TRIGGER_AT> the push of the head commit, the PR's opening as non-draft
#                   or its marking as ready for review, or the last
#                   `@codex review` comment (Step 5). A 👍 dated after it approves the current
#                   head, and stays true.
# <HANDLED_UNTIL>   Codex events (reviews, comments) older than this were
#                   already handled. Starts equal to <LAST_TRIGGER_AT>; when
#                   re-arming without a new trigger (e.g. after reading feedback
#                   with nothing to fix while the 👍 or CI is still awaited),
#                   set it to the time you finished handling, otherwise
#                   the same event ends every new watcher at once.
# <HEAD_SHA>        the PR's head commit at <LAST_TRIGGER_AT>
#                   (`gh pr view <PR_NUMBER> --json headRefOid -q .headRefOid`).
# A 👍 says nothing about which commit it approves: Codex removes its 👍 and
# puts 👀 when a review starts, then answers with a 👍 or a review. So a 👍 is
# trusted for the head only if the head has not moved and no review started
# before the trigger is still running — such a review's 👍 would arrive after
# the trigger yet be about an older commit.
# A failed query (expired auth, network, rate limit) must never be read as
# data — "0 events" or "PR not open": every query is checked, a failure is
# retried, and a lasting one ends the watcher with its own reason.
set -o pipefail
# Counts matches across ALL pages (an endpoint returns at most 100 items per
# page, and reviews come oldest first): the --jq filter must be per-page safe.
count() { local n; n=$(gh api --paginate "$1?per_page=100" --jq "$2" | wc -l) && echo "$n"; }
bot='.user.login=="chatgpt-codex-connector[bot]"'
# approving receipt naming the head: its "Reviewed commit" (abbreviated SHA)
# must be a prefix of <HEAD_SHA>; a receipt about an older commit is ordinary
# feedback to read, never an approval
receipt='((.body|test("codex-review-receipt")) and (.body|test("find any major issues"))
  and (([.body|match("Reviewed commit:[*]+ `([0-9a-f]{7,40})`").captures[0].string][0] // "")
  as $s | $s != "" and ("<HEAD_SHA>"|startswith($s))))'
# A review starts when Codex puts its 👀: none 2 minutes after the trigger is a
# review that did not start; no verdict 10 minutes after the 👀 is a stuck
# Codex. The first 👀 seen is remembered, Codex removing it when it concludes.
epoch() { date -u -d "$1" +%s; }
trigger=$(epoch "<LAST_TRIGGER_AT>") started=""
reason=timeout fails=0 saw_old=0
for i in $(seq 1 60); do
  # /issues/<PR_NUMBER>/comments = the PR's own conversation comments (a PR is
  # an issue for the API), not a ticket's
  if up=$(count "repos/{owner}/{repo}/issues/<PR_NUMBER>/reactions" \
          ".[]|select(.content==\"+1\" and $bot and .created_at>=\"<LAST_TRIGGER_AT>\")") &&
     old=$(count "repos/{owner}/{repo}/issues/<PR_NUMBER>/reactions" \
          ".[]|select(.content==\"eyes\" and $bot and .created_at<\"<LAST_TRIGGER_AT>\")") &&
     rv=$(count "repos/{owner}/{repo}/pulls/<PR_NUMBER>/reviews" \
          ".[]|select($bot and .submitted_at>=\"<HANDLED_UNTIL>\")") &&
     cm=$(count "repos/{owner}/{repo}/pulls/<PR_NUMBER>/comments" \
          ".[]|select($bot and .created_at>=\"<HANDLED_UNTIL>\")") &&
     ic=$(count "repos/{owner}/{repo}/issues/<PR_NUMBER>/comments" \
          ".[]|select($bot and .created_at>=\"<HANDLED_UNTIL>\" and ($receipt|not))") &&
     rc=$(count "repos/{owner}/{repo}/issues/<PR_NUMBER>/comments" \
          ".[]|select($bot and $receipt)") &&
     eyes=$(gh api --paginate "repos/{owner}/{repo}/issues/<PR_NUMBER>/reactions?per_page=100" \
          --jq ".[]|select(.content==\"eyes\" and $bot and .created_at>=\"<LAST_TRIGGER_AT>\")|.created_at" | sort | head -1) &&
     sp=$(gh pr view <PR_NUMBER> --json state,headRefOid,statusCheckRollup \
          -q '"\(.state) \(.headRefOid) \(.statusCheckRollup|length) \([.statusCheckRollup[]|select(.status!="COMPLETED")]|length)"')
  then
    fails=0; read -r st head checks pending <<< "$sp"
    echo "thumbs=$up receipt=$rc old_review=$old reviews=$rv inline=$cm issue=$ic state=$st head=${head:0:7} checks=$checks pending=$pending"
    if [ "$rv" -ne 0 ] || [ "$cm" -ne 0 ] || [ "$ic" -ne 0 ]; then
      reason="new Codex feedback"; break
    fi
    if [ "$st" != OPEN ]; then reason="PR $st"; break; fi
    if [ "$head" != "<HEAD_SHA>" ]; then reason="head changed"; break; fi
    # a receipt names the commit it approves: no need to tie it to the head
    # through the review's course, unlike the 👍 below
    if [ "$rc" -ne 0 ] && [ "$checks" -gt 0 ] && [ "$pending" -eq 0 ]; then
      reason="approved, CI done"; break
    fi
    # a review started before the trigger is still running: keep watching,
    # and remember it — the 👍 that ends it is about an older head
    if [ "$old" -ne 0 ]; then saw_old=1; sleep 60; continue; fi
    if [ "$saw_old" -eq 1 ]; then reason="earlier review ended"; break; fi
    # approval is terminal only once CI has run: checks registered (an empty
    # rollup means CI has not started, not that it is done) and all completed
    if [ "$up" -ne 0 ] && [ "$checks" -gt 0 ] && [ "$pending" -eq 0 ]; then
      reason="approved, CI done"; break
    fi
    # an approval waiting for CI is a verdict: neither timer applies
    if [ "$up" -eq 0 ] && [ "$rc" -eq 0 ]; then
      now=$(date -u +%s)
      [ -z "$started" ] && [ -n "$eyes" ] && started=$(epoch "$eyes")
      if [ -z "$started" ] && [ $((now - trigger)) -ge 120 ]; then reason="review not started"; break; fi
      if [ -n "$started" ] && [ $((now - started)) -ge 600 ]; then reason="Codex stuck"; break; fi
    fi
  else
    fails=$((fails + 1)); echo "GitHub query failed ($fails in a row)"
    if [ "$fails" -ge 5 ]; then reason="GitHub queries keep failing"; break; fi
  fi
  sleep 60
done
echo "exit: $reason"
gh pr view <PR_NUMBER> --json statusCheckRollup -q '[.statusCheckRollup[]|"\(.name) \(.status) \(.conclusion)"]'
```

### Step 1 — Wait for Codex

Codex reacts with 👀 within seconds; within a few minutes it either approves
the head commit — with a bare 👍, or with an approving receipt naming it,
often without posting any review — or posts a review with findings. So never wait for a review to appear: run the watcher
above, `<LAST_TRIGGER_AT>` being the push just made (Step 3), the PR's
opening as non-draft or its marking as ready for review, or, on a cycle
entered from Step 5 without a new push, the `@codex review` comment's
`created_at`. Then act on its exit reason:

- `approved, CI done` → "Exit" (if a CI check failed, handle it first: the
  merge needs it green).
- `new Codex feedback` → if it opened review threads: Step 2. Anything else
  (a top-level comment, or a review without threads): read it. A usage-limit
  message **suspends** the PR: no review is coming until Codex's budget is
  back, so tell the user and stop — no watcher, and no `@codex review`, which
  would be refused and restart the 5-hour cooldown (see "Codex budget
  cooldown"); resume only when the user asks, once the cooldown is over. Feedback
  reporting nothing to fix: re-arm with `<HANDLED_UNTIL>` set to now and keep
  waiting for the approval — never answer it with `@codex review`, which would pay
  for a needless extra review.
- `head changed` (someone pushed) → re-arm with that push as the new trigger
  and head.
- `earlier review ended` (a push landed while Codex was reviewing an older
  head; the watcher waited for that review to finish) → request a review of
  the current head with a top-level `@codex review` comment and re-arm with
  that comment as trigger: Codex does not start a new review on its own for a
  push made during one. If that older review left feedback instead, the
  watcher exits on `new Codex feedback`: handle it (Step 2) first.
- `PR MERGED` / `PR CLOSED` → tell the user and stop.
- `review not started` (no 👀 2 minutes after the trigger) or
  `Codex stuck` (no verdict 10 minutes after the 👀) → if the trigger was not
  already a request made for one of these reasons, run the budget check,
  request a review of the head with a top-level `@codex review` comment and
  re-arm with that comment as trigger. If it was, the PR is **suspended**
  (below).
- `timeout` (e.g. Codex approved but CI never completed),
  `GitHub queries keep failing`, or a suspended PR → tell the user and stop,
  with no watcher (a new one would only wait in vain); resume only when the
  user asks.

A PR can carry a stale 👍 from an earlier head, or a 👍/comment/review from a
human or an unrelated bot whose login happens to contain "codex" (a
collaborator called e.g. `codex-fan`, or another integration). The watcher
handles the first case (trigger time, head commit, review started before the
trigger), and every identity check in
this skill — the watcher's queries and the thread filter in Step 2 —
matches the **exact** bot login rather than a substring, so none of them can
be satisfied by an unrelated account. The exact string differs by API: REST
endpoints (reviews, reactions, comments — used by the watcher) report bot
accounts as `chatgpt-codex-connector[bot]`, the login named in
`.speckit/gestion-projet/`; GraphQL's `author.login` on review thread
comments (Step 2) reports the same bot without the `[bot]` suffix, as
`chatgpt-codex-connector` — a documented inconsistency between GitHub's REST
and GraphQL representations of App bots, confirmed against this PR's live
data.

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
value just because it sounds plausible. Codex reviews the diff only: it knows
neither the project context (`.speckit/`, architecture, settled choices) nor
the PR's objective, so its findings are indications to confront with both —
never instructions to follow to the letter. Codex can go very deep into detail;
depth alone is not validity. The bar is a **user-proof** application, not
code that survives every theoretical or adversarial twist.

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

**Scope** — mandatory once a finding is judged valid and its root cause
established (policy: `.speckit/gestion-projet/revue-tickets.md` § « Traiter un retour »,
steps 2–3): Codex does not know the PR's objective, so a valid finding is not
automatically fixed in this PR. What is confronted with the objective is the
**finding as a whole** — what it raises, its root cause and its fix — not
the flagged lines: a symptom inside the diff can come from a cause, or call
for a fix, outside the PR's scope.

- **Whole finding within the PR's objective**: fix it here (step b).
- **Outside it** — by what it raises, its cause or its fix — even when the
  symptom is in the diff: the fix may be
  deferred, never lost, and never replaced by a local patch in this PR that
  leaves the cause in place. Where it is traced depends on the phase of the
  implementation plan (`.speckit/suivi-implementation.md`) it belongs to:
  - **already finished phase**: a ticket;
  - **phase already broken down into work items**: the open ticket of the
    work item it belongs to, or else a new ticket;
  - **phase not yet broken down**: no ticket — add a note on that phase in
    `suivi-implementation.md`, so the point is planned when the phase is
    broken down.
- A deferred finding is still a valid one: rate it 👍, and reply with the
  ticket or plan note that carries its fix.

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

**b. If the finding is valid and, as a whole, within the PR's objective**
(see "Scope"), fix it — and every other instance of the same
underlying issue found by the scan above, and the deeper weakness if one was
identified — in one dedicated commit per underlying finding (do not batch
multiple *unrelated* findings into one commit, but do not split one finding's
holistic fix across several either). Keep track of each thread's
`<COMMENT_ID>` / `<THREAD_ID>` (from Step 2's query) and its outcome (fixed,
with which commit — deferred, with which ticket or plan note — or rejected, with why) for
Step 4. Do **not** reply to or
resolve threads yet.

### Step 3 — Push, before any reply or resolution

Never push while a Codex review is running — its 👀 is on the PR:

```bash
# same all-page counting as the watcher; pipefail, so that a failed query
# fails the command instead of printing a misleading 0
set -o pipefail
gh api --paginate "repos/{owner}/{repo}/issues/<PR_NUMBER>/reactions?per_page=100" \
  --jq '.[]|select(.content=="eyes" and .user.login=="chatgpt-codex-connector[bot]")' | wc -l
```

If the command fails, whether a review is running is unknown: do not push,
retry the check.

If this is `> 0`, wait for the verdict first (watcher): a push made during a
review gets no review of its own, and the 👍 that ends the running review
would wrongly look like an approval of the new head.

Nor while the Codex budget cooldown is running (see "Codex budget cooldown").

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

**c. Reply to the thread** — always, whether accepted, deferred or rejected
(`<COMMENT_ID>` is the `databaseId` of the thread's first comment, from
Step 2's query):

```bash
gh api repos/{owner}/{repo}/pulls/<PR_NUMBER>/comments/<COMMENT_ID>/replies \
  -f body="<reply: what was fixed, which ticket or plan note carries the deferred fix, or why the finding was rejected>"
```

Never write the literal text `@codex` inside this reply. Per Codex's GitHub
integration, any `@codex` mention followed by anything other than exactly
`review` starts a cloud task using the PR as context (i.e. an unwanted fix
attempt) — only the dedicated top-level PR comment in Step 5, containing
exactly `@codex review` and nothing else, should ever contain that mention.
The same holds for the PR description and every other comment, even when the
mention is quoted, in backticks or used to describe a rule: grep each text
for it right before `gh pr create` / `gh pr edit` / `gh pr comment` / `gh api`.
In chat with the user, never type it either (policy: `.speckit/gestion-projet/gouvernance.md`
§ Budget IA, « Mention de Codex »).

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

If no fix commit was made in this cycle — every finding was rejected or deferred, all
threads were already resolved, or Step 2 found no unresolved Codex thread at
all (yet Step 1 still found no approval) — Codex will **not** re-review on
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

The Codex gate is satisfied once Codex has approved the current head commit
(👍, or an approving receipt naming it). Merging still requires the CI checks to also pass (see
`.speckit/gestion-projet/revue-tickets.md` § Règle de merge : non-régression et revue Codex obligatoires).

Exception: after 3 requests where Codex repeated only a verified signature
false positive (see Step 5) with no other comment, the PR is considered
validated by Codex even without an approval.
