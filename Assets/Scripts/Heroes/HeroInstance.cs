using UnityEngine;

/// <summary>
/// Instância viva de um aventureiro da guilda.
/// Controla o nível atual, XP, vida (HP), mana (MP), equipamentos vestidos nos 6 slots
/// e calcula os atributos finais com os bônus de itens.
/// </summary>
[System.Serializable]
public class HeroInstance
{
    [Header("Dados do Arquétipo")]
    public HeroData heroData;

    [Header("Progressão de Nível")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    [Header("Recursos Vitais em Batalha")]
    public int currentHP;
    public int currentMP;

    [Header("Os 6 Slots de Equipamento")]
    public ItemData equippedWeapon;
    public ItemData equippedHelmet;
    public ItemData equippedChest;
    public ItemData equippedPants;
    public ItemData equippedBoots;
    public ItemData equippedGloves;

    public bool IsAlive => currentHP > 0;

    /// <summary>
    /// Construtor para inicializar o aventureiro a partir de um HeroData.
    /// </summary>
    public HeroInstance(HeroData data, int startingLevel = 1)
    {
        heroData = data;
        level = startingLevel;
        currentXP = 0;
        CalculateXPRequirement();

        currentHP = GetMaxHP();
        currentMP = GetMaxMP();
    }

    /// <summary>
    /// Calcula os atributos brutos atuais com base no nível do herói.
    /// </summary>
    public HeroAttributes GetCurrentAttributes()
    {
        int levelsGained = level - 1;
        return new HeroAttributes(
            heroData.baseAttributes.strength + (heroData.growthPerLevel.strength * levelsGained),
            heroData.baseAttributes.dexterity + (heroData.growthPerLevel.dexterity * levelsGained),
            heroData.baseAttributes.intelligence + (heroData.growthPerLevel.intelligence * levelsGained),
            heroData.baseAttributes.vitality + (heroData.growthPerLevel.vitality * levelsGained)
        );
    }

    #region Cálculos de Estatísticas de Combate (Base + Equipamentos)

    public int GetMaxHP()
    {
        int baseVal = heroData.CalculateMaxHP(level, GetCurrentAttributes());
        int equipBonus = GetEquipBonus(i => i.bonusMaxHP);
        return baseVal + equipBonus;
    }

    public int GetMaxMP()
    {
        int baseVal = heroData.CalculateMaxMP(level, GetCurrentAttributes());
        int equipBonus = GetEquipBonus(i => i.bonusMaxMP);
        return baseVal + equipBonus;
    }

    public int GetPhysicalAttack()
    {
        int baseVal = heroData.CalculatePhysicalAttack(GetCurrentAttributes());
        int equipBonus = GetEquipBonus(i => i.bonusAttack);
        return baseVal + equipBonus;
    }

    public int GetPhysicalDefense()
    {
        int baseVal = heroData.CalculatePhysicalDefense(GetCurrentAttributes());
        int equipBonus = GetEquipBonus(i => i.bonusDefense);
        return baseVal + equipBonus;
    }

    public int GetMagicAttack()
    {
        int baseVal = heroData.CalculateMagicAttack(GetCurrentAttributes());
        int equipBonus = GetEquipBonus(i => i.bonusMagicAttack);
        return baseVal + equipBonus;
    }

    public int GetSpeed()
    {
        int baseVal = heroData.CalculateSpeed(GetCurrentAttributes());
        int equipBonus = GetEquipBonus(i => i.bonusSpeed);
        return baseVal + equipBonus;
    }

    public float GetCritRate()
    {
        float baseVal = heroData.CalculateCritRate(GetCurrentAttributes());
        float equipBonus = 0f;
        ForEachEquippedItem(i => equipBonus += i.bonusCritRate);
        return baseVal + equipBonus;
    }

    #endregion

    #region Sistema de Equipamento (Validação de Requisitos Dark Souls)

    /// <summary>
    /// Tenta equipar um item. Se o herói não tiver os atributos necessários, a tentativa é recusada.
    /// </summary>
    public bool TryEquipItem(ItemData item, out string feedbackMessage)
    {
        if (item == null)
        {
            feedbackMessage = "Item inválido.";
            return false;
        }

        // Valida atributos Dark Souls
        if (!item.CanBeEquippedBy(level, GetCurrentAttributes(), out feedbackMessage))
        {
            return false;
        }

        // Equipa no slot correspondente
        switch (item.itemType)
        {
            case ItemType.Weapon:
                equippedWeapon = item;
                break;
            case ItemType.Helmet:
                equippedHelmet = item;
                break;
            case ItemType.ChestArmor:
                equippedChest = item;
                break;
            case ItemType.Pants:
                equippedPants = item;
                break;
            case ItemType.Boots:
                equippedBoots = item;
                break;
            case ItemType.Gloves:
                equippedGloves = item;
                break;
            default:
                feedbackMessage = "Este item não é um equipamento vestível.";
                return false;
        }

        feedbackMessage = $"{heroData.heroName} equipou {item.itemName} com sucesso!";
        return true;
    }

    public void UnequipItem(ItemType slot)
    {
        switch (slot)
        {
            case ItemType.Weapon: equippedWeapon = null; break;
            case ItemType.Helmet: equippedHelmet = null; break;
            case ItemType.ChestArmor: equippedChest = null; break;
            case ItemType.Pants: equippedPants = null; break;
            case ItemType.Boots: equippedBoots = null; break;
            case ItemType.Gloves: equippedGloves = null; break;
        }
    }

    private delegate int StatSelector(ItemData item);
    private int GetEquipBonus(StatSelector selector)
    {
        int total = 0;
        ForEachEquippedItem(item => total += selector(item));
        return total;
    }

    private void ForEachEquippedItem(System.Action<ItemData> action)
    {
        if (equippedWeapon != null) action(equippedWeapon);
        if (equippedHelmet != null) action(equippedHelmet);
        if (equippedChest != null) action(equippedChest);
        if (equippedPants != null) action(equippedPants);
        if (equippedBoots != null) action(equippedBoots);
        if (equippedGloves != null) action(equippedGloves);
    }

    #endregion

    #region Vida, Dano e Progressão de Nível

    public void TakeDamage(int rawDamage, bool isMagic = false)
    {
        int defense = isMagic ? Mathf.RoundToInt(GetMagicAttack() * 0.4f) : GetPhysicalDefense();
        int finalDamage = Mathf.Max(1, rawDamage - defense);

        currentHP -= finalDamage;
        if (currentHP < 0) currentHP = 0;

        Debug.Log($"{heroData.heroName} recebeu {finalDamage} de dano! (HP restante: {currentHP}/{GetMaxHP()})");
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(GetMaxHP(), currentHP + amount);
    }

    public void GainXP(int amount)
    {
        currentXP += amount;
        Debug.Log($"{heroData.heroName} ganhou {amount} XP! ({currentXP}/{xpToNextLevel})");

        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        level++;
        CalculateXPRequirement();

        // Cura completa ao subir de nível (recompensa clássica de RPG)
        currentHP = GetMaxHP();
        currentMP = GetMaxMP();

        Debug.Log($"⭐ LEVEL UP! {heroData.heroName} agora é Nível {level}! Atributos aumentados!");
    }

    private void CalculateXPRequirement()
    {
        // Fórmula de progressão de XP: escala suavemente
        xpToNextLevel = Mathf.RoundToInt(100 * Mathf.Pow(level, 1.35f));
    }

    #endregion
}
