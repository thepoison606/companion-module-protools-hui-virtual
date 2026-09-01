# companion-module-protools-hui-virtual

Prototype Bitfocus Companion module that exposes a **virtual MIDI/HUI endpoint directly to Pro Tools on Windows**.

The user-facing architecture is:

```text
Companion
  |
  +-- Pro Tools HUI Virtual MIDI module
        |
        +-- bundled Windows helper
              |
              +-- Windows MIDI Services Virtual Device transport
                    |
                    +-- "Companion Pro Tools HUI" MIDI 1.0 compatibility port
                              |
                              +-- Pro Tools HUI
```

There is no separately configured loopMIDI or Bome layer. The small native helper is an implementation detail of the Companion module because the Windows MIDI Services virtual-device API is a WinRT API and is not directly exposed by Companion's Node.js runtime.

## Prototype features

- Creates one bidirectional virtual MIDI endpoint named `Companion Pro Tools HUI` by default.
- Requests Windows MIDI Services MIDI 1.0 compatibility ports for legacy/WinMM clients.
- Implements the HUI keepalive handshake.
- Toggle mute for HUI strips 1..8.
- Set mute on/off when the actual Pro Tools mute state is known.
- Parses Pro Tools HUI mute status and exposes it as Companion feedback.
- Exposes connection/mute states as Companion variables.

## Source layout

```text
companion/                 Companion manifest/help
src/                       Companion Node.js module
helper/                    C# Windows MIDI Services helper
runtime/win-x64/           built helper copied here before packaging
scripts/                    PowerShell build/package scripts
docs/HUI.md                HUI subset implemented by the prototype
```

## Build on the target Windows machine

### 1. Prerequisites

Install/enable Windows MIDI Services, then install the .NET 10 SDK. The C# project currently follows Microsoft's Windows MIDI Services C# samples and targets Windows build API level 26100.

### 2. Build the helper

From PowerShell in the repo root:

```powershell
.\scripts\build-windows-helper.ps1
```

This publishes the self-contained x64 helper into:

```text
runtime\win-x64\ProToolsHuiBridge.exe
```

### 3. Optional: test the virtual endpoint without Companion

```powershell
.\scripts\test-helper.ps1
```

While that process stays open, check whether `Companion Pro Tools HUI` appears in Pro Tools. This isolates Windows MIDI Services / Pro Tools compatibility from the Companion module itself.

### 4. Build the Companion module package

```powershell
.\scripts\package-module.ps1
```

Or run both stages:

```powershell
.\scripts\build-all.ps1
```

The Companion module build tool creates an importable `.tgz` package.

## Pro Tools setup

After importing/enabling the module in Companion:

1. In Pro Tools open **Setup -> Peripherals -> MIDI Controllers**.
2. Add **HUI**.
3. Set **Receive From** to `Companion Pro Tools HUI`.
4. Set **Send To** to `Companion Pro Tools HUI`.
5. Use 8 channels.

Then create a Companion button with **Toggle track mute** and add the **Track is muted** feedback for the same strip.

## JSON-line protocol between module and bundled helper

Companion -> helper examples:

```json
{"cmd":"toggleMute","track":1}
{"cmd":"setMute","track":1,"muted":true}
{"cmd":"getState"}
{"cmd":"shutdown"}
```

Helper -> Companion examples:

```json
{"event":"ready","endpoint":"Companion Pro Tools HUI"}
{"event":"connected","connected":true}
{"event":"mute","track":1,"muted":true}
{"event":"state","connected":true,"mutes":[true,false,null,null,null,null,null,null]}
```

## Known uncertainties to validate on the Pro Tools machine

This repository is a prototype. The critical first test is whether the Windows MIDI Services virtual-device transport creates a MIDI 1.0 compatibility endpoint that the installed Pro Tools build exposes in its HUI `Receive From`/`Send To` lists. The code explicitly sets `CreateOnlyUmpEndpoints = false` and declares the function block as a MIDI 1.0 connection.

The second test is the exact HUI mute feedback behavior of the installed Pro Tools version. The parser currently implements the common HUI zone/port state messages documented in `docs/HUI.md`.

## Not implemented yet

- HUI banking / tracks beyond the current eight strips
- track-name mapping
- solo / record arm
- faders / pan
- transport controls
- automatic helper recovery after a crash
- installer/checker for Windows MIDI Services
