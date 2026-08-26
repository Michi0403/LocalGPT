# Cross-platform compiler and runtime toolchain discovery

LocalGPT treats toolchain discovery as local knowledge, not as a hardcoded operating-system table. Discovery always checks the current `PATH` first, then explicitly named environment roots, then the platform roots below, and finally user-supplied roots. These profiles are seeds: users may add or override toolchain knowledge in the Knowledge Database. No online lookup is performed automatically.

Each `localgpt-toolchain` block is machine-readable by `ToolchainKnowledgeService`. Paths may use `$HOME`-style environment interpolation; Windows `%VARIABLE%` expansion is also supported by the runtime.

```
{"key":"dotnet-sdk","displayName":".NET SDK","language":"DotNet","kind":"sdk","executableNames":["dotnet","dotnet.exe"],"environmentRootVariables":["DOTNET_ROOT"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/dotnet","%ProgramFiles(x86)%/dotnet","%USERPROFILE%/.dotnet"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","/usr/share/dotnet","/usr/local/share/dotnet","$HOME/.dotnet"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","/usr/local/share/dotnet","/opt/homebrew/share/dotnet","$HOME/.dotnet"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["global.json","*.csproj","*.fsproj","*.vbproj","*.sln","*.slnx"],"contextTags":["dotnet","sdk"],"maximumSearchDepth":2}
```

```
{"key":"msbuild","displayName":"MSBuild","language":"DotNet","kind":"build-tool","executableNames":["msbuild","msbuild.exe","MSBuild.exe"],"environmentRootVariables":["MSBUILD_EXE_PATH"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/Microsoft Visual Studio","%ProgramFiles(x86)%/Microsoft Visual Studio"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"-version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["*.sln","*.csproj"],"contextTags":["dotnet","msbuild"],"maximumSearchDepth":5}
```

```
{"key":"java-jdk","displayName":"Java JDK compiler","language":"Java","kind":"compiler","executableNames":["javac","javac.exe"],"environmentRootVariables":["JAVA_HOME","JDK_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/Java","%ProgramFiles%/Eclipse Adoptium","%USERPROFILE%/.jdks"],"linuxSearchRoots":["/usr/bin","/usr/lib/jvm","/usr/java","$HOME/.sdkman/candidates/java"],"macOsSearchRoots":["/usr/bin","/Library/Java/JavaVirtualMachines","$HOME/.sdkman/candidates/java","/opt/homebrew/opt/openjdk"],"validationArguments":"-version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["pom.xml","build.gradle","build.gradle.kts","settings.gradle","settings.gradle.kts"],"contextTags":["java","jdk"],"maximumSearchDepth":4}
```

```
{"key":"java-runtime","displayName":"Java runtime","language":"Java","kind":"runtime","executableNames":["java","java.exe"],"environmentRootVariables":["JAVA_HOME","JRE_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/Java","%ProgramFiles%/Eclipse Adoptium"],"linuxSearchRoots":["/usr/bin","/usr/lib/jvm","$HOME/.sdkman/candidates/java"],"macOsSearchRoots":["/usr/bin","/Library/Java/JavaVirtualMachines","$HOME/.sdkman/candidates/java","/opt/homebrew/opt/openjdk"],"validationArguments":"-version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["pom.xml","build.gradle","build.gradle.kts"],"contextTags":["java","runtime"],"maximumSearchDepth":4}
```

```
{"key":"gradle","displayName":"Gradle","language":"Java","kind":"build-tool","executableNames":["gradle","gradle.bat","gradlew","gradlew.bat"],"environmentRootVariables":["GRADLE_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.gradle","%USERPROFILE%/.sdkman/candidates/gradle"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.sdkman/candidates/gradle"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","$HOME/.sdkman/candidates/gradle"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["gradlew","gradlew.bat","build.gradle","build.gradle.kts"],"contextTags":["gradle","java"],"maximumSearchDepth":3}
```

```
{"key":"maven","displayName":"Apache Maven","language":"Java","kind":"build-tool","executableNames":["mvn","mvn.cmd","mvnw","mvnw.cmd"],"environmentRootVariables":["MAVEN_HOME","M2_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.m2"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.m2"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","$HOME/.m2"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["pom.xml","mvnw","mvnw.cmd"],"contextTags":["maven","java"],"maximumSearchDepth":2}
```

```
{"key":"python","displayName":"Python","language":"Python","kind":"runtime","executableNames":["python","python.exe","python3","python3.exe","py.exe"],"environmentRootVariables":["PYTHONHOME","VIRTUAL_ENV"],"commonSearchRoots":[],"windowsSearchRoots":["%LOCALAPPDATA%/Programs/Python","%USERPROFILE%/AppData/Local/Microsoft/WindowsApps"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.local/bin"],"macOsSearchRoots":["/usr/bin","/usr/local/bin","/opt/homebrew/bin","$HOME/.local/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["pyproject.toml","requirements.txt","setup.py","Pipfile"],"contextTags":["python"],"maximumSearchDepth":2}
```

```
{"key":"node","displayName":"Node.js","language":"JavaScript","kind":"runtime","executableNames":["node","node.exe"],"environmentRootVariables":["NODE_HOME","NVM_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/nodejs","%APPDATA%/nvm"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.nvm/versions/node"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","$HOME/.nvm/versions/node"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["package.json","package-lock.json","pnpm-lock.yaml","yarn.lock"],"contextTags":["node","javascript"],"maximumSearchDepth":3}
```

