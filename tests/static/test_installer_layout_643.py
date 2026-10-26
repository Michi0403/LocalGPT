"""Offline regression checks for versioned ZIP wrappers and canonical Windows install locations.

This checks source and ZIP-layout assumptions. The .NET executable must still be compiled and
exercised with real winx64/setupwinx64 archives on Windows to verify runtime behavior.
"""
from pathlib import Path
import io, unittest, zipfile

ROOT = Path(__file__).resolve().parents[2]
PROGRAM = (ROOT/'src/LocalGPTInstallerConsole/Program.cs').read_text()
RELEASE = (ROOT/'Build-Release.ps1').read_text()
GUARD = (ROOT/'build/Assert-InstallerLayout.ps1').read_text()
TARGETS = (ROOT/'Directory.Build.targets').read_text()


def layout_members(names, canonical, required):
    """Fixture of the declared C# installer wrapper/flat input contract."""
    first = {name.split('/')[0] for name in names}
    wrap = None
    if len(first) == 1 and all('/' in name for name in names):
        part = first.pop()
        if part == canonical or part.startswith('LocalGPT-'):
            wrap = part
    member = f'{wrap}/{required}' if wrap else required
    if member not in names:
        raise ValueError('missing required top-level member')
    return wrap

class InstallerLayout643Tests(unittest.TestCase):
    def test_guard_is_attached_to_both_projects_and_release(self):
        self.assertIn('AssertLocalGptInstallerLayout', TARGETS)
        self.assertIn("'$(MSBuildProjectName)' == 'LocalGPTInstallerConsole'",TARGETS)
        self.assertIn('build/Assert-InstallerLayout.ps1', (ROOT/'Build-Release.ps1').read_text())
        self.assertIn('INSTALLLAYOUT0004', GUARD)

    def test_installer_enforces_canonical_directories_and_refuses_recursive_shortcuts(self):
        self.assertIn('InstallPortableReleaseAsset(zipPath, targetPath, "winx64", "LocalGPT.exe", logger)', PROGRAM)
        self.assertIn('InstallPortableReleaseAsset(setupZipPath, targetPath, "setupwinx64", "Start.cmd", logger)', PROGRAM)
        self.assertIn('CopyReleasePayload(source, target)', PROGRAM)
        self.assertIn('var cmdPath = Path.Combine(localGptRoot, "setupwinx64", cmdFileName)',PROGRAM)
        installer_method = PROGRAM.split('private static async Task InstallLocalGptAsync(')[1].split('private static void InstallPortableReleaseAsset(')[0]
        self.assertNotIn('ExtractZipWithFallback(zipPath, targetPath, logger)',installer_method)
        self.assertNotIn('ExtractZipWithFallback(setupZipPath, targetPath, logger)',installer_method)
        self.assertIn('InvalidDataException("Installed LocalGPT layout is incomplete', PROGRAM)
        self.assertNotIn('DeleteIfExists(targetPath, logger)',installer_method)
        self.assertIn('DeleteIfExists(Path.Combine(targetPath, "winx64"), logger)',installer_method)
        self.assertIn('DeleteIfExists(Path.Combine(targetPath, "setupwinx64"), logger)',installer_method)
        self.assertIn('throw new FileNotFoundException($"LocalGPT canonical setup shortcut target is missing:', PROGRAM)

    def test_wrapped_versioned_and_legacy_release_zip_samples(self):
        specimens = [
            (['LocalGPT-6.4.3-win-x64/LocalGPT.exe','LocalGPT-6.4.3-win-x64/RELEASE-VERSION.txt'], 'winx64','LocalGPT.exe','LocalGPT-6.4.3-win-x64'),
            (['LocalGPT-Setup-6.4.3-win-x64/Start.cmd','LocalGPT-Setup-6.4.3-win-x64/RELEASE-VERSION.txt'], 'setupwinx64','Start.cmd','LocalGPT-Setup-6.4.3-win-x64'),
            (['winx64/LocalGPT.exe'], 'winx64', 'LocalGPT.exe', 'winx64'),
            (['Start.cmd','LocalGPTInstallerConsole.exe'], 'setupwinx64', 'Start.cmd', None),
        ]
        for members,folder,required,wrapper in specimens:
            with self.subTest(folder=folder,wrapper=wrapper):
                stream = io.BytesIO()
                with zipfile.ZipFile(stream,'w') as archive:
                    for member in members: archive.writestr(member,'test payload')
                with zipfile.ZipFile(stream) as archive:
                    self.assertEqual(layout_members(archive.namelist(),folder,required), wrapper)
        with self.assertRaises(ValueError):
            layout_members(['LocalGPT-6.4.3-win-x64/some-folder/LocalGPT.exe'],'winx64','LocalGPT.exe')

    def test_repair_of_versioned_installer_debris_is_after_canonical_success_and_lossless(self):
        install_section = PROGRAM.split('private static async Task InstallLocalGptAsync(')[1].split('private static void InstallPortableReleaseAsset(')[0]
        assert_before = install_section.index('Installed LocalGPT layout is incomplete')
        recover = install_section.index('RecoverLegacyVersionedInstallFolders(targetPath, logger)')
        self.assertLess(assert_before, recover)
        migration = PROGRAM.split('private static void RecoverLegacyVersionedInstallFolders(')[1].split('private static void CopyReleasePayload(')[0]
        for requirement in ('SearchOption.TopDirectoryOnly', 'TryIdentifyLegacyReleaseWrapper(name, out var requiredFile)',
                            'File.Exists(Path.Combine(legacyPath, requiredFile))', 'FileAttributes.ReparsePoint',
                            '"runtime", "recovered-installer-payloads"', 'Directory.Move(legacyPath, recoveryPath)',
                            'Version.TryParse(versionText, out var version)', '"LocalGPT-Setup-"', '"LocalGPT-"'):
            self.assertIn(requirement, migration)
        self.assertNotIn('Directory.Delete(',migration)
        self.assertNotIn('File.Delete(', migration)
        self.assertNotIn('SearchOption.AllDirectories',migration)
        for check in ('INSTALLLAYOUT0007','INSTALLLAYOUT0008','INSTALLLAYOUT0009'):
            self.assertIn(check,GUARD)

    def test_app_and_setup_packaging_keep_version_provenance(self):
        self.assertIn('$appRootFolderName = "LocalGPT-$appVersion-$Rid"', RELEASE)
        self.assertIn('$setupRootFolderName = "LocalGPT-Setup-$appVersion-$Rid"', RELEASE)
        self.assertIn('RELEASE-VERSION.txt', RELEASE)

if __name__ == '__main__': unittest.main()
