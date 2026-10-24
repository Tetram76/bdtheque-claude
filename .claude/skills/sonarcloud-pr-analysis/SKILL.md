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
`.speckit/gestion-projet/revue-tickets.md` → « Analyse SonarQube Cloud », setup
in `.speckit/gestion-projet/depot.md` → « Analyse de la qualité du code ». This
skill is only the operational procedure; on any divergence, the speckit applies.

The project (`Tetram76_bdtheque-claude`) is public: every query below works
without a token. Never ask the user for a SonarQube Cloud token. The queries
are PowerShell (`Invoke-RestMethod`): neither Python nor `jq` is installed.

## 1. Wait for the analysis of the head commit

The `Code quality` workflow analyzes every push, draft PRs included.

```powershell
$base = 'https://sonarcloud.io/api'; $p = 'Tetram76_bdtheque-claude'; $pr = '<PR>'
$head = gh pr view $pr --json headRefOid -q .headRefOid
$analysis = (Invoke-RestMethod "$base/project_pull_requests/list?project=$p").pullRequests |
    Where-Object key -eq $pr
[pscustomobject]@{ IsHead = $analysis.commit.sha -eq $head; Date = $analysis.analysisDate; Gate = $analysis.status.qualityGateStatus }
```

The analysis is the head's only when `IsHead` is true. Otherwise check the run
(`gh run list --workflow code-quality.yml --branch <branch> -L 1`): still
running → wait for it in the background (never a foreground sleep); failed or
absent (fork, outage, missing `SONAR_TOKEN`) → the analysis is unavailable:
tell the user and go on with the rest of the PR cycle, it does not block. A
failed query is never read as « no analysis »: retry it.

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

Each issue and hotspot is a review finding (`revue-tickets.md` →
« Traiter un retour »): counter-analysis on verified facts in the global
context of the code, root cause, scope of the PR, then fix at the root (one
commit per finding), defer (ticket or plan note) or reject with its reason.
Push fixes while the PR is a draft; on a ready PR, fold them into the next push
of a Codex fix when one is coming.

## 4. Trace deferred and rejected findings

Keep one PR comment, headed `## Analyse SonarQube Cloud`, listing each deferred
or rejected finding (key, rule, file:line, decision, justification), and edit
it (`gh api -X PATCH repos/Tetram76/bdtheque-claude/issues/comments/<id> -F body=@<file>`)
at each handled analysis instead of posting a new one. Never write the Codex
mention in it. The user marks these findings in SonarQube Cloud.
