using UnityEngine;

/// <summary>
/// Gerador procedural e fábrica de equipamentos, armas e consumíveis com ícones do Raven Fantasy.
/// Calibra equipamentos iniciais Nv. 1, progressão de nível por andar, tabela de raridade por fonte
/// (Monstro Comum / Baú de Tesouro / Chefe do Andar) e escalonamento proporcional de status e requisitos.
/// </summary>
public static class ItemGenerator
{
    private struct DropTemplate
    {
        public string name;
        public ItemType type;
        public string desc;
        public string iconPath;
        public HeroClassType[] allowed;
        public int baseAtk;
        public int baseDef;
        public int baseMatq;
        public int baseHp;
        public int baseMp;
        public int baseSpd;
        public float baseCrit;
        public int baseReqStr;
        public int baseReqDex;
        public int baseReqInt;
        public int baseGold;
    }

    public static ItemData CreateItem(
        string name,
        ItemType type,
        string desc,
        string iconPath,
        HeroClassType[] allowedClasses = null,
        ItemRarity rarity = ItemRarity.Common,
        int atk = 0,
        int def = 0,
        int matq = 0,
        int hp = 0,
        int mp = 0,
        int speed = 0,
        float crit = 0f,
        int reqStr = 0,
        int reqDex = 0,
        int reqInt = 0,
        int reqLvl = 1,
        int healHP = 0,
        int healMP = 0,
        int gold = 50)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName = name;
        item.itemType = type;
        item.description = desc;
        item.iconPath = iconPath;
        item.allowedClasses = allowedClasses;
        item.rarity = rarity;

        item.bonusAttack = atk;
        item.bonusDefense = def;
        item.bonusMagicAttack = matq;
        item.bonusMaxHP = hp;
        item.bonusMaxMP = mp;
        item.bonusSpeed = speed;
        item.bonusCritRate = crit;

        item.requiredStrength = reqStr;
        item.requiredDexterity = reqDex;
        item.requiredIntelligence = reqInt;
        item.requiredLevel = reqLvl;
        item.healHP = healHP;
        item.healMP = healMP;
        item.goldValue = gold;

