# Bonobo.Bepuphysics2

Native AOT / WASM-compatible pure C# 3D physics library for the Bonobo Engine ecosystem.
Forked from [bepuphysics v2](https://github.com/bepu/bepuphysics2) (Apache-2.0, Ross Nordby / Bepu Entertainment LLC).

- **Namespaces:** `Bonobo.Bepuphysics2` (physics) and `Bonobo.BepuUtilities` (math, memory, task scheduling).
- **Package:** single NuGet package `Bonobo.Bepuphysics2` (bundles `Bonobo.BepuUtilities.dll` plus XML docs), version `1.0.0`.
- **Target:** .NET 10, `LangVersion` 14, `IsAotCompatible=true`, unsigned assemblies, zero external NuGet dependencies.

## Install

```powershell
dotnet add package Bonobo.Bepuphysics2 --version 1.0.0 --source X:\DEVSERVER\Nuget
```

## Usage

```csharp
using Bonobo.Bepuphysics2;
using Bonobo.Bepuphysics2.Collidables;
using Bonobo.Bepuphysics2.CollisionDetection;
using Bonobo.BepuUtilities.Memory;

var pool = new BufferPool();
var simulation = Simulation.Create(
    pool,
    new NarrowPhaseCallbacks(),
    new PoseIntegratorCallbacks(new Vector3(0, -10, 0)),
    new SolveDescription(8, 1));

// ... add bodies/statics ...

// null dispatcher => deterministic, single-threaded, zero-allocation steady state.
simulation.Timestep(1f / 60f);
```

A complete callbacks + body/static sample is in [`src/Bonobo.Bepu.AotProbe/Program.cs`](src/Bonobo.Bepu.AotProbe/Program.cs).

## Bonobo Engine integration contract

- **Native AOT:** no `System.Reflection` / `Activator.CreateInstance` / dynamic IL on any path. Trim and AOT analyzers are clean; verified by a real native publish of the probe.
- **Zero allocation per tick:** the null-dispatcher `Timestep` steady state allocates **0** managed bytes (asserted by the probe).
- **Deterministic:** always call `Timestep(dt)` with a `null` `IThreadDispatcher` on the engine/WASM path.
- **ECS-friendly data:** `BodyHandle` / `StaticHandle` are blittable structs; `Buffer<T>` implicitly converts to `Span<T>`, so pose state (`Bodies.ActiveSet.DynamicsState`) can be read zero-copy into the engine's `Float64` pinned transform buffer.
- **No C# collision events:** implement `INarrowPhaseCallbacks` and accumulate contacts into preallocated buffers, then drain them after `Timestep`.

See [`docs/ecs-integration.md`](docs/ecs-integration.md) for the BonoboECS mapping and engine migration steps.

## Build, pack, publish

```powershell
dotnet build src/Bonobo.Bepuphysics2.sln -c Release
dotnet pack  src/Bonobo.Bepuphysics2/Bonobo.Bepuphysics2.csproj -c Release -o artifacts
dotnet nuget push artifacts/Bonobo.Bepuphysics2.1.0.0.nupkg -s X:\DEVSERVER\Nuget
```

## Verification

```powershell
dotnet run     --project src/Bonobo.Bepu.AotProbe -c Release            # zero-alloc + null-dispatcher probe
dotnet publish src/Bonobo.Bepu.AotProbe -c Release -r win-x64           # native AOT smoke test
```

Native AOT publish on Windows requires `vswhere.exe` on `PATH` (`C:\Program Files (x86)\Microsoft Visual Studio\Installer`).

## Documentation

XML documentation is generated for both assemblies (`GenerateDocumentationFile=true`) and is packed into the NuGet package as the API reference.
Conceptual docs from upstream live in [`docs/bepuphysics2/`](docs/bepuphysics2/) (GettingStarted, Substepping, PerformanceTips, ...).

## License

Apache-2.0. See [`LICENSE.md`](LICENSE.md) and [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).
