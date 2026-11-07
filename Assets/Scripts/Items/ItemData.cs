using UnityEngine;

/// <summary>
/// Tipos de itens e equipamentos (os 6 slots do personagem + consumíveis).
/// </summary>
public enum ItemType
{
    Weapon,     // Arma (Espadas, Machados, Cajados, Adagas, Arcos)
    Helmet,     // Capacete / Elmo
    ChestArmor, // Peitoral / Armadura de Tronco
    Pants,      // Calças / Perneiras
    Boots,      // Botas
    Gloves,     // Manoplas / Luvas
    Consumable  // Poções de cura, elixires de mana, etc.
}

public enum ItemRarity
{
    Common,     // Comum (Branco)
    Uncommon,   // Incomum (Verde)
    Rare,       // Raro (Azul)
    Epic,       // Épico (Roxo)
    Legendary   // Lendário (Dourado - Estilo 5★ Pick Me Up!)
}

/// <summary>
/// Modelo ScriptableObject para os equipamentos e itens do jogo.
/// Possui requisitos de atributos no estilo Dark Souls:
/// um herói só pode equipar se atender à Força, Destreza, Inteligência ou Vitalidade exigidas!
/// </summary>
[CreateAssetMenu(fileName = "NovoItem", menuName = "Dungeon Master RPG/Novo Item")]
public class ItemData : ScriptableObject
{
    [Header("Identificação")]
    public string itemName = "Novo Item";
    [TextArea(2, 3)]
    public string description = "Descrição do equipamento ou item.";
    public ItemType itemType = ItemType.Weapon;
    public ItemRarity rarity = ItemRarity.Common;
    public Sprite icon;
    [Tooltip("Caminho alternativo nos Resources (ex: 'Sprites/Equipment/wp_sword_iron')")]
    public string iconPath = "";

    public Sprite GetIcon()
    {
        if (icon != null) return icon;
        if (!string.IsNullOrEmpty(iconPath))
        {
            icon = Resources.Load<Sprite>(iconPath);
        }
        return icon;
    }

    [Header("Economia")]
    [Tooltip("Valor em moedas de ouro")]
    public int goldValue = 50;

    [Header("Requisitos de Classe e Atributos (Estilo Dark Souls)")]
    [Tooltip("Classes permitidas para usar este item (vazio = todas as classes)")]
    public HeroClassType[] allowedClasses;

    [Tooltip("Força mínima necessária para empunhar/usar")]
    public int requiredStrength = 0;

    [Tooltip("Destreza mínima necessária")]
    public int requiredDexterity = 0;

    [Tooltip("Inteligência mínima necessária")]
    public int requiredIntelligence = 0;

    [Tooltip("Nível mínimo do herói")]
    public int requiredLevel = 1;

    [Header("Efeitos de Consumível (Poções)")]
    public int healHP = 0;
    public int healMP = 0;

    [Header("Bônus de Estatísticas (Concedidos quando equipado)")]
    public int bonusAttack = 0;
    public int bonusDefense = 0;
    public int bonusMagicAttack = 0;
    public int bonusMaxHP = 0;
    public int bonusMaxMP = 0;
    public int bonusSpeed = 0;
    public float bonusCritRate = 0f;

    public bool IsClassAllowed(HeroClassType heroClass)
    {
        if (allowedClasses == null || allowedClasses.Length == 0) return true;
        for (int i = 0; i < allowedClasses.Length; i++)
        {
            if (allowedClasses[i] == heroClass) return true;
        }
        return false;
    }

    public string GetAllowedClassesText()
    {
        if (allowedClasses == null || allowedClasses.Length == 0) return "Todas as Classes";
        System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string>();
        for (int i = 0; i < allowedClasses.Length; i++)
        {
            names.Add(FormatClassName(allowedClasses[i]));
        }
        return string.Join(", ", names);
    }

    public static string FormatClassName(HeroClassType c)
    {
        switch (c)
        {
            case HeroClassType.Knight: return "Cavaleiro";
            case HeroClassType.HeavyKnight: return "Cav. Pesado";
            case HeroClassType.Mage: return "Mago";
            case HeroClassType.Rogue: return "Ladino";
            default: return c.ToString();
        }
    }

    /// <summary>
    /// Verifica se o herói atende à classe e aos atributos exigidos pelo equipamento.
    /// </summary>
    public bool CanBeEquippedBy(HeroInstance hero, out string reason)
    {
        if (hero == null)
        {
            reason = "Herói inválido.";
            return false;
        }

        if (!IsClassAllowed(hero.heroClass))
        {
            reason = $"Classe incompatível ({FormatClassName(hero.heroClass)}). Exclusivo para: {GetAllowedClassesText()}.";
            return false;
        }

        return CanBeEquippedBy(hero.level, hero.GetCurrentAttributes(), out reason);
    }

    /// <summary>
    /// Verifica se um herói atende a todos os requisitos de Dark Souls para equipar este item.
    /// </summary>
    public bool CanBeEquippedBy(int heroLevel, HeroAttributes heroAttrs, out string reason)
    {
        if (heroLevel < requiredLevel)
        {
            reason = $"Nível insuficiente! Requer Nv. {requiredLevel}.";
            return false;
        }

        if (heroAttrs.strength < requiredStrength)
        {
            reason = $"Força insuficiente! Requer {requiredStrength} FOR (Atual: {heroAttrs.strength}).";
            return false;
        }

        if (heroAttrs.dexterity < requiredDexterity)
        {
            reason = $"Destreza insuficiente! Requer {requiredDexterity} DES (Atual: {heroAttrs.dexterity}).";
            return false;
        }

        if (heroAttrs.intelligence < requiredIntelligence)
        {
            reason = $"Inteligência insuficiente! Requer {requiredIntelligence} INT (Atual: {heroAttrs.intelligence}).";
            return false;
        }

        reason = "Compatível e apto para equipar!";
        return true;
    }
}
