using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Dados serializáveis de um equipamento ou consumível para persistência em JSON (Depósito da Guilda e Slots Equipados).
/// </summary>
[Serializable]
public class ItemSaveData
{
    public string itemName;
    public string description;
    public int itemType;
    public int rarity;
    public string iconPath;
    public int goldValue;

    public int[] allowedClasses;
    public int requiredStrength;
    public int requiredDexterity;
    public int requiredIntelligence;
    public int requiredLevel;

    public int healHP;
    public int healMP;

    public int bonusAttack;
    public int bonusDefense;
    public int bonusMagicAttack;
    public int bonusMaxHP;
    public int bonusMaxMP;
    public int bonusSpeed;
    public float bonusCritRate;

    public static ItemSaveData FromItemData(ItemData item)
    {
        if (item == null) return null;

        int[] classesInt = null;
        if (item.allowedClasses != null && item.allowedClasses.Length > 0)
        {
            classesInt = new int[item.allowedClasses.Length];
            for (int i = 0; i < item.allowedClasses.Length; i++)
            {
                classesInt[i] = (int)item.allowedClasses[i];
            }
        }

        return new ItemSaveData
        {
            itemName = item.itemName,
            description = item.description,
            itemType = (int)item.itemType,
            rarity = (int)item.rarity,
            iconPath = item.iconPath,
            goldValue = item.goldValue,
            allowedClasses = classesInt,
            requiredStrength = item.requiredStrength,
            requiredDexterity = item.requiredDexterity,
            requiredIntelligence = item.requiredIntelligence,
            requiredLevel = item.requiredLevel,
            healHP = item.healHP,
            healMP = item.healMP,
            bonusAttack = item.bonusAttack,
            bonusDefense = item.bonusDefense,
            bonusMagicAttack = item.bonusMagicAttack,
            bonusMaxHP = item.bonusMaxHP,
            bonusMaxMP = item.bonusMaxMP,
            bonusSpeed = item.bonusSpeed,
            bonusCritRate = item.bonusCritRate
        };
    }

    public ItemData ToItemData()
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        HeroClassType[] allowed = null;
        if (allowedClasses != null && allowedClasses.Length > 0)
        {
            allowed = new HeroClassType[allowedClasses.Length];
            for (int i = 0; i < allowedClasses.Length; i++)
            {
                allowed[i] = (HeroClassType)allowedClasses[i];
            }
        }

        return ItemGenerator.CreateItem(
            name: itemName,
            type: (ItemType)itemType,
            desc: description,
            iconPath: iconPath,
            allowedClasses: allowed,
            rarity: (ItemRarity)rarity,
            atk: bonusAttack,
            def: bonusDefense,
            matq: bonusMagicAttack,
            hp: bonusMaxHP,
            mp: bonusMaxMP,
            speed: bonusSpeed,
            crit: bonusCritRate,
            reqStr: requiredStrength,
            reqDex: requiredDexterity,
            reqInt: requiredIntelligence,
            reqLvl: requiredLevel,
            healHP: healHP,
            healMP: healMP,
            gold: goldValue
        );
    }
}

/// <summary>
/// Dados serializáveis de um Herói da Guilda (atributos base, nível, XP, pontos distribuídos e os 6 equipamentos vestidos).
/// </summary>
[Serializable]
public class HeroSaveData
{
    public string heroName;
    public string biography;
    public int heroClass;
    public int currentStars;
    public bool isGoldStar;

    // Atributos base e crescimento por nível do HeroData
    public int baseStrength;
    public int baseDexterity;
    public int baseIntelligence;
    public int baseVitality;

    public int growthStrength;
    public int growthDexterity;
    public int growthIntelligence;
    public int growthVitality;

    // Progressão e Nível
    public int level;
    public int currentXP;
    public int xpToNextLevel;

    // Recursos Vitais e Poções
    public int currentHP;
    public int currentMP;
    public int healingPotions;

    // Pontos de Atributo Distribuídos
    public int unallocatedAttributePoints;
    public int allocatedStrength;
    public int allocatedDexterity;
    public int allocatedIntelligence;
    public int allocatedVitality;

    // Os 6 Equipamentos Vestidos
    public bool hasWeapon;
    public ItemSaveData equippedWeapon;

