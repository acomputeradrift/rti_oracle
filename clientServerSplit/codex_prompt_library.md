# Codex Prompt Library

## Purpose

This document stores the reusable prompts for the multi-agent execution model so the user only needs to paste a single orchestrator prompt.

The orchestrator should read this file and reuse the worker and integration prompt templates from here instead of requiring the user to paste multiple prompts manually.

## Orchestrator Rule

The user should only need to start the run with one orchestrator prompt.

After that:

- the orchestrator reads this file
- the orchestrator reuses the templates below
- the orchestrator issues worker instructions based on these templates
- the orchestrator manages the flow without requiring manual prompt construction by the user

## Worker Prompt Template

Use this template for any worker packet.

```text
You are Agent #<N> for this repository.

Your orchestrator is authoritative for this run. Follow the assigned packet exactly. Do not expand scope on your own.

Read these documents in order:
1. /mac/Home/Desktop/Development/Oracle/clientServerSplit/README.md
2. /mac/Home/Desktop/Development/Oracle/clientServerSplit/project_north_star.md
3. /mac/Home/Desktop/Development/Oracle/clientServerSplit/agent_<N>_<packet_name>_brief.md
4. /mac/Home/Desktop/Development/Oracle/clientServerSplit/agent_handoff_template.md

Your assigned branch:
- <branch-name>

Your rules:
- Stay inside allowed files only.
- Do not modify forbidden files.
- Own your packet's unit tests.
- Commit at logical milestones with clear, specific commit messages.
- Do not perform unrelated cleanup.
- If you need a forbidden file or a scope change, stop and report back to the orchestrator.
- When your packet is complete, blocked, needs re-scope, or failed, report using the handoff template.

Begin by confirming:
1. your packet
2. your allowed files
3. your forbidden files
4. your first logical commit checkpoint
```

## Phase 1 Worker Mappings

These are the standard worker prompt substitutions for the first wave.

### Agent #1

- `brief`: `agent_1_log_filter_service_brief.md`
- `branch`: `agent-1-log-filter-service`

### Agent #2

- `brief`: `agent_2_status_message_formatter_brief.md`
- `branch`: `agent-2-status-message-formatter`

### Agent #3

- `brief`: `agent_3_structured_message_parser_brief.md`
- `branch`: `agent-3-structured-message-parser`

### Agent #4

- `brief`: `agent_4_log_search_service_brief.md`
- `branch`: `agent-4-log-search-service`

### Agent #5

- `brief`: `agent_5_tagged_diagnostics_analyzer_brief.md`
- `branch`: `agent-5-tagged-diagnostics-analyzer`

## Phase 2 Worker Mappings

These are the standard worker prompt substitutions for the later controlled wave.

### Agent #6

- `brief`: `agent_6_project_processing_coordinator_brief.md`
- `branch`: `agent-6-project-processing-coordinator`

### Agent #7

- `brief`: `agent_7_loglevel_command_coordinator_brief.md`
- `branch`: `agent-7-loglevel-command-coordinator`

### Agent #8

- `brief`: `agent_8_mainwindow_integration_brief.md`
- `branch`: `agent-8-mainwindow-integration`

## Integration / Validation Prompt Template

Use this template for the designated integration or validation agent.

```text
You are the integration and validation agent for this repository.

Read these documents in order:
1. /mac/Home/Desktop/Development/Oracle/clientServerSplit/README.md
2. /mac/Home/Desktop/Development/Oracle/clientServerSplit/project_north_star.md
3. /mac/Home/Desktop/Development/Oracle/clientServerSplit/multi_agent_git_workflow.md
4. /mac/Home/Desktop/Development/Oracle/clientServerSplit/phase_1_execution_checklist.md
5. /mac/Home/Desktop/Development/Oracle/clientServerSplit/phase_2_execution_checklist.md
6. /mac/Home/Desktop/Development/Oracle/clientServerSplit/agent_8_mainwindow_integration_brief.md
7. /mac/Home/Desktop/Development/Oracle/clientServerSplit/agent_handoff_template.md

Your job:
- merge completed packet branches in the documented order
- resolve conflicts on the integration branch only
- preserve packet boundaries and contracts
- perform the MainWindow integration only in the designated single-agent step
- run the full repository validation sequence
- make narrow regression fixes only when required for integration correctness
- commit at logical integration milestones with clear commit messages

Use the documented integration branch strategy. Start by confirming:
1. the integration branch name
2. the merge order
3. the validation sequence you will run
4. the criteria you will use to accept or reject the phase
```

## One-Shot Usage

The orchestrator prompt should instruct the orchestrator to:

1. read this prompt library
2. use the templates in this file for worker assignment
3. manage all worker prompting from this file

That keeps the user to a single pasted prompt while preserving a repeatable process.
