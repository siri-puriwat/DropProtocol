# Art and Licensing

## Direction

**Stylized low-poly military science fiction.** Photorealism is explicitly out of scope.

Priorities, in order:

1. readable silhouettes at top-down camera distance
2. clear team/enemy identification
3. strong combat feedback
4. simple geometry and restrained material complexity

## Characters

Prototype characters use CC0 models. The Milestone 1 player is Kenney's Blocky Characters
`character-g`, scaled to 0.65 (about 1.75 m) as a presentation child of a capsule
`CharacterController`; `character-h` is the natural second-player variant. The pack ships rigged
with 27 clips per character (idle, walk, sprint, holding-*-shoot, die, interact, ...), which is what
the Milestone 8 animation work will build on. A later version gets one custom low-poly DropProtocol
soldier: helmeted, armored, backpack, no facial animation. Player variation comes from materials,
armor colors, helmet and backpack variants — not from multiple unique character models.

## Animation

Locomotion uses an Animator blend tree; upper-body aiming uses a transform-masked layer rather
than a unique clip per aim direction. The body already turns toward the aim, so no procedural
rigging is needed (see `Architecture.md`, Presentation split).

```text
Base Locomotion + Upper Body Aim + Action Animation = Final Character Pose
```

Only `character-g.fbx` imports animation; the other letters import meshes and share its clips
because every blocky rig has the same bone paths. Clip mapping (Kenney has no strafe, reload or
flinch clips, so the nearest CC0 stand-ins are used):

| Need | Clip | Note |
|---|---|---|
| idle / walk | `idle`, `walk` | `walk` fills all four blend-tree directions; backward plays it reversed |
| aim / fire | `holding-both`, `holding-both-shoot` | upper-body layer |
| reload, interact | `interact-left` | looped while the replicated flag is set |
| throw (protocol call) | `attack-melee-right` | upper-body layer |
| downed / get up | `die`, `pick-up` | |
| enemy attack | `attack-melee-right` (Grunt), `attack-kick-right` (Brute), `holding-right-shoot` (Spitter) | override controllers |
| hit flash | none | `_BaseColor` tint through a property block |

Enemies are blocky characters too: Grunt `character-c` (0.6, red skin), Spitter `character-m`
(0.55, green), Brute `character-p` (0.9, purple). The letters are interchangeable; the tinted skin
materials in `Art/Materials/` are what keep them readable as enemies from the top-down camera.

## Weapons and props

Guns and props are a subset of Kenney's Blaster Kit under `Assets/Kenney/BlasterKit/`, imported
with no materials (`materialImportMode` None) and one shared URP Lit material on the kit atlas. The
player rifle is `blaster-f` on a `GunAnchor` under `Visual/root/torso/arm-right`; the blocky rig has
no hand bone, so the anchor is placed by eye in the aim pose. The anchor is presentation only: the
hitscan still starts 0.9 m above the root (`WeaponController` muzzle), and each peer draws its
tracer and flash from its own barrel tip (`HitscanTracer`, `CharacterPresentation`) because the
server samples the muzzle on a culled animator. The sentry carries `blaster-e`, the strike beacon
`grenade-b`, the supply pod the animated `crate-medium` (lid opens on arrival) and the target
dummy a `target-large` plate. Squad slots are told apart by four skin materials on the player body,
the same pattern as the enemy skins; the marker keeps its property block.

## Level kits

