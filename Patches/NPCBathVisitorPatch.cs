extern alias GSD2;

using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace PKS2BathNPCPatch.Patches;

/// <summary>
/// Restores the cut 1998 Suikoden II feature allowing random recruited castle companions to take a bath with you,
/// utilizing the accessible bath sprites and preventing the native engine crash when bathing with non-combatants in Suikoden 2.
/// Automatically fills unoccupied bath slots with recruited castle companions (e.g. Apple, Shu, Leona, Richmond, Barbara, Pilika, Teresa, etc.).
/// </summary>
public static class NPCBathVisitorPatch
{
    private static bool _initialized = false;
    public static bool IsInsideBath { get; private set; } = false;
    public static GSD2::EventOverlayClass.furo_h.FURO_WORK CurrentFuroWork { get; private set; }
    public static readonly Dictionary<int, int> CharacterToFuroSlot = new Dictionary<int, int>();
    public static readonly Dictionary<int, int> FuroSlotToCharacter = new Dictionary<int, int>();
    public static readonly HashSet<byte> ActiveBathVisitors = new HashSet<byte>();
    public static readonly List<byte> ActivePartyInBath = new List<byte>();

    private static byte[] _savedPartyMembers = null;
    private static bool _isPartyModified = false;
    private static bool _isSpecialVignetteParty = false;

    private static GSD2::P_GRAPH_DATA _dummyGraphData = null;

    // Castle recruit, convoy, and facility female NPCs with authentic bath sprites in hp_furo_XX_atlas
    public static readonly HashSet<byte> FemaleCastleNPCsSet = new HashSet<byte>
    {
        84,  // Apple (npc_84)
        85,  // Apple
        91,  // Leona
        94,  // Hilda
        96,  // Emilia
        98,  // Barbara
        101, // Teresa
        102, // Yuzu
        103, // Taki
        111, // Raura
        112, // Annallee
        116, // Jeane
        123, // Pilika
        172  // Elza (player_eri_d)
    };

    // Backward-compatibility alias
    public static HashSet<byte> FemaleCharaIds => FemaleCastleNPCsSet;

    /// <summary>
    /// Checks whether a character ID is female.
    /// Defers directly to vanilla native GetSeibetu for playable characters (0..83),
    /// and uses FemaleCastleNPCsSet for castle NPCs (>= 84).
    /// </summary>
    public static bool IsFemale(byte charaId)
    {
        try
        {
            return GSD2::EventOverlayClass.f_main.GetSeibetu(charaId) == 1;
        }
        catch
        {
            return FemaleCastleNPCsSet.Contains(charaId);
        }
    }

    // Recruited castle female NPCs with authentic bath sprites in hp_furo_XX_atlas
    public static readonly byte[] FemaleCastleNPCs = new byte[]
    {
        85,  // Apple
        91,  // Leona
        94,  // Hilda
        96,  // Emilia
        98,  // Barbara
        101, // Teresa
        102, // Yuzu
        103, // Taki
        111, // Raura
        112, // Annallee
        116, // Jeane
        123  // Pilika
    };

    // Recruited castle male NPCs with verified, authentic bath sprites in hp_furo_XX_atlas
    // Excluded due to missing/unfinished head graphics or placeholder silhouettes:
    // Tony (104), Yam Koo (100), Hans (107), Jude (115), Maximillian (119), Marlowe (120), Gilbert (121), Boris (122), Gremio (125).
    public static readonly byte[] MaleCastleNPCs = new byte[]
    {
        86,  // Templeton
        87,  // Kiba
        88,  // Fitcher
        89,  // Shu
        90,  // Tetsu
        92,  // Huan
        93,  // Jess
        95,  // Alex
        97,  // Tenkou
        99,  // Richmond
        105, // Adlai
        106, // Gordon
        108, // Connell
        109, // Lebrante
        110, // Tessai
        113, // Pico
        114, // Alberto
        117, // Ridley
        118  // Klaus
    };

