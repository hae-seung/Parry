# Stage01 — Outer Ruins

- Scene: Assets/Scenes/Stage01.unity
- Original SampleScene is preserved.
- Approximate horizontal span: x=-8 to x=118.
- Player starts at x=3. Four existing Goblin prefab instances occupy x=18, 39, 72, 79.
- Empty boss court begins after the gate at x=88. BossSpawn is at x=105.
- No boss, scene transition, checkpoints, encounter gates, or new combat rules are added.

## Editing

Stage01_OuterRuins contains GroundGrid/Ground, BackgroundGrid/Background, and DecorationGrid/Decoration.
Ground uses TilemapCollider2D merged into a static CompositeCollider2D. Paint collision terrain on this Tilemap.
Background and Decoration tiles have no collision. Tile assets are in Assets/Data/Stage01.
Ground cells are 1 unit; decoration cells are 0.25 units.
Cinemachine follows the player and is confined by CameraBounds.

## Validation

VerifyStage01Traversal runs only during Play and temporarily disables monsters to check actual walking/jumping through the terrain.
Stop Play to restore the normal four encounters. The stage itself does not include the verification code.
