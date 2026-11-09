# Core Critical native host and launcher

Build source for the Windows x64 native host and .NET 8 WPF launcher.
The project preserves the original game's translated logic while supplying modern
platform services. This tree contains no website, deployment system, private Git
history, signing material, original game content or translated game binaries.

Retained
lookup tables are interoperability metadata; shader arrays compile authored GLSL.
Project contributions use the included MIT license. Third-party components retain
their own licenses and notices; original game content is not covered by MIT.

## Build the launcher

Install the .NET 8 SDK on Windows, then run from this root:

```powershell
dotnet build tools/mw3-remaster-launcher/MW3RemasterLauncher.csproj -c Release
```

For controller support, first run the pinned SDL download helper:

```powershell
pwsh -File tools/mw3-remaster-launcher/fetch_controller_runtime.ps1
dotnet publish tools/mw3-remaster-launcher/MW3RemasterLauncher.csproj -c Release -r win-x64 --self-contained true -o local/launcher
```

Network distribution services are not configured. Automatic online updates need
an operator-supplied online-updates.json and independently trusted update keys in
update-trust.json. No production feed, bucket, website or account details are
included. Local game/pack handling remains source code. Crash submission requires
an explicit HTTPS CORE_CRITICAL_CRASH_REPORT_URL; otherwise it fails visibly and
players can export a support report locally. Do not enable reporting to a service
without its consent and privacy policy.

## Build the native host

Install Python 3.11+, Visual Studio C++ Build Tools and Windows SDK, LLVM21.1.8,
CMake/Ninja, Remill v6.0.1 headers and Vulkan-Headers v1.4.321. Versions, header
integrity pins and upstream source URLs are in
tools/m4-crt-heap-carry/host-dependencies.json. The Windows wrapper locates the
compiler environment. From a committed local Git checkout:

```powershell
pwsh -File tools/m4-crt-heap-carry/build_host.ps1 `
  -GameDirectory 'C:/Games/MechWarrior 3' `
  -RuntimeDirectory 'C:/BuildInputs/MW3Runtime' `
  -RemillInclude 'C:/Dev/remill/include' `
  -VulkanInclude 'C:/Dev/Vulkan-Headers/include' `
  -LLVMDirectory 'C:/Dev/LLVM/bin' `
  -OutputDirectory 'C:/Dev/host-output' -Release
```

Use a fresh output directory. The build validates supported original Mech3.exe,
runtime.json, the existing bin/MW3Translated.dll, host source pins and consumed
dependency headers; it compiles seven authored host objects and copies the
translated DLL unchanged. It records source provenance in source-snapshot.json.

Important build boundary: this minimal tree builds the native host, not the
translated game module. An existing locally generated runtime is an explicit
prerequisite. This export does not yet provide a reviewed standalone path from
only an original installation to MW3Translated.dll. No original executable,
provider DLL, generated instruction body, media, assets or compiled runtime is
distributed here. A complete game build from
original media alone requires a separate translation build.

## Source maintenance

tools/m4-crt-heap-carry/host is canonical authored host source. Refresh its
host-source-manifest.json hashes for deliberate changes. The other runtime and
tools headers in this tree are its compiler-consumed dependencies. Preserve
third-party notices and exact pinned bytes. Compiled project-authored shader
arrays are included as host build inputs. This minimal tree includes only some
standalone shader sources and does not yet
provide a complete shader-regeneration workflow. Keep original inputs and build
outputs outside Git.
