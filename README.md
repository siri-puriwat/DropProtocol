# DropProtocol

![Unity 6000.3](https://img.shields.io/badge/Unity-6000.3-black?logo=unity)
![URP](https://img.shields.io/badge/render%20pipeline-URP-blue)
![Netcode for GameObjects 2.13](https://img.shields.io/badge/Netcode%20for%20GameObjects-2.13-blueviolet)
![License MIT](https://img.shields.io/badge/license-MIT-green)

A 1–4 player cooperative top-down 3D shooter built in Unity 6 with server-authoritative
networking, bot squadmates, and a data-driven combat loop.

> **Deploy → Complete Objectives → Survive Enemy Pressure → Extract**

![Squad of four, bots included, holding a wave off in the mission scene](Documentation/Images/hero.gif)

DropProtocol is a deliberately small multiplayer **vertical slice**: the goal is clean, readable
architecture rather than feature count. Architecture, networking and asset notes live in
[Documentation/](Documentation/). A playable Windows build is on the
[Releases page](../../releases/latest).

## Highlights

- Server-authoritative multiplayer with Netcode for GameObjects (host/client, 1–4 players)
- A command abstraction that makes human input, network input, bots, and tests interchangeable
- Bot squadmates driving the *same* player character implementation as humans
- Data-driven weapons, enemies and protocols via ScriptableObjects, effects and sound cue sets included
- A threat-budget enemy director rather than fixed spawn timers
- Host-validated support protocols: directional sequences that call in a supply drop, a sentry or an air strike
- Animation, VFX and audio derived from replicated state on every peer — no `NetworkAnimator`
- A uGUI + TextMeshPro HUD bound to `NetworkVariable`s, with a runtime network stats overlay
- EditMode tests for every pure rule set and PlayMode tests that run an in-process host over real Netcode

## In pictures

| | |
|---|---|
| ![Mission HUD](Documentation/Images/mission-hud.png) | ![Enemies telegraphing an attack](Documentation/Images/enemies.png) |
| The mission HUD: objective, clock, squad list, protocol loadout | Grunt, Spitter and Brute; the windup tint is the telegraph |
| ![Strike protocol](Documentation/Images/protocol-strike.gif) | ![Bot revive](Documentation/Images/bots-revive.gif) |
| Sentry then Strike, typed as arrow sequences and validated by the host | A bot revives the downed host |
| ![Network stats overlay](Documentation/Images/netstats.png) | ![Main menu](Documentation/Images/main-menu.png) |
| F3: Multiplayer Tools net stats plus tick rate, RTT and object counts | Host, join by address, or open the sandbox |
| ![Mission map](Documentation/Images/mission-map.png) | ![Relay bunker](Documentation/Images/relay-bunker.png) |
| A seeded map: sixteen tiles picked and rotated to match, relays and extraction on tile sites | A relay site on a grey-box tile, all Kenney space kits |

## How it works

- **Command abstraction** — every producer (input, network, bot, test) emits one `PlayerCommand`;
  the character never knows who is driving. [Architecture › Command abstraction](Documentation/Architecture.md#command-abstraction-milestone-1)
- **Server authority** — commands go up on an unreliable RPC, the host simulates, `NetworkTransform`
  and `NetworkVariable`s come back down. [Networking › Authority](Documentation/Networking.md#authority)
- **Bots** — a state machine on the host that outputs the same `PlayerCommand`, so bots and humans
  are the same prefab. [Architecture › Bots](Documentation/Architecture.md#bots-milestone-5)
- **Threat budget** — the director spends a growing budget on Grunts, Spitters and Brutes instead of
  running timers. [Architecture › Enemies and the director](Documentation/Architecture.md#enemies-and-the-director-milestone-4)
- **Seeded map** — the host picks a seed, every peer assembles the same tile grid locally and the
  NavMesh is built at load; nothing about the level crosses the network. [Architecture › Mission map](Documentation/Architecture.md#mission-map-seeded-tiles)
- **Support protocols** — one reliable RPC per key press, matched on the host, payload spawned as a
  NetworkObject. [Architecture › Support Protocols](Documentation/Architecture.md#support-protocols-milestone-7)
- **Presentation split** — locomotion from the transform delta, everything else from state that
  already replicates; nothing is sent for animation. [Architecture › Presentation split](Documentation/Architecture.md#presentation-split-milestone-8)

## Tech stack

| | |
|---|---|
| Engine | Unity **6000.3.15f1** |
| Render pipeline | Universal Render Pipeline (URP) |
| Networking | Netcode for GameObjects, Unity Multiplayer Services SDK |
| Input | Unity Input System |
| Navigation | Unity AI Navigation |
| Animation | Animator blend trees + masked upper-body layer, no NetworkAnimator |
| Camera | Cinemachine |
| Testing | Unity Test Framework (EditMode + PlayMode) |
| Multiplayer testing | Multiplayer Play Mode, Multiplayer Tools |

## Getting started

1. Install Unity **6000.3.15f1** (Unity Hub).
2. Clone the repository — it uses **Git LFS**, so run `git lfs install` once beforehand.
3. Open the project folder in Unity. Packages resolve on first import.
4. Open `Assets/_Project/Scenes/00_Bootstrap.unity` and press Play for the full application
   entry point (Host lands in the mission), `10_Mission_Test.unity` to play the mission directly,
   or `90_Sandbox.unity` to poke at systems in isolation.

### Controls

| Action | Keyboard & mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Aim | Mouse cursor | Right stick |
| Fire | Left mouse button | Right trigger |
| Reload | R | X / Square |
| Interact | E | A / Cross |
| Protocol | Arrow keys | D-pad |
| Debug panel (host only) | F1 | — |
| Network stats overlay | F3 | — |
| Screenshot | F12 | — |

Hold Fire to shoot the assault rifle (an empty magazine reloads on its own), press Reload to
top up early, and hold Interact next to a downed squadmate to revive them. Friendly fire is on.
In the mission, hold Interact inside a relay's ring to activate it (progress is kept if you let
go), and once all relays are up stand inside the extraction zone until the countdown ends.

Support protocols are typed as short arrow sequences and land a few metres in front of you:
Supply `↓ ↓ ↑ →` (heals and refills each squadmate once), Sentry `↑ → ↓ ↑` (a turret that shoots
enemies for a while), Strike `↑ ↓ → ← ↑` (an area attack after a three-second warning that hurts
everyone inside the ring). A wrong key or a pause cancels the sequence; each protocol has its own
cooldown, and none can be called while downed or during deployment.

Optional, recommended for merges: register Unity's YAML merge tool, which `.gitattributes`
already references (path is machine-local, so it is not committed):

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'<Unity>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p \"\$BASE\" \"\$REMOTE\" \"\$LOCAL\" \"\$MERGED\""
git config merge.unityyamlmerge.recursive binary
```

No other undocumented setup steps are required.

## Scenes

| Scene | Purpose |
|---|---|
| `00_Bootstrap` | Application entry point; persistent networking root, then loads the menu |
| `01_MainMenu` | Host a session or join one by IP address |
| `10_Mission_Test` | The playable mission: deploy, activate three communication relays, extract; bots fill the empty squad slots. Hosted from the menu, or directly from the HUD's session card |
| `90_Sandbox` | Developer playground — movement, weapons, enemies, bots without the menu flow; hosts from the HUD's session card, has target dummies, and the F1 debug panel (host only) offers debug damage/revive, enemy director controls, a bot fill toggle (both off by default) and a weapon cycle (assault rifle, shotgun, machine gun) |

## Testing multiplayer locally

1. Open **Window ▸ Multiplayer ▸ Multiplayer Play Mode** and enable one or more virtual players.
2. Open `00_Bootstrap` (or `90_Sandbox`) and press Play.
3. In the main Editor choose **Host**. In each virtual player choose **Join** with `127.0.0.1`.

Sessions use Unity Transport directly on UDP port 7777. Another machine on the same network joins
with the host's LAN IP. Relay and join codes are planned once the project is linked to Unity Gaming
Services; see [Documentation/Networking.md](Documentation/Networking.md).

The same works with two copies of the release build: the player runs in the background, so host in
one window and join `127.0.0.1` from the other.

## Running the tests

Open **Window ▸ General ▸ Test Runner**. EditMode tests cover the pure rules (`*Rules`, `*State`,
`*Math`, `HudFormat`) and run in under a second; PlayMode tests start an in-process host on a private
port per fixture and drive real prefabs, scenes and RPCs. Both suites are expected to be green.

## Building

**DropProtocol ▸ Build ▸ Windows x64** in the editor menu, or from a shell:

```bash
Unity -batchmode -quit -projectPath . -executeMethod DropProtocol.Editor.BuildScript.BuildWindowsFromCommandLine -logFile Logs/build.log
```

The player lands in `Builds/Windows/`. F12 in the editor or the player writes a screenshot to
`Recordings/`.

## Project layout

```text
Assets/
├── _Project/                      project-owned content (Art, Audio, Prefabs, Scenes, Settings, Tests)
├── _ProjectScripts.DropProtocol/  runtime code; folder name matches the assembly
├── Settings/                      URP render pipeline and volume profile assets
└── Kenney/                        imported CC0 Kenney assets
Documentation/                     architecture, networking, netcode references, art & licensing
```

Runtime code lives in a single flat `DropProtocol` namespace; folders express feature
organization, not namespaces. Assembly definitions: `DropProtocol`, `DropProtocol.Tests.EditMode`,
`DropProtocol.Tests.PlayMode`, plus the editor-only `DropProtocol.Editor` for the build script.

## Milestones

| # | Milestone | Status |
|---|---|---|
| 0 | Project foundation | Done |
| 1 | Character foundation (command abstraction, motor, top-down camera) | Done |
| 2 | Multiplayer foundation (host/join, spawning, synced movement) | Done |
| 3 | Combat (health, weapons, authoritative damage, revive) | Done |
| 4 | PvE (Grunt, Spitter, Brute, navigation, enemy director) | Done |
| 5 | Bots (bot command source, follow/combat/revive behavior) | Done |
| 6 | Mission (relay objectives, extraction, results) | Done |
| 7 | Support Protocols (directional sequences, Supply/Sentry/Strike) | Done |
| 8 | Presentation and polish (animation, VFX, audio, HUD, overlay, media, release build) | Done |

## Licensing

Code is MIT licensed (see [LICENSE](LICENSE)). Every third-party asset in this repository is
redistributable and recorded in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