The mission map is built from two Kenney kits under `Assets/Kenney/`, both imported with no
materials (one URP Lit material per kit on the kit atlas, GPU instancing on) and no rigs, and only
the pieces the map uses. The **Modular Space Kit** is authored at human scale and imports at scale 1:
its 4 m module fits the 80 m ground exactly and its walls (4.00 x 4.25 x 1.00 m, pivot on the inner
face) form the perimeter. The **Space Station Kit** is a diorama kit and imports at global scale 2,
which is what puts its props above the 0.9 m muzzle line the combat rules assume: interior walls
2.00 m, `container` 1.20, `container-wide` 1.40, `container-tall` 1.80, `computer-system` 1.20,
`skip` 1.00, while rails and tables (0.80 m) and rocks (0.71 m) deliberately stay below it. Floor
slabs are 0.60 m thick with the walking face on top, so they sit at y -0.59 over the ground plane.
`structure-barrier` is an open frame of 17 meshes and never gets a collider. Level prefabs live in
`Prefabs/Level/`, are plain GameObjects (no `NetworkObject`), and only pieces that must obstruct
carry a hand-sized `BoxCollider`; the perimeter uses four long colliders instead of one per wall.
The cover floor drops from the 1.5 m primitive cubes to 1.2 m containers.

Measured after import (bounds size, metres):

| Piece | Size | Note |
|---|---|---|
| Modular `template-wall` | 4.00 x 4.25 x 1.00 | body on the pivot's -Z side |
| Modular `template-wall-corner` | 1.00 x 4.05 x 1.00 | pivot at a corner |
| Modular `template-floor` | 4.00 x 0.00 x 4.00 | flat quad |
| Station `wall` | 2.00 x 2.00 x 0.60 | centred |
| Station `wall-pillar` | 2.00 x 2.00 x 1.00 | centred |
| Station `container` / `-wide` / `-tall` | 1.15 x 1.20 / 1.20 x 1.40 / 1.20 x 1.80 | centred |
| Station `container-flat` | 1.32 x 1.20 x 2.18 | centred |
| Station `computer-system` | 1.80 x 1.20 x 1.39 | base pivot |
| Station `floor`, `floor-panel*` | 2.00 x 0.60 x 2.00 | walk on the top face |
| Station `rail` / `rail-narrow` | 2.00 / 1.00 x 0.80 x 0.20 | below the muzzle line |
| Station `skip` | 1.40 x 1.00 x 2.40 | marginal cover |
| Station `rocks` | 2.22 x 0.71 x 2.03 | pivot at a corner |

## Effects and audio

Particle prefabs live in `Prefabs/Vfx/` on one additive material; every burst is under fifty
particles and destroys itself. Sounds are CC0 subsets of Kenney's Sci-Fi Sounds, Impact Sounds,
Interface Sounds and Music Jingles packs, flat under `Assets/Kenney/<Pack>/` with the Kenney file
names untouched, routed through `Audio/DropProtocol.mixer` (Master → SFX, UI, Music, Ambience).
Which clips play is data: an `SfxCue` (clip set, volume, pitch range) on the weapon, enemy and
protocol definitions and on the payload indicators. Music Jingles are short stingers only; there is
no music loop in any pack, so the mission has stingers and ambience beds, not music. Loop beds
import as compressed-in-memory; everything else keeps the default import settings.

## Asset pipeline

- Project-owned content lives under `Assets/_Project/`.
- Imported CC0 Kenney content lives under `Assets/Kenney/` — no `ThirdParty` wrapper folder.
  Each pack gets its own folder, `Assets/Kenney/<Pack>/`. Model packs hold `Models/`, `Textures/`
  and the pack's `License.txt`; sound packs are flat (`.ogg` beside `License.txt`). Import the FBX
  flavour only, and only the pieces the game uses; GLB/OBJ duplicates and unused pieces stay out
  of the repository. Raw downloaded zips sit in the gitignored `Private/ExternalAssets/` folder.
- Large binaries (`.fbx .blend .png .tga .psd .wav .ogg`) are tracked with Git LFS. Judge each type
  by size and change frequency rather than pushing every binary into LFS reflexively.

## Licensing rules

The repository is **public**, so everything committed must be redistributable:

1. assets created for DropProtocol, or
2. assets whose license explicitly permits redistribution, or
3. CC0 assets.

Never commit raw Unity Asset Store content that forbids redistribution, and never commit raw Mixamo
character or animation files.

Record every external asset — name, creator, source, license — in
[`../THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md) at the time it is imported, not later.
