# PKS2BathNPCPatch (Suikoden II NPC Bathhouse Visitors - Cut Content Restoration)

A **BepInEx 6 (IL2CPP)** mod for **Suikoden I & II HD Remaster** (Steam) that restores the cut 1998 PlayStation feature allowing recruited castle companions and NPCs to take a bath with you. It automatically populates unoccupied tub positions with random recruited castle companions, unlocks full 8-character bathhouse capacity, eliminates native engine null-dereference crashes, and enforces canonical gender separation.

---

## 📖 Whitepaper: Why This Mod Exists

### 1. The Cut 1998 Feature & Accessible Bath Sprites
In *Suikoden II*, recruiting **Tetsu** into the Dunan Republic Castle HQ unlocks the Castle Bathhouse. During the development of the original 1998 PlayStation release, Konami's artists designed and drew complete bathhouse soaking sprites for non-combatant castle recruits and story companions (such as **Apple**, **Shu**, **Leona**, **Richmond**, **Barbara**, **Pilika**, **Dr. Huan**, and **Teresa**). 

These sprites were included in the game data, proving that Konami originally planned for castle companions to visit and take a bath alongside Riou. However, this feature was cut before release: the bathhouse was restricted strictly to the 6 members of your active combat party, leaving non-combatant bath sprites unused and unreachable in normal gameplay.

When Konami developed the *Suikoden I & II HD Remaster*, these assets were preserved and remain accessible in the game files inside the bathhouse sprite bundles (`hp_furo`). **PKS2BathNPCPatch** brings this cut concept to life by allowing recruited castle companions to randomly join you and bathe in unoccupied tub spots.

---

### 2. The Native Engine Obstacles in Vanilla HD Remaster

Simply putting non-combatant castle companions (IDs $\ge 83$) into the bathhouse under the unmodded game engine triggers fatal bugs and engine crashes:

#### A. Fatal Null-Dereference Crash (`0x182016059` in `CreateFuroChara`)
When entering the bathhouse, `EventOverlayClass.f_main$$CreateFuroChara` queries character graphics metadata by calling `G2_SYS.G2_gra_dat(pcno)`.
- For combatants (IDs `0 .. 82`), `G2_gra_dat` returns a fully initialized `P_GRAPH_DATA` object containing `field_data` (`PLAYER_FIELD`) and `battle_data` (`PLAYER_BATTLE`).
- For non-combatant NPCs (IDs $\ge 83$), `G2_gra_dat` returns either `null` or an object where `field_data` and `battle_data` are null.
- `CreateFuroChara` immediately attempts to read animation offsets from `battle_data` without null-checking, triggering an instant native memory access violation crash (`c0000005` at address `0x182016059`), crashing the game to desktop.

#### B. Variable Data Out-of-Bounds Crash (`G2_SYS.G2_varia_dat`)
During bathhouse initialization, the engine calls `EventOverlayClass.f_main.CheckfuroItemEquip` to verify if bath toys or items are equipped. It calls `G2_SYS.G2_varia_dat(chano)`. In Suikoden 2, the `c_varia_dat` array is strictly hardcoded to a length of **83 elements** (`0 .. 82`). Calling it with an NPC ID $\ge 83$ causes an immediate `IndexOutOfRangeException`.

#### C. Legacy Gender Flag Bugs in `ckd_info` (`GetSeibetu`)
Vanilla Suikoden II routes bathers to separate tubs via `EventOverlayClass.f_main.GetFuroPos(fw, chara_id)`:
- **Positions `0 .. 5`**: Men's Tub (Right side, positive X).
- **Positions `6 .. 11`**: Women's Tub (Left side, negative X, behind the wooden partition).
- **Position Assignment**: Decided by `f_main.GetSeibetu(chara_id)` (0 = Male, 1 = Female, 2 = Unisex).

In Konami's static dataset (`ckd_info`), several male characters (such as **Bob** ID 4, **Chaco** ID 17, **Kiba** ID 87, and **Alex** ID 95) were errantly flagged as female, causing them to leak into the women's tub. Conversely, because non-combatants were never hooked up to bathe, all non-combatant NPCs defaulted to `0` (Male), routing female companions like Apple, Leona, Barbara, and Teresa into the men's tub.

#### D. The 6-Bather Capacity Bottleneck
The bathhouse engine internally allocates an 8-element array for bathers in `FURO_WORK` and features 12 physical tub coordinates (6 on each side). However, `G2_SYS.G2_pamem_num(0)` strictly returned the combat party count (clamped to 6). As a result, slots 6 and 7 were always ignored, leaving the bath half-empty even with a full party.

