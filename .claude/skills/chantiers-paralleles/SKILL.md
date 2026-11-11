---
name: chantiers-paralleles
description: >-
  Runs a batch of chantiers in parallel on this repository, each in its own
  session and worktree: picks them among the open tickets in the order of
  priority of the .speckit, evaluates the resources of the machine before each
  launch, starts a session per chantier and starts the next one each time a
  pull request of the batch is merged. Use when the user asks to run the next
  chantiers « en parallèle » / « en // », to launch a batch of chantiers, or
  when a session of a running batch reports a merge.
---

# Chantiers en parallèle

The `.speckit/` is the source of truth:
`.speckit/gestion-projet/gouvernance/chantiers-en-parallele.md`. **Read it again,
in full, before step 1**: this skill only orders its steps and gives the
commands, it adds no rule. This session is the **orchestrator**: it
chooses, measures, launches and reports. That is essentially its role: in
principle it does not do what belongs to a chantier's session (code, heavy
runs, actions on the chantier's pull request: review, merge, closing), and it
forwards to that session a question of the user about the chantier
(`SendMessage`).

## 1. The batch

- A batch is **always run in parallel**, never one chantier after the other
  by choice: **3 at once at most**, fewer only when the resources (step 2.3)
  or a dependency between chantiers require it. Never ask the user how many
  at once.
- The user gives the **number of chantiers** of the batch. It is not fixed:
  take the latest the user gave in this conversation; ask for it if never
  given, never assume it.
- Count the places taken (a chantier whose pull request is not merged yet,
  and the functional consistency check until its conclusion is written).
- A sub-ticket is a chantier like the others. When it is launched, give it to
  the session that split its ticket only if that session's context suits it
  (what it knows serves the sub-ticket, and its conversation is not too
  loaded to carry it end to end — read it with `get_session` /
  `list_events`): then send it the brief with `SendMessage`. Otherwise start a
  new session (step 3).

## 2. Before each launch — every time, never reused

1. **Tickets and pull requests, now**:

   ```bash
   gh issue list --state open --limit 200 --json number,title,milestone,labels
   gh pr list --state open --json number,title,isDraft,headRefName
   ```

   **Tickets created by the sessions of the batch** — the orchestrator never
   relies on a note alone to know them; it reads them on GitHub:
   - the sub-tickets of every ticket of the batch, launched or not:
     `gh api repos/Tetram76/bdtheque-claude/issues/<n>/sub_issues --jq '.[] | "#\(.number) \(.state) \(.title)"'`;
   - the tickets opened since the batch started (a problem a session
     reported, a review point deferred to a ticket):
     `gh issue list --state open --search "created:>=<start date of the batch>" --json number,title,milestone`.

   Each of them is a candidate of step 2 like any other ticket, and each
   sub-ticket counts in the number of chantiers of the batch.
2. **Candidates**: open tickets only, none invented; drop those already in
   progress (a pull request, a session of the batch). A sub-ticket ranks as
   its parent ticket would (a sub-ticket of a plan ticket is a plan
   chantier) and depends on the sub-tickets its parent orders before it.
   Order them:
   CI stabilization (a flaky test, an intermittent failure — not an
   optimization) → the functional consistency check, when due (its tracking
   ticket, labels `ergonomy-audit` and `harness`, bears no check of the
   current calendar week, done or in progress; checked by the
   orchestrator alone, once for the batch,
   `controle-de-coherence-a-la-reprise-du-travail.md`) → plan of
   `.speckit/suivi-implementation/` → milestone of the next version → the rest. Then drop each one that would touch the same
   files as a chantier in progress, or that depends on one not merged yet.
   Read the body of the remaining first candidates to check this.
3. **Resources, now** (PowerShell, every command bounded in time):

   ```powershell
   $os = Get-CimInstance Win32_OperatingSystem
   "RAM free GB: $([math]::Round($os.FreePhysicalMemory/1MB,1))"
   "CPU load %: $((Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average)"
   Get-PSDrive -PSProvider FileSystem | Where-Object Used | ForEach-Object { "$($_.Name): free $([math]::Round($_.Free/1GB,1)) GB" }
   $j = Start-Job { docker info --format '{{.NCPU}} cpu / {{.MemTotal}} mem / {{.ContainersRunning}} running'; docker ps --format '{{.Names}} {{.Status}}' }
   if (Wait-Job $j -Timeout 20) { Receive-Job $j } else { 'docker: no answer in 20 s'; Stop-Job $j }; Remove-Job $j -Force
   ```

   Decide from these figures how many chantiers more fit, within the maximum
   of 3 at once, beside the
   heavy runs in progress (end-to-end tests, captures, `bdtheque-validation-*`
   containers of the functional validation), and launch that many of the
   candidates (step 3) — every free place filled. If none fits, do not
   launch: tell the user which resource holds it back.