    public static void Initialize(Harmony harmony)
    {
        if (_initialized) return;

        try
        {
            harmony.PatchAll(typeof(NPCBathVisitorPatch));
            _initialized = true;
            Plugin.Log.LogInfo("[NPCBathVisitorPatch] Successfully initialized Suikoden 2 NPC Bath Sprites & Castle Visitors patch.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[NPCBathVisitorPatch] Failed to initialize: {ex}");
        }
    }

    private static GSD2::P_GRAPH_DATA GetDummyGraphData()
    {
        if (_dummyGraphData == null)
        {
            try
            {
                _dummyGraphData = ScriptableObject.CreateInstance(Il2CppInterop.Runtime.Il2CppType.Of<GSD2::P_GRAPH_DATA>())?.Cast<GSD2::P_GRAPH_DATA>();
            }
            catch
            {
                _dummyGraphData = new GSD2::P_GRAPH_DATA();
            }

            if (_dummyGraphData != null)
            {
                _dummyGraphData.field_data = new GSD2::PLAYER_FIELD();
                _dummyGraphData.battle_data = new GSD2::PLAYER_BATTLE();
            }
        }
        return _dummyGraphData;
    }

    /// <summary>
    /// Corrects character gender classification for castle bathhouse separation (GetFuroPos).
    /// For playable characters (IDs 0..83), defers directly to vanilla native GetSeibetu.
    /// For Castle NPC visitors (IDs >= 84), classifies true canonical females into women's tub (1)
    /// and males into men's tub (0), fixing the vanilla issue where non-combatant NPCs have zeroed ckd_info.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.f_main), nameof(GSD2::EventOverlayClass.f_main.GetSeibetu))]
    [HarmonyPrefix]
    public static bool GetSeibetu_Prefix(int chara_id, ref int __result)
    {
        // Defer all playable party characters (IDs 0..83) to vanilla's native GetSeibetu.
        // Vanilla natively handles all combatants, including Viki (4), Tomo (17), Bob (40),
        // Chaco (54), Hoi (71), Rulodia (75), and Vincent/Simone (64/65).
        if (chara_id < 84)
        {
            return true; // Run vanilla native GetSeibetu
        }

        // Intervene only for Castle NPC bath visitors (IDs >= 84)
        if (FemaleCastleNPCsSet.Contains((byte)chara_id))
        {
            __result = 1; // 1 = Female (routes to women's bath positions 6..11 behind partition)
        }
        else
        {
            __result = 0; // 0 = Male (routes to men's bath positions 0..5)
        }
        return false;
    }

    /// <summary>
    /// Diagnostically logs exact tub position and side (Men's 0..5 vs Women's 6..11) for every bather.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.f_main), nameof(GSD2::EventOverlayClass.f_main.GetFuroPos))]
    [HarmonyPostfix]
    public static void GetFuroPos_Postfix(GSD2::EventOverlayClass.furo_h.FURO_WORK fw, int chara_id, int __result)
    {
        CharacterToFuroSlot[chara_id] = __result;
        FuroSlotToCharacter[__result] = chara_id;
        string tubSide = (__result >= 6 && __result <= 11) ? "Women's tub" : "Men's tub";
        Plugin.Log.LogInfo($"[NPCBathVisitorPatch] GetFuroPos: Character ID {chara_id} placed in tub position {__result} ({tubSide}).");
    }

    /// <summary>
    /// Unlocks full 8-character bathhouse capacity (matching FURO_WORK internal array sizes).
    /// In vanilla, G2_pamem_num(0) only counted combat members (0..5, max 6).
    /// When inside the bathhouse, returns the count of all active bathers across all 8 slots (0..7),
    /// allowing up to 8 characters (combat members + convoy + castle visitors) to soak simultaneously.
    /// Bypasses override if a special vanilla bath vignette party is active so exact party counts (3 or 4) are preserved.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::G2_SYS), nameof(GSD2::G2_SYS.G2_pamem_num))]
    [HarmonyPrefix]
    public static bool G2_pamem_num_Prefix(int mode, ref int __result)
    {
        if (!Plugin.Config.EnableNPCBathVisitors.Value || !Plugin.Config.EnableEightBatherCapacity.Value) return true;

        if (IsInsideBath && !_isSpecialVignetteParty)
        {
            try
            {
                var party = GSD2::G2_SYS.G2_party_dat();
                if (party?.party_cha_no != null)
                {
                    int count = 0;
                    var arr = party.party_cha_no;
                    for (int i = 0; i < arr.Length && i < 8; i++)
                    {
                        if (arr[i] > 0) count++;
                    }
                    __result = count;
                    return false;
                }
            }
            catch { }
        }
        return true;
    }

