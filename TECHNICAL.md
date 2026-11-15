# PKS2BathNPCPatch — Technical Reference

Technical whitepaper and implementation notes for [NPCBathVisitorPatch.cs](Patches/NPCBathVisitorPatch.cs).

---

## 1. Background: Cut 1998 Feature & `hp_furo` Assets

In *Suikoden II*, recruiting **Tetsu** unlocks the Dunan Castle Bathhouse. Konami's artists created bathhouse soaking sprites (`hp_furo`) for non-combatant castle recruits and story companions (Apple, Shu, Leona, Richmond, Barbara, Pilika, Dr. Huan, Teresa, etc.), proving they originally intended castle residents to bathe alongside Riou. The feature was cut before release, restricting the bathhouse strictly to the active combat party (max 6). Both PS1 and HD Remaster preserved these sprites in `hp_furo`.

---

## 2. Native Engine Obstacles in Vanilla HD Remaster

1. **Fatal Null-Dereference Crash (`0x182016059` in `CreateFuroChara`)**
   - `EventOverlayClass.f_main.CreateFuroChara` queries `G2_SYS.G2_gra_dat(pcno)`.
   - Combatants (`0..82`) return a `P_GRAPH_DATA` object with valid `field_data` (`PLAYER_FIELD`) and `battle_data` (`PLAYER_BATTLE`).
   - Non-combatant NPCs (`>= 83`) return `null` or an object with null `field_data`/`battle_data`, causing an access violation (`c0000005`) when `CreateFuroChara` reads animation offsets.
2. **Variable Data Out-of-Bounds (`G2_SYS.G2_varia_dat` & `G2_hoka`)**
   - `CheckfuroItemEquip` calls `G2_SYS.G2_varia_dat(chano)`. In Suikoden II, `c_varia_dat` has a fixed length of **83 elements** (`0..82`), so any ID `>= 83` throws `IndexOutOfRangeException`.
   - `G2_SYS.G2_hoka` likewise requires bounds guarding for injected non-combatants.
3. **Legacy Gender Flag Bugs (`GetSeibetu` / `ckd_info`)**
   - `f_main.GetFuroPos(fw, chara_id)` routes bathers to Positions `0..5` (Men's Tub, right) or `6..11` (Women's Tub, left) via `f_main.GetSeibetu(chara_id)` (`0` = Male, `1` = Female, `2` = Unisex).
   - In `ckd_info`, several male characters (**Bob** 4, **Chaco** 17, **Kiba** 87, **Alex** 95) were misflagged as female, and non-combatant NPCs defaulted to `0` (Male).
4. **6-Bather Capacity Bottleneck**
   - `FURO_WORK` allocates 8 bather slots across 12 tub coordinates, but `G2_SYS.G2_pamem_num(0)` clamps to the active combat party (max 6), leaving slots 6 and 7 unused.
5. **Narrative Consistency (Pilika)**
   - Pilika leaves Castle HQ during the **Muse Peace Conference** and stays in Highland (L'Renouille) for the rest of the war.

---

## 3. Harmony Hooks (`NPCBathVisitorPatch.cs`)

- **`f_main.FuroInit` (Prefix) / `f_main.FuroEnd` (Postfix):** Snapshots and restores party/bather state cleanly around each bathhouse session.
- **`G2_SYS.G2_pamem_num` (Prefix):** Expands bathhouse count up to 8 slots when `EnableEightBatherCapacity` is enabled.
- **`G2_SYS.G2_gra_dat` (Prefix & Postfix):** Supplies synthetic `P_GRAPH_DATA` (with valid `field_data` and `battle_data`) for injected non-combatants (`>= 83`), neutralizing crash `0x182016059`.
- **`G2_SYS.G2_varia_dat` (Prefix) & `f_main.CheckfuroItemEquip` (Prefix) & `G2_SYS.G2_hoka` (Prefix):** Bounds-checks NPC IDs `>= 83` so they safely report no equippable bath items without out-of-bounds reads.
- **`f_main.GetSeibetu` (Prefix) & `f_main.GetFuroPos` (Postfix):** Enforces canonical gender partitioning (`0..5` Male, `6..11` Female) while preserving Case 2 unisex handling for **Vincent** (64) and **Simone** (65) and balancing tub attendance.
- **`f_phase.FuroPhaseCheckCharaEvent` (Prefix), `fc_event.CheckCharaEvent04` (Postfix), `fc_event.CheckCharaEvent00` (Postfix):** Protects special bath vignettes (e.g., Flik & Viktor, Knights of Matilda) and handles companion event checks cleanly.
- **`IsPilikaInCastle()`:** Gates Pilika (ID `123`) using Story Flag 14 (`event_flag[14] & 0x40/0x80`) and later war progression flags (`0x9A`, `0x90`, `0x77`, `0x78`, `0xCA`).

---

## 4. Canonical Character Roster (IDs 85–123)

### Female Pool — Women's Tub (Positions `6..11`)
| ID | Character | Availability |
| :---: | :--- | :--- |
| **85** | **Apple** | Permanent Castle Resident |
| **91** | **Leona** | Permanent Castle Resident |
| **94** | **Hilda** | Once recruited |
| **96** | **Emilia** | Once recruited |
| **98** | **Barbara** | Permanent Castle Resident |
| **101** | **Teresa** | Once joined |
| **102** | **Yuzu** | Once recruited |
| **103** | **Taki** | Once recruited |
| **111** | **Raura** | Once recruited |
| **112** | **Annallee** | Once recruited |
| **116** | **Jeane** | Once recruited |
| **123** | **Pilika** | HQ founding until Muse Peace Conference |

### Male Pool — Men's Tub (Positions `0..5`)
| ID | Character | Availability |
| :---: | :--- | :--- |
| **86** | **Templeton** | Once recruited |
| **87** | **Kiba** | Once recruited |
| **88** | **Fitcher** | Once joined |
| **89** | **Shu** | Permanent Castle Resident |
| **90** | **Tetsu** | Once recruited |
| **92** | **Dr. Huan** | Permanent Castle Resident |
| **93** | **Jess** | Once joined |
| **95** | **Alex** | Once recruited |
| **97** | **Tenkou** | Once recruited |
| **99** | **Richmond** | Permanent Castle Resident |
| **104** | **Tony** | Once recruited |
| **105** | **Adlai** | Once recruited |
| **106** | **Gordon** | Once recruited |
| **108** | **Connell** | Once recruited |
| **109** | **Lebrante** | Once recruited |
| **110** | **Tessai** | Once recruited |
| **113** | **Pico** | Once recruited |
| **114** | **Alberto** | Once recruited |
| **117** | **Ridley** | Once joined |
| **118** | **Klaus** | Once recruited |

> **Excluded Characters:** Yam Koo (`100`), Hans (`107`), Jude (`115`), Maximillian (`119`), Marlowe (`120`), Gilbert (`121`), Boris (`122`), and Gremio (`125`) are excluded due to missing/unfinished head graphics in the game files. Pilika (`123`) is included with eye blinking disabled due to incomplete lower frames.
