# Pro Tools HUI Virtual MIDI — prototype

This module creates a virtual MIDI 1.0-compatible endpoint through Windows MIDI Services and exposes a small HUI subset for Pro Tools.

## Requirements

- Windows 11 with Windows MIDI Services and the Virtual Device transport available
- Companion 4.x
- The bundled `runtime/win-x64/ProToolsHuiBridge.exe`
- Pro Tools with HUI peripheral support

The helper is bundled with the module. No loopMIDI or Bome MIDI Translator is required.

## Pro Tools setup

1. Enable this Companion connection and wait until its status is OK.
2. Open **Setup -> Peripherals -> MIDI Controllers** in Pro Tools.
3. Add one HUI controller.
4. Choose **Companion Pro Tools HUI** (or your configured endpoint name) for both **Receive From** and **Send To**.
5. Set the HUI controller to 8 channels.

If the port does not appear, first verify Windows MIDI Services is installed/running. The prototype asks Windows MIDI Services to create legacy MIDI 1.0 compatibility ports (`CreateOnlyUmpEndpoints = false`) specifically so WinMM/legacy MIDI applications can see it.

## Actions

- **Toggle track mute** — clicks the HUI mute switch for strips 1..8.
- **Set track mute** — only clicks when the current state is known and differs from the requested state.
- **Request helper state** — asks the helper to resend its state snapshot.

## Feedbacks

- **Track is muted** — true when Pro Tools reports the HUI mute LED/state as active.
- **Pro Tools HUI keepalive is active** — true while Pro Tools HUI ping messages are arriving.

## Important prototype limits

- Tracks mean the eight HUI strips of the current HUI bank, not persistent Pro Tools track IDs.
- There is no bank navigation yet.
- Windows MIDI Services Virtual Device APIs are still moving; the helper project pins the SDK package version used by Microsoft's current sample at the time this prototype was created.
