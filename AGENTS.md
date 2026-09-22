# Agent Guide: Bonobo.Bepuphysics2

Fork of [bepuphysics v2](https://github.com/bepu/bepuphysics2) packaged for the Bonobo Engine
(Native AOT WebAssembly) and BonoboECS (Arch fork).

Consumer context: `bonoboengine.wasm.3D/AGENTS.md` (engine) and <https://github.com/josejaviercanon/Bonobo-ECS>.

## Repository map

- `src/Bonobo.Bepuphysics2/` — physics library **and** the NuGet package project (`Bonobo.Bepuphysics2.csproj`).
- `src/Bonobo.BepuUtilities/` — math / memory / task-scheduling utilities. `IsPackable=false`; ships inside the physics package.
- `src/Bonobo.Bepu.AotProbe/` — verification console app: native AOT, zero-allocation, null-dispatcher determinism.
- `docs/bepuphysics2/` — upstream conceptual docs (GettingStarted, Substepping, PerformanceTips, changelog).
- `docs/ecs-integration.md` — BonoboECS mapping and engine migration steps.
- `CommonSettings.props` — shared TFM / version / AOT / package metadata.

## Hard invariants

1. **AOT strict compliance.** No `System.Reflection`, `Activator.CreateInstance`, or dynamic IL outside `#if DEBUG`.
   `IsAotCompatible=true`. The probe treats `IL2026`, `IL2046`, `IL3050`, `IL3051`, `IL3052` as build errors.
2. **Zero managed allocation in the tick.** All dynamic collections route through `BufferPool` / `Buffer<T>`; no `class`
   instantiation inside `Timestep`. The probe asserts 0 allocated bytes across 256 steady-state steps.
3. **Deterministic single-threaded solves.** Engine and WASM paths call `Simulation.Timestep(dt)` with a `null`
   `IThreadDispatcher`. Threading machinery (`TaskStack`, `SpinLock`, worker loops) must stay inert when the dispatcher is null.
4. **No C# events for collisions.** Contact data flows through `INarrowPhaseCallbacks` into preallocated buffers and is
   drained after `Timestep`.
5. **Blittable handles.** `BodyHandle` / `StaticHandle` remain blittable structs usable as ECS component payloads.
6. **Namespace ABI.** Public namespaces are `Bonobo.Bepuphysics2` and `Bonobo.BepuUtilities`. Never reintroduce the
   `BepuPhysics` / `BepuUtilities` root namespaces.
7. **Single package.** `Bonobo.Bepuphysics2` bundles both assemblies. `Bonobo.BepuUtilities` is never published on its own.
8. **Zero external NuGet dependencies.** Vendor code into the repo instead of adding `PackageReference` entries.

## Commands

```powershell
dotnet build   src/Bonobo.Bepuphysics2.sln -c Release
dotnet run     --project src/Bonobo.Bepu.AotProbe -c Release
dotnet publish src/Bonobo.Bepu.AotProbe -c Release -r win-x64
dotnet pack    src/Bonobo.Bepuphysics2/Bonobo.Bepuphysics2.csproj -c Release -o artifacts
dotnet nuget push artifacts/Bonobo.Bepuphysics2.1.0.0.nupkg -s X:\DEVSERVER\Nuget
```

Native AOT publish on Windows requires `vswhere.exe` on `PATH` (`C:\Program Files (x86)\Microsoft Visual Studio\Installer`).

## Documentation

`GenerateDocumentationFile=true` emits XML API docs for both assemblies, and they are packed into the NuGet package.
When public API changes, update the XML comments, `README.md`, and `docs/ecs-integration.md`.

## Conventions

- Do not change `TargetFramework`, `LangVersion`, `IsAotCompatible`, or `PackageId` without an explicit request.
- Do not add comments to source files unless asked.
- Keep `CommonSettings.props` as the single source of shared build metadata.
