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

## Effects and audio

Particle prefabs live in `Prefabs/Vfx/` on one additive material; every burst is under fifty
particles and destroys itself. Sounds are a CC0 subset of Kenney's Sci-Fi, Impact and Interface
packs under `Assets/Kenney/<Pack>/`, routed through `Audio/DropProtocol.mixer`
(Master → SFX, UI). Which clip plays is data on the weapon, enemy and protocol definitions.

## Asset pipeline

- Project-owned content lives under `Assets/_Project/`.
- Imported CC0 Kenney content lives under `Assets/Kenney/` — no `ThirdParty` wrapper folder.
  Each pack gets its own folder, `Assets/Kenney/<Pack>/`, holding `Models/`, `Textures/`, and the
  pack's `License.txt`. Import the FBX flavour only; GLB/OBJ duplicates stay out of the repository.
  Raw downloaded zips sit in the gitignored `Private/ExternalAssets/` folder.
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
