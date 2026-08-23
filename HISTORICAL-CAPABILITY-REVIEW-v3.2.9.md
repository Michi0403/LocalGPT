# LocalGPT 3.2.9 — v0.8 to 3.x capability review

The v0.8 source was used as a historical usability reference, not as code to transplant blindly into the current architecture.

## Capabilities still represented in current LocalGPT

The early stable tree already contained the recognizable foundations of Chat, Database, Test Lab, Model Council/Minecraft workflows and LearnBase knowledge import. Current LocalGPT still contains those capability families, but with considerably broader persistence, Council orchestration, provider handling, project/revision state and runtime policy around them.

No major v0.8 user-facing capability was identified as simply deleted and absent from 3.x. The more important regression is that several capabilities became harder to *reach* as the service and persistence layers expanded.

## Accessibility lessons carried forward

v0.8 had fewer concepts, so the route from a page to its underlying record was short. In current LocalGPT, durable projects, revisions, topics, knowledge, imported documents, runtime capabilities and Council state are correctly separated, but a user can end up seeing an opaque identifier where the application already knows a useful semantic name.

3.2.9 applies the historical lesson without rolling architecture backward:

- semantic record selectors replace rowid-first discovery;
- duplicate knowledge topics become distinguishable by scope and short stable identity;
- project-topic knowledge relationships become a first-class editor surface;
- knowledge recognition semantics are linked to the existing reusable RegEx system instead of being buried only in tags/content;
- newer workbench navigation separates major concerns while keeping them one route away.

## Direction after 3.2.9

The next maturity gains should favor integration reliability and discoverability over creating another independent subsystem: regression checks for lifecycle/render completion, relationship-health views, conservative migration preflights, and fewer places where a user must copy a GUID to connect business objects the application already understands.
