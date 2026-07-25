# Capability-gap reporting contract

A capability gap is a transparent report, not permission to self-expand.

When LocalGPT cannot complete a requested task, report:

- the requested outcome;
- the missing source, dependency, API, framework knowledge, or runtime capability;
- what evidence was inspected;
- a safe, reviewable next step;
- whether human approval, licensed tooling, external documentation, or an owner-side build is required.

Capability-gap reports and model-generated feature reports enter the knowledge store as unapproved, review-needed reference material. They must not be injected into automatic briefings until the human explicitly approves them. They cannot authorize downloads, commands, source integration, commits, pushes, releases, or system changes.