```
{"key":"powershell","displayName":"PowerShell","language":"PowerShell","kind":"runtime","executableNames":["pwsh","pwsh.exe","powershell.exe"],"environmentRootVariables":[],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/PowerShell","%SystemRoot%/System32/WindowsPowerShell/v1.0"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","/opt/microsoft/powershell"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","/usr/local/microsoft/powershell"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["*.ps1","*.psm1","*.psd1"],"contextTags":["powershell"],"maximumSearchDepth":3}
```

```
{"key":"gcc","displayName":"GNU C compiler","language":"C","kind":"compiler","executableNames":["gcc","gcc.exe"],"environmentRootVariables":[],"commonSearchRoots":[],"windowsSearchRoots":["C:/msys64/usr/bin","C:/msys64/mingw64/bin","C:/mingw64/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/bin","/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["CMakeLists.txt","Makefile","*.c"],"contextTags":["gcc","c"],"maximumSearchDepth":1}
```

```
{"key":"gpp","displayName":"GNU C++ compiler","language":"Cpp","kind":"compiler","executableNames":["g++","g++.exe"],"environmentRootVariables":[],"commonSearchRoots":[],"windowsSearchRoots":["C:/msys64/usr/bin","C:/msys64/mingw64/bin","C:/mingw64/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/bin","/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["CMakeLists.txt","Makefile","*.cpp"],"contextTags":["gcc","cpp"],"maximumSearchDepth":1}
```

```
{"key":"clang","displayName":"Clang compiler","language":"Cpp","kind":"compiler","executableNames":["clang","clang.exe","clang++","clang++.exe"],"environmentRootVariables":["LLVM_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/LLVM/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/bin","/usr/local/bin","/opt/homebrew/opt/llvm/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["CMakeLists.txt","Makefile","*.c","*.cpp"],"contextTags":["clang","llvm"],"maximumSearchDepth":2}
```

```
{"key":"cmake","displayName":"CMake","language":"Native","kind":"build-tool","executableNames":["cmake","cmake.exe"],"environmentRootVariables":["CMAKE_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/CMake/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","/Applications/CMake.app/Contents/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["CMakeLists.txt","CMakePresets.json"],"contextTags":["cmake","native"],"maximumSearchDepth":2}
```

```
{"key":"rust-cargo","displayName":"Rust Cargo","language":"Rust","kind":"package-build-tool","executableNames":["cargo","cargo.exe","rustc","rustc.exe"],"environmentRootVariables":["CARGO_HOME","RUSTUP_HOME"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.cargo/bin"],"linuxSearchRoots":["$HOME/.cargo/bin","/usr/bin","/usr/local/bin"],"macOsSearchRoots":["$HOME/.cargo/bin","/usr/local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["Cargo.toml","Cargo.lock"],"contextTags":["rust","cargo"],"maximumSearchDepth":1}
```

```
{"key":"go","displayName":"Go toolchain","language":"Go","kind":"compiler-runtime","executableNames":["go","go.exe"],"environmentRootVariables":["GOROOT","GOPATH"],"commonSearchRoots":[],"windowsSearchRoots":["%ProgramFiles%/Go/bin","%USERPROFILE%/go/bin"],"linuxSearchRoots":["/usr/bin","/usr/local/go/bin","$HOME/go/bin"],"macOsSearchRoots":["/usr/local/go/bin","/opt/homebrew/bin","$HOME/go/bin"],"validationArguments":"version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["go.mod","go.work"],"contextTags":["go","golang"],"maximumSearchDepth":1}
```

```
{"key":"platformio","displayName":"PlatformIO Core","language":"Embedded","kind":"build-tool","executableNames":["platformio","platformio.exe","pio","pio.exe"],"environmentRootVariables":["PLATFORMIO_CORE_DIR"],"commonSearchRoots":[],"windowsSearchRoots":["%USERPROFILE%/.platformio/penv/Scripts"],"linuxSearchRoots":["$HOME/.platformio/penv/bin","$HOME/.local/bin"],"macOsSearchRoots":["$HOME/.platformio/penv/bin","$HOME/.local/bin","/opt/homebrew/bin"],"validationArguments":"--version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["platformio.ini"],"contextTags":["platformio","embedded"],"maximumSearchDepth":1}
```

```
{"key":"arduino-cli","displayName":"Arduino CLI","language":"Embedded","kind":"build-tool","executableNames":["arduino-cli","arduino-cli.exe"],"environmentRootVariables":["ARDUINO_DIRECTORIES_DATA","ARDUINO_DIRECTORIES_USER"],"commonSearchRoots":[],"windowsSearchRoots":["%LOCALAPPDATA%/Programs/Arduino IDE/resources/app/lib/backend/resources","%ProgramFiles%/Arduino IDE/resources/app/lib/backend/resources"],"linuxSearchRoots":["/usr/bin","/usr/local/bin","$HOME/.local/bin"],"macOsSearchRoots":["/usr/local/bin","/opt/homebrew/bin","/Applications/Arduino IDE.app/Contents/Resources/app/lib/backend/resources"],"validationArguments":"version","versionRegexPatternName":"builtin.toolchain-version-token","projectMarkers":["*.ino","arduino-cli.yaml"],"contextTags":["arduino","embedded"],"maximumSearchDepth":2}
```

## Version knowledge

A discovery profile only tells LocalGPT how to find and probe a tool locally. Exact-version behavior belongs in normal Council Knowledge entries. Tag such an article with `toolchain:<profile-key>` and `version:<exact-version>` where practical. If LocalGPT detects a version for which no approved/pinned article exists, it asks the local user for a Markdown file, Knowledge Database article, or text blob through Human Collaboration. The request does not trigger an online lookup.
