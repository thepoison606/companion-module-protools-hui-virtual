# Prototype validation status

Validated in the current build environment:

- Companion-side JavaScript parses successfully with Node.js 22.
- Manifest structure follows the current Companion connection-module schema and declares `child-process` permission.
- `build-config.cjs` explicitly packages the helper publish output as Companion `extraFiles`.
- HUI message generation/parsing matches the documented zone/port direction split used by HUI: surface-to-host `0F/2F`, host-to-surface `0C/2C`.

Not validated in this environment:

- The C# helper has not been compiled here because this environment does not have the .NET SDK and is not Windows.
- Windows MIDI Services virtual endpoint creation must be tested on the target Windows machine.
- Pro Tools visibility of the generated MIDI 1.0 compatibility port must be tested with the installed Pro Tools/Windows build.
- Exact mute feedback behavior should be confirmed with MIDI logging on the target system.

The first useful Windows test is simply: build/run the helper, then check whether `Companion Pro Tools HUI` appears in Pro Tools' HUI Receive From / Send To lists.
