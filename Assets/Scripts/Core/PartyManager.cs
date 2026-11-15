using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O Cérebro da Guilda de Aventureiros (Party & Quartel).
/// Gerencia:
/// - O Grupo de Expedição Ativo (activeParty: até 4 heróis enviados para a Dungeon)
/// - Os Heróis na Reserva do Quartel da Guilda (reserveHeroes: aventureiros disponíveis para troca/escalação)
/// - O Armazém / Depósito Compartilhado da Guilda (inventory: equipamentos e itens adquiridos)
/// - As Moedas de Ouro e distribuição de XP.
/// </summary>
public class PartyManager : MonoBehaviour
{
    private static PartyManager _instance;
    public static PartyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<PartyManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    public const int MAX_ACTIVE_PARTY_SIZE = 4;

    [Header("Configuração de Convocação")]
    [Tooltip("Se ativado, convoca aventureiros aleatórios proceduralmente no início (estilo Pick Me Up!)")]
    public bool generateRandomPartyOnStart = true;

    [Header("Modelos dos Heróis Pré-definidos (Opcional)")]
    [Tooltip("Se quiser definir heróis fixos em vez de aleatórios, coloque os HeroData aqui")]
    public HeroData[] initialHeroArchetypes = new HeroData[4];

    [Header("Esquadrão de Expedição (Até 4 Heróis enviados para a Dungeon)")]
    public List<HeroInstance> activeParty = new List<HeroInstance>();

    [Header("Reserva do Quartel da Guilda (Heróis aguardando na Guilda)")]
    public List<HeroInstance> reserveHeroes = new List<HeroInstance>();

