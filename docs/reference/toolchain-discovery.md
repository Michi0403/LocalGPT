# Cross-platform compiler and runtime toolchain discovery

LocalGPT treats toolchain discovery as local knowledge, not as a hardcoded operating-system table. Discovery always checks the current `PATH` first, then explicitly named environment roots, then the platform roots below, and finally user-supplied roots. These profiles are seeds: users may add or override toolchain knowledge in the Knowledge Database. No online lookup is performed automatically.

Each `localgpt-toolchain` block is machine-readable by `ToolchainKnowledgeService`. Paths may use `$HOME`-style environment interpolation; Windows `%VARIABLE%` expansion is also supported by the runtime.

```localgpt-toolchain
{"key":"dotnet-sdk","displayName":".NET SDK","language":"DotNet","kind":"sdk","executableNames":["dotnet","dotnet.exe"],"environmentRootVariables":["DOTNET_ROOT"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/dotnet","%ProgramFiles(x86)%/dotnet","%USERPROFILE%/.dotnet"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","/usr/share/dotnet","/usr/local/share/dotnet","$HOME/.dotnet"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","/usr/local/share/dotnet","/opt/homebrew/share/dotnet","$HOME/.dotnet"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["global.json","*.csproj","*.fsproj","*.vbproj","*.sln","*.slnx"],"contextTags":["dotnet","sdk"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"msbuild","displayName":"MSBuild","language":"DotNet","kind":"build-tool","executableNames":["msbuild","msbuild.exe","MSBuild.exe"],"environmentRootVariables":["MSBUILD_EXE_PATH"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/Microsoft Visual Studio","%ProgramFiles(x86)%/Microsoft Visual Studio"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"-version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["*.sln","*.csproj"],"contextTags":["dotnet","msbuild"],"maximumSearchDepth":5}
```

```localgpt-toolchain
{"key":"java-jdk","displayName":"Java JDK compiler","language":"Java","kind":"compiler","executableNames":["javac","javac.exe"],"environmentRootVariables":["JAVA_HOME","JDK_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/Java","%ProgramFiles%/Eclipse Adoptium","%USERPROFILE%/.jdks"],"linuxSearchRoots":["/usr/bin","/usr/lib/jvm","/usr/java","$HOME/.sdkman/candidates/java"],"macOsSearchRoots":["/usr/bin","/Library/Java/JavaVirtualMachines","$HOME/.sdkman/candidates/java","/opt/homebrew/opt/openjdk"],"validationArguments":"-version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["pom.xml","build.gradle","build.gradle.kts","settings.gradle","settings.gradle.kts"],"contextTags":["java","jdk"],"maximumSearchDepth":4}
```

```localgpt-toolchain
{"key":"java-runtime","displayName":"Java runtime","language":"Java","kind":"runtime","executableNames":["java","java.exe"],"environmentRootVariables":["JAVA_HOME","JRE_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/Java","%ProgramFiles%/Eclipse Adoptium"],"linuxSearchRoots":["/usr/bin","/usr/lib/jvm","$HOME/.sdkman/candidates/java"],"macOsSearchRoots":["/usr/bin","/Library/Java/JavaVirtualMachines","$HOME/.sdkman/candidates/java","/opt/homebrew/opt/openjdk"],"validationArguments":"-version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["pom.xml","build.gradle","build.gradle.kts"],"contextTags":["java","runtime"],"maximumSearchDepth":4}
```

```localgpt-toolchain
{"key":"gradle","displayName":"Gradle","language":"Java","kind":"build-tool","executableNames":["gradle","gradle.bat","gradlew","gradlew.bat"],"environmentRootVariables":["GRADLE_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.gradle","%USERPROFILE%/.sdkman/candidates/gradle"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.sdkman/candidates/gradle"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","$HOME/.sdkman/candidates/gradle"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["gradlew","gradlew.bat","build.gradle","build.gradle.kts"],"contextTags":["gradle","java"],"maximumSearchDepth":3}
```

```localgpt-toolchain
{"key":"maven","displayName":"Apache Maven","language":"Java","kind":"build-tool","executableNames":["mvn","mvn.cmd","mvnw","mvnw.cmd"],"environmentRootVariables":["MAVEN_HOME","M2_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.m2"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.m2"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","$HOME/.m2"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["pom.xml","mvnw","mvnw.cmd"],"contextTags":["maven","java"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"python","displayName":"Python","language":"Python","kind":"runtime","executableNames":["python","python.exe","python3","python3.exe","py.exe"],"executablePatterns":["python3.*","python3*.exe"],"environmentRootVariables":["PYTHONHOME","VIRTUAL_ENV","CONDA_PREFIX","PYENV_ROOT"],"commonSearchRoots":[],"windowsSearchRoots":["%LOCALAPPDATA%/Programs/Python","%USERPROFILE%/AppData/Local/Microsoft/WindowsApps","%USERPROFILE%/.pyenv/pyenv-win/versions","%USERPROFILE%/miniconda3","%USERPROFILE%/anaconda3","%USERPROFILE%/.virtualenvs"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.local/bin","$HOME/.pyenv/versions","$HOME/miniconda3","$HOME/anaconda3"],"macOsSearchRoots":["/usr/bin","/usr/local/bin","/opt/homebrew/bin","$HOME/.local/bin","$HOME/.pyenv/versions","$HOME/miniconda3","$HOME/anaconda3"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["pyproject.toml","requirements.txt","setup.py","Pipfile"],"contextTags":["python"],"maximumSearchDepth":3}
```

