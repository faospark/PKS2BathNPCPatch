# Changelog - PKS2BathNPCPatch

All notable changes to PKS2BathNPCPatch will be documented in this file.

---

## [1.0.4] - 2026-11-09

### Fixed
- **Purged Tony (`104`) Duplicate Sprite ([NPCBathVisitorPatch.cs](Patches/NPCBathVisitorPatch.cs))**:
  - Identified via live ImGui Bathhouse Telemetry that Tony (`104`) has no authentic bath head sprite in Konami's atlas assets and erroneously rendered Taki's (`103`) bonnet head graphic inside the Men's Tub.
  - Purged Tony (`104`) from the active male castle visitor pool (`MaleCastleNPCs`), resolving the duplicate Taki glitch.

---

## [1.0.3] - 2026-11-09

### Fixed
- **Purged Marlowe (`120`) Placeholder Silhouette ([NPCBathVisitorPatch.cs](Patches/NPCBathVisitorPatch.cs))**:
  - Re-verified Sheet 24 (`hp_furo_24_atlas`): confirmed Marlowe (`120`) is also an unpainted violet/red placeholder silhouette identical to Gilbert (`121`) and Boris (`122`).
  - Purged Marlowe (`120`) from the active male visitor pool, bringing total excluded unfinished characters to 8 (Yam Koo, Hans, Jude, Maximillian, Marlowe, Gilbert, Boris, Gremio).

---

## [1.0.2] - 2026-11-08

### Fixed
- **Audited & Purged Incomplete / Defective Castle NPC Sprites ([NPCBathVisitorPatch.cs](Patches/NPCBathVisitorPatch.cs))**:
  - Forensically audited all bathhouse sprite texture atlases (`hp_furo_16` through `hp_furo_24`).
  - Purged 7 characters whose bath sprites were either left unfinished by Konami or lack required idle head frames at `v=0..24`:
    - **Gilbert (`121`)** & **Boris (`122`)**: Unpainted purple/slate silhouette shapes in Sheet 24 without facial features or hair.
    - **Gremio (`125`)**: Reused town NPC head fragment with pink placeholder scribbles in Sheet 24 (never drawn by Konami).
    - **Yam Koo (`100`)**, **Hans (`107`)**, **Jude (`115`)**, and **Maximillian (`119`)**: Missing idle head frames (`v=0..24` has 0 pixels or cut-off fragments).
  - Retained **Pilika (`123`)** as a special static bather (complete face at `v=0..24` with pink headband; blinking animation omitted).
- **Log Diagnostics Clarity ([NPCBathVisitorPatch.cs](Patches/NPCBathVisitorPatch.cs))**:
  - Changed log message from `bath slot {i}` to `party slot {i}` to avoid confusion with physical tub positions (0..5 Men vs 6..11 Women).
  - Added real-time tracking in `GetFuroPos_Postfix` to log exact tub positions (0..11) and sides (Men's tub vs Women's tub) for every bather.

---

## [1.0.1] - 2026-11-08

### Changed
- **Scoped Gender Routing Strictly to Castle NPCs (`chara_id >= 84`) ([NPCBathVisitorPatch.cs](Patches/NPCBathVisitorPatch.cs))**:
  - Refactored `GetSeibetu_Prefix` so all playable combat characters (IDs `0..83`) execute 100% vanilla native engine logic (`return true;`), leaving Viki (4), Tomo (17), Bob (40), Chaco (54), Hoi (71), Rulodia (75), and Vincent/Simone (64/65) completely untouched.
  - Intervenes only for Castle NPC bath visitors (`chara_id >= 84`) via `FemaleCastleNPCsSet`.
- **Protected All 4 Native Bathhouse Vignettes ([NPCBathVisitorPatch.cs](Patches/NPCBathVisitorPatch.cs))**:
  - Reverse-engineered `fc_event.cs` (`CheckCharaEvent00..04`) in `GameAssembly.dll` and updated `IsSpecialBathEventParty` with exact byte-level condition checks:
    1. Flik & Viktor in Marble Bath (`CheckCharaEvent00`: party count == 3, IDs 2 & 3).
    2. Flik, Viktor & Nina in Rock Bath (`CheckCharaEvent01`: party count == 4, IDs 2, 3 & 55).
    3. Heavyweight Muscle Men in Regular Bath (`CheckCharaEvent03`: party count == 4, Long Chan-Chan [69], Bolgan [21], Gantetsu [70]).
    4. Flik & Viktor in Cypress Bath (`CheckCharaEvent04`: party count == 3, IDs 2 & 3).
  - Preserves exact vanilla party counts for all vignette parties, ensuring native cutscenes trigger without interruption from visitor injection.

### Fixed
- **FuroEnd IndexOutOfRangeException Guard (`G2_SYS.G2_hoka`) ([NPCBathVisitorPatch.cs](Patches/NPCBathVisitorPatch.cs))**:
  - Guarded `G2_SYS.G2_hoka(mode, pcno)` to safely return `0` when `pcno < 0 || pcno >= 6`.
  - Vanilla party buffers only allocate 6 combat party member slots. When 8-bather capacity is enabled, `FuroEnd` iterates across all 8 bather slots upon bath completion; querying slots 6 and 7 previously threw repeated `System.IndexOutOfRangeException` errors in the engine logs.
- **Female Character Classification**:
  - Added Viki (`ID 4`) and Lo Wen (`ID 60`) to female character identification, fixing vanilla gender data bugs where male characters like Bob (`ID 40`), Chaco (`ID 54`), Kiba (`ID 87`), and Alex (`ID 95`) were misclassified.

---

## [1.0.0] - 2026-10-27

### Added
- **Castle NPC Bathhouse Visitors ([NPCBathVisitorPatch.cs](Patches/NPCBathVisitorPatch.cs))**:
  - Allows recruited non-combatant Castle NPCs (IDs $\ge 83$, e.g. Leona, Richmond, Barbara, Hai Yo, Apple, Shu, Jeane, Raura, etc.) to visit and soak in the Toran/North Window Castle bathhouse alongside combat party members.
  - Dynamically injects available castle NPCs into vacant bath slots across both men's and women's tubs based on castle census and recruitment flags.
- **Fatal Null-Dereference Crash Fix (`CreateFuroChara` / `0x182016059`)**:
  - Intercepts `EventOverlayClass.f_main$$CreateFuroChara` to safely handle non-combatant NPCs who lack standard `P_GRAPH_DATA` (`field_data` and `battle_data`), preventing native access violation crashes (`0xc0000005`).
- **Gender Segregation Routing**:
  - Implemented gender detection to automatically route male NPCs to the men's tub (slots 0..5) and female NPCs to the women's tub (slots 6..11).
