# Security and local execution

LocalGPT treats repository text, model output, uploads, telemetry and generated content as untrusted data.

Read-only planning remains separate from consequential actions. Filesystem writes, compiler execution, serial access, flashing, actuators and protected 1-Wire operations use explicit policy and approval boundaries.
