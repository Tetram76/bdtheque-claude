#Requires -Version 7.3
<#
.SYNOPSIS
    Before a command that starts a moment of the work, reminds the agent of the .speckit concepts to read.

.DESCRIPTION
    PreToolUse hook of the repository (.claude/settings.json). Some rules only apply at a given moment of the work
    (creating a pull request, pushing, merging…): they are not read at the start of every conversation, and the table
    of moments of AGENTS.md says which concepts each moment requires. This hook reads that table, the only source of
    the mapping, and, when the command about to run is one of a moment, adds its concepts to the agent's context. It
    never blocks the command.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Claude Code exchanges UTF-8; the Windows console encoding would mangle the accents of the table.
[Console]::InputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)

$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
# A tool of a GitHub MCP server is named mcp__<server>__<tool>: its last part stands for the command.
$command = if ($payload.tool_name -like 'mcp__*') { ($payload.tool_name -split '__')[-1] } else { $payload.tool_input.command }
if (-not $command) { exit 0 }

$agents = Join-Path $PSScriptRoot '../../AGENTS.md'
$moments = foreach ($line in [IO.File]::ReadLines($agents)) {
    $cells = $line -split '\|'
    if ($cells.Count -lt 5) { continue }
    $commands = @([regex]::Matches($cells[2], '`([^`]+)`') | ForEach-Object { $_.Groups[1].Value })
    if (-not $commands) { continue }
    [pscustomobject]@{
        Moment = $cells[1].Trim()
        Commands = $commands
        Concepts = @([regex]::Matches($cells[3], '\]\((?<path>[^)]+)\)') | ForEach-Object { $_.Groups['path'].Value })
    }
}

# A command line may chain several commands; each is compared with the commands of the moments.
$parts = $command -split '&&|\|\||;|\r?\n' | ForEach-Object { $_.Trim() }
$started = @($moments | Where-Object { $m = $_; $parts | Where-Object { $p = $_; $m.Commands | Where-Object { $p.StartsWith($_) } } })
if (-not $started) { exit 0 }

$lines = foreach ($m in $started) { "- $($m.Moment) : $($m.Concepts -join ', ')" }
$context = "Moment de la table des moments d'AGENTS.md : avant d'exécuter cette commande, lire en entier ceux de ces concepts qui ne l'ont pas été depuis leur dernière modification.`n" + ($lines -join "`n")
@{ hookSpecificOutput = @{ hookEventName = 'PreToolUse'; additionalContext = $context } } | ConvertTo-Json -Depth 3 -Compress