    /// <summary>
    /// Prevents IndexOutOfRangeException and null-dereference crashes (0x182016059) in CreateFuroChara
    /// by supplying dummy P_GRAPH_DATA with non-null field_data & battle_data when an NPC is loaded into the bathhouse.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::G2_SYS), nameof(GSD2::G2_SYS.G2_gra_dat))]
    [HarmonyPrefix]
    public static bool G2_gra_dat_Prefix(int pcno, ref GSD2::P_GRAPH_DATA __result)
    {
        if (!Plugin.Config.EnableNPCBathVisitors.Value) return true;

        if (IsInsideBath && (pcno < 0 || pcno >= 8))
        {
            __result = GetDummyGraphData();
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(GSD2::G2_SYS), nameof(GSD2::G2_SYS.G2_gra_dat))]
    [HarmonyPostfix]
    public static void G2_gra_dat_Postfix(int pcno, ref GSD2::P_GRAPH_DATA __result)
    {
        if (!Plugin.Config.EnableNPCBathVisitors.Value) return;

        if (IsInsideBath)
        {
            if (__result == null)
            {
                __result = GetDummyGraphData();
            }
            else
            {
                if (__result.field_data == null)
                    __result.field_data = GetDummyGraphData().field_data;
                if (__result.battle_data == null)
                    __result.battle_data = GetDummyGraphData().battle_data;
            }
        }
    }

