using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O Cérebro do Grupo de Aventureiros (Party).
/// Gerencia os 4 membros ativos da equipe, o inventário compartilhado da guilda,
/// as moedas de ouro acumuladas e a distribuição de XP após as batalhas da masmorra.
/// </summary>
public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    [Header("Modelos dos 4 Heróis Iniciais")]
    [Tooltip("Arraste aqui os 4 HeroData: Cavaleiro, Cavaleiro Pesado, Mago e Ladino")]
    public HeroData[] initialHeroArchetypes = new HeroData[4];

    [Header("Heróis Ativos no Grupo (Instâncias Vivas)")]
    public List<HeroInstance> activeParty = new List<HeroInstance>();

    [Header("Recursos da Guilda")]
    public int goldCoins = 0;
    public List<ItemData> inventory = new List<ItemData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        InitializeParty();
    }

    /// <summary>
    /// Constrói os 4 heróis ativos a partir dos HeroData fornecidos.
    /// </summary>
    public void InitializeParty()
    {
        activeParty.Clear();

        for (int i = 0; i < initialHeroArchetypes.Length; i++)
        {
            if (initialHeroArchetypes[i] != null)
            {
                HeroInstance newHero = new HeroInstance(initialHeroArchetypes[i], startingLevel: 1);
                activeParty.Add(newHero);

                Debug.Log($"🛡️ Herói Convocado: [{newHero.heroData.heroName}] Classe: {newHero.heroData.heroClass} | HP: {newHero.GetMaxHP()} | ATK: {newHero.GetPhysicalAttack()} | DEF: {newHero.GetPhysicalDefense()} | VEL: {newHero.GetSpeed()}");
            }
        }

        Debug.Log($"⚔️ Grupo da Guilda formado com {activeParty.Count} heróis prontos para explorar o Andar 1!");
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
