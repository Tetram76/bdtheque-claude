---
name: chantiers-paralleles
description: >-
  Runs a batch of chantiers in parallel on this repository, each in its own
  session and worktree, to complete the current milestone first: picks them
  among all its open tickets (within the user's scope, if any) in the order of
  priority of the .speckit, the current milestone's tickets first, linking tickets on close subjects into one chantier,
  briefs each session with what is specific to the batch only, never how to
  do its work, relays the user's instructions to the sessions, evaluates the
  resources of the machine before each
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

The orchestrator **never commits**, so it has **no branch and no worktree**:
it works from the main checkout, read-only (`gh`, `git fetch`,
`git worktree list`), and never runs `git switch` there, other sessions using
it. If this session was opened in a worktree (`git rev-parse --show-toplevel`
under `.claude/worktrees/`), say so to the user in your first message; when
the session is archived, its worktree and branch are removed like a
chantier's (step 4, cleanup).

A batch serves to **complete the current milestone first**: the current
milestone has priority, it is not exclusive. Its chantiers are chosen among
all the open tickets, that milestone's tickets before the others (step 2.2). The user may also **restrict the candidates to a scope** (a list
of tickets, a label, a screen, a part of the plan…): take the latest scope the
user gave in this conversation; only the tickets within it are candidates.

## 1. The batch

- A batch is **always run in parallel**, never one chantier after the other
  by choice: **3 at once at most**, the harness session included (below),
  fewer only when the resources (step 2.3)
  or a dependency between chantiers require it. Never ask the user how many
  at once.
- The user gives the **number of chantiers** of the batch. It is not fixed:
  take the latest the user gave in this conversation; never assume it. If
  never given, ask for it **only after** step 2.2: once every candidate is
  identified and ranked (dependencies and independence checked), present the
  ranked candidates and the dropped ones, then ask how many chantiers the
  batch counts — never before, the user deciding from that ranking.
- Count the places taken: a chantier whose pull request is not merged yet,
  and the harness session until every harness given to it is done (its
  pull request merged included). A harness never counts in the number of
  chantiers of the batch (below), but its session takes a place.
- A sub-ticket is a chantier like the others. When it is launched, give it to
  the session that split its ticket only if that session's context suits it
  (what it knows serves the sub-ticket, and its conversation is not too
  loaded to carry it end to end — read it with `get_session` /
  `list_events`): then send it the brief with `SendMessage`. Otherwise start a
  new session (step 3).

## Harnesses: never counted as chantiers, run as one

The harnesses due when work resumes — version survey
(`releve-des-versions-a-la-reprise-du-travail.md`), quality treatment
(`traitement-du-controle-qualite-a-la-reprise-du-travail.md`), functional
consistency check (`controle-de-coherence-a-la-reprise-du-travail.md`) — are
handled during the batch, but are **not counted as chantiers of the batch**:
never in the order of priority, never counted in the number of chantiers the
user set. They are **run as a chantier**, though: their session **takes one
of the places** of the chantiers run at once — it counts in the maximum of 3,
from its launch until every harness given to it is done, its pull request
merged included; once freed, its place is filled like a chantier's.

- **Checked once for the batch, by the orchestrator alone**, at its start:
  read each tracking ticket (labels `security-audit`, `quality-audit`,
  `ergonomy-audit`, each with `harness`) and decide from those concepts which
  are due. Give the due ones to **one separate session** (step 3, launched
  like a chantier's, resources evaluated, in one of the places; title
  `Harnais <week or date>`), whose brief names the harnesses given to it and
  is otherwise a chantier's brief (step 3); no chantier session checks or
  runs them.
- **A pull request opened by a harness itself** (monthly or security version
  bump, corrections of the quality treatment), with no ticket of its own, is
  part of that harness: its session carries it to the merge; it is not a
  chantier of the batch. It counts for the **independence** of the chantiers
  (step 2.2): a chantier touching the same files waits for its merge.
- **Follow-up tickets**: the harness session follows the rule on creating
  tickets (`.speckit/gestion-projet/revue-tickets/issues.md`, « Décision de
  créer un ticket »): what a harness treatment can fix is fixed in its pull
  request. What it does open as a ticket (consistency gap, the check having
  no pull request; major version bump of the stack, its review of the
  existing code included; improvement too big for a bump's pull request;
  deferred quality entry) is a ticket like any other: a candidate like any
  open ticket.
