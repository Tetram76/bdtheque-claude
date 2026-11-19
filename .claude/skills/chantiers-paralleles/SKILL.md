---
name: chantiers-paralleles
description: >-
  Runs a batch of chantiers in parallel on this repository, each in its own
  session and worktree, to complete the current milestone: picks them among
  its open tickets (within the user's scope, if any) in the order of priority
  of the .speckit, linking tickets on close subjects into one chantier,
  briefs each session to notify the orchestrator of major
  events and to run its own time-boxed watches, evaluates the
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

A batch serves to **complete the current milestone**, never to anticipate
future work: its chantiers are chosen among that milestone's tickets only
(step 2.2). The user may also **restrict the candidates to a scope** (a list
of tickets, a label, a screen, a part of the plan…): take the latest scope the
user gave in this conversation; only the tickets within it are candidates.

## 1. The batch

- A batch is **always run in parallel**, never one chantier after the other
  by choice: **3 at once at most**, fewer only when the resources (step 2.3)
  or a dependency between chantiers require it. Never ask the user how many
  at once.
- The user gives the **number of chantiers** of the batch. It is not fixed:
  take the latest the user gave in this conversation; never assume it. If
  never given, ask for it **only after** step 2.2: once every candidate is
  identified and ranked (dependencies and independence checked), present the
  ranked candidates and the dropped ones, then ask how many chantiers the
  batch counts — never before, the user deciding from that ranking.
- Count the places taken: a chantier whose pull request is not merged yet.
  A harness never takes a place nor counts in the batch (below).
- A sub-ticket is a chantier like the others. When it is launched, give it to
  the session that split its ticket only if that session's context suits it
  (what it knows serves the sub-ticket, and its conversation is not too
  loaded to carry it end to end — read it with `get_session` /
  `list_events`): then send it the brief with `SendMessage`. Otherwise start a
  new session (step 3).

## Harnesses: handled, never chantiers of the batch

The harnesses due when work resumes — version survey
(`releve-des-versions-a-la-reprise-du-travail.md`), quality treatment
(`traitement-du-controle-qualite-a-la-reprise-du-travail.md`), functional
consistency check (`controle-de-coherence-a-la-reprise-du-travail.md`) — are
handled during the batch, but are **not chantiers of the batch**: never in the
order of priority, never counted in the number of chantiers the user set,
never taking one of the places of the chantiers run at once.

- **Checked once for the batch, by the orchestrator alone**, at its start:
  read each tracking ticket (labels `security-audit`, `quality-audit`,
  `ergonomy-audit`, each with `harness`) and decide from those concepts which
  are due. Give the due ones to **one separate session** (step 3, same
  launch, title `Harnais <week or date>`), whose brief names them and the
  concepts to follow, with the same notification, exchange and watch rules
  as a chantier's brief (step 3); no chantier session checks or runs them.
- **A pull request opened by a harness itself** (monthly or security version
  bump, corrections of the quality treatment), with no ticket of its own, is
  part of that harness: its session carries it to the merge; it is not a
  chantier of the batch. It counts for the **independence** of the chantiers
  (step 2.2): a chantier touching the same files waits for its merge.
- **Follow-up tickets** a harness opens (consistency gap, major version bump
  of the stack and its review, improvement found by a review, deferred
  quality entry) are tickets like any other: candidates when they are in the
  current milestone.
- Its heavy runs count in the resources (step 2.3) like any session's.

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
   optimization) → plan of
   `.speckit/suivi-implementation/` **in** the milestone of the next version →
   the other tickets of that milestone. A ticket outside the current
   milestone (another milestone or none, plan tickets included) is **not a
   candidate**, unless the user asked for it explicitly for the batch; only
   CI stabilization (any milestone) is exempt. A harness is never a
   candidate (above).

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
   - name its **reference ticket**, the one its single pull request closes;
     the others are associated with it through GitHub's relations, cited
     without a closing keyword (`suivi-par-tickets.md`, correspondence;
     `lien-entre-pull-requests-et-issues.md`);
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
   of 3 at once, beside the
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
   - each selected chantier: all its tickets (`#<n>` and title of each, the
     reference ticket named for linked tickets), rank in the order of
     priority, state — done (PR merged), in progress (its PR), launched now,
     or to come, with what it waits for (a free place, a dependency: name
     it). The ones to come are the current choice, which the next pass may
     change;
   - the candidates dropped, each with its reason (independence, dependency),
     and the shortfall when the selection counts fewer chantiers than the
     batch for lack of candidates.

   Then launch (step 3) without waiting: the number is the user's, who may
   correct it at any time.

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
  ask one at a time); for linked tickets, every ticket of the group, the
  reference one its pull request closes, and the others to associate with
  it, each closed or commented at the merge as `suivi-par-tickets.md` says;
