# LocalGPT 4.7.1

LocalGPT 4.7.1 is a focused build-policy repair release for the 4.7.0 repository/OCR/controller feature set.

The bounded workspace OCR service now normalizes the configured Ollama host before constructing its URI, satisfying the repository system-variable-initialization guard without weakening the guard or moving OCR ownership out of the service layer. Repository intake, review-gated Knowledge/regex learning, PublisherStudio capability routing, idle-invisible drag/drop and additive Control/Cursor controller behavior are otherwise preserved from 4.7.0.

See `CHANGELOG-v4.7.1-BUILD-POLICY-REPAIR.md` and `VALIDATION-v4.7.1-source.md`.