- Besides its place, its heavy runs count in the resources (step 2.3) like
  any session's.

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
   optimization, any milestone) → plan of
   `.speckit/suivi-implementation/` **in** the milestone of the next version →
   the other tickets of that milestone → plan chantiers outside that milestone
   (another milestone or none) → the other tickets outside it. The current
   milestone has priority, it is not exclusive: a ticket outside it is a
   candidate like any open ticket, planned the same way, **after** those of
   the current milestone. A harness is never a candidate (above).

   **Scope of the user**, if given: keep only the tickets within it, and the
   sub-tickets of those tickets. It adds to the criteria above and to the
   checks below, never lifts them, except for the tickets the user names
   explicitly. A ticket created during the batch outside the scope is not a
   candidate. When the extent of the scope is ambiguous, ask the user before
   ranking.

   Then, **for each candidate you consider, in that order, you check yourself
   both conditions below** — never one without the other, never left to the
   chantier's session or to the user. Read the **whole body** of the ticket,
   and its line in `.speckit/suivi-implementation/` with the paragraph that
   introduces it when it is a plan ticket:
   - **Independence**: it does not touch the same files as a chantier in
     progress, or as the open pull request of a harness (same screen, theme,
     workflow, test base).
   - **Dependencies**: every precondition that the ticket or its plan line
     sets is met. A dependency is **any** precondition, not only another
     chantier: a prerequisite ticket not merged yet, a sub-ticket its parent
     orders before it, a published version, a production deployment
     (`.speckit/journal-evenements.md`), a calendar (« sans objet avant… »,
     « à réaliser avant… », a « Calendrier » section). A ticket with an unmet
     precondition is **not a candidate**, whatever its rank, even when the
     plan lists it (e.g. #267, « sans objet avant la première mise en
     production »).

   Drop each candidate that fails either check, and go on to the next one.

   **Linked tickets**: among the candidates kept, you may link tickets on
   close subjects into **one chantier** — overlapping tickets (same files,
   same component) are good ones to link, instead of dropping one for
   independence until the other is merged. For a group:
   - check independence (against the chantiers in progress and a harness
     pull request) and dependencies for **each** of its tickets, not only
     one: a ticket failing either check is not a candidate and joins no
     group;
   - keep the **volume of changes** of the group « raisonnable »: its pull
     request stays contained enough to be reviewed and validated in one go —
     it may carry more changes than a single ticket, never so much that its
     review or functional validation gets hard; a group that would exceed it
     is reduced, the tickets taken out staying chantiers of their own;
   - its single pull request **fully handles each** of its tickets, as if
     each had its own pull request — a derogation to « un ticket, une PR »
     the user wants: no reference ticket, no associated ticket; the pull
     request cites every ticket with a closing keyword, and each is closed,
     commented and locked at the merge (`suivi-par-tickets.md`, tickets
     reliés; `lien-entre-pull-requests-et-issues.md`);
   - it is one chantier: one session, one place, one in the number of the
     batch, ranked as the best ranked of its tickets.
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
   of 3 at once (the harness session, while it runs, taking one), beside the
   heavy runs in progress (end-to-end tests, captures, `bdtheque-validation-*`
   containers of the functional validation), and launch that many of the
   candidates (step 3) — every free place filled. If none fits, do not
   launch: tell the user which resource holds it back.
4. **Present the whole selection to the user**, before starting any session
   (step 3), at every pass of step 2 — the first launch and each freed
   place. Never only the chantiers launched now: without the whole
   selection, the user cannot confirm the number of chantiers of the batch.
   In French, as a table or a list:
   - the scope the user set, if any;
   - the number of chantiers of the batch the user set, and how many the
     selection counts (sub-tickets included);
   - each selected chantier: all its tickets (`#<n>` and title of each), rank in the order of
     priority, state — done (PR merged), in progress (its PR), launched now,
     or to come, with what it waits for (a free place, a dependency: name
     it). The ones to come are the current choice, which the next pass may
     change;
   - the harness session, when harnesses are due: the harnesses it handles,
     its state and the place it takes, outside the number of chantiers of
     the selection;
   - the candidates dropped, each with its reason (independence, dependency),
     and the shortfall when the selection counts fewer chantiers than the
     batch for lack of candidates.

   Then launch (step 3) without waiting: the number is the user's, who may
   correct it at any time.

