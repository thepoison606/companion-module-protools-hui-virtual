# HUI subset used by this prototype

This prototype deliberately implements only the HUI messages needed for eight mute buttons and their status feedback.

## Keepalive

Pro Tools -> surface:

```text
90 00 00
```

Surface -> Pro Tools:

```text
90 00 7F
```

The helper answers the ping immediately and considers Pro Tools connected while pings continue to arrive.

## Surface -> Pro Tools mute click

For HUI strip `N` (1..8), zone is `N - 1`.

Press:

```text
B0 0F <zone>
B0 2F 42
```

Release:

```text
B0 0F <zone>
B0 2F 02
```

Port 2 is the HUI mute switch. Bit 0x40 indicates the pressed state.

## Pro Tools -> surface mute LED/state

Pro Tools uses the host-to-surface selector pair:

```text
B0 0C <zone>
B0 2C <value>
```

The helper treats low nibble `2` as the mute port and bit `0x40` as the mute LED/state bit. This state becomes the Companion feedback.

## Scope

Only HUI strips 1..8 are implemented. Banking, solo, record, faders, scribble strips and transport are intentionally out of scope for the prototype.