## 3. Launch

One session per chantier: `start_session`, `initiation: "user_asked"`,
`context: "fresh"`, `use_worktree: true`, title `#<n> <ticket title>`. The
brief (`prompt`) says:

- the ticket, its milestone, and that the chantier is carried out end to end
  — up to the merge, the closing comment and the lock of the ticket, the
  local branch and worktree cleaned — following `AGENTS.md` to the letter
  (map and core first, then the concepts the chantier needs, naming the main
  ones), the conversation renamed after the ticket;
- what the ticket requires in particular (functional validation or not,
  associated or duplicate tickets, a split expected, decisions of the user to
  ask one at a time);
- that, if it splits the ticket, it carries out the first sub-ticket only and
  reports the others, which are launched as chantiers of the batch.

The `background` says: the batch and the other chantiers in progress (by
ticket and pull request) with the files they may share; the resources measured
at this launch and the rule of the heavy runs (check `docker ps`, bounded,
before any command that starts containers or browsers; wait for another
session's run to end; size the run for the real resources; one functional
validation environment at a time); whether the version survey is due
(`releve-des-versions-a-la-reprise-du-travail.md`), checked once for the batch;
that the functional consistency check
(`controle-de-coherence-a-la-reprise-du-travail.md`) is the orchestrator's,
not the session's, unless the session is the one it was given to;
that the superpowers plugin is forbidden; that it answers in French.

Tell the user, in one line per chantier, which ticket started and why it was
chosen before the others.

## 4. While the batch runs

- Each note of a session of the batch: report it to the user in a line or two,
  with the links of its pull request; never fabricate its result.
- A note from the functional consistency check session (it has no pull
  request): its place is freed once its conclusion is in the body of its
  tracking ticket (labels `ergonomy-audit` and `harness`) — check it there
  (`gh issue view <n> --json body`), with a ticket for each of its follow-ups;
  then archive the session, leave the ticket open, and launch the next
  chantier as after a merge.
- A note that reports a merged (or abandoned) pull request: **check the
  cleanup** and finish it if needed, before launching the next chantier:

  ```bash
  # gh issue/pr view do not expose the lock: the REST API does (a PR is an issue there)
  gh api repos/Tetram76/bdtheque-claude/issues/<n> --jq '"#\(.number) \(.state) locked=\(.locked) reason=\(.active_lock_reason) comments=\(.comments)"'
  gh api repos/Tetram76/bdtheque-claude/issues/<pr> --jq '"#\(.number) \(.state) locked=\(.locked) reason=\(.active_lock_reason)"'
  git ls-remote --heads origin <branch>   # empty: remote branch deleted
  git branch --list <branch>              # empty: local branch deleted
  git worktree list                       # the session's worktree, until archived
  ```

  Then archive the session (`archive_session`, reason « PR #<pr> merged »),
  unless the app already did. Archiving does not always remove its worktree,
  and the app reuses worktree folders for new sessions (a folder's name says
  nothing of its chantier): after it, a worktree of `git worktree list` that
  is the `cwd` of no running session (`list_sessions`) and has nothing
  uncommitted is removed (`git worktree remove`, then `git worktree prune`);
  a local branch with no commit missing from `origin/main`
  (`git rev-list --count origin/main..<branch>` is 0, or its pull request is
  squash-merged) is deleted with `git branch -D`.
  Report to the user in one line what was cleaned.
- A note that reports a split or a new ticket: tell the user which tickets
  join the candidates; they are planned at the next pass of step 2 (now, if a
  place is free), after checking them on GitHub (step 2.1).
- A merge (a note, or `main` that moved) frees a place: back to step 2 —
  tickets, candidates **and resources** read again — then step 3, unasked,
  until the number of chantiers of the batch is reached (sub-tickets count).
