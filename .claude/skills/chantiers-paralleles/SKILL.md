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
commands, it adds no rule. This session launches and reports; it never
carries out a chantier itself.

## 1. The batch

- The user gives the **number of chantiers** of the batch and the **maximum
  run at once**. Neither is fixed: take the latest the user gave in this
  conversation; ask for one never given, never assume it.
- Count the chantiers already launched (sub-tickets of a split ticket count,
  each one) and the places taken (a pull request not merged yet).

## 2. Before each launch — every time, never reused

1. **Tickets and pull requests, now**:

   ```bash
   gh issue list --state open --limit 200 --json number,title,milestone,labels
   gh pr list --state open --json number,title,isDraft,headRefName
   ```

   and the sub-tickets of every chantier of the batch:
   `gh api repos/Tetram76/bdtheque-claude/issues/<n>/sub_issues --jq '.[] | "#\(.number) \(.state) \(.title)"'`.
2. **Candidates**: open tickets only, none invented; drop those already in
   progress (a pull request, a session of the batch). Order them:
   CI stabilization (a flaky test, an intermittent failure — not an
   optimization) → plan of `.speckit/suivi-implementation/` → milestone of the
   next version → the rest. Then drop each one that would touch the same
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

   Decide from these figures whether one chantier more fits, beside the
   heavy runs in progress (end-to-end tests, captures, `bdtheque-validation-*`
   containers of the functional validation). If not, do not launch: tell the
   user which resource holds it back.

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
- that its sub-tickets, if it splits the ticket, are carried out by the same
  session, one after the other.

The `background` says: the batch and the other chantiers in progress (by
ticket and pull request) with the files they may share; the resources measured
at this launch and the rule of the heavy runs (check `docker ps`, bounded,
before any command that starts containers or browsers; wait for another
session's run to end; size the run for the real resources; one functional
validation environment at a time); whether the version survey is due
(`releve-des-versions-a-la-reprise-du-travail.md`), checked once for the batch;
that the superpowers plugin is forbidden; that it answers in French.

Tell the user, in one line per chantier, which ticket started and why it was
chosen before the others.

## 4. While the batch runs

- Each note of a session of the batch: report it to the user in a line or two,
  with the links of its pull request; never fabricate its result.
- A merge (a note, or `main` that moved) frees a place: back to step 2 —
  tickets, candidates **and resources** read again — then step 3, unasked,
  until the batch count is reached.
- A session that splits its ticket keeps its place for the sub-tickets; the
  batch count includes them.