#### E. Narrative Inconsistency (Pilika Leaving the Castle)
In the story, Pilika lives in Castle HQ from its founding until the **Muse Peace Conference**. During that conference, Pilika spots Jowy and runs to him; Jowy takes her back to the Highland Royal Palace (L'Renouille), where she stays with Jillia Blight for the remainder of the war. A static recruit pool would allow Pilika to magically appear in the Castle HQ bathhouse long after her departure.

---

## 🛠️ How PKS2BathNPCPatch Solves It

**PKS2BathNPCPatch** delivers an authentic, bug-free castle bathhouse experience through native in-memory Harmony patches:

1. **Random Castle Companion Bathing**:
   - Automatically queries your recruited castle companions. When you enter the bathhouse with empty slots, random recruited castle NPCs spawn into the water to soak alongside you.
2. **Synthetic `P_GRAPH_DATA` Dummy Provider**:
   - Intercepts `G2_SYS.G2_gra_dat` during bath initialization. Supplies valid dummy `P_GRAPH_DATA` instances equipped with valid `field_data` and `battle_data`, completely neutralizing crash `0x182016059`.
3. **Bounds-Safe Variable Interceptor**:
   - Intercepts `G2_SYS.G2_varia_dat` and `CheckfuroItemEquip` for IDs $\ge 83$, gracefully treating NPCs as having no equippable inventory without indexing out-of-bounds arrays.
4. **Canonical Gender Partitioning**:
   - Replaces `f_main.GetSeibetu` with ground-truth mapping. Males are routed to slots `0..5`, females to slots `6..11`, and vanilla Case 2 unisex logic for **Vincent** (64) and **Simone** (65) is preserved.
5. **Full 8-Character Bathhouse Capacity (+2 Castle Companions)**:
   - Patches `G2_SYS.G2_pamem_num` inside the bathhouse to count all 8 slots. Even with a full 6-character combat party, **2 recruited castle companions** join in to bathe together! If the player enters with fewer combatants, all remaining slots are filled with castle recruits.
6. **Dynamic Tub Balancing**:
   - Segregates recruited candidates by gender and balances attendance across both tubs so neither the men's nor women's tub sits desolate.
7. **Vignette Protection**:
   - Detects special vanilla bath cutscenes (such as the Flik & Viktor argument requiring strictly 3 bathers, or the Knights of Matilda vignette requiring strictly 4 bathers). When a vignette composition enters, visitor injection is automatically bypassed, preserving pure vanilla execution.
8. **Narrative Story Progression Gate (`IsPilikaInCastle`)**:
   - Intercepts **Story Flag 14** (`event_flag[14] & 0x40/0x80`) and subsequent story chapters (Tinto zombie invasion `0x9A`, Banner Pass `0x90`, Rockaxe `0x77`, Muse liberation `0x78`, L'Renouille assault `0xCA`). Pilika appears in the bathhouse during the early war, but permanently ceases to appear the moment she leaves for Highland.

---

## 👥 Canonical Character Roster (IDs 85–123)

### **Female Pool (`f`)** — Women's Tub (Positions 6..11)
| ID | Character | Role & Availability |
| :---: | :--- | :--- |
| **85** | **Apple** | Strategist (Permanent Castle Resident) |
| **91** | **Leona** | Tavern Keeper (Permanent Castle Resident) |
| **94** | **Hilda** | Innkeeper (Available once recruited) |
| **96** | **Emilia** | Castle Archivist / Library (Available once recruited) |
| **98** | **Barbara** | Storage Vault Keeper (Permanent Castle Resident) |
| **101** | **Teresa** | Greenhill Mayor (Available once joined) |
| **102** | **Yuzu** | Castle Shepherd / Farm (Available once recruited) |
| **103** | **Taki** | Castle Elder / Cook (Available once recruited) |
| **111** | **Raura** | Scroll Maker / Rune Scribe (Available once recruited) |
| **112** | **Annallee** | Castle Musician (Available once recruited) |
| **116** | **Jeane** | Castle Runemaster (Available once recruited) |
| **123** | **Pilika** | Companion (Available from HQ founding until Muse Peace Conference) |

### **Male Pool (`m`)** — Men's Tub (Positions 0..5)
| ID | Character | Role & Availability |
| :---: | :--- | :--- |
| **86** | **Templeton** | Mapmaker (Available once recruited) |
| **87** | **Kiba** | General (Available once recruited) |
| **88** | **Fitcher** | Muse Diplomat (Available once joined) |
| **89** | **Shu** | Chief Strategist (Permanent Castle Resident) |
| **90** | **Tetsu** | Bathhouse Master (Available once recruited) |
| **92** | **Dr. Huan** | Castle Physician (Permanent Castle Resident) |
| **93** | **Jess** | General (Available once joined) |
| **95** | **Alex** | Item Shopkeeper (Available once recruited) |
| **97** | **Tenkou** | Window Craftsman (Available once recruited) |
| **99** | **Richmond** | Private Investigator (Permanent Castle Resident) |
| **100** | **Yam Koo** | Fisherman (Available once recruited) |
| **104** | **Tony** | Castle Farmer (Available once recruited) |
| **105** | **Adlai** | Elevator Inventor (Available once recruited) |
| **106** | **Gordon** | Castle Appraiser (Available once recruited) |
| **107** | **Hans** | Castle Armorer (Available once recruited) |
| **108** | **Connell** | Castle Sound Engineer (Available once recruited) |
| **109** | **Lebrante** | Castle Appraiser (Available once recruited) |
| **110** | **Tessai** | Castle Blacksmith (Available once recruited) |
| **113** | **Pico** | Castle Bard (Available once recruited) |
| **114** | **Alberto** | Castle Violinist (Available once recruited) |
| **115** | **Jude** | Castle Sculptor (Available once recruited) |
| **117** | **Ridley** | Kobold General (Available once joined) |
| **118** | **Klaus** | Strategist (Available once recruited) |
| **119** | **Maximillian** | Knight (Available once recruited) |
| **120** | **Marlowe** | Greenhill Scholar (Available once joined) |
| **121** | **Gilbert** | Mercenary Captain (Available once recruited) |
| **122** | **Boris** | Ridley's Son (Available if joined) |

---

## 💻 Requirements

1. **Suikoden I & II HD Remaster** (Steam edition, 64-bit Windows)
2. **BepInEx 6.0 (Unity IL2CPP x64)**
   - Download **[BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip)**.

---

## 📦 Installation Guide (Steam)

### 1. Locate Game Root Directory
In your Steam client:
1. Open your **Library** and right-click **Suikoden I & II HD Remaster**.
2. Select **Manage** → **Browse local files**.
3. A file explorer window will open at your game's root directory:
   ```text
   ...\steamapps\common\Suikoden I and II HD Remaster\
   ```

### 2. Install BepInEx 6 (If not already installed)
1. Download **[BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip)**.
2. Extract the contents directly into your **`Suikoden I and II HD Remaster`** root folder.
3. Launch the game once via Steam and exit to desktop to generate IL2CPP interop folders.

### 3. Install PKS2BathNPCPatch
1. Place **`PKS2BathNPCPatch.dll`** into your game's BepInEx plugins folder:
   ```text
   Suikoden I and II HD Remaster/
   ├── BepInEx/
   │   ├── config/
   │   │   └── faospark.pks2bathnpcpatch.cfg (auto-generated on first launch)
   │   └── plugins/
   │       └── PKS2BathNPCPatch.dll
   ```

### 4. Verify Installation
Start the game through Steam and check `BepInEx/LogOutput.log`:
```text
[Info   :   BepInEx] Loading [PKS2 Bath NPC Patch 1.0.0]
[Info   :PKS2 Bath NPC Patch] Loading PKS2BathNPCPatch (Suikoden II NPC Bathhouse Sprites & Castle Visitors Restoration)...
[Info   :PKS2 Bath NPC Patch] PKS2BathNPCPatch loaded successfully!
```

---

## ⚙️ Configuration Reference

Edit `BepInEx/config/faospark.pks2bathnpcpatch.cfg` with Notepad or any text editor:

```ini
[Suikoden 2 NPC Bath Visitors]

## Restores the cut 1998 Suikoden II feature allowing recruited castle companions to visit and take a bath with you, automatically filling unoccupied bath slots.
# Setting type: Boolean
# Default value: true
EnableNPCBathVisitors = true

## Unlocks the bathhouse internal 8-character capacity (up to 8 bathers across 12 tub positions), allowing 2 castle companions to join a full 6-man battle party.
# Setting type: Boolean
# Default value: true
EnableEightBatherCapacity = true

[Diagnostics]

## Enables verbose diagnostics in LogOutput.log for character tub assignments, slot placement, and party restoration.
# Setting type: Boolean
# Default value: false
DetailedLogs = false
```

---

## 📜 Credits & Acknowledgements

- **faospark**: Reverse engineering, native IL2CPP crash prevention, and architecture implementation.
- **d3xMachina**: Creator of `Suikoden Fix`.
- **Konami**: Original artists and creators of the Suikoden series and bathhouse sprite assets.
- **Moogles & Mods Community**: Ongoing research and support.