## 3. Launch

One session per chantier: `start_session`, `initiation: "user_asked"`,
`context: "fresh"`, `use_worktree: true`, title `#<n> <ticket title>`.

The brief tells the session **only what is specific to the batch and that it
cannot find by itself** (« Brief d'une session » of the concept). It never
tells it how to do its work: the session applies `AGENTS.md` and the
`.speckit/` by itself (reading the map, the core and what its chantier
requires; pull request, review, validation and closing rules). So the brief
gives **no list of concepts** (not even « the main ones », which a session
takes for the scope of its reading), **copies no rule** of the `.speckit/`
and says nothing of how to carry out the chantier. Written in French, the
brief (`prompt`, and `background` for the rest) holds:

- its ticket and its title, or every ticket of a group of linked tickets;
  for the harness session, the harnesses given to it;
- the orchestrator's session id, and the session ids of the other sessions
  of the batch;
- the other chantiers in progress (by ticket, pull request and session id)
  and the files they may share with this one;
- the harnesses, kept apart: which are due and which session runs them
  (with its pull request, if any), or that none is due;
- the resources measured at this launch and how they are shared (e.g. how to
  start Docker on this machine, a heavy run in progress elsewhere);
- the pointer to `.speckit/gestion-projet/gouvernance/chantiers-en-parallele.md`
  for what the sessions of a batch do in their own right (notifications,
  tickets submitted to the orchestrator, exchanges, watches), never a copy
  of it.

Tell the user, in one line per chantier, which ticket started and why it was
chosen before the others; the whole selection was presented at step 2.4.

## 4. While the batch runs

- **Relay, never instruct**: the orchestrator relays the user's instructions
  to the sessions (`SendMessage`) and helps the sessions communicate with
  each other (e.g. a session id, a shared file, a heavy run to coordinate).
  It sends them no instruction of its own on how to do their work, not even
  after an incident: never a copy of a rule of the `.speckit/`, nor a list of
  concepts to read.
- **A message may be read very late, or lost** (« Messages lus tard, voire
  perdus » of the concept): a successful `SendMessage` only says it reached
  the session, not that it was read; silence is neither a reply nor an
  agreement. Never expect an immediate reply, never wait for one
  indefinitely: go on with what does not depend on it; wait for a needed
  reply within a bounded delay, then check the real state (GitHub,
  `get_session` / `list_events` of the session) and send it again if still
  needed.
- Each message or note of a session of the batch: report it to the user in a
  line or two, with the links of its pull request; never fabricate its
  result. A message is the session's report, not a proof: check on GitHub
  what it announces (cleanup, tickets created) before acting on it. The notes
  the app relays at the end of a session's turn do not always say whether the
  chantier is over, and an archived session sends none: when a session goes
  quiet, check its pull request on GitHub.
- A note from the harness session: once every harness given to it is done,
  check its trace in the body of each tracking ticket
  (`gh issue view <n> --json body`), a ticket for each follow-up, and its
  pull request merged, cleaned as below; then archive the session and leave
  the tracking tickets open. Its place is then free: back to step 2, like
  after a merge. Its follow-up tickets join the candidates (step 2.1).
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
- A ticket a session submits before creating it, or a problem it reports:
  never turn it into a ticket yourself. Check that no chantier of the batch
  in progress already handles it (if one does, tell that chantier's session,
  with `SendMessage`, and no ticket is created); otherwise, if the session
  really cannot fix it in its pull request (`issues.md`, « Décision de créer
  un ticket »), agree, and the session creates it. A problem the **user**
  reports stays a ticket.
- A note that reports a split or a new ticket: tell the user which tickets
  join the candidates (within the user's scope, if any); they are planned at the next pass of step 2 (now, if a
  place is free), after checking them on GitHub (step 2.1).
- A merge (a note, or `main` that moved) frees a place: back to step 2 —
  tickets, candidates **and resources** read again, the selection presented
  again — then step 3, unasked,
  until the number of chantiers of the batch is reached (sub-tickets count).