    /// <summary>
    /// Prevents IndexOutOfRangeException when querying variable character data for NPCs (IDs >= 83).
    /// In Suikoden 2, c_varia_dat only contains entries for combat characters (0..82).
    /// When an NPC is queried, safely return null so callers like CheckfuroItemEquip gracefully treat them as having no equipment.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::G2_SYS), nameof(GSD2::G2_SYS.G2_varia_dat))]
    [HarmonyPrefix]
    public static bool G2_varia_dat_Prefix(int chano, ref GSD2::C_VARIA_DAT __result)
    {
        if (chano < 0 || chano >= 83)
        {
            __result = null;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Preemptively intercepts bath equipment checks for NPCs (IDs >= 83), returning 0 (no bath item equipped).
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.f_main), nameof(GSD2::EventOverlayClass.f_main.CheckfuroItemEquip))]
    [HarmonyPrefix]
    public static bool CheckfuroItemEquip_Prefix(int chara_no, int item_no, ref int __result)
    {
        if (chara_no < 0 || chara_no >= 83)
        {
            __result = 0;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Prevents IndexOutOfRangeException in FuroEnd when 8-bather capacity is enabled.
    /// Vanilla party buffers only allocate 6 combat slots (0..5). When FuroEnd loops through
    /// active bathers up to slot 7 and calls G2_hoka, slots >= 6 trigger an out-of-bounds error.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::G2_SYS), nameof(GSD2::G2_SYS.G2_hoka))]
    [HarmonyPrefix]
    public static bool G2_hoka_Prefix(int mode, int pcno, ref int __result)
    {
        if (pcno < 0 || pcno >= 6)
        {
            __result = 0;
            return false;
        }
        return true;
    }

    /// <summary>
    /// When bathhouse initialization begins, mark bath state and populate party with convoy / recruited castle NPCs.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.f_main), nameof(GSD2::EventOverlayClass.f_main.FuroInit))]
    [HarmonyPrefix]
    public static void FuroInit_Prefix(GSD2::EventOverlayClass.furo_h.FURO_WORK fw)
    {
        CurrentFuroWork = fw;
        CharacterToFuroSlot.Clear();
        FuroSlotToCharacter.Clear();
        ActiveBathVisitors.Clear();
        ActivePartyInBath.Clear();
        _isSpecialVignetteParty = false;

        if (Plugin.Config.DetailedLogs.Value)
            Plugin.Log.LogInfo($"[NPCBathVisitorPatch] FuroInit: select_furo_no (Bath Style) = {fw?.select_furo_no}.");

        if (!Plugin.Config.EnableNPCBathVisitors.Value) return;

        IsInsideBath = true;
        PrepareBathParty();
    }

    /// <summary>
    /// Diagnostically logs execution of native character bath vignette checks on bath exit.
    /// In Suikoden 2, bath vignettes are evaluated when selecting "Leave Bath" (FuroPhaseExitFuro00 -> FuroPhaseCheckCharaEvent).
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.f_phase), nameof(GSD2::EventOverlayClass.f_phase.FuroPhaseCheckCharaEvent))]
    [HarmonyPrefix]
    public static void FuroPhaseCheckCharaEvent_Prefix(GSD2::EventOverlayClass.furo_h.FURO_WORK fw)
    {
        int count = GSD2::G2_SYS.G2_pamem_num(0);
        Plugin.Log.LogInfo($"[NPCBathVisitorPatch] FuroPhaseCheckCharaEvent: Player selected Leave Bath. Evaluating native bath vignettes (BathStyle={fw?.select_furo_no}, PartyCount={count})...");
    }

    [HarmonyPatch(typeof(GSD2::EventOverlayClass.fc_event), nameof(GSD2::EventOverlayClass.fc_event.CheckCharaEvent04))]
    [HarmonyPostfix]
    public static void CheckCharaEvent04_Postfix(GSD2::EventOverlayClass.furo_h.FURO_WORK fw, int __result)
    {
        int count = GSD2::G2_SYS.G2_pamem_num(0);
        Plugin.Log.LogInfo($"[NPCBathVisitorPatch] CheckCharaEvent04 (Flik & Viktor Cypress/Jungle Bath) evaluated. Result={__result} (1=Trigger Scene, 0=Skip). BathStyle={fw?.select_furo_no}, PartyCount={count}.");
    }

    [HarmonyPatch(typeof(GSD2::EventOverlayClass.fc_event), nameof(GSD2::EventOverlayClass.fc_event.CheckCharaEvent00))]
    [HarmonyPostfix]
    public static void CheckCharaEvent00_Postfix(GSD2::EventOverlayClass.furo_h.FURO_WORK fw, int __result)
    {
        int count = GSD2::G2_SYS.G2_pamem_num(0);
        Plugin.Log.LogInfo($"[NPCBathVisitorPatch] CheckCharaEvent00 (Flik & Viktor Marble Bath) evaluated. Result={__result} (1=Trigger Scene, 0=Skip). BathStyle={fw?.select_furo_no}, PartyCount={count}.");
    }

    /// <summary>
    /// When bathhouse exits, restore the original party and reset state.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.f_main), nameof(GSD2::EventOverlayClass.f_main.FuroEnd))]
    [HarmonyPostfix]
    public static void FuroEnd_Postfix(GSD2::EventOverlayClass.furo_h.FURO_WORK fw)
    {
        IsInsideBath = false;
        CurrentFuroWork = null;
        RestoreParty();
    }

    private static readonly HashSet<byte> PermanentCastleFemales = new HashSet<byte>
    {
        85,  // Apple
        91,  // Leona
        98   // Barbara
    };

    private static readonly HashSet<byte> PermanentCastleMales = new HashSet<byte>
    {
        89,  // Shu
        92,  // Huan
        99   // Richmond
    };

    /// <summary>
    /// Checks whether Pilika (ID 123) is currently residing in Castle HQ.
    /// Pilika stays in the castle from the founding of Castle HQ until the Muse Peace Conference,
    /// where she runs to Jowy and is taken to the Highland Royal Palace for the rest of the war.
    /// </summary>
    public static bool IsPilikaInCastle()
    {
        try
        {
            var gw = GSD2::OldSrcBase.game_work;
            if (gw == null || gw.event_flag == null) return true;

            var ef = gw.event_flag;

            // Story Flag 14: Muse Peace Conference progression.
            // Bit 64 (0x40) is set during the peace conference when Pilika spots Jowy and runs to him.
            // Bit 128 (0x80) is set upon completing the conference.
            if (ef.Length > 14 && (ef[14] & 0x40) != 0)
                return false;

            // Subsequent storyline flags (Tinto, Toran alliance, Rockaxe, Muse liberation, L'Renouille)
            if (ef.Length > 0x9A && (ef[0x9A] & 0x01) != 0) // Tinto zombie invasion
                return false;

            if (ef.Length > 0x90 && (ef[0x90] & 0x20) != 0) // Banner Pass / Gregminster alliance
                return false;

            if (ef.Length > 0x77 && (ef[0x77] & 0x10) != 0) // Rockaxe / Matilda campaign
                return false;

            if (ef.Length > 0x78 && (ef[0x78] & 0x10) != 0) // Muse liberation
                return false;

            if (ef.Length > 0xCA && (ef[0xCA] & 0x02) != 0) // Sajah / L'Renouille assault
                return false;

            return true;
        }
        catch
        {
            return true;
        }
    }

    private static bool SafeIsRecruited(byte npcId)
    {
        if (npcId == 123) // Pilika leaves castle after the Muse peace conference
            return IsPilikaInCastle();

        if (PermanentCastleFemales.Contains(npcId) || PermanentCastleMales.Contains(npcId))
            return true;

        if (npcId >= 128) // chara_flag array length bounds (0x80 = 128)
            return false;

        try
        {
            return GSD2::G2_SYS.G2_cha_flag(2, npcId) == 1;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSpecialBathEventParty(byte[] partyArr)
    {
        if (partyArr == null) return false;

        int count = 0;
        HashSet<byte> members = new HashSet<byte>();
        for (int i = 0; i < 6 && i < partyArr.Length; i++)
        {
            byte id = partyArr[i];
            if (id > 0)
            {
                count++;
                members.Add(id);
            }
        }

        // 1. Flik & Viktor vignette (CheckCharaEvent00 [Marble] & CheckCharaEvent04 [Jungle]: asserts party count == 3)
        if (count == 3 && members.Contains(2) && members.Contains(3))
            return true;

        // 2. Flik, Viktor & Nina vignette (CheckCharaEvent01 [Rock]: asserts party count == 4)
        if (count == 4 && members.Contains(2) && members.Contains(3) && members.Contains(55))
            return true;

        // 3. Heavyweight Muscle Men vignette (CheckCharaEvent03 [Normal]: asserts party count == 4)
        // Riou + Long Chan-Chan (69), Bolgan (21), Gantetsu (70)
        if (count == 4 && members.Contains(69) && members.Contains(21) && members.Contains(70))
            return true;

        return false;
    }

    private static void PrepareBathParty()
    {
        try
        {
            var party = GSD2::G2_SYS.G2_party_dat();
            if (party == null || party.party_cha_no == null) return;

            var chaNoArray = party.party_cha_no;
            int totalSlots = chaNoArray.Length;
            if (totalSlots < 6) return;

            // Preserve special vanilla bath vignettes (e.g. Flik & Viktor, Knights of Matilda)
            if (IsSpecialBathEventParty(chaNoArray))
            {
                _isSpecialVignetteParty = true;
                Plugin.Log.LogInfo("[NPCBathVisitorPatch] Special bath vignette party detected. Preserving vanilla party composition and count.");
                return;
            }

            // Backup original party (all slots)
            _savedPartyMembers = new byte[totalSlots];
            for (int i = 0; i < totalSlots; i++)
            {
                _savedPartyMembers[i] = chaNoArray[i];
            }
            _isPartyModified = true;

            // Track existing characters in all slots (0..7) and count gender
            HashSet<byte> activeIds = new HashSet<byte>();
            int maleCount = 0;
            int femaleCount = 0;

            int maxSlotsToFill = Plugin.Config.EnableEightBatherCapacity.Value ? 8 : 6;

            for (int i = 0; i < totalSlots && i < maxSlotsToFill; i++)
            {
                byte id = chaNoArray[i];
                if (id > 0)
                {
                    activeIds.Add(id);
                    if (IsFemale(id))
                    {
                        femaleCount++;
                    }
                    else
                    {
                        maleCount++;
                    }
                }
            }

            // Collect recruited castle candidates, segregated by gender
            List<byte> recruitedFemales = new List<byte>();
            List<byte> recruitedMales = new List<byte>();

            foreach (byte npcId in FemaleCastleNPCs)
            {
                if (!activeIds.Contains(npcId) && SafeIsRecruited(npcId))
                {
                    recruitedFemales.Add(npcId);
                }
            }

            foreach (byte npcId in MaleCastleNPCs)
            {
                if (!activeIds.Contains(npcId) && SafeIsRecruited(npcId))
                {
                    recruitedMales.Add(npcId);
                }
            }

            if (Plugin.Config.DetailedLogs.Value)
                Plugin.Log.LogInfo($"[NPCBathVisitorPatch] Bath entry: initial party has {maleCount} males, {femaleCount} females. Castle pool: {recruitedFemales.Count} females, {recruitedMales.Count} males.");

            // Shuffle both candidate pools
            System.Random rng = new System.Random();
            Shuffle(recruitedFemales, rng);
            Shuffle(recruitedMales, rng);

            int femaleIdx = 0;
            int maleIdx = 0;

            // Fill empty slots across index 1..maxSlotsToFill - 1.
            for (int i = 1; i < totalSlots && i < maxSlotsToFill; i++)
            {
                if (chaNoArray[i] == 0)
                {
                    byte chosenId = 0;

                    // Guarantee female representation first if women's tub is empty, or alternate to balance tubs
                    if (femaleCount < 6 && (femaleCount == 0 || (femaleCount < maleCount && femaleIdx < recruitedFemales.Count)))
                    {
                        if (femaleIdx < recruitedFemales.Count)
                        {
                            chosenId = recruitedFemales[femaleIdx++];
                            femaleCount++;
                        }
                        else if (maleCount < 6 && maleIdx < recruitedMales.Count)
                        {
                            chosenId = recruitedMales[maleIdx++];
                            maleCount++;
                        }
                    }
                    else if (maleCount < 6 && maleIdx < recruitedMales.Count)
                    {
                        chosenId = recruitedMales[maleIdx++];
                        maleCount++;
                    }
                    else if (femaleCount < 6 && femaleIdx < recruitedFemales.Count)
                    {
                        chosenId = recruitedFemales[femaleIdx++];
                        femaleCount++;
                    }

                    if (chosenId > 0)
                    {
                        chaNoArray[i] = chosenId;
                        activeIds.Add(chosenId);
                        ActiveBathVisitors.Add(chosenId);
                        string genderStr = IsFemale(chosenId) ? "Female (Women's tub)" : "Male (Men's tub)";
                        Plugin.Log.LogInfo($"[NPCBathVisitorPatch] Added castle visitor NPC ID {chosenId} [{genderStr}] to party slot {i}. Total bathers now: {maleCount + femaleCount} (Men: {maleCount}, Women: {femaleCount}).");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[NPCBathVisitorPatch] Error in PrepareBathParty: {ex}");
            RestoreParty();
        }
    }

    private static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            T tmp = list[i];
            list[i] = list[k];
            list[k] = tmp;
        }
    }

    public static void RestoreParty()
    {
        if (!_isPartyModified || _savedPartyMembers == null) return;

        try
        {
            var party = GSD2::G2_SYS.G2_party_dat();
            if (party != null && party.party_cha_no != null)
            {
                for (int i = 0; i < party.party_cha_no.Length && i < _savedPartyMembers.Length; i++)
                {
                    party.party_cha_no[i] = _savedPartyMembers[i];
                }
                if (Plugin.Config.DetailedLogs.Value)
                    Plugin.Log.LogInfo("[NPCBathVisitorPatch] Restored original party data after bathhouse exit.");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[NPCBathVisitorPatch] Error restoring party data: {ex}");
        }
        finally
        {
            _isPartyModified = false;
            _savedPartyMembers = null;
            _isSpecialVignetteParty = false;
        }
    }
}