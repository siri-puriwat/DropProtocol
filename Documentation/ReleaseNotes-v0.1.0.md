# DropProtocol v0.1.0

First public build of the vertical slice: milestones 0 to 8.

## What is in the build

- Host or join a 1–4 player session by IP over Unity Transport; bots fill the empty squad slots.
- One mission: deploy, activate three communication relays, extract. Failure on squad wipe or
  time limit.
- Assault rifle with server-resolved hitscan, downed state and teammate revive, friendly fire on.
- Grunt, Spitter and Brute driven by a threat-budget director.
- Support protocols typed as arrow sequences: Supply `↓ ↓ ↑ →`, Sentry `↑ → ↓ ↑`,
  Strike `↑ ↓ → ← ↑`.
- Animated Kenney blocky characters, particle effects and CC0 sound, all derived from replicated
  state on every peer.
- TextMeshPro HUD, host-only debug panel (F1), network stats overlay (F3), screenshot hotkey (F12).

## Running it

Unzip and start `DropProtocol.exe`. To test multiplayer on one machine, start it twice, host in
one window and join `127.0.0.1` in the other. The window keeps running in the background.

## Known limitations

- Direct IP only; Relay and join codes are not wired yet.
- No client-side movement prediction: remote clients feel about one round trip of input latency.
- Kenney's blocky pack has no strafe or reload clips, so the walk cycle stands in for strafes and
  an interact loop stands in for reloading.
- The mission map is a test layout with primitive obstacles.
