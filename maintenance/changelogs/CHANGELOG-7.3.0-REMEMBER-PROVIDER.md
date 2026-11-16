# LocalGPT 7.3.0 — model choice is remembered

- Chat always started on the first configured provider (here gpt-oss:20b on the laptop), so every test and run silently used it. The provider you pick in Chat is now stored (LastChatProviderSession) and restored on the next visit.
- New installs start with 16,384 answer / 32,768 context tokens instead of 262,144.
- Benchmark reviewer text no longer claims gpt-oss:20b is preferred.
