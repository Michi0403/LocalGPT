# LocalGPT 6.1.4 — source-only validation

- Base: supplied LocalGPT 6.1.3 source ZIP.
- Version: `<Version>6.1.4</Version>` in the LocalGPT project file.
- New DXFunction descriptors are in DI-scanned service namespace and all added schemas parse as JSON.
- `python -m unittest discover -s tests/static -p 'test_*release.py' -v`: **5 tests passed**.
- `python -m py_compile src/LocalGPT/Runtime/Python/localgpt_runtime_bridge.py`: **passed**.
- Model-load probe preceeds manifest publication and failure deletes the newly installed files; validation covers each declared executable capability.
- Explicit human confirmation remains the only supported mutation path for Council team changes and model/provider installation.
- Packaging ZIP entries / CRC verified at source release creation time.

**Not run:** repository PowerShell architecture gates (PowerShell absent); `.NET` restore/build/test (SDK absent); EF/DevExpress compiler; runtime 1-Wire; live media model downloads, HF authentication and hardware inference. No certification of runtime functionality is implied.