```localgpt-toolchain
{"key":"node","displayName":"Node.js","language":"JavaScript","kind":"runtime","executableNames":["node","node.exe"],"environmentRootVariables":["NODE_HOME","NVM_HOME","NVM_SYMLINK","VOLTA_HOME","FNM_DIR"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/nodejs","%APPDATA%/nvm","%USERPROFILE%/.volta/tools/image/node","%LOCALAPPDATA%/Volta/tools/image/node","%LOCALAPPDATA%/fnm/node-versions","%USERPROFILE%/.fnm/node-versions"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.nvm/versions/node","$HOME/.volta/tools/image/node","$HOME/.local/share/fnm/node-versions","$HOME/.local/share/mise/installs/node","$HOME/.asdf/installs/nodejs"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","$HOME/.nvm/versions/node","$HOME/.volta/tools/image/node","$HOME/Library/Application Support/fnm/node-versions","$HOME/.local/share/mise/installs/node","$HOME/.asdf/installs/nodejs"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["package.json","package-lock.json","pnpm-lock.yaml","yarn.lock"],"contextTags":["node","javascript"],"maximumSearchDepth":4}
```

```localgpt-toolchain
{"key":"powershell","displayName":"PowerShell","language":"PowerShell","kind":"runtime","executableNames":["pwsh","pwsh.exe","powershell.exe"],"environmentRootVariables":[],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/PowerShell","%SystemRoot%/System32/WindowsPowerShell/v1.0"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","/opt/microsoft/powershell"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","/usr/local/microsoft/powershell"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["*.ps1","*.psm1","*.psd1"],"contextTags":["powershell"],"maximumSearchDepth":3}
```

```localgpt-toolchain
{"key":"gcc","displayName":"GNU C compiler","language":"C","kind":"compiler","executableNames":["gcc","gcc.exe"],"environmentRootVariables":[],"commonSearchRoots":[],"windowsSearchRoots":["C:/msys64/usr/bin","C:/msys64/mingw64/bin","C:/mingw64/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/bin","/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["CMakeLists.txt","Makefile","*.c"],"contextTags":["gcc","c"],"maximumSearchDepth":1}
```

```localgpt-toolchain
{"key":"gpp","displayName":"GNU C++ compiler","language":"Cpp","kind":"compiler","executableNames":["g++","g++.exe"],"environmentRootVariables":[],"commonSearchRoots":[],"windowsSearchRoots":["C:/msys64/usr/bin","C:/msys64/mingw64/bin","C:/mingw64/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/bin","/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["CMakeLists.txt","Makefile","*.cpp"],"contextTags":["gcc","cpp"],"maximumSearchDepth":1}
```

```localgpt-toolchain
{"key":"clang","displayName":"Clang compiler","language":"Cpp","kind":"compiler","executableNames":["clang","clang.exe","clang++","clang++.exe"],"environmentRootVariables":["LLVM_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/LLVM/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/bin","/usr/local/bin","/opt/homebrew/opt/llvm/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["CMakeLists.txt","Makefile","*.c","*.cpp"],"contextTags":["clang","llvm"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"cmake","displayName":"CMake","language":"Native","kind":"build-tool","executableNames":["cmake","cmake.exe"],"environmentRootVariables":["CMAKE_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/CMake/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","/Applications/CMake.app/Contents/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["CMakeLists.txt","CMakePresets.json"],"contextTags":["cmake","native"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"rust-cargo","displayName":"Rust Cargo","language":"Rust","kind":"package-build-tool","executableNames":["cargo","cargo.exe","rustc","rustc.exe"],"environmentRootVariables":["CARGO_HOME","RUSTUP_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.cargo/bin"],"linuxSearchRoots":["$HOME/.cargo/bin","/usr/bin","/usr/local/bin"],"macOsSearchRoots":["$HOME/.cargo/bin","/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["Cargo.toml","Cargo.lock"],"contextTags":["rust","cargo"],"maximumSearchDepth":1}
```

