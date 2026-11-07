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

        InitializeDefaultSkills();
    }

    [Header("Habilidades e Ataques do Herói")]
    public System.Collections.Generic.List<HeroSkill> availableSkills = new System.Collections.Generic.List<HeroSkill>();

    /// <summary>
    /// Inicializa os ataques básicos e as habilidades iniciais da classe do aventureiro.
    /// </summary>
    public void InitializeDefaultSkills()
    {
        if (availableSkills == null)
            availableSkills = new System.Collections.Generic.List<HeroSkill>();
        else
            availableSkills.Clear();

        // 1 e 2. Habilidades específicas da Classe (incluindo ícones Raven)
        switch (heroClass)
        {
            case HeroClassType.Knight:
                availableSkills.Add(new HeroSkill("Ataque Básico", "Golpe físico padrão utilizando a espada temperada.", 0, 1.0f, false, 0f, "Sprites/Icons/skill_knight_slash"));
                availableSkills.Add(new HeroSkill("Golpe Pesado", "Concentra força na lâmina desferindo um ataque com 160% de dano físico.", 10, 1.6f, false, 5f, "Sprites/Icons/skill_knight_heavy"));
                availableSkills.Add(new HeroSkill("Investida com Escudo", "Avança com o escudo, causando 130% de dano com alta chance crítica.", 14, 1.3f, false, 20f, "Sprites/Icons/skill_knight_shield"));
                break;

            case HeroClassType.HeavyKnight:
                availableSkills.Add(new HeroSkill("Ataque Básico", "Golpe contundente com espada montante de ferro.", 0, 1.0f, false, 0f, "Sprites/Icons/skill_heavy_slash"));
                availableSkills.Add(new HeroSkill("Pancada de Ferro", "Impacto esmagador aproveitando o peso da armadura (150% de dano físico).", 10, 1.5f, false, 0f, "Sprites/Icons/skill_heavy_bash"));
                availableSkills.Add(new HeroSkill("Fenda Sísmica", "Golpe brutal no solo causando 190% de impacto físico na criatura.", 16, 1.9f, false, 5f, "Sprites/Icons/skill_heavy_quake"));
                break;

            case HeroClassType.Mage:
                availableSkills.Add(new HeroSkill("Ataque Básico", "Projétil de energia concentrada disparado pelo cajado.", 0, 1.0f, true, 0f, "Sprites/Icons/skill_mage_bolt"));
                availableSkills.Add(new HeroSkill("Bola de Fogo", "Esfera incandescente causando 175% de dano mágico baseado na Inteligência.", 12, 1.75f, true, 5f, "Sprites/Icons/skill_mage_fireball"));
                availableSkills.Add(new HeroSkill("Descarga Elétrica", "Raio arcano que atravessa o monstro causando 220% de dano mágico.", 18, 2.2f, true, 10f, "Sprites/Icons/skill_mage_lightning"));
                break;

            case HeroClassType.Rogue:
                availableSkills.Add(new HeroSkill("Ataque Básico", "Corte ágil com lâmina curta afiada.", 0, 1.0f, false, 0f, "Sprites/Icons/skill_rogue_stab"));
                availableSkills.Add(new HeroSkill("Ataque Furtivo", "Golpe certeiro em pontos fracos com 150% de dano e +35% de chance crítica.", 10, 1.5f, false, 35f, "Sprites/Icons/skill_rogue_sneak"));
                availableSkills.Add(new HeroSkill("Lâmina Penetrante", "Perfuração rápida causando 200% de dano físico crítico.", 16, 2.0f, false, 15f, "Sprites/Icons/skill_rogue_pierce"));
                break;

            default:
                availableSkills.Add(new HeroSkill("Ataque Básico", "Golpe físico padrão utilizando a arma equipada.", 0, 1.0f, false, 0f, "Sprites/Icons/skill_knight_slash"));
                break;
        }
    }

    /// <summary>
    /// Calcula os atributos brutos atuais com base no nível do herói somados aos pontos livres distribuídos.
    /// </summary>
    public HeroAttributes GetCurrentAttributes()
    {
        if (heroData == null)
            return new HeroAttributes(10 + allocatedStrength, 10 + allocatedDexterity, 10 + allocatedIntelligence, 10 + allocatedVitality);

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
        int baseVal = heroData != null ? heroData.CalculateMaxHP(level, GetCurrentAttributes()) : 100;
        int equipBonus = GetEquipBonus(i => i.bonusMaxHP);
        return baseVal + equipBonus;
    }

    public int GetMaxMP()
    {
        int baseVal = heroData != null ? heroData.CalculateMaxMP(level, GetCurrentAttributes()) : 50;
        int equipBonus = GetEquipBonus(i => i.bonusMaxMP);
        return baseVal + equipBonus;
    }

    public int GetPhysicalAttack()
    {
        int baseVal = heroData != null ? heroData.CalculatePhysicalAttack(GetCurrentAttributes()) : 15;
        int equipBonus = GetEquipBonus(i => i.bonusAttack);
        return baseVal + equipBonus;
    }

    public int GetPhysicalDefense()
    {
        int baseVal = heroData != null ? heroData.CalculatePhysicalDefense(GetCurrentAttributes()) : 8;
        int equipBonus = GetEquipBonus(i => i.bonusDefense);
        return baseVal + equipBonus;
    }

    public int GetMagicAttack()
    {
        int baseVal = heroData != null ? heroData.CalculateMagicAttack(GetCurrentAttributes()) : 15;
        int equipBonus = GetEquipBonus(i => i.bonusMagicAttack);
        return baseVal + equipBonus;
    }

    public int GetSpeed()
    {
        int baseVal = heroData != null ? heroData.CalculateSpeed(GetCurrentAttributes()) : 10;
        int equipBonus = GetEquipBonus(i => i.bonusSpeed);
        return baseVal + equipBonus;
    }

    public float GetCritRate()
    {
        float baseVal = heroData != null ? heroData.CalculateCritRate(GetCurrentAttributes()) : 5f;
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

        // Valida classe e atributos Dark Souls
        if (!item.CanBeEquippedBy(this, out feedbackMessage))
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