    public bool hasHelmet;
    public ItemSaveData equippedHelmet;

    public bool hasChest;
    public ItemSaveData equippedChest;

    public bool hasPants;
    public ItemSaveData equippedPants;

    public bool hasBoots;
    public ItemSaveData equippedBoots;

    public bool hasGloves;
    public ItemSaveData equippedGloves;

    public static HeroSaveData FromHeroInstance(HeroInstance hero)
    {
        if (hero == null) return null;

        HeroAttributes baseAttrs = hero.heroData != null ? hero.heroData.baseAttributes : new HeroAttributes(10, 10, 10, 10);
        HeroAttributes growthAttrs = hero.heroData != null ? hero.heroData.growthPerLevel : new HeroAttributes(2, 2, 2, 2);

        return new HeroSaveData
        {
            heroName = hero.heroName,
            biography = hero.heroData != null ? hero.heroData.biography : "",
            heroClass = (int)hero.heroClass,
            currentStars = hero.currentStars,
            isGoldStar = hero.isGoldStar,

            baseStrength = baseAttrs.strength,
            baseDexterity = baseAttrs.dexterity,
            baseIntelligence = baseAttrs.intelligence,
            baseVitality = baseAttrs.vitality,

            growthStrength = growthAttrs.strength,
            growthDexterity = growthAttrs.dexterity,
            growthIntelligence = growthAttrs.intelligence,
            growthVitality = growthAttrs.vitality,

            level = hero.level,
            currentXP = hero.currentXP,
            xpToNextLevel = hero.xpToNextLevel,

            currentHP = hero.currentHP,
            currentMP = hero.currentMP,
            healingPotions = hero.healingPotions,

            unallocatedAttributePoints = hero.unallocatedAttributePoints,
            allocatedStrength = hero.allocatedStrength,
            allocatedDexterity = hero.allocatedDexterity,
            allocatedIntelligence = hero.allocatedIntelligence,
            allocatedVitality = hero.allocatedVitality,

            hasWeapon = hero.equippedWeapon != null,
            equippedWeapon = hero.equippedWeapon != null ? ItemSaveData.FromItemData(hero.equippedWeapon) : null,

            hasHelmet = hero.equippedHelmet != null,
            equippedHelmet = hero.equippedHelmet != null ? ItemSaveData.FromItemData(hero.equippedHelmet) : null,

            hasChest = hero.equippedChest != null,
            equippedChest = hero.equippedChest != null ? ItemSaveData.FromItemData(hero.equippedChest) : null,

            hasPants = hero.equippedPants != null,
            equippedPants = hero.equippedPants != null ? ItemSaveData.FromItemData(hero.equippedPants) : null,

            hasBoots = hero.equippedBoots != null,
            equippedBoots = hero.equippedBoots != null ? ItemSaveData.FromItemData(hero.equippedBoots) : null,

            hasGloves = hero.equippedGloves != null,
            equippedGloves = hero.equippedGloves != null ? ItemSaveData.FromItemData(hero.equippedGloves) : null
        };
    }

