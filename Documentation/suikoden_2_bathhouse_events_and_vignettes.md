# Suikoden II Bathhouse Events, Vignettes & Interactions Reference

This document provides a complete technical and gameplay reference for all native bathhouse events, special character cutscenes, decorative item effects, and interactions in **Suikoden II** (`GSD2`), reverse-engineered directly from `GameAssembly.dll` and `EventOverlayClass` decompiled sources.

---

## 1. Character Cutscene Vignettes (`CheckCharaEvent00 .. 04`)

Native execution of bathhouse character vignettes is orchestrated by `EventOverlayClass.f_phase$$FuroPhaseCheckCharaEvent` (`0x18201D760`).

### Critical Gameplay Trigger Mechanic (How Vignettes Trigger):
> [!IMPORTANT]
> In vanilla Suikoden II, **bath vignettes do NOT play automatically upon entering the bathhouse or while idling in the tub**.
>
> 1. When you enter the bathhouse with Tetsu, your characters take their positions and soak quietly while the bath menu is displayed:
>    * `Place an item`
>    * `Take an item`
>    * `Remodel Bath`
>    * **`Leave Bath`**
> 2. The special character vignettes are evaluated **only when you choose `Leave Bath`** (`FuroPhaseExitFuro00` -> `FuroPhaseCheckCharaEvent`).
> 3. If conditions match (correct bath style, exact party members, and exact party count), the engine **intercepts the exit transition**, suppresses the screen fade-out, shifts the camera, and plays the full dialogue and animation cutscene (`CE00..04_Func00..02`).
> 4. Once Flik & Viktor (or the corresponding group) finish their dialogue, the bathhouse session ends and the party exits to Tetsu's anteroom.

The engine iterates through an internal delegate table and invokes each condition check in sequence:

| Event ID | Event Name | Native VA | Bath Style | Required Party (Combat Party) | Exact Native Conditions Checked | Scene Synopsis |
| :---: | :--- | :---: | :---: | :--- | :--- | :--- |
| **`CheckCharaEvent00`** | **The Bickering Comrades** | `0x182000BF0` | **Marble Bath**<br>*(Type 1)* | **Riou + 2 companions**:<br>• Flik (`2`)<br>• Viktor (`3`) | • `G2_pamem_num(0) == 3`<br>• Bath Style == `1`<br>• Party contains Flik (`2`) & Viktor (`3`) | Flik complains about the water temperature while Viktor teases him about being soft since the Toran Liberation War. |
| **`CheckCharaEvent01`** | **Nina Pursuing Flik** | `0x182000D40` | **Rock Bath**<br>*(Type 2)* | **Riou + 3 companions**:<br>• Flik (`2`)<br>• Viktor (`3`)<br>• Nina (`55`) | • `G2_pamem_num(0) == 4`<br>• Bath Style == `2`<br>• Party contains Flik (`2`), Viktor (`3`), Nina (`55`) | Nina attempts to scale or peek across the rock partition to see Flik. Viktor laughs uncontrollably while Flik panics and scrambles for cover. |
| **`CheckCharaEvent02`** | **The Octopus Urn Animation** | `0x182000ED0` | **Any Bath** | **Riou + Party**<br>*(With Abizboah / Rulodia)* | • `CheckFuroItemNum(0x0C, 0x40) == 6`<br>• 6 Octopus Urns displayed in bath | When 6 Octopus Urns are placed around the bathhouse deck, recruited kraken companions perform unique swimming and tentacle-flailing animations. |
| **`CheckCharaEvent03`** | **The Heavyweight Muscle Men** | `0x182001030` | **Regular / Wood Bath**<br>*(Type 0)* | **Riou + 3 companions**:<br>• Long Chan-Chan (`69`)<br>• Bolgan (`21`)<br>• Gantetsu (`70`) | • `G2_pamem_num(0) == 4`<br>• Bath Style == `0`<br>• Party contains L.C. Chan (`69`), Bolgan (`21`), Gantetsu (`70`) | The three brawniest martial artists crowd into the regular wooden tub, flexing, competing over hot water endurance, and practically boiling Riou. |
| **`CheckCharaEvent04`** | **The Hot Springs Standoff** | `0x1820011C0` | **Jungle / Cypress Bath**<br>*(Type 3)* | **Riou + 2 companions**:<br>• Flik (`2`)<br>• Viktor (`3`) | • `G2_pamem_num(0) == 3`<br>• Bath Style == `3`<br>• Party contains Flik (`2`) & Viktor (`3`) | Viktor and Flik enjoy the aromatic cypress and jungle greenery, reminiscing about past battles and teasing Riou about his leadership responsibilities. |

---

## 2. Mod Protection for Native Vignettes (`IsSpecialBathEventParty`)

In the vanilla engine, `CheckCharaEvent00`, `01`, `03`, and `04` strictly assert exact combat party sizes:

```x86asm
; CheckCharaEvent00 / 04:
call    G2_SYS$$G2_pamem_num    ; returns eax = party size
cmp     eax, 3                  ; must equal 3!
jne     EventFailed

; CheckCharaEvent01 / 03:
call    G2_SYS$$G2_pamem_num    ; returns eax = party size
cmp     eax, 4                  ; must equal 4!
jne     EventFailed
```

### The Conflict:
Mods that inject Castle NPCs or expand bath capacity to 8 bathers alter `G2_pamem_num(0)` to return `6` or `8`. When this happens:
1. `cmp eax, 3` and `cmp eax, 4` fail immediately.
2. The vanilla engine assumes the condition was not met.
3. The cutscene is completely skipped.

### The Solution (`PKS2BathNPCPatch`):
`PKS2BathNPCPatch` implements `IsSpecialBathEventParty` to inspect incoming parties before any NPC injection occurs:
* If the party matches any of the exact vignette compositions (Flik & Viktor, Nina & Flik & Viktor, or the Muscle Men trio), the patch flags `_isSpecialVignetteParty = true`.
* Visitor NPC injection is bypassed for that session.
* `G2_pamem_num` returns the pure vanilla party count (3 or 4).
* The native cutscenes trigger with 100% fidelity.

---

## 3. Peeping & Partition Interaction (`FuroPhaseLookEvent`)

Located at `EventOverlayClass.f_phase$$FuroPhaseLookEvent` (`0x18201F180`), this event handles Riou interacting with the wooden dividing wall.

* **Interaction Logic**:
  1. The engine checks if any character in the active bath is located in **Tub Slots 6 .. 11** (the female side of the bath).
  2. If women are present, Riou receives unique startled reactions and reprimands depending on who is bathing:
     - **Nanami (`18`)**: Immediately scolds and gives Riou an earful.
     - **Nina (`55`)**: Screams in surprise and panic.
     - **Karen (`14`) / Eilie (`19`) / Rina (`20`)**: Blushing, flustered reactions.
     - **Oulan (`46`) / Wakaba (`68`) / Hanna (`23`)**: Aggressive, martial combat threats and splashing water back over the partition.
  3. If no female characters are present behind the partition, Riou simply looks over and observes empty water.

---

## 4. Bathhouse Antique & Decoration Events (`CheckFuroEvent00 .. 06`)

`EventOverlayClass.ff_event$$CheckFuroEvent00..06` checks for sets of decorative antiques placed by Tetsu or Riou in the bathhouse deck slots:

| Routine | Native VA | Required Decoration Displayed | Item IDs / Quantity | In-Game Effect |
| :---: | :---: | :--- | :--- | :--- |
| **`CheckFuroEvent00`** | `0x1820023C0` | **Hexagonal Vases / Urns** | Item `0x0A` (Qty: 6) | Water chime audio trigger and soft sparkling ambient ripples. |
| **`CheckFuroEvent01`** | `0x182002410` | **Celadon Urns** | Item `0x02` (Qty: 6) | Serene mist particles and gentle water lapping audio. |
| **`CheckFuroEvent02`** | `0x182002520` | **Wall Scrolls & Graffiti** | Items `0x0F` & `0x10` | Unique inspection dialogue commenting on the wall artwork. |
| **`CheckFuroEvent03`** | `0x182002590` | **Wooden Ducks & Bath Toys** | Item `0x03` (Qty: 6) + Item `0x13` (Qty: 2) | Yellow wooden toy ducks bob and float across the water surface around the bathers. |
| **`CheckFuroEvent04`** | `0x182002600` | **Flower / Bonsai Arrangements** | Plant decorations | Floating floral petals drift on top of the bathwater. |
| **`CheckFuroEvent05`** | `0x182002690` | **Dragon Vases & Incense Burners** | Item `0x05` (Qty: 6) + Item `0x15` (Qty: 2) | Decorative dragon steam plumes billow upward from the surface. |
| **`CheckFuroEvent06`** | `0x182002700` | **Octopus Urns** | Item `0x09` / `0x0C` (Qty: 6) | Activates dynamic tentacle animations and splashes when octopus companions are recruited. |

---

## 5. Unique Character In-Bath Sprites & Behaviors

Certain characters possess dedicated sprite sheets in the `hp_furo_*_atlas` assets that deviate from standard bather animations:

1. **Gadget (`ID 26`)**:
   - Rather than sitting directly in the communal bath, Meg's barrel robot sits upright inside his own personal mini wooden wash bucket placed inside the men's tub.
2. **Shiro (`ID 48`)**:
   - Kinnison's faithful wolf does not submerge in the water. Instead, he sits peacefully on the dry wooden deck above the tub, watching over Riou.
3. **Mukumuku & Squirrel Companions (`IDs 76, 83`)**:
   - Float on top of the water using playful splash frames and head-bobbing animations.
4. **Abizboah (`ID 74`), Rulodia (`ID 75`), Chuchara (`ID 76`)**:
   - Huge multi-slot kraken bathers that take up 2 full character slots each. They submerge with massive tentacle ripples and water bubbles.
