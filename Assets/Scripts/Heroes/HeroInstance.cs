using UnityEngine;

/// <summary>
/// Instância viva de um aventureiro da guilda.
/// Controla o nível atual, XP, vida (HP), mana (MP), equipamentos vestidos nos 6 slots
/// e calcula os atributos finais com os bônus de itens.
/// </summary>
[System.Serializable]
public class HeroInstance
{
    [Header("Arquétipo do Herói")]
    public HeroData heroData;

    [Header("Identidade e Estrelas (Pick Me Up!)")]
    public string heroName;
    public HeroClassType heroClass;
    [Range(1, 5)]
    public int currentStars = 1;
    public bool isGoldStar = false;

    [Header("Progressão de Nível")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    public int MaxLevel => HeroData.GetMaxLevelForStars(currentStars);
    public bool IsAtMaxLevel => level >= MaxLevel;

    [Header("Recursos Vitais em Batalha")]
    public int currentHP;
    public int currentMP;

    [Header("Consumíveis (Poções de Cura)")]
    public int healingPotions = 6;

    [Header("Distribuição Livre de Atributos (Level Up)")]
    public int unallocatedAttributePoints = 0;
    public int allocatedStrength = 0;
    public int allocatedDexterity = 0;
    public int allocatedIntelligence = 0;
    public int allocatedVitality = 0;

    [Header("Estado do Turno na Batalha")]
    public bool hasActedThisRound = false;

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
        heroName = data.heroName;
        heroClass = data.heroClass;
        currentStars = data.stars;
        isGoldStar = data.isGoldStar;

        level = startingLevel;
        currentXP = 0;
        healingPotions = 6;
        unallocatedAttributePoints = 0;
        allocatedStrength = 0;
        allocatedDexterity = 0;
        allocatedIntelligence = 0;
        allocatedVitality = 0;
        hasActedThisRound = false;

        CalculateXPRequirement();

        currentHP = GetMaxHP();
        currentMP = GetMaxMP();
    }

    /// <summary>
    /// Calcula os atributos brutos atuais com base no nível do herói somados aos pontos livres distribuídos.
    /// </summary>
    public HeroAttributes GetCurrentAttributes()
    {
        int levelsGained = level - 1;
        return new HeroAttributes(
            heroData.baseAttributes.strength + (heroData.growthPerLevel.strength * levelsGained) + allocatedStrength,
            heroData.baseAttributes.dexterity + (heroData.growthPerLevel.dexterity * levelsGained) + allocatedDexterity,
            heroData.baseAttributes.intelligence + (heroData.growthPerLevel.intelligence * levelsGained) + allocatedIntelligence,
            heroData.baseAttributes.vitality + (heroData.growthPerLevel.vitality * levelsGained) + allocatedVitality
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

    /// <summary>
    /// Usa uma Poção de Cura se disponível.
    /// Cura balanceada: 35% da Vida Máxima + 10 pontos.
    /// </summary>
    public bool UseHealingPotion(out int amountHealed)
    {
        amountHealed = 0;
        if (healingPotions <= 0)
        {
            return false;
        }

        amountHealed = Mathf.RoundToInt(GetMaxHP() * 0.35f) + 10;
        healingPotions--;
        Heal(amountHealed);
        Debug.Log($"🧪 {heroName} consumiu uma Poção de Cura (+{amountHealed} HP)! Restam: {healingPotions}/6");
        return true;
    }

    /// <summary>
    /// Distribui 1 ponto livre de atributo ganho por subir de nível.
    /// </summary>
    public bool AllocateAttributePoint(string attributeName)
    {
        if (unallocatedAttributePoints <= 0) return false;

        switch (attributeName.ToUpper())
        {
            case "STR":
            case "FORÇA":
                allocatedStrength++;
                break;
            case "DEX":
            case "DESTREZA":
                allocatedDexterity++;
                break;
            case "INT":
            case "INTELIGÊNCIA":
                allocatedIntelligence++;
                break;
            case "VIT":
            case "VITALIDADE":
                allocatedVitality++;
                currentHP = Mathf.Min(GetMaxHP(), currentHP + 15);
                break;
            default:
                return false;
        }

        unallocatedAttributePoints--;
        return true;
    }

    public void GainXP(int amount)
    {
        if (IsAtMaxLevel)
        {
            Debug.Log($"⚠️ {heroName} atingiu o limite de nível {MaxLevel} ({currentStars}★)! Promova a estrela do herói para continuar evoluindo!");
            return;
        }

        currentXP += amount;
        Debug.Log($"{heroName} ganhou {amount} XP! ({currentXP}/{xpToNextLevel})");

        while (currentXP >= xpToNextLevel && !IsAtMaxLevel)
        {
            LevelUp();
        }

        if (IsAtMaxLevel)
        {
            currentXP = 0;
            Debug.Log($"🛑 LIMITE ALCANÇADO: {heroName} atingiu o Nível Máximo {MaxLevel} para {currentStars} Estrelas!");
        }
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        level++;
        CalculateXPRequirement();

        // Concede 3 pontos livres de atributo para o jogador administrar!
        unallocatedAttributePoints += 3;

        // Cura completa ao subir de nível (recompensa clássica de RPG)
        currentHP = GetMaxHP();
        currentMP = GetMaxMP();

        Debug.Log($"⭐ LEVEL UP! {heroName} agora é Nível {level}! +3 Pontos de Atributo para distribuir!");
    }

    /// <summary>
    /// Promove o herói para o próximo nível de estrela (Pick Me Up!).
    /// Aumenta o limite máximo de nível em +20 e concede um bônus permanente de poder!
    /// </summary>
    public bool PromoteStar(out string message)
    {
        if (currentStars >= 5)
        {
            message = $"{heroName} já atingiu o grau máximo de 5 Estrelas!";
            return false;
        }

        if (!IsAtMaxLevel)
        {
            message = $"{heroName} precisa atingir o nível máximo atual ({MaxLevel}) antes de ascender para {currentStars + 1}★!";
            return false;
        }

        currentStars++;
        message = $"✨ ASCENSÃO DE ESTRELA! {heroName} agora é um herói de {currentStars}★! Limite de nível expandido para Nv. {MaxLevel}!";
        Debug.Log(message);
        return true;
    }

    private void CalculateXPRequirement()
    {
        // Fórmula de progressão de XP: escala suavemente
        xpToNextLevel = Mathf.RoundToInt(100 * Mathf.Pow(level, 1.35f));
    }

    #endregion
}
