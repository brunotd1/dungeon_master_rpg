using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O Cérebro do Grupo de Aventureiros (Party).
/// Gerencia os 4 membros ativos da equipe, o inventário compartilhado da guilda,
/// as moedas de ouro acumuladas e a distribuição de XP após as batalhas da masmorra.
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

    [Header("Configuração de Convocação")]
    [Tooltip("Se ativado, convoca 4 aventureiros aleatórios proceduralmente no início (estilo Pick Me Up!)")]
    public bool generateRandomPartyOnStart = true;

    [Header("Modelos dos Heróis Pré-definidos (Opcional)")]
    [Tooltip("Se quiser definir heróis fixos em vez de aleatórios, coloque os HeroData aqui")]
    public HeroData[] initialHeroArchetypes = new HeroData[4];

    [Header("Heróis Ativos no Grupo (Instâncias Vivas)")]
    public List<HeroInstance> activeParty = new List<HeroInstance>();

    [Header("Recursos da Guilda")]
    public int goldCoins = 0;
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
        InitializeParty();
    }

    /// <summary>
    /// Constrói os 4 heróis ativos (seja por geração aleatória ou por arquétipos pré-definidos).
    /// </summary>
    [ContextMenu("Convocar 4 Aventureiros Aleatórios")]
    public void InitializeParty()
    {
        activeParty.Clear();

        if (generateRandomPartyOnStart)
        {
            // Convoca 4 aventureiros sorteados com atributos aleatórios
            for (int i = 0; i < 4; i++)
            {
                HeroInstance summonedHero = HeroGenerator.GenerateRandomHero(startingStars: 1, goldStarChance: 0.10f);
                activeParty.Add(summonedHero);
            }
        }
        else
        {
            for (int i = 0; i < initialHeroArchetypes.Length; i++)
            {
                if (initialHeroArchetypes[i] != null)
                {
                    HeroInstance newHero = new HeroInstance(initialHeroArchetypes[i], startingLevel: 1);
                    activeParty.Add(newHero);
                }
            }
        }

        Debug.Log($"⚔️ [GUILDA PRONTA] Grupo de 4 aventureiros convocado para invadir a masmorra!");
    }

    /// <summary>
    /// Distribui XP igualmente entre todos os heróis vivos do grupo.
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
    /// Adiciona um item obtido por drop ou baú ao inventário.
    /// </summary>
    public void AddItemToInventory(ItemData item)
    {
        if (item == null) return;
        inventory.Add(item);
        Debug.Log($"📦 Item guardado na mochila: {item.itemName} ({item.itemType} - {item.rarity})");
    }
}
