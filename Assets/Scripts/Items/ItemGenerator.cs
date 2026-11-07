using UnityEngine;

/// <summary>
/// Gerador procedural e fábrica de equipamentos, armas e consumíveis com ícones do Raven Fantasy.
/// Define compatibilidade de classes e requisitos de atributos equilibrados para cada arquétipo.
/// </summary>
public static class ItemGenerator
{
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
            desc: "Frasco de essência carmesim que restaura 45% do HP máximo do aventureiro.",
            iconPath: "Sprites/Icons/potion_hp",
            allowedClasses: null,
            rarity: ItemRarity.Common,
            healHP: 50,
            gold: 25
        );
    }

    /// <summary>
    /// Equipa o conjunto completo de equipamentos iniciais para o herói conforme sua classe,
    /// com compatibilidade de classe e requisitos coerentes com os atributos de Nível 1.
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
                hero.equippedWeapon = CreateItem("Espada de Ferro", ItemType.Weapon, "Espada longa equilibrada para cavaleiros.", "Sprites/Equipment/wp_sword_iron", knightOnly, atk: 8, reqStr: 10, reqDex: 8, gold: 80);
                hero.equippedHelmet = CreateItem("Elmo de Ferro", ItemType.Helmet, "Elmo de aço com proteção nasal.", "Sprites/Equipment/helm_iron_knight", vanguardClasses, def: 3, hp: 10, reqStr: 10, gold: 50);
                hero.equippedChest = CreateItem("Peitoral de Malha", ItemType.ChestArmor, "Couraça de ferro sobre malha de aço.", "Sprites/Equipment/chest_iron_knight", vanguardClasses, def: 6, hp: 20, reqStr: 10, gold: 110);
                hero.equippedPants = CreateItem("Perneiras de Ferro", ItemType.Pants, "Grevas metálicas resistentes para as pernas.", "Sprites/Equipment/pants_iron_knight", vanguardClasses, def: 4, reqStr: 9, gold: 60);
                hero.equippedBoots = CreateItem("Botas de Ferro", ItemType.Boots, "Botas reforçadas com chapas de aço.", "Sprites/Equipment/boots_iron_knight", vanguardClasses, def: 2, speed: 1, reqStr: 8, gold: 45);
                hero.equippedGloves = CreateItem("Manoplas de Aço", ItemType.Gloves, "Manoplas articuladas para firmeza no combate.", "Sprites/Equipment/gloves_iron_knight", vanguardClasses, def: 2, atk: 1, reqStr: 8, gold: 45);
                break;

            case HeroClassType.HeavyKnight:
                hero.equippedWeapon = CreateItem("Montante de Aço", ItemType.Weapon, "Espada colossal de duas mãos de alto impacto.", "Sprites/Equipment/wp_greatsword_steel", heavyOnly, atk: 14, reqStr: 12, gold: 120);
                hero.equippedHelmet = CreateItem("Elmo de Placas", ItemType.Helmet, "Elmo fechado blindado de aço maciço.", "Sprites/Equipment/helm_heavy_plate", heavyOnly, def: 5, hp: 15, reqStr: 12, gold: 75);
                hero.equippedChest = CreateItem("Couraça de Placas", ItemType.ChestArmor, "Armadura pesada completa capaz de defletir golpes.", "Sprites/Equipment/chest_heavy_plate", heavyOnly, def: 10, hp: 35, reqStr: 12, gold: 160);
                hero.equippedPants = CreateItem("Perneiras Blindadas", ItemType.Pants, "Placas pesadas para proteção das pernas.", "Sprites/Equipment/pants_heavy_plate", heavyOnly, def: 6, reqStr: 11, gold: 85);
                hero.equippedBoots = CreateItem("Botas Blindadas", ItemType.Boots, "Sabatons pesados de metal forjado.", "Sprites/Equipment/boots_heavy_plate", heavyOnly, def: 3, reqStr: 10, gold: 60);
                hero.equippedGloves = CreateItem("Manoplas Pesadas", ItemType.Gloves, "Manoplas blindadas para a linha de frente.", "Sprites/Equipment/gloves_heavy_plate", heavyOnly, def: 3, reqStr: 10, gold: 60);
                break;

            case HeroClassType.Mage:
                hero.equippedWeapon = CreateItem("Cajado Arcano", ItemType.Weapon, "Cajado canalizador encimado por cristal rubi.", "Sprites/Equipment/wp_staff_arcane", mageOnly, atk: 2, matq: 12, reqInt: 12, gold: 100);
                hero.equippedHelmet = CreateItem("Chapéu Arcano", ItemType.Helmet, "Chapéu pontudo tradicional de mago.", "Sprites/Equipment/helm_mage_hat", mageOnly, def: 1, mp: 15, matq: 2, reqInt: 10, gold: 50);
                hero.equippedChest = CreateItem("Manto dos Elementos", ItemType.ChestArmor, "Túnica arcana tecida com fios de mana.", "Sprites/Equipment/chest_mage_robe", mageOnly, def: 3, mp: 30, matq: 5, reqInt: 12, gold: 130);
                hero.equippedPants = CreateItem("Calças de Seda", ItemType.Pants, "Vestimenta leve que amplia o fluxo mágico.", "Sprites/Equipment/pants_mage_cloth", mageOnly, def: 2, mp: 10, reqInt: 9, gold: 50);
                hero.equippedBoots = CreateItem("Sapatos Leves", ItemType.Boots, "Calçado encantado confortável e ágil.", "Sprites/Equipment/boots_mage_shoes", mageOnly, def: 1, speed: 2, reqInt: 8, gold: 40);
                hero.equippedGloves = CreateItem("Luvas Rúnicas", ItemType.Gloves, "Luvas místicas bordadas para conjuração.", "Sprites/Equipment/gloves_mage_cloth", mageOnly, def: 1, mp: 10, matq: 2, reqInt: 8, gold: 45);
                break;

            case HeroClassType.Rogue:
                hero.equippedWeapon = CreateItem("Adagas de Aço", ItemType.Weapon, "Punhal furtivo perfeitamente balanceado.", "Sprites/Equipment/wp_dagger_steel", rogueOnly, atk: 7, crit: 5f, reqDex: 12, gold: 90);
                hero.equippedHelmet = CreateItem("Capuz das Sombras", ItemType.Helmet, "Capuz de couro escuro que oculta o rosto.", "Sprites/Equipment/helm_rogue_hood", rogueOnly, def: 2, crit: 3f, reqDex: 10, gold: 55);
                hero.equippedChest = CreateItem("Gibão de Couro", ItemType.ChestArmor, "Casaco de couro batido leve e flexível.", "Sprites/Equipment/chest_rogue_leather", rogueOnly, def: 5, hp: 15, speed: 2, reqDex: 11, gold: 110);
                hero.equippedPants = CreateItem("Calças Furtivas", ItemType.Pants, "Perneiras de couro macio sem ruído.", "Sprites/Equipment/pants_rogue_leather", rogueOnly, def: 3, speed: 1, reqDex: 9, gold: 60);
                hero.equippedBoots = CreateItem("Botas Silenciosas", ItemType.Boots, "Botas leves para passos imperceptíveis.", "Sprites/Equipment/boots_rogue_boots", rogueOnly, def: 2, speed: 3, reqDex: 9, gold: 55);
                hero.equippedGloves = CreateItem("Luvas de Gatuno", ItemType.Gloves, "Braçadeiras de couro para precisão crítica.", "Sprites/Equipment/gloves_rogue_leather", rogueOnly, def: 2, crit: 2f, reqDex: 8, gold: 50);
                break;
        }

        // Atualiza a vida e mana máximas com os bônus dos equipamentos
        hero.currentHP = hero.GetMaxHP();
        hero.currentMP = hero.GetMaxMP();
    }

    /// <summary>
    /// Inicializa a mochila da guilda contendo APENAS os itens de cura (Poções de Vida) iniciais.
    /// </summary>
    public static void PopulateGuildBackpack(PartyManager party)
    {
        if (party == null) return;

        party.inventory.Clear();

        // Começa apenas com as Poções de Cura na mochila
        int initialPotions = 6;
        for (int i = 0; i < initialPotions; i++)
        {
            party.inventory.Add(CreateHealingPotion());
        }
    }

    /// <summary>
    /// Sorteia um espólio (Poção de Vida, Arma ou Armadura) quando um monstro é derrotado ou um baú é aberto.
    /// </summary>
    public static ItemData GenerateRandomDungeonDrop()
    {
        // 40% de chance do drop ser uma Poção de Vida (que agrupa no mesmo slot da mochila!)
        if (Random.value < 0.40f)
        {
            return CreateHealingPotion();
        }

        HeroClassType[] knightOnly = new HeroClassType[] { HeroClassType.Knight };
        HeroClassType[] heavyOnly = new HeroClassType[] { HeroClassType.HeavyKnight };
        HeroClassType[] vanguard = new HeroClassType[] { HeroClassType.Knight, HeroClassType.HeavyKnight };
        HeroClassType[] mageOnly = new HeroClassType[] { HeroClassType.Mage };
        HeroClassType[] rogueOnly = new HeroClassType[] { HeroClassType.Rogue };

        int roll = Random.Range(0, 10);
        switch (roll)
        {
            case 0:
                return CreateItem("Machado de Batalha", ItemType.Weapon, "Lâmina pesada de ferro escuro para guerreiros.", "Sprites/Equipment/wp_axe_battle", vanguard, ItemRarity.Uncommon, atk: 12, reqStr: 12, gold: 110);
            case 1:
                return CreateItem("Arco de Caçador", ItemType.Weapon, "Arco longo de madeira flexível com alta precisão.", "Sprites/Equipment/wp_bow_hunter", rogueOnly, ItemRarity.Uncommon, atk: 9, crit: 6f, reqDex: 12, gold: 100);
            case 2:
                return CreateItem("Espada de Ferro", ItemType.Weapon, "Espada longa equilibrada para cavaleiros.", "Sprites/Equipment/wp_sword_iron", knightOnly, ItemRarity.Common, atk: 8, reqStr: 10, reqDex: 8, gold: 80);
            case 3:
                return CreateItem("Montante de Aço", ItemType.Weapon, "Espada colossal de duas mãos de alto impacto.", "Sprites/Equipment/wp_greatsword_steel", heavyOnly, ItemRarity.Uncommon, atk: 14, reqStr: 12, gold: 120);
            case 4:
                return CreateItem("Cajado Arcano", ItemType.Weapon, "Cajado canalizador encimado por cristal rubi.", "Sprites/Equipment/wp_staff_arcane", mageOnly, ItemRarity.Common, atk: 2, matq: 12, reqInt: 12, gold: 100);
            case 5:
                return CreateItem("Adagas de Aço", ItemType.Weapon, "Punhal furtivo perfeitamente balanceado.", "Sprites/Equipment/wp_dagger_steel", rogueOnly, ItemRarity.Common, atk: 7, crit: 5f, reqDex: 12, gold: 90);
            case 6:
                return CreateItem("Peitoral de Malha", ItemType.ChestArmor, "Couraça de ferro sobre malha de aço.", "Sprites/Equipment/chest_iron_knight", vanguard, ItemRarity.Common, def: 6, hp: 20, reqStr: 10, gold: 110);
            case 7:
                return CreateItem("Couraça de Placas", ItemType.ChestArmor, "Armadura pesada completa capaz de defletir golpes.", "Sprites/Equipment/chest_heavy_plate", heavyOnly, ItemRarity.Rare, def: 10, hp: 35, reqStr: 12, gold: 160);
            case 8:
                return CreateItem("Manto dos Elementos", ItemType.ChestArmor, "Túnica arcana tecida com fios de mana.", "Sprites/Equipment/chest_mage_robe", mageOnly, ItemRarity.Uncommon, def: 3, mp: 30, matq: 5, reqInt: 12, gold: 130);
            default:
                return CreateItem("Gibão de Couro", ItemType.ChestArmor, "Casaco de couro batido leve e flexível.", "Sprites/Equipment/chest_rogue_leather", rogueOnly, ItemRarity.Common, def: 5, hp: 15, speed: 2, reqDex: 11, gold: 110);
        }
    }
}
