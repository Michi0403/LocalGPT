# LocalGPT 2.0.1 final27 - optional provider timeout and maintainer manifest fix

- Reclassified unavailable optional LM Studio and local OpenAI-compatible discovery endpoints as normal discovery outcomes instead of application errors.
- Configured-timeout and connection-refused outcomes are logged at information level without exception stack traces; caller cancellation is still rethrown and unexpected failures remain errors.
- Added the same reviewed PowerShell manifest refresher used on the PublisherStudio side.
- The refresher cannot rewrite final19 security hashes, runs the existing safeguards, and restores manifests if post-write validation fails.
