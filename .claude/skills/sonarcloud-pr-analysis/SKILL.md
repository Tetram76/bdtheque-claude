---
name: sonarcloud-pr-analysis
description: >-
  Reads and handles the SonarQube Cloud analysis of a pull request on this
  repository (new issues, security hotspots, quality gate) for its head commit.
  Use before marking a draft PR ready for review, after each push to a PR that
  touches analyzed files, before merging, or when the user asks to "traiter
  l'analyse Sonar" or similar.
---

# SonarQube Cloud PR analysis

The `.speckit/` is the source of truth: policy in
`.speckit/gestion-projet/revue-tickets/analyse-sonarqube-cloud.md`, setup in
`.speckit/gestion-projet/depot/analyse-de-la-qualite-du-code.md`. This
skill is only the operational procedure; on any divergence, the speckit applies.

The project (`Tetram76_bdtheque-claude`) is public: every query below works
without a token. Never ask the user for a SonarQube Cloud token. The queries
are PowerShell (`Invoke-RestMethod`): neither Python nor `jq` is installed.

## 1. Wait for the analysis of the head commit

The `SonarQube Cloud analysis` job of the CI analyzes every push, draft PRs included.

```powershell
$base = 'https://sonarcloud.io/api'; $p = 'Tetram76_bdtheque-claude'; $pr = '<PR>'
$head = gh pr view $pr --json headRefOid -q .headRefOid
$analysis = (Invoke-RestMethod "$base/project_pull_requests/list?project=$p").pullRequests |
    Where-Object key -eq $pr
[pscustomobject]@{ IsHead = $analysis.commit.sha -eq $head; Date = $analysis.analysisDate; Gate = $analysis.status.qualityGateStatus }
```

The analysis is the head's only when `IsHead` is true. Otherwise check the run
(the `SonarQube Cloud analysis` job of `gh run list --workflow ci.yml --branch <branch> -L 1`): still
running → wait for it in the background (never a foreground sleep); failed or
absent → no quality gate, so the PR cannot be merged (the Ruleset requires the
`SonarCloud Code Analysis` check): find the cause of a failed run and fix it;
an outage of the service or a fork PR (no `SONAR_TOKEN`) is reported to the
user, the merge waiting. A failed query is never read as « no analysis »:
retry it.

## 2. Read the findings

```powershell
(Invoke-RestMethod "$base/qualitygates/project_status?projectKey=$p&pullRequest=$pr").projectStatus
(Invoke-RestMethod "$base/issues/search?componentKeys=$p&pullRequest=$pr&resolved=false&ps=500").issues |
    Select-Object key, rule, severity, type, component, line, message
(Invoke-RestMethod "$base/hotspots/search?projectKey=$p&pullRequest=$pr&status=TO_REVIEW&ps=500").hotspots |
    Select-Object key, ruleKey, vulnerabilityProbability, component, line, message
```

A rule's rationale: `(Invoke-RestMethod "$base/rules/show?key=<rule>&organization=tetram76").rule.htmlDesc`.

## 3. Handle each finding

Each issue and hotspot is a review finding (`gestion-projet/revue-tickets/revue-codex-bloquante.md`,
« Traiter un retour »): counter-analysis on verified facts in the global
context of the code, root cause, scope of the PR, then fix at the root (one
commit per finding), defer (ticket or plan note) or reject with its reason.
Push fixes while the PR is a draft; on a ready PR, fold them into the next push
of a Codex fix when one is coming.

The quality gate must pass before marking the PR ready and before merging:
deferred and rejected findings stop counting once marked (step 5); a coverage
shortfall is fixed by the missing tests, or, for a file the .NET tests cannot
cover, by a coverage exclusion justified in `gestion-projet/depot/analyse-de-la-qualite-du-code.md`. Once findings are
marked, check that the `SonarCloud Code Analysis` check of the head commit
shows the gate passed (`gh pr checks <PR>`); if it still shows the earlier
verdict, re-run the analysis job (`gh run rerun <run-id>`), which publishes a
fresh one without a new commit.