    public HeroInstance ToHeroInstance()
    {
        HeroClassType cType = (HeroClassType)heroClass;

        HeroData data = ScriptableObject.CreateInstance<HeroData>();
        data.heroName = heroName;
        data.biography = string.IsNullOrEmpty(biography) ? "Um aventureiro veterano da guilda." : biography;
        data.heroClass = cType;
        data.stars = Mathf.Clamp(currentStars, 1, 5);
        data.isGoldStar = isGoldStar;
        data.baseAttributes = new HeroAttributes(baseStrength, baseDexterity, baseIntelligence, baseVitality);
        data.growthPerLevel = new HeroAttributes(growthStrength, growthDexterity, growthIntelligence, growthVitality);
        data.battleSprite = HeroGenerator.GetClassSprite(cType);
        data.portrait = data.battleSprite;

        HeroInstance instance = new HeroInstance(data, Mathf.Max(1, level));
        instance.currentStars = data.stars;
        instance.isGoldStar = data.isGoldStar;
        instance.currentXP = currentXP;
        instance.CalculateXPRequirement();

        instance.unallocatedAttributePoints = unallocatedAttributePoints;
        instance.allocatedStrength = allocatedStrength;
        instance.allocatedDexterity = allocatedDexterity;
        instance.allocatedIntelligence = allocatedIntelligence;
        instance.allocatedVitality = allocatedVitality;

        instance.equippedWeapon = (hasWeapon && equippedWeapon != null) ? equippedWeapon.ToItemData() : null;
        instance.equippedHelmet = (hasHelmet && equippedHelmet != null) ? equippedHelmet.ToItemData() : null;
        instance.equippedChest = (hasChest && equippedChest != null) ? equippedChest.ToItemData() : null;
        instance.equippedPants = (hasPants && equippedPants != null) ? equippedPants.ToItemData() : null;
        instance.equippedBoots = (hasBoots && equippedBoots != null) ? equippedBoots.ToItemData() : null;
        instance.equippedGloves = (hasGloves && equippedGloves != null) ? equippedGloves.ToItemData() : null;

        instance.healingPotions = Mathf.Clamp(healingPotions, 0, 6);
        instance.currentHP = currentHP > 0 ? Mathf.Min(currentHP, instance.GetMaxHP()) : instance.GetMaxHP();
        instance.currentMP = currentMP > 0 ? Mathf.Min(currentMP, instance.GetMaxMP()) : instance.GetMaxMP();
        instance.InitializeDefaultSkills();

        return instance;
    }
}

/// <summary>
/// Estrutura completa de um Slot de Save (Andares Liberados, Heróis e Depósito da Guilda).
/// </summary>
[Serializable]
public class GameSaveData
{
    public int slotIndex = 1;
    public string saveName = "Expedicao da Guilda";
    public string lastSavedDate = "";
    public long timestampTicks = 0;

    // Progresso da Masmorra e Andares Liberados
    public int currentFloor = 1;
    public int maxUnlockedFloor = 1;
    public int clearedRoomsThisFloor = 0;
    public bool isFloorBossDefeated = false;

    // Ouro e Recursos
    public int goldCoins = 0;

    // Grupo de Heróis Titulares (Expedição) e Reserva do Quartel da Guilda
    public List<HeroSaveData> heroes = new List<HeroSaveData>();
    public List<HeroSaveData> reserveHeroes = new List<HeroSaveData>();

    // Armazém / Depósito da Guilda (Todos os equipamentos e itens adquiridos guardados)
    public List<ItemSaveData> guildDeposit = new List<ItemSaveData>();
}

/// <summary>
/// Gerenciador estático de persistência de dados (Save / Load em múltiplos slots JSON).
/// Salva automaticamente o progresso dos andares liberados, evolução dos heróis e o Depósito da Guilda.
/// </summary>
public static class SaveSystem
{
    public const int MAX_SAVE_SLOTS = 4;
    private const string LAST_SLOT_PREF_KEY = "DungeonMasterRPG_LastSaveSlot";

    public static int CurrentActiveSlot
    {
        get => PlayerPrefs.GetInt(LAST_SLOT_PREF_KEY, 1);
        set
        {
            PlayerPrefs.SetInt(LAST_SLOT_PREF_KEY, Mathf.Clamp(value, 1, MAX_SAVE_SLOTS));
            PlayerPrefs.Save();
        }
    }

    public static string GetSlotFilePath(int slotIndex)
    {
        int clamped = Mathf.Clamp(slotIndex, 1, MAX_SAVE_SLOTS);
        return Path.Combine(Application.persistentDataPath, $"dungeon_save_slot_{clamped}.json");
    }

    public static bool DoesSlotExist(int slotIndex)
    {
        return File.Exists(GetSlotFilePath(slotIndex));
    }

    public static bool HasAnySave()
    {
        for (int i = 1; i <= MAX_SAVE_SLOTS; i++)
        {
            if (DoesSlotExist(i)) return true;
        }
        return false;
    }

    public static int GetExistingSaveCount()
    {
        int count = 0;
        for (int i = 1; i <= MAX_SAVE_SLOTS; i++)
        {
            if (DoesSlotExist(i)) count++;
        }
        return count;
    }

    public static int GetFirstEmptyOrOldestSlot()
    {
        for (int i = 1; i <= MAX_SAVE_SLOTS; i++)
        {
            if (!DoesSlotExist(i)) return i;
        }
        return 1;
    }

