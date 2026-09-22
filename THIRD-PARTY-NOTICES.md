# Third-Party Notices

`Bonobo.Bepuphysics2` is a fork of bepuphysics v2 and redistributes its source code and binaries.

## bepuphysics v2 (BepuPhysics / BepuUtilities)

- Upstream: <https://github.com/bepu/bepuphysics2>
- Copyright: © Bepu Entertainment LLC
- Author: Ross Nordby
- License: Apache License 2.0
- Vendored version: `2.5.0-beta.24` (forked at upstream commit `c230dd11`)
- Modifications in this fork:
  - Root namespaces renamed to `Bonobo.Bepuphysics2` and `Bonobo.BepuUtilities`.
  - Assemblies renamed accordingly; both are bundled in the single `Bonobo.Bepuphysics2` NuGet package.
  - Target framework raised to .NET 10 (`LangVersion` 14) with `IsAotCompatible=true`.
  - Strong-name signing removed; `Microsoft.SourceLink.GitHub` dependency removed.
  - Demos, benchmark, and legacy test projects removed; source moved under `src/`.
  - Added `src/Bonobo.Bepu.AotProbe` (Native AOT / zero-allocation / determinism verification).
  - No algorithmic source changes; public API surface is unchanged apart from namespaces.

The full license text is in [`LICENSE.md`](LICENSE.md).
