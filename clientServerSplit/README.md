# Client/Server Split Planning Index

## Purpose

This folder contains the planning documents for the RTI Oracle preparatory refactor that:

- reduces LOC
- separates responsibilities
- preserves current behavior and performance
- prepares the codebase for a future client/server split

This folder is documentation-only planning for the current phase.

## Read First

1. `project_north_star.md`
2. `client_server_classification_plan.md`
3. `mainwindow_client_server_classification.md`

These documents define the objective and the architectural lens for the work.

## Multi-Agent Governance

Read these next before any implementation is assigned:

1. `multi_agent_work_rules.md`
2. `multi_agent_git_workflow.md`
3. `orchestrator_agent_role.md`
4. `agent_handoff_template.md`
5. `codex_prompt_library.md`
6. `success_criteria.md`

These define agent boundaries, branch rules, escalation, and orchestration authority.

## Execution Order

Use these documents to run the first implementation wave:

1. `parallel_work_packets.md`
2. `phase_1_execution_checklist.md`

Use this document for the controlled follow-up wave:

3. `phase_2_execution_checklist.md`

These define packet ownership, merge sequencing, and the transition from parallel work to controlled single-agent work.

## Agent Briefs

These are the self-contained briefs for the first parallel-safe packet wave:

1. `agent_1_log_filter_service_brief.md`
2. `agent_2_status_message_formatter_brief.md`
3. `agent_3_structured_message_parser_brief.md`
4. `agent_4_log_search_service_brief.md`
5. `agent_5_tagged_diagnostics_analyzer_brief.md`

Each brief defines:

- the owning agent
- packet scope
- allowed files
- forbidden files
- required unit tests
- escalation triggers

## Single-Agent Briefs

These are the controlled follow-up briefs for higher-risk extraction and integration work:

1. `agent_6_project_processing_coordinator_brief.md`
2. `agent_7_loglevel_command_coordinator_brief.md`
3. `agent_8_mainwindow_integration_brief.md`

These briefs cover the later phase where work must move out of the parallel-safe lane and into controlled single-agent execution.

## Current Phase Summary

The current plan is:

1. Extract the first portable shared-domain services in parallel.
2. Keep `MainWindow.xaml.cs` out of the first wave.
3. Require each packet owner to supply its own unit tests.
4. Merge through an integration branch.
5. Move to single-agent integration only after the first service wave is stable.

## Immediate Next Likely Docs

The next likely planning additions are:

1. A phase 3 checklist only if the next wave reveals a new execution stage.
2. Optional packet-specific acceptance checklists if implementation reveals recurring review issues.

## Rule

If a future agent is unsure where to start, begin with:

1. `project_north_star.md`
2. `README.md`
3. the relevant packet brief for the assigned work