## 4. Trace deferred and rejected findings

Keep one PR comment, headed `## Analyse SonarQube Cloud`, listing each deferred
or rejected finding (key, rule, file:line, decision, justification), and edit
it (`gh api -X PATCH repos/Tetram76/bdtheque-claude/issues/comments/<id> -F body=@<file>`)
at each handled analysis instead of posting a new one. Never write the Codex
mention in it.

## 5. Mark the deferred and rejected findings in SonarQube Cloud

Marking them is the agent's job, never handed to the user. Do it through the
web API, authenticated by the user's personal token that the `SONAR_TOKEN`
environment variable of the workstation holds (`gestion-projet/depot/repository.md`).
Never print, log or echo the token, and never act through a browser session.
If the variable is missing, say so and ask the user to create the token in
SonarQube Cloud (My Account → Security) and set it as a user environment
variable, without ever sending its value in the conversation.

```powershell
$base = 'https://sonarcloud.io/api'
$auth = @{ Authorization = "Bearer $env:SONAR_TOKEN" }
# per finding: the transition with its status change comment; $share decided
# case by case (below)
Invoke-RestMethod -Method Post "$base/issues/do_transition" -Headers $auth -Body @{
    issue = $key; transition = 'falsepositive'   # or 'accept'
    comment = $reason; isFeedback = $share }     # 'true' or 'false'
# per security hotspot reviewed and judged safe (no sharing with Sonar in the API)
Invoke-RestMethod -Method Post "$base/hotspots/change_status" -Headers $auth -Body @{
    hotspot = $key; status = 'REVIEWED'; resolution = 'SAFE'; comment = $reason }
```

`$reason` is written in English, like every comment posted in SonarQube Cloud
(issue or hotspot): its readers, on the public project or in Sonar's teams
when shared, do not know the project and may not read French. It states the exact reason,
detailed enough to stand on its own for a reader who knows neither the project
nor the PR: what the flagged code does; why the rule's premise is wrong there,
or why the rule does not fit the case, or, for a deferred fix, what makes the
finding valid and which ticket or plan note carries the fix. It ends with the
link to the PR comment (`Justification in the PR: <comment URL>`), a plus that
never replaces the reason: a comment that only points to the PR is not enough.
The PR comment of step 4, on GitHub, stays in French.

`$share` is decided per finding: share (`'true'`) only when the comment
teaches Sonar's teams something about the analysis itself, which they read
with the file and the rule to improve it. As a rule: share a `falsepositive`
(the rule fired wrongly), and an `accept` for a rule that ignores a legitimate,
general use of the language or of a library; do not share (`'false'`) an
`accept` whose fix is deferred (the rule was right), nor one for a project
convention or trade-off that says nothing about the rule's accuracy.

A shared comment leaves the project, and the public SonarQube Cloud project
already shows the others to anyone: every marking comment, issue or hotspot,
holds nothing the public repository does not already show. Never put in it a secret, token or
credential (even partial), an address, host name, port or path of the
workstation, the NAS or the internal network, personal data (email, name of a
person other than an already public comic author), data of the user's
collection, or content of a conversation with the user or of a non-public
ticket. State the reason through the code, the rule and the expected behaviour.
Re-read each `$reason` for such information before sending it.

The same token serves the project settings that the analysis configuration
does not carry (e.g. the new code definition, the quality gate of the project),
through the matching endpoints of the web API.

`falsepositive` for a finding whose premise is wrong and that no simpler design would satisfy — a finding raised because
the rule cannot follow the code (an indirection) first points to a possible design defect, looked for and fixed before
any rejection (`gestion-projet/revue-tickets/analyse-sonarqube-cloud.md`); `accept` for a rule that
does not fit the case, and for a valid finding whose fix is deferred (the
comment then names the ticket or plan note that carries the fix): accepted
means acknowledged but not fixed now, and an open finding would fail the gate.
Check the outcome with `/api/issues/search?issues=<keys>&additionalFields=comments`
(`issueStatus` `FALSE_POSITIVE` or `ACCEPTED`, one comment each, carrying the
full reason).
