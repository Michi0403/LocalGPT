# LocalGPT project stance

## Purpose

LocalGPT is an independent, garage-built software project created and maintained by Michael Fleischer (`Michi0403`). It exists first as a tool the maintainer wants to use, as a platform shared with friends, and as a public engineering workshop.

It is not a company, a paid service, a conventional consumer application, or a promise of enterprise support.

The project aims to make local AI work practical, inspectable, expressive, and enjoyable. It combines serious application architecture with experimentation: AI councils, role-driven workflows, human participation, persistent knowledge, database-backed configuration, local model providers, diagnostics, frontend design, installers, and release tooling all live in one public system.

## Built for use, not for a sales story

LocalGPT is designed around real workflows rather than a staged product demonstration. Features may begin as experiments, reveal unexpected behavior, and later become deliberate architecture after they prove useful.

The primary question is not, “Will this appeal to every user?” It is, “Does this make the system more capable, understandable, safe, and enjoyable for the people actively using and building it?”

That priority can produce an application that is unusually powerful while still visibly carrying the character of a one-person project. Both are true, and neither needs to be hidden.

## Public development cadence

The repository is active and may change quickly. Local work is normally reviewed, checked, merged into the working source, and committed soon afterward. In practice, repository development can closely follow ongoing engineering sessions.

Because of that pace:

- the current commit history is more reliable than old screenshots or cached search results;
- the latest release page is more reliable than fixed version numbers in third-party references;
- documentation may describe stable architectural intent while implementation details continue to evolve;
- downstream adopters should pin the exact commit or release they validate.

Fast public iteration does not mean every commit is a polished release. Release artifacts follow their own validation and packaging process.

## Relationship with regular users

LocalGPT is publicly available, but general-user convenience is not the project's governing priority.

The maintainer does not promise:

- beginner-oriented onboarding for every workflow;
- compatibility with every machine, model, provider, or platform;
- a response to every issue or feature request;
- long-term preservation of every UI shape or experimental behavior;
- free integration work for downstream organizations;
- production support, uptime, warranties, or service-level commitments.

This is not hostility toward users. It is a clear boundary around a one-person project. People who need a different product shape are welcome to fork the source and create it.

## Relationship with companies and institutions

The Apache License 2.0 is a deliberate invitation to adapt the work.

Companies, research groups, educational institutions, public-interest organizations, and independent developers may study, modify, integrate, and redistribute LocalGPT under the license terms. Large-scale or highly specialized use is welcome, but it remains the adopter's responsibility.

Downstream adopters should expect to own:

- security and threat modeling;
- legal and license review;
- deployment and operational controls;
- accessibility and user support;
- data governance and compliance;
- provider and model evaluation;
- release management and regression testing;
- organization-specific integrations and guarantees.

Commercial adoption does not create an obligation for the upstream maintainer to become a vendor, contractor, or support department.

## Engineering value

LocalGPT intentionally publishes complete examples rather than only simplified demonstrations. The repository includes real interactions between:

- ASP.NET Core and Interactive Blazor Server;
- DevExpress components and application-owned state;
- EF Core migrations and database-backed workflows;
- streamed model output and persistent UI behavior;
- role orchestration and human participation;
- provider adapters and bounded execution policies;
- diagnostics, build guards, installers, desktop wrappers, and release tooling.

That end-to-end context can be more useful than a small vendor sample because it exposes the collisions, recovery paths, and ownership boundaries that appear in a real application.

The repository should still be read critically. Public code is not automatically correct because it is ambitious, and downstream users are responsible for validating the parts they adopt.

## Donations and public-interest support

LocalGPT is free and open source. No payment is expected.

A donation mechanism may be added later, but its meaning must remain explicit:

- a donation is voluntary;
- it is not a purchase or subscription;
- it does not buy support, priority, influence, or roadmap control;
- it does not create a warranty or service relationship;
- it does not change the Apache License 2.0 rights granted by the repository.

The preferred future direction is to highlight selected foundations, educational projects, research efforts, humanitarian organizations, or other public-interest institutions. Direct donation links may be preferable to collecting funds through the project itself.

No organization should be presented as endorsed, partnered, or officially supported until that relationship is verified and documented.

## Contributions

Contributions are welcome when they fit the project, but contribution is not ownership of the roadmap.

Useful contributions are usually:

- focused and reviewable;
- accompanied by a clear explanation;
- compatible with existing safety and ownership boundaries;
- validated against the relevant build and runtime paths;
- honest about limitations and untested environments.

The maintainer may decline or postpone work even when it is technically reasonable. Forking is an expected part of open source and may be the best solution for a different product direction.

## AI-assisted development and responsibility

LocalGPT has been developed through extensive human-led collaboration with AI systems, including OpenAI's ChatGPT, `gpt-oss-20b`, and LocalGPT's own review workflows.

AI assistance is acknowledged because it is part of the engineering history. It does not transfer authority or responsibility. Models do not own the repository, approve releases, grant permissions, or replace human review.

The maintainer remains responsible for decisions, public commits, releases, licensing, and the direction of the project.

## The simplest summary

> **Garage-built for personal use. Open for everyone. Powerful enough for much more.**

LocalGPT is a workshop, not a store. People are welcome to inspect the tools, use the blueprints, improve the machinery, and build their own version.