- that, if it splits the ticket, it carries out the first sub-ticket only and
  reports the others, which are launched as chantiers of the batch;
- that it **notifies the orchestrator** with `SendMessage` (`to`: the
  orchestrator's session id, given in the brief) at **each major event, and
  only then**: the end of its chantier (pull request merged, ticket commented
  and locked, branches and worktree cleaned), with the pull request, the
  squashed commit and what is left to do; its abandonment; a problem that
  blocks it or goes beyond its chantier; a split of its ticket or a ticket
  created; a wait on the user (decision, functional validation). It never
  reports its progress at regular intervals;
- that it may exchange directly with the sessions of the other chantiers of
  the batch, only for information **useful** in the light of the context its
  brief gives (coordinating a heavy run or a shared environment, a file both
  chantiers touch, a finding that concerns the other chantier); nothing else;
- that the **watches** of its chantier (CI, Codex review, SonarQube Cloud
  analysis…) are its own: nobody does them for it, neither the orchestrator
  nor the user. Each watch is **time-boxed** (maximum delay set from the
  expected duration, `regles-de-travail-de-l-agent.md`): when the delay
  expires, it reads the real state itself (checks, comments, reactions)
  instead of trusting the silence of its watching script — a badly written
  script may detect nothing without noticing it is badly written.

The `background` says: the batch and the other chantiers in progress (by
ticket, pull request and **session id**) with the files they may share; the
orchestrator's session id; the resources measured
at this launch and the rule of the heavy runs (check `docker ps`, bounded,
before any command that starts containers or browsers; wait for another
session's run to end; size the run for the real resources; one functional
validation environment at a time); that the harnesses (version survey,
quality treatment, functional consistency check) are checked by the
orchestrator once for the batch and run by the harness session, not by a
chantier session, with the harness pull request open if any;
that the superpowers plugin is forbidden; that it answers in French.

Tell the user, in one line per chantier, which ticket started and why it was
chosen before the others; the whole selection was presented at step 2.4.

## 4. While the batch runs

- Each message or note of a session of the batch: report it to the user in a
  line or two, with the links of its pull request; never fabricate its
  result. A message is the session's report, not a proof: check on GitHub
  what it announces (cleanup, tickets created) before acting on it. The notes
  the app relays at the end of a session's turn do not always say whether the
  chantier is over, and an archived session sends none: when a session goes
  quiet, check its pull request on GitHub.
- A note from the harness session: it frees no place. Once every harness
  given to it is done, check its trace in the body of each tracking ticket
  (`gh issue view <n> --json body`), a ticket for each follow-up, and its
  pull request merged, cleaned as below; then archive the session and leave
  the tracking tickets open. Its follow-up tickets join the candidates
  (step 2.1).
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
  join the candidates (within the user's scope, if any); they are planned at the next pass of step 2 (now, if a
  place is free), after checking them on GitHub (step 2.1).
- A merge (a note, or `main` that moved) frees a place: back to step 2 —
  tickets, candidates **and resources** read again, the selection presented
  again — then step 3, unasked,
  until the number of chantiers of the batch is reached (sub-tickets count).
