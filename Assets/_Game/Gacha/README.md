# Gacha Runtime Assets

This directory contains production-facing gacha assets and config files.

## Directory layout

- `Art/Sprites/UI`: background panels, buttons, decorative frames.
- `Art/Sprites/Cards`: character and weapon card portraits.
- `Art/Sprites/Effects`: reveal overlays, flashes, streak textures.
- `Audio/Sfx`: click, reveal, rarity stingers.
- `Audio/Bgm`: wish page background loops.
- `Config`: optional exported runtime config bundles.

## Naming conventions

- Folders: PascalCase (`Sprites`, `Cards`, `Effects`)
- Files: lower_snake_case (`five_star_flash.png`, `wish_btn_ten.png`)
- Reward portraits: use reward id (`c_001.png`, `w_501.png`)

## Import rules

- Sprite textures: `Sprite (2D and UI)`, `RGBA`, preserve alpha.
- Keep source PSD/AI outside runtime directories.
- Any third-party package must be logged in `ThirdParty/ASSET_LICENSES.md`.
