from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path

BUILD_DIR = Path(__file__).resolve().parents[1]
ROOT = BUILD_DIR.parent
MODULE_PATH = BUILD_DIR / "audit_application_architecture.py"
SPEC = importlib.util.spec_from_file_location("architecture_audit", MODULE_PATH)
assert SPEC and SPEC.loader
AUDIT = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = AUDIT
SPEC.loader.exec_module(AUDIT)

PRODUCT = "localgpt" if (ROOT / "LocalGPTWebviewWrapper" / "LocalGPT").is_dir() else "publisherstudio"
APP_ROOT = ROOT / ("LocalGPTWebviewWrapper/LocalGPT" if PRODUCT == "localgpt" else "src/PublisherStudio.Web")


class ArchitectureAuditTests(unittest.TestCase):
    def test_current_repository_has_no_application_static_findings(self) -> None:
        self.assertEqual([], AUDIT.static_audit(APP_ROOT, PRODUCT))

    def test_current_repository_runtime_values_use_approved_boundaries(self) -> None:
        self.assertEqual([], AUDIT.runtime_value_audit(APP_ROOT, PRODUCT))

    def test_current_maintained_operational_methods_have_diagnostics(self) -> None:
        self.assertEqual([], AUDIT.method_audit(APP_ROOT, PRODUCT))

    def test_raw_source_templates_do_not_become_false_static_findings(self) -> None:
        source = '''public sealed class TemplateOwner\n{\n    public string Template => """\n        public static final String VALUE = "generated";\n        """;\n}\n'''
        masked = AUDIT.mask_csharp(source)
        self.assertNotIn("public static final", masked)


if __name__ == "__main__":
    unittest.main()