    public static GameSaveData ReadSaveSlotHeader(int slotIndex)
    {
        string path = GetSlotFilePath(slotIndex);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Falha ao ler o Slot {slotIndex}: {e.Message}");
            return null;
        }
    }

    public static List<GameSaveData> GetAllExistingSaves()
    {
        List<GameSaveData> list = new List<GameSaveData>();
        for (int i = 1; i <= MAX_SAVE_SLOTS; i++)
        {
            GameSaveData data = ReadSaveSlotHeader(i);
            if (data != null)
            {
                list.Add(data);
            }
        }
        return list;
    }

    /// <summary>
    /// Retorna o Save mais recente para o botão [ CONTINUAR JOGO ].
    /// </summary>
    public static GameSaveData GetMostRecentSave()
    {
        int preferredSlot = CurrentActiveSlot;
        GameSaveData best = null;

        for (int i = 1; i <= MAX_SAVE_SLOTS; i++)
        {
            GameSaveData candidate = ReadSaveSlotHeader(i);
            if (candidate == null) continue;

            if (best == null || candidate.timestampTicks > best.timestampTicks)
            {
                best = candidate;
            }
        }

        if (best == null && DoesSlotExist(preferredSlot))
        {
            best = ReadSaveSlotHeader(preferredSlot);
        }

        return best;
    }

    /// <summary>
    /// Salva imediatamente o progresso atual no slot ativo (Auto-Save ou Save Manual).
    /// </summary>
    public static void AutoSave(string reason = "")
    {
        SaveGame(CurrentActiveSlot, reason);
    }

    /// <summary>
    /// Serializa e grava no disco os andares liberados, os heróis e o Depósito da Guilda no slot indicado.
    /// </summary>
    public static bool SaveGame(int slotIndex, string reason = "")
    {
        if (PartyManager.Instance == null) return false;

        int slot = Mathf.Clamp(slotIndex, 1, MAX_SAVE_SLOTS);
        CurrentActiveSlot = slot;

        int curFloor = DungeonManager.Instance != null ? DungeonManager.Instance.currentFloor : 1;
        int maxFloor = DungeonManager.Instance != null ? Mathf.Max(DungeonManager.Instance.maxUnlockedFloor, curFloor) : 1;
        int clearedRooms = DungeonManager.Instance != null ? DungeonManager.Instance.clearedRoomsThisFloor : 0;
        bool bossDead = DungeonManager.Instance != null && DungeonManager.Instance.isFloorBossDefeated;

        GameSaveData save = new GameSaveData
        {
            slotIndex = slot,
            saveName = $"Slot {slot} - Andar {curFloor} (Max: {maxFloor})",
            lastSavedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            timestampTicks = DateTime.Now.Ticks,

            currentFloor = curFloor,
            maxUnlockedFloor = maxFloor,
            clearedRoomsThisFloor = clearedRooms,
            isFloorBossDefeated = bossDead,

            goldCoins = PartyManager.Instance.goldCoins
        };

        // 1. Salva todos os Heróis da Party Ativa e da Reserva do Quartel da Guilda
        foreach (var hero in PartyManager.Instance.activeParty)
        {
            if (hero != null)
            {
                save.heroes.Add(HeroSaveData.FromHeroInstance(hero));
            }
        }

        if (PartyManager.Instance.reserveHeroes != null)
        {
            foreach (var rHero in PartyManager.Instance.reserveHeroes)
            {
                if (rHero != null)
                {
                    save.reserveHeroes.Add(HeroSaveData.FromHeroInstance(rHero));
                }
            }
        }

        // 2. Salva todos os Equipamentos e Itens no Depósito da Guilda (Mochila Compartilhada)
        if (PartyManager.Instance.inventory != null)
        {
            foreach (var item in PartyManager.Instance.inventory)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemName))
                {
                    save.guildDeposit.Add(ItemSaveData.FromItemData(item));
                }
            }
        }

        try
        {
            string json = JsonUtility.ToJson(save, true);
            string path = GetSlotFilePath(slot);
            File.WriteAllText(path, json);
            string tag = string.IsNullOrEmpty(reason) ? "Manual" : reason;
            Debug.Log($"💾 [SAVE CONCLUÍDO - SLOT {slot} ({tag})] Andar Atual: {curFloor} (Liberados: 1 a {maxFloor}) | Heróis: {save.heroes.Count} | Depósito da Guilda: {save.guildDeposit.Count} itens | Ouro: {save.goldCoins} G");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Erro ao salvar no Slot {slot}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Carrega um Slot de Save, restaurando os heróis, seus níveis/atributos, o Depósito da Guilda e os andares liberados.
    /// </summary>
    public static bool LoadGame(int slotIndex)
    {
        GameSaveData save = ReadSaveSlotHeader(slotIndex);
        if (save == null)
        {
            Debug.LogWarning($"[SaveSystem] Slot {slotIndex} não encontrado para carregar.");
            return false;
        }

        CurrentActiveSlot = slotIndex;

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.goldCoins = save.goldCoins;

            // Restaura os Heróis da Party Ativa e da Reserva do Quartel
            PartyManager.Instance.activeParty.Clear();
            if (save.heroes != null)
            {
                foreach (var hData in save.heroes)
                {
                    if (hData != null && !string.IsNullOrEmpty(hData.heroName))
                    {
                        PartyManager.Instance.activeParty.Add(hData.ToHeroInstance());
                    }
                }
            }

            PartyManager.Instance.reserveHeroes.Clear();
            if (save.reserveHeroes != null)
            {
                foreach (var rData in save.reserveHeroes)
                {
                    if (rData != null && !string.IsNullOrEmpty(rData.heroName))
                    {
                        PartyManager.Instance.reserveHeroes.Add(rData.ToHeroInstance());
                    }
                }
            }

            if (PartyManager.Instance.activeParty.Count == 0)
            {
                PartyManager.Instance.InitializeParty();
            }

            // Restaura o Depósito da Guilda (Equipamentos e Consumíveis adquiridos)
            PartyManager.Instance.inventory.Clear();
            if (save.guildDeposit != null)
            {
                foreach (var iData in save.guildDeposit)
                {
                    if (iData != null && !string.IsNullOrEmpty(iData.itemName))
                    {
                        ItemData restoredItem = iData.ToItemData();
                        if (restoredItem != null)
                        {
                            PartyManager.Instance.inventory.Add(restoredItem);
                        }
                    }
                }
            }
        }

        // Restaura os Andares Liberados e o estado da Masmorra
        if (DungeonManager.Instance != null)
        {
            DungeonManager.Instance.maxUnlockedFloor = Mathf.Max(1, save.maxUnlockedFloor);
            DungeonManager.Instance.currentFloor = Mathf.Clamp(save.currentFloor, 1, DungeonManager.Instance.maxUnlockedFloor);
            DungeonManager.Instance.clearedRoomsThisFloor = Mathf.Max(0, save.clearedRoomsThisFloor);
            DungeonManager.Instance.isFloorBossDefeated = save.isFloorBossDefeated;
            DungeonManager.Instance.GenerateDoors();
        }

        Debug.Log($"📂 [SAVE CARREGADO - SLOT {slotIndex}] Andar {save.currentFloor} (Liberados: 1 a {save.maxUnlockedFloor}) | Ouro: {save.goldCoins} G | Depósito da Guilda: {PartyManager.Instance.inventory.Count} itens.");
        return true;
    }

    /// <summary>
    /// Inicia um Novo Jogo do zero no Slot escolhido, convocando 4 novos aventureiros e salvando imediatamente.
    /// </summary>
    public static void StartNewGameInSlot(int slotIndex)
    {
        int slot = Mathf.Clamp(slotIndex, 1, MAX_SAVE_SLOTS);
        CurrentActiveSlot = slot;

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.InitializeParty();
        }

        if (DungeonManager.Instance != null)
        {
            DungeonManager.Instance.maxUnlockedFloor = 1;
            DungeonManager.Instance.StartFloor(1);
        }

        SaveGame(slot, "Novo Jogo");
    }

    /// <summary>
    /// Apaga o arquivo de save de um slot específico.
    /// </summary>
    public static void DeleteSaveSlot(int slotIndex)
    {
        string path = GetSlotFilePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"🗑️ [SAVE REMOVIDO] O Slot {slotIndex} foi apagado.");
        }
    }
}
