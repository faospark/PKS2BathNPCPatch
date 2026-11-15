# P.K. - Bath NPC Patch

**By [faospark](https://github.com/faospark)**

A **BepInEx 6 (IL2CPP)** mod for **Suikoden I & II HD Remaster** that restores the cut 1998 PlayStation feature allowing recruited castle companions and non-combatant NPCs to bathe alongside your party in Dunan Castle's Bathhouse.

## Features

- **Random Castle Companion Bathing**: Automatically fills empty bathhouse slots with recruited castle residents (32 supported companions across both tubs, excluding 8 NPCs with unfinished head graphics).
- **8-Bather Capacity**: Unlocks the bathhouse's full 8-character capacity (`EnableEightBatherCapacity`), letting 2 castle companions join even a full 6-character combat party.
- **Canonical Gender Separation**: Fixes vanilla gender-flag bugs (e.g., Bob, Chaco, Kiba, Alex) and routes male/female companions to their proper tubs while preserving Vincent & Simone's unisex behavior.
- **Vignette & Story Protection**: Automatically steps aside during special bathhouse vignettes (Flik & Viktor, Knights of Matilda) and story-gates Pilika so she stops appearing after leaving for Highland.
- **Crash & Bounds Fixes**: Prevents native crashes when loading non-combatant bath sprites (`hp_furo`).

> Looking for the reverse-engineering notes, Harmony hooks, and full NPC ID roster? See [TECHNICAL.md](TECHNICAL.md).

## Configuration

Edit `BepInEx/config/faospark.pks2bathnpcpatch.cfg`:

- **`EnableNPCBathVisitors`** (default: `true`) — Enables recruited castle companions visiting the bathhouse.
- **`EnableEightBatherCapacity`** (default: `true`) — Unlocks 8-bather capacity (+2 castle companions with a full 6-person party).
- **`DetailedLogs`** (default: `false`) — Enables verbose bathhouse diagnostics in `LogOutput.log`.

## Requirements

- **BepInEx 6.0.0-pre.2 IL2CPP**
- **Suikoden I & II HD Remaster**
- **Recommended:** [Suikoden Fix](https://github.com/d3xMachina/SuikodenFix) by d3xMachina

## Installation

1. Install [BepInEx 6.0 (Unity IL2CPP x64)](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip) into your game root directory.
2. Place `PKS2BathNPCPatch.dll` into `BepInEx/plugins/`.
3. Launch the game once to generate `BepInEx/config/faospark.pks2bathnpcpatch.cfg`.

## Credits & License

- **[faospark](https://github.com/faospark)** — Primary Author & Lead Researcher
- **[d3xMachina](https://github.com/d3xMachina/Suikoden-Fix)** — Creator of Suikoden-Fix & BepInEx IL2CPP foundation
- **[asilverthorn](https://github.com/asilverthorn/suikoden_ref)** — Reference tables and internal IDs
- **Mod Suite:** [Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)
- **Framework:** [BepInEx](https://github.com/BepInEx/BepInEx) & [HarmonyX](https://github.com/BepInEx/HarmonyX)

Distributed under the [MIT License](LICENSE).