    [Header("Recursos e Armazém da Guilda")]
    public int goldCoins = 100;
    public List<ItemData> inventory = new List<ItemData>();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        if (activeParty.Count == 0)
        {
            InitializeParty();
        }
    }

    /// <summary>
    /// Constrói o elenco inicial da Guilda: 4 heróis no Grupo de Expedição + 2 heróis na Reserva do Quartel,
    /// permitindo ao jogador testar imediatamente a seleção e troca de heróis na Guilda antes de ir para a Dungeon.
    /// </summary>
    [ContextMenu("Convocar Elenco Inicial da Guilda")]
    public void InitializeParty()
    {
        activeParty.Clear();
        reserveHeroes.Clear();

        if (generateRandomPartyOnStart)
        {
            // 4 Heróis titulares na Esquadra de Expedição
            for (int i = 0; i < MAX_ACTIVE_PARTY_SIZE; i++)
            {
                HeroInstance summonedHero = HeroGenerator.GenerateRandomHero(startingStars: 1, goldStarChance: 0.12f);
                ItemGenerator.EquipStarterGear(summonedHero);
                activeParty.Add(summonedHero);
            }

            // 2 Heróis extras na Reserva do Quartel da Guilda para o jogador escalar/trocar
            for (int i = 0; i < 2; i++)
            {
                HeroInstance reserveHero = HeroGenerator.GenerateRandomHero(startingStars: 1, goldStarChance: 0.12f);
                ItemGenerator.EquipStarterGear(reserveHero);
                reserveHeroes.Add(reserveHero);
            }
        }
        else
        {
            for (int i = 0; i < initialHeroArchetypes.Length; i++)
            {
                if (initialHeroArchetypes[i] != null)
                {
                    HeroInstance newHero = new HeroInstance(initialHeroArchetypes[i], startingLevel: 1);
                    ItemGenerator.EquipStarterGear(newHero);
                    activeParty.Add(newHero);
                }
            }
        }

        goldCoins = 100; // Ouro inicial da Guilda
        ItemGenerator.PopulateGuildBackpack(this);

        Debug.Log($"⚔️ [GUILDA PRONTA] {activeParty.Count} aventureiros na Party de Expedição e {reserveHeroes.Count} na Reserva do Quartel!");
    }

    /// <summary>
    /// Move um herói da Esquadra de Expedição (activeParty) para a Reserva do Quartel (reserveHeroes).
    /// Garante que pelo menos 1 aventureiro permaneça na party ativa.
    /// </summary>
    public bool MoveHeroToReserve(HeroInstance hero, out string feedback)
    {
        if (hero == null || !activeParty.Contains(hero))
        {
            feedback = "Herói não está na equipe ativa.";
            return false;
        }

        if (activeParty.Count <= 1)
        {
            feedback = "A Esquadra de Expedição precisa ter pelo menos 1 aventureiro!";
            return false;
        }

        activeParty.Remove(hero);
        if (!reserveHeroes.Contains(hero))
        {
            reserveHeroes.Add(hero);
        }

        feedback = $"{hero.heroName} foi movido para a Reserva da Guilda.";
        SaveSystem.AutoSave("Alteracao na Party");
        return true;
    }

    /// <summary>
    /// Escala um herói da Reserva do Quartel para a Esquadra de Expedição (activeParty).
    /// Se a party já tiver 4 heróis, troca de lugar com o último herói (ou com o herói selecionado).
    /// </summary>
    public bool AssignReserveHeroToParty(HeroInstance reserveHero, HeroInstance heroToReplace, out string feedback)
    {
        if (reserveHero == null || !reserveHeroes.Contains(reserveHero))
        {
            feedback = "Herói não encontrado na reserva.";
            return false;
        }

        if (activeParty.Count < MAX_ACTIVE_PARTY_SIZE)
        {
            reserveHeroes.Remove(reserveHero);
            activeParty.Add(reserveHero);
            feedback = $"{reserveHero.heroName} entrou na Esquadra de Expedição!";
            SaveSystem.AutoSave("Heroi Escalado na Party");
            return true;
        }

        // Se já tem 4 heróis, faz a substituição pelo herói indicado (ou pelo último da fila)
        HeroInstance targetOut = (heroToReplace != null && activeParty.Contains(heroToReplace))
            ? heroToReplace
            : activeParty[activeParty.Count - 1];

        int idx = activeParty.IndexOf(targetOut);
        reserveHeroes.Remove(reserveHero);
        activeParty[idx] = reserveHero;
        reserveHeroes.Add(targetOut);

        feedback = $"{reserveHero.heroName} entrou no lugar de {targetOut.heroName} na Esquadra!";
        SaveSystem.AutoSave("Troca de Herois na Party");
        return true;
    }

    /// <summary>
    /// Convoca um novo aventureiro aleatório para a Guilda pagando Moedas de Ouro.
    /// </summary>
    public bool RecruitNewHero(int goldCost, out HeroInstance recruited, out string feedback)
    {
        recruited = null;
        if (goldCoins < goldCost)
        {
            feedback = $"Ouro insuficiente! São necessários {goldCost} G (Você possui {goldCoins} G).";
            return false;
        }

        goldCoins -= goldCost;
        recruited = HeroGenerator.GenerateRandomHero(startingStars: 1, goldStarChance: 0.15f);
        ItemGenerator.EquipStarterGear(recruited);

        if (activeParty.Count < MAX_ACTIVE_PARTY_SIZE)
        {
            activeParty.Add(recruited);
            feedback = $"Novo herói convocado direto para a Party: {recruited.heroName}!";
        }
        else
        {
            reserveHeroes.Add(recruited);
            feedback = $"Novo herói convocado para a Reserva do Quartel: {recruited.heroName}!";
        }

        SaveSystem.AutoSave("Novo Heroi Convocado");
        return true;
    }

    /// <summary>
    /// Restaura 100% do HP, MP e Poções de todos os heróis da Guilda (Ativos e Reserva) ao descansar na Sede.
    /// </summary>
    public void RestAllHeroesAtGuild()
    {
        foreach (var h in activeParty)
        {
            if (h == null) continue;
            h.currentHP = h.GetMaxHP();
            h.currentMP = h.GetMaxMP();
            h.healingPotions = 6;
        }
        foreach (var h in reserveHeroes)
        {
            if (h == null) continue;
            h.currentHP = h.GetMaxHP();
            h.currentMP = h.GetMaxMP();
            h.healingPotions = 6;
        }
    }

    /// <summary>
    /// Retorna uma lista unificada com todos os heróis da Guilda (Titulares + Reserva).
    /// </summary>
    public List<HeroInstance> GetAllGuildHeroes()
    {
        List<HeroInstance> all = new List<HeroInstance>(activeParty);
        all.AddRange(reserveHeroes);
        return all;
    }

    /// <summary>
    /// Distribui XP igualmente entre todos os heróis vivos do grupo que participou da batalha.
    /// </summary>
    public void DistributeXP(int totalXP)
    {
        int livingCount = 0;
        foreach (var hero in activeParty)
        {
            if (hero.IsAlive) livingCount++;
        }

        if (livingCount == 0) return;

        int xpPerHero = totalXP / livingCount;
        foreach (var hero in activeParty)
        {
            if (hero.IsAlive)
            {
                hero.GainXP(xpPerHero);
            }
        }
    }

    /// <summary>
    /// Adiciona moedas de ouro ganhas na masmorra.
    /// </summary>
    public void AddGold(int amount)
    {
        goldCoins += amount;
        Debug.Log($"💰 +{amount} Moedas de Ouro obtidas! (Total: {goldCoins})");
    }

    /// <summary>
    /// Adiciona um item obtido por drop ou baú ao Armazém / Depósito da Guilda.
    /// </summary>
    public void AddItemToInventory(ItemData item)
    {
        if (item == null) return;
        inventory.Add(item);
        Debug.Log($"📦 Item guardado no Armazém da Guilda: {item.itemName} ({item.itemType} - {item.rarity})");
    }
}
