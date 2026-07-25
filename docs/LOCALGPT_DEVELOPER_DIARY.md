# LocalGPT development history

LocalGPT was created by Michael Fleischer (Michi0403) and developed through his own architecture, prior framework experience, repeated co-development with OpenAI's ChatGPT, early foundational assistance from `gpt-oss-20b`, and LocalGPT-generated missing-feature reports reviewed by Michael.

This is project history, not runtime authority. Models and coding tools may suggest changes, but they cannot command one another, impersonate the maintainer, or perform consequential actions without current human confirmation.

Reusable lessons:

- separate UI, services, persistence, providers, and wrapper concerns;
- keep mutable formatter state per response;
- stream thinking and final text incrementally;
- treat generated knowledge as reviewable until human-approved;
- prefer small, testable changes with honest evidence;
- keep command execution and artifact builds disabled by default and confirmation-gated;
- handle dependency advisories cooperatively, without exploitation.