```localgpt-toolchain
{"key":"go","displayName":"Go toolchain","language":"Go","kind":"compiler-runtime","executableNames":["go","go.exe"],"environmentRootVariables":["GOROOT","GOPATH"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/Go/bin","%USERPROFILE%/go/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/go/bin","$HOME/go/bin"],"macOsSearchRoots":["/usr/local/go/bin","/opt/homebrew/bin","$HOME/go/bin"],"validationArguments":"version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["go.mod","go.work"],"contextTags":["go","golang"],"maximumSearchDepth":1}
```

```localgpt-toolchain
{"key":"platformio","displayName":"PlatformIO Core","language":"Embedded","kind":"build-tool","executableNames":["platformio","platformio.exe","pio","pio.exe"],"environmentRootVariables":["PLATFORMIO_CORE_DIR"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.platformio/penv/Scripts"],"linuxSearchRoots":["$HOME/.platformio/penv/bin","$HOME/.local/bin"],"macOsSearchRoots":["$HOME/.platformio/penv/bin","$HOME/.local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["platformio.ini"],"contextTags":["platformio","embedded"],"maximumSearchDepth":1}
```

```localgpt-toolchain
{"key":"arduino-cli","displayName":"Arduino CLI","language":"Embedded","kind":"build-tool","executableNames":["arduino-cli","arduino-cli.exe"],"environmentRootVariables":["ARDUINO_DIRECTORIES_DATA","ARDUINO_DIRECTORIES_USER"],"commonSearchRoots":[],"windowsSearchRoots":["%LOCALAPPDATA%/Programs/Arduino IDE/resources/app/lib/backend/resources","%ProgramFiles%/Arduino IDE/resources/app/lib/backend/resources"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.local/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","/Applications/Arduino IDE.app/Contents/Resources/app/lib/backend/resources"],"validationArguments":"version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["*.ino","arduino-cli.yaml"],"contextTags":["arduino","embedded"],"maximumSearchDepth":2}
```

## Version knowledge

A discovery profile only tells LocalGPT how to find and probe a tool locally. Exact-version behavior belongs in normal Council Knowledge entries. Tag such an article with `toolchain:<profile-key>` and `version:<exact-version>` where practical. If LocalGPT detects a version for which no approved/pinned article exists, it asks the local user for a Markdown file, Knowledge Database article, or text blob through Human Collaboration. The request does not trigger an online lookup.

## Additional common profiles

The following profiles extend PATH-first discovery for common native, web and embedded build environments. They remain knowledge entries rather than hardcoded process rules, so users can replace or supplement them without changing LocalGPT.

```localgpt-toolchain
{"key":"msvc-cl","displayName":"Microsoft C/C++ compiler (cl)","language":"Cpp","kind":"compiler","executableNames":["cl.exe"],"environmentRootVariables":["VCToolsInstallDir","VCINSTALLDIR"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/Microsoft Visual Studio","%ProgramFiles(x86)%/Microsoft Visual Studio"],"linuxSearchRoots":[],"macOsSearchRoots":[],"validationArguments":"/?","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["*.sln","*.vcxproj","CMakeLists.txt"],"contextTags":["msvc","cpp","visualstudio"],"maximumSearchDepth":5}
```

```localgpt-toolchain
{"key":"ninja","displayName":"Ninja","language":"Native","kind":"build-tool","executableNames":["ninja","ninja.exe"],"environmentRootVariables":[],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/CMake/bin","C:/msys64/usr/bin","C:/msys64/mingw64/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.local/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["build.ninja","CMakeLists.txt"],"contextTags":["ninja","native"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"make","displayName":"Make","language":"Native","kind":"build-tool","executableNames":["make","make.exe","mingw32-make.exe","nmake.exe"],"environmentRootVariables":[],"commonSearchRoots":[],"windowsSearchRoots":["C:/msys64/usr/bin","C:/msys64/mingw64/bin","C:/mingw64/bin","%ProgramFiles%/Microsoft Visual Studio"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/bin","/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["Makefile","makefile","GNUmakefile"],"contextTags":["make","native"],"maximumSearchDepth":5}
```

