# Project collaboration model

LocalGPT stores user-created projects so conversations, AI Council work, knowledge, versions, and file locations can be organized without turning the application into an autonomous coding agent.

## Project records

A project may contain:

- a user-selected name and constructive purpose;
- an optional root-path string;
- a current version and version history;
- user-approved topics;
- links from topics to reviewed council knowledge entries;
- a preference to recommend Git.

Saving a path records text only. It does not authorize scanning, opening, modifying, executing, building, deleting, or publishing files at that path.

## Git guidance

LocalGPT may recommend placing a project under Git version control and explain ordinary Git practices. The project feature does not initialize repositories, stage files, commit, reset, clean, push, alter remotes, or enforce Git. Any future Git integration must be a separate bounded service with a preview and fresh user confirmation for each consequential operation.

## Council cooperation

A user may select a project and topic before an AI Council run. The project briefing provides limited context: purpose, recorded path, version, and topic. Council phases act as bounded brain parts—proposal, critique, verification, synthesis, and documentation—within that one run.

A generated knowledge entry is linked to the topic only when the user explicitly selects that option for the current run. The confirmation is cleared after the run. Models and stored knowledge cannot link themselves to projects or create continuing work.

## LocalGPT development project

The user may create a project record for LocalGPT itself and keep its versions and planned topics there. This supports deliberate co-development. It does not grant LocalGPT permission to edit or rebuild itself without a current user request and the same confirmation boundaries as every other project.
