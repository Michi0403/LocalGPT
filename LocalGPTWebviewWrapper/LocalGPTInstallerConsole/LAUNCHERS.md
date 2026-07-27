# LocalGPT launcher behavior

- `Install.cmd` performs a reviewed fresh application install, creates shortcuts, starts LocalGPT on `http://127.0.0.1:5000`, prints the actual runtime URL, and opens it. A fresh install replaces the application directory.
- `Update.cmd` updates without `--force-delete`, preserving the runtime identity, MFA trust, local databases, and user data.
- `Start.cmd` reuses a running LocalGPT process or starts it on the canonical loopback port, waits for the PID-owned `server.json`, prints the clickable URL, and opens the browser.
- `Uninstall.cmd` removes LocalGPT application files and shortcuts. The learning base, Ollama, and installed models are not removed.