```localgpt-toolchain
{"key":"typescript","displayName":"TypeScript compiler","language":"TypeScript","kind":"compiler","executableNames":["tsc","tsc.cmd","tsc.exe"],"environmentRootVariables":["NPM_CONFIG_PREFIX","NODE_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%APPDATA%/npm","%ProgramFiles%/nodejs"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.local/bin","$HOME/.npm-global/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","$HOME/.local/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["tsconfig.json","package.json"],"contextTags":["typescript","node"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"npm","displayName":"npm","language":"JavaScript","kind":"package-build-tool","executableNames":["npm","npm.cmd","npm.exe"],"environmentRootVariables":["NPM_CONFIG_PREFIX","NODE_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%APPDATA%/npm","%ProgramFiles%/nodejs"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.local/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["package.json","package-lock.json"],"contextTags":["npm","node","javascript"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"pnpm","displayName":"pnpm","language":"JavaScript","kind":"package-build-tool","executableNames":["pnpm","pnpm.cmd","pnpm.exe"],"environmentRootVariables":["PNPM_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%PNPM_HOME%","%APPDATA%/npm"],"linuxSearchRoots":["$HOME/.local/share/pnpm","/usr/bin","/usr/local/bin"],"macOsSearchRoots":["$HOME/Library/pnpm","/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["package.json","pnpm-lock.yaml","pnpm-workspace.yaml"],"contextTags":["pnpm","node","javascript"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"yarn","displayName":"Yarn","language":"JavaScript","kind":"package-build-tool","executableNames":["yarn","yarn.cmd","yarn.exe"],"environmentRootVariables":[],"commonSearchRoots":[],"windowsSearchRoots":["%APPDATA%/npm","%LOCALAPPDATA%/Yarn/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.yarn/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","$HOME/.yarn/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["package.json","yarn.lock"],"contextTags":["yarn","node","javascript"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"deno","displayName":"Deno","language":"JavaScript","kind":"runtime","executableNames":["deno","deno.exe"],"environmentRootVariables":["DENO_INSTALL"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.deno/bin"],"linuxSearchRoots":["$HOME/.deno/bin","/usr/bin","/usr/local/bin"],"macOsSearchRoots":["$HOME/.deno/bin","/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["deno.json","deno.jsonc"],"contextTags":["deno","typescript","javascript"],"maximumSearchDepth":1}
```

```localgpt-toolchain
{"key":"bun","displayName":"Bun","language":"JavaScript","kind":"runtime-build-tool","executableNames":["bun","bun.exe"],"environmentRootVariables":["BUN_INSTALL"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.bun/bin"],"linuxSearchRoots":["$HOME/.bun/bin","/usr/local/bin"],"macOsSearchRoots":["$HOME/.bun/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["package.json","bun.lock","bun.lockb"],"contextTags":["bun","javascript","typescript"],"maximumSearchDepth":1}
```

```localgpt-toolchain
{"key":"php","displayName":"PHP","language":"PHP","kind":"runtime","executableNames":["php","php.exe"],"environmentRootVariables":[],"commonSearchRoots":[],"windowsSearchRoots":["C:/php","C:/xampp/php"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["composer.json","*.php"],"contextTags":["php"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"ruby","displayName":"Ruby","language":"Ruby","kind":"runtime","executableNames":["ruby","ruby.exe"],"environmentRootVariables":["RUBY_ROOT"],"commonSearchRoots":[],"windowsSearchRoots":["C:/Ruby","%USERPROFILE%/.rubies"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.rbenv/shims"],"macOsSearchRoots":["/usr/bin","/usr/local/bin","/opt/homebrew/bin","$HOME/.rbenv/shims"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["Gemfile","*.rb"],"contextTags":["ruby"],"maximumSearchDepth":3}
```

```localgpt-toolchain
{"key":"zig","displayName":"Zig","language":"Zig","kind":"compiler","executableNames":["zig","zig.exe"],"environmentRootVariables":["ZIG_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["C:/zig"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.local/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["build.zig","build.zig.zon","*.zig"],"contextTags":["zig","native"],"maximumSearchDepth":2}
```

```localgpt-toolchain
{"key":"avr-gcc","displayName":"AVR GCC","language":"Embedded","kind":"compiler","executableNames":["avr-gcc","avr-gcc.exe","avr-g++","avr-g++.exe"],"environmentRootVariables":["ARDUINO_DIRECTORIES_DATA"],"commonSearchRoots":[],"windowsSearchRoots":["%LOCALAPPDATA%/Arduino15/packages","%USERPROFILE%/.platformio/packages"],"linuxSearchRoots":["/usr/bin","$HOME/.arduino15/packages","$HOME/.platformio/packages"],"macOsSearchRoots":["/usr/local/bin","$HOME/Library/Arduino15/packages","$HOME/.platformio/packages"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["*.ino","platformio.ini","Makefile"],"contextTags":["avr","arduino","embedded"],"maximumSearchDepth":5}
```

```localgpt-toolchain
{"key":"esp-idf","displayName":"Espressif IDF","language":"Embedded","kind":"build-tool","executableNames":["idf.py","idf.py.exe"],"environmentRootVariables":["IDF_PATH","IDF_TOOLS_PATH"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.espressif","%IDF_PATH%/tools"],"linuxSearchRoots":["$HOME/.espressif","$IDF_PATH/tools"],"macOsSearchRoots":["$HOME/.espressif","$IDF_PATH/tools"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token-v2","projectMarkers":["sdkconfig","CMakeLists.txt","components"],"contextTags":["esp32","esp-idf","embedded"],"maximumSearchDepth":4}
```