        return item;
    }

    public static ItemData CreateHealingPotion()
    {
        return CreateItem(
            name: "Poção de Vida",
            type: ItemType.Consumable,
            desc: "Frasco de essência carmesim que restaura HP do aventureiro.",
            iconPath: "Sprites/Icons/potion_hp",
            allowedClasses: null,
            rarity: ItemRarity.Common,
            healHP: 45,
            gold: 25
        );
    }

    /// <summary>
    /// Equipa o conjunto completo inicial de Nível 1 (Comum) coerente com a classe,
    /// permitindo que drops de Nv. 2 a Nv. 8 sejam evoluções reais durante o 1º Andar.
    /// </summary>
    public static void EquipStarterGear(HeroInstance hero)
    {
        if (hero == null) return;

        HeroClassType[] knightOnly = new HeroClassType[] { HeroClassType.Knight };
        HeroClassType[] heavyOnly = new HeroClassType[] { HeroClassType.HeavyKnight };
        HeroClassType[] vanguardClasses = new HeroClassType[] { HeroClassType.Knight, HeroClassType.HeavyKnight };
        HeroClassType[] mageOnly = new HeroClassType[] { HeroClassType.Mage };
        HeroClassType[] rogueOnly = new HeroClassType[] { HeroClassType.Rogue };

        switch (hero.heroClass)
        {
            case HeroClassType.Knight:
                hero.equippedWeapon = CreateItem("Espada de Ferro", ItemType.Weapon, "Espada longa de recruta equilibrada para cavaleiros.", "Sprites/Equipment/wp_sword_iron", knightOnly, ItemRarity.Common, atk: 5, reqStr: 10, reqDex: 8, reqLvl: 1, gold: 55);
                hero.equippedHelmet = CreateItem("Elmo de Ferro", ItemType.Helmet, "Elmo de aço simples com proteção nasal.", "Sprites/Equipment/helm_iron_knight", vanguardClasses, ItemRarity.Common, def: 2, hp: 5, reqStr: 10, reqLvl: 1, gold: 35);
                hero.equippedChest = CreateItem("Peitoral de Malha", ItemType.ChestArmor, "Couraça inicial sobre malha leve.", "Sprites/Equipment/chest_iron_knight", vanguardClasses, ItemRarity.Common, def: 4, hp: 12, reqStr: 10, reqLvl: 1, gold: 70);
                hero.equippedPants = CreateItem("Perneiras de Ferro", ItemType.Pants, "Grevas metálicas iniciais para as pernas.", "Sprites/Equipment/pants_iron_knight", vanguardClasses, ItemRarity.Common, def: 2, reqStr: 9, reqLvl: 1, gold: 40);
                hero.equippedBoots = CreateItem("Botas de Ferro", ItemType.Boots, "Botas reforçadas com chapas finas de aço.", "Sprites/Equipment/boots_iron_knight", vanguardClasses, ItemRarity.Common, def: 1, speed: 1, reqStr: 8, reqLvl: 1, gold: 30);
                hero.equippedGloves = CreateItem("Manoplas de Aço", ItemType.Gloves, "Manoplas simples para firmeza na empunhadura.", "Sprites/Equipment/gloves_iron_knight", vanguardClasses, ItemRarity.Common, def: 1, atk: 1, reqStr: 8, reqLvl: 1, gold: 30);
                break;

            case HeroClassType.HeavyKnight:
                hero.equippedWeapon = CreateItem("Montante de Aço", ItemType.Weapon, "Espada pesada de duas mãos para aprendizes de vanguarda.", "Sprites/Equipment/wp_greatsword_steel", heavyOnly, ItemRarity.Common, atk: 6, reqStr: 12, reqLvl: 1, gold: 70);
                hero.equippedHelmet = CreateItem("Elmo de Placas", ItemType.Helmet, "Elmo fechado inicial de aço batido.", "Sprites/Equipment/helm_heavy_plate", heavyOnly, ItemRarity.Common, def: 2, hp: 8, reqStr: 12, reqLvl: 1, gold: 45);
                hero.equippedChest = CreateItem("Couraça de Placas", ItemType.ChestArmor, "Armadura pesada básica para conter golpes frontais.", "Sprites/Equipment/chest_heavy_plate", heavyOnly, ItemRarity.Common, def: 4, hp: 18, reqStr: 12, reqLvl: 1, gold: 85);
                hero.equippedPants = CreateItem("Perneiras Blindadas", ItemType.Pants, "Placas iniciais para proteção das pernas.", "Sprites/Equipment/pants_heavy_plate", heavyOnly, ItemRarity.Common, def: 2, reqStr: 11, reqLvl: 1, gold: 45);
                hero.equippedBoots = CreateItem("Botas Blindadas", ItemType.Boots, "Sabatons pesados de ferro simples.", "Sprites/Equipment/boots_heavy_plate", heavyOnly, ItemRarity.Common, def: 2, reqStr: 10, reqLvl: 1, gold: 35);
                hero.equippedGloves = CreateItem("Manoplas Pesadas", ItemType.Gloves, "Manoplas resistentes para a linha de frente.", "Sprites/Equipment/gloves_heavy_plate", heavyOnly, ItemRarity.Common, def: 2, reqStr: 10, reqLvl: 1, gold: 35);
                break;

            case HeroClassType.Mage:
                hero.equippedWeapon = CreateItem("Cajado Arcano", ItemType.Weapon, "Cajado de aprendiz encimado por cristal rubi.", "Sprites/Equipment/wp_staff_arcane", mageOnly, ItemRarity.Common, atk: 1, matq: 6, reqInt: 12, reqLvl: 1, gold: 60);
                hero.equippedHelmet = CreateItem("Chapéu Arcano", ItemType.Helmet, "Chapéu pontudo tradicional de iniciado.", "Sprites/Equipment/helm_mage_hat", mageOnly, ItemRarity.Common, def: 1, mp: 10, matq: 1, reqInt: 10, reqLvl: 1, gold: 35);
                hero.equippedChest = CreateItem("Manto dos Elementos", ItemType.ChestArmor, "Túnica leve tecida com fios sutis de mana.", "Sprites/Equipment/chest_mage_robe", mageOnly, ItemRarity.Common, def: 3, hp: 10, mp: 15, matq: 2, reqInt: 12, reqLvl: 1, gold: 75);
                hero.equippedPants = CreateItem("Calças de Seda", ItemType.Pants, "Vestimenta simples que facilita o fluxo mágico.", "Sprites/Equipment/pants_mage_cloth", mageOnly, ItemRarity.Common, def: 1, mp: 8, reqInt: 9, reqLvl: 1, gold: 35);
                hero.equippedBoots = CreateItem("Sapatos Leves", ItemType.Boots, "Calçado confortável para longas caminhadas.", "Sprites/Equipment/boots_mage_shoes", mageOnly, ItemRarity.Common, def: 1, speed: 1, reqInt: 8, reqLvl: 1, gold: 30);
                hero.equippedGloves = CreateItem("Luvas Rúnicas", ItemType.Gloves, "Luvas místicas básicas de conjuração.", "Sprites/Equipment/gloves_mage_cloth", mageOnly, ItemRarity.Common, def: 1, mp: 8, matq: 1, reqInt: 8, reqLvl: 1, gold: 35);
                break;

            case HeroClassType.Rogue:
                hero.equippedWeapon = CreateItem("Adagas de Aço", ItemType.Weapon, "Punhal furtivo leve de iniciante.", "Sprites/Equipment/wp_dagger_steel", rogueOnly, ItemRarity.Common, atk: 4, crit: 4f, reqDex: 12, reqLvl: 1, gold: 60);
                hero.equippedHelmet = CreateItem("Capuz das Sombras", ItemType.Helmet, "Capuz simples de couro escuro.", "Sprites/Equipment/helm_rogue_hood", rogueOnly, ItemRarity.Common, def: 1, crit: 2f, reqDex: 10, reqLvl: 1, gold: 40);
                hero.equippedChest = CreateItem("Gibão de Couro", ItemType.ChestArmor, "Casaco leve de couro curtido.", "Sprites/Equipment/chest_rogue_leather", rogueOnly, ItemRarity.Common, def: 3, hp: 10, speed: 1, reqDex: 11, reqLvl: 1, gold: 70);
                hero.equippedPants = CreateItem("Calças Furtivas", ItemType.Pants, "Perneiras flexíveis de couro macio.", "Sprites/Equipment/pants_rogue_leather", rogueOnly, ItemRarity.Common, def: 2, speed: 1, reqDex: 9, reqLvl: 1, gold: 40);
                hero.equippedBoots = CreateItem("Botas Silenciosas", ItemType.Boots, "Botas leves para deslocamento discreto.", "Sprites/Equipment/boots_rogue_boots", rogueOnly, ItemRarity.Common, def: 1, speed: 2, reqDex: 9, reqLvl: 1, gold: 40);
                hero.equippedGloves = CreateItem("Luvas de Gatuno", ItemType.Gloves, "Braçadeiras de couro para maior precisão.", "Sprites/Equipment/gloves_rogue_leather", rogueOnly, ItemRarity.Common, def: 1, crit: 2f, reqDex: 8, reqLvl: 1, gold: 35);
                break;
        }

        hero.currentHP = hero.GetMaxHP();
        hero.currentMP = hero.GetMaxMP();
    }

    /// <summary>
    /// Inicializa a mochila da guilda contendo APENAS as Poções de Vida iniciais.
    /// </summary>
    public static void PopulateGuildBackpack(PartyManager party)
    {
        if (party == null) return;

        party.inventory.Clear();

        int initialPotions = 6;
        for (int i = 0; i < initialPotions; i++)
        {
            party.inventory.Add(CreateHealingPotion());
        }
    }

    /// <summary>
    /// Sorteia um equipamento calibrado pelo andar, nível do alvo/fonte e tabela de raridade.
    /// </summary>
    public static ItemData GenerateRandomDungeonDrop(int floor = 1, int targetItemLevel = -1, bool isTreasureChest = false, bool isBossDrop = false)
    {
        int safeFloor = Mathf.Max(1, floor);
        int reqLvl = ResolveDropItemLevel(safeFloor, targetItemLevel, isTreasureChest, isBossDrop);
        ItemRarity rarity = RollDropRarity(safeFloor, isTreasureChest, isBossDrop);

        DropTemplate[] pool = GetDropTemplates();
        DropTemplate t = pool[Random.Range(0, pool.Length)];

        float levelMult = 1f + ((reqLvl - 1) * 0.25f);
        float rarityMult = GetRarityMultiplier(rarity);
        float totalMult = levelMult * rarityMult;

        int atk = t.baseAtk > 0 ? Mathf.Max(1, Mathf.RoundToInt(t.baseAtk * totalMult)) : 0;
        int def = t.baseDef > 0 ? Mathf.Max(1, Mathf.RoundToInt(t.baseDef * totalMult)) : 0;
        int matq = t.baseMatq > 0 ? Mathf.Max(1, Mathf.RoundToInt(t.baseMatq * totalMult)) : 0;
        int hp = t.baseHp > 0 ? Mathf.Max(5, Mathf.RoundToInt(t.baseHp * totalMult)) : 0;
        int mp = t.baseMp > 0 ? Mathf.Max(5, Mathf.RoundToInt(t.baseMp * totalMult)) : 0;
        int spd = t.baseSpd > 0 ? Mathf.Max(1, Mathf.RoundToInt(t.baseSpd * (1f + (reqLvl - 1) * 0.12f) * Mathf.Sqrt(rarityMult))) : 0;
        float crit = t.baseCrit > 0f ? Mathf.Round(t.baseCrit * (1f + (reqLvl - 1) * 0.10f) * Mathf.Sqrt(rarityMult) * 10f) / 10f : 0f;

        int attrScale = Mathf.RoundToInt((reqLvl - 1) * 1.5f);
        int reqStr = t.baseReqStr > 0 ? t.baseReqStr + attrScale : 0;
        int reqDex = t.baseReqDex > 0 ? t.baseReqDex + attrScale : 0;
        int reqInt = t.baseReqInt > 0 ? t.baseReqInt + attrScale : 0;
        int gold = Mathf.Max(15, Mathf.RoundToInt(t.baseGold * totalMult));

        string sourceTag = isBossDrop ? "Troféu de Chefe" : (isTreasureChest ? "Tesouro de Baú" : "Espólio de Masmorra");
        string fullName = $"{t.name} (Nv. {reqLvl})";
        string fullDesc = $"{sourceTag} [{FormatRarityName(rarity)}] - Andar {safeFloor} (Nv. {reqLvl}). {t.desc}";

        return CreateItem(
            name: fullName,
            type: t.type,
            desc: fullDesc,
            iconPath: t.iconPath,
            allowedClasses: t.allowed,
            rarity: rarity,
            atk: atk,
            def: def,
            matq: matq,
            hp: hp,
            mp: mp,
            speed: spd,
            crit: crit,
            reqStr: reqStr,
            reqDex: reqDex,
            reqInt: reqInt,
            reqLvl: reqLvl,
            gold: gold
        );
    }

    public static string FormatRarityName(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon:  return "Incomum";
            case ItemRarity.Rare:      return "Raro";
            case ItemRarity.Epic:      return "Épico";
            case ItemRarity.Legendary: return "Lendário";
            default:                   return "Comum";
        }
    }

    private static int ResolveDropItemLevel(int floor, int targetItemLevel, bool isTreasureChest, bool isBossDrop)
    {
        int minCommonLvl = (floor - 1) * 10 + 2;
        int maxCommonLvl = (floor - 1) * 10 + 6;
        int bossLvl = (floor - 1) * 10 + 7;

        if (isBossDrop)
        {
            return targetItemLevel > 0 ? Mathf.Clamp(targetItemLevel, bossLvl, bossLvl + 1) : bossLvl;
        }

        if (isTreasureChest)
        {
            int chestMin = (floor - 1) * 10 + 3;
            return targetItemLevel > 0
                ? Mathf.Clamp(targetItemLevel, chestMin, maxCommonLvl)
                : Random.Range(chestMin, maxCommonLvl + 1);
        }

        return targetItemLevel > 0
            ? Mathf.Clamp(targetItemLevel, minCommonLvl, maxCommonLvl)
            : Random.Range(minCommonLvl, maxCommonLvl + 1);
    }

    private static ItemRarity RollDropRarity(int floor, bool isTreasureChest, bool isBossDrop)
    {
        float roll = Random.value;
        float floorBonus = Mathf.Min(0.30f, Mathf.Max(0, floor - 1) * 0.05f);

        if (isBossDrop)
        {
            // Boss no 1º Andar (Nv. 7): 48% Incomum, 44% Raro, 8% Épico
            float legChance = floor >= 2 ? Mathf.Min(0.15f, (floor - 1) * 0.03f) : 0f;
            float epicChance = 0.08f + floorBonus * 0.50f;
            float rareChance = 0.44f + floorBonus * 0.30f;

            if (roll < legChance) return ItemRarity.Legendary;
            if (roll < legChance + epicChance) return ItemRarity.Epic;
            if (roll < legChance + epicChance + rareChance) return ItemRarity.Rare;
            return ItemRarity.Uncommon;
        }

        if (isTreasureChest)
        {
            // Baú no 1º Andar: 42% Comum, 44% Incomum, 12% Raro, 2% Épico
            float epicChance = 0.02f + floorBonus * 0.25f;
            float rareChance = 0.12f + floorBonus * 0.40f;
            float uncommonChance = 0.44f;

            if (roll < epicChance) return ItemRarity.Epic;
            if (roll < epicChance + rareChance) return ItemRarity.Rare;
            if (roll < epicChance + rareChance + uncommonChance) return ItemRarity.Uncommon;
            return ItemRarity.Common;
        }

        // Monstro Comum no 1º Andar: 74% Comum, 22% Incomum, 4% Raro, 0% Épico/Lendário
        float mEpicChance = floor >= 2 ? floorBonus * 0.20f : 0f;
        float mRareChance = 0.04f + floorBonus * 0.35f;
        float mUncommonChance = 0.22f + floorBonus * 0.25f;

        if (roll < mEpicChance) return ItemRarity.Epic;
        if (roll < mEpicChance + mRareChance) return ItemRarity.Rare;
        if (roll < mEpicChance + mRareChance + mUncommonChance) return ItemRarity.Uncommon;
        return ItemRarity.Common;
    }

    private static float GetRarityMultiplier(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon:  return 1.22f;
            case ItemRarity.Rare:      return 1.48f;
            case ItemRarity.Epic:      return 1.78f;
            case ItemRarity.Legendary: return 2.15f;
            default:                   return 1.00f;
        }
    }

    private static DropTemplate[] GetDropTemplates()
    {
        HeroClassType[] knightOnly = new HeroClassType[] { HeroClassType.Knight };
        HeroClassType[] heavyOnly = new HeroClassType[] { HeroClassType.HeavyKnight };
        HeroClassType[] vanguard = new HeroClassType[] { HeroClassType.Knight, HeroClassType.HeavyKnight };
        HeroClassType[] mageOnly = new HeroClassType[] { HeroClassType.Mage };
        HeroClassType[] rogueOnly = new HeroClassType[] { HeroClassType.Rogue };

        return new DropTemplate[]
        {
            // Armas
            new DropTemplate { name = "Machado de Batalha", type = ItemType.Weapon, desc = "Lâmina pesada de ferro forjado para combate cerrado.", iconPath = "Sprites/Equipment/wp_axe_battle", allowed = vanguard, baseAtk = 6, baseReqStr = 11, baseGold = 75 },
            new DropTemplate { name = "Espada Temperada", type = ItemType.Weapon, desc = "Lâmina longa balanceada de aço temperado.", iconPath = "Sprites/Equipment/wp_sword_iron", allowed = knightOnly, baseAtk = 5, baseReqStr = 10, baseReqDex = 8, baseGold = 65 },
            new DropTemplate { name = "Montante Guardiã", type = ItemType.Weapon, desc = "Espada colossal de alto impacto físico.", iconPath = "Sprites/Equipment/wp_greatsword_steel", allowed = heavyOnly, baseAtk = 7, baseReqStr = 12, baseGold = 80 },
            new DropTemplate { name = "Cajado de Rubi", type = ItemType.Weapon, desc = "Cajado arcano amplificador de energia mágica.", iconPath = "Sprites/Equipment/wp_staff_arcane", allowed = mageOnly, baseAtk = 1, baseMatq = 7, baseReqInt = 11, baseGold = 70 },
            new DropTemplate { name = "Arco de Caçador", type = ItemType.Weapon, desc = "Arco longo flexível de precisão letal.", iconPath = "Sprites/Equipment/wp_bow_hunter", allowed = rogueOnly, baseAtk = 5, baseCrit = 4f, baseReqDex = 11, baseGold = 70 },
            new DropTemplate { name = "Adagas Sombrias", type = ItemType.Weapon, desc = "Par de lâminas rápidas perfeitas para críticos.", iconPath = "Sprites/Equipment/wp_dagger_steel", allowed = rogueOnly, baseAtk = 5, baseCrit = 5f, baseReqDex = 11, baseGold = 68 },

            // Peitorais
            new DropTemplate { name = "Peitoral de Aço", type = ItemType.ChestArmor, desc = "Couraça reforçada com placas sobrepostas.", iconPath = "Sprites/Equipment/chest_iron_knight", allowed = vanguard, baseDef = 4, baseHp = 14, baseReqStr = 10, baseGold = 75 },
            new DropTemplate { name = "Armadura de Bastião", type = ItemType.ChestArmor, desc = "Armadura pesada maciça forjada para tanques.", iconPath = "Sprites/Equipment/chest_heavy_plate", allowed = heavyOnly, baseDef = 5, baseHp = 20, baseReqStr = 12, baseGold = 90 },
            new DropTemplate { name = "Manto Rúnico", type = ItemType.ChestArmor, desc = "Túnica encantada com tramas protetoras de mana.", iconPath = "Sprites/Equipment/chest_mage_robe", allowed = mageOnly, baseDef = 3, baseHp = 10, baseMp = 16, baseMatq = 2, baseReqInt = 11, baseGold = 80 },
            new DropTemplate { name = "Gibão Sombrio", type = ItemType.ChestArmor, desc = "Casaco reforçado de couro maleável.", iconPath = "Sprites/Equipment/chest_rogue_leather", allowed = rogueOnly, baseDef = 3, baseHp = 12, baseSpd = 1, baseReqDex = 11, baseGold = 75 },

            // Elmos, Perneiras, Botas e Manoplas
            new DropTemplate { name = "Elmo de Vanguarda", type = ItemType.Helmet, desc = "Elmo metálico resistente a impactos.", iconPath = "Sprites/Equipment/helm_iron_knight", allowed = vanguard, baseDef = 2, baseHp = 8, baseReqStr = 9, baseGold = 45 },
            new DropTemplate { name = "Chapéu de Arquimago", type = ItemType.Helmet, desc = "Chapéu consagrado que amplia a reserva arcana.", iconPath = "Sprites/Equipment/helm_mage_hat", allowed = mageOnly, baseDef = 1, baseMp = 12, baseMatq = 1, baseReqInt = 10, baseGold = 45 },
            new DropTemplate { name = "Capuz de Espreitador", type = ItemType.Helmet, desc = "Capuz escuro que favorece ataques precisos.", iconPath = "Sprites/Equipment/helm_rogue_hood", allowed = rogueOnly, baseDef = 1, baseCrit = 2.5f, baseReqDex = 10, baseGold = 45 },
            new DropTemplate { name = "Grevas de Aço", type = ItemType.Pants, desc = "Proteção articulada para as pernas em batalha.", iconPath = "Sprites/Equipment/pants_iron_knight", allowed = vanguard, baseDef = 2, baseHp = 6, baseReqStr = 9, baseGold = 45 },
            new DropTemplate { name = "Botas de Assalto", type = ItemType.Boots, desc = "Botas leves para avanço rápido na masmorra.", iconPath = "Sprites/Equipment/boots_rogue_boots", allowed = rogueOnly, baseDef = 1, baseSpd = 2, baseReqDex = 9, baseGold = 42 },
            new DropTemplate { name = "Manoplas de Guerra", type = ItemType.Gloves, desc = "Manoplas pesadas que reforçam o golpe.", iconPath = "Sprites/Equipment/gloves_heavy_plate", allowed = vanguard, baseDef = 2, baseAtk = 1, baseReqStr = 9, baseGold = 45 }
        };
    }
}
