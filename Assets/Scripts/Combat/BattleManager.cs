using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState
{
    Idle,
    HeroTurn,
    EnemyTurn,
    Victory,
    Defeat
}

/// <summary>
/// O Maestro do Combate Clássico (Estilo Final Fantasy).
/// Gerencia a visão lateral dos 4 aventureiros contra os monstros,
/// a fila de turnos por velocidade, menus de ação (Atacar, Habilidade, Defender)
/// e a distribuição de espólios após a vitória.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Estado do Combate")]
    public BattleState currentState = BattleState.Idle;
    public int currentTurnRound = 1;

    [Header("Combatentes na Arena")]
    public List<HeroInstance> heroesInBattle = new List<HeroInstance>();
    public List<EnemyInstance> enemiesInBattle = new List<EnemyInstance>();

    [Header("Monstros Padrão para Batalhas (Configuração)")]
    public EnemyData[] commonFloorEnemies;
    public EnemyData floorBossEnemy;

    [Header("Turno Atual")]
    public int currentHeroIndex = 0;
    public HeroInstance CurrentHero => (currentHeroIndex >= 0 && currentHeroIndex < heroesInBattle.Count) ? heroesInBattle[currentHeroIndex] : null;

    // Registra quais heróis estão em postura defensiva nesta rodada
    private HashSet<HeroInstance> defendingHeroes = new HashSet<HeroInstance>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Inicia uma batalha na arena contra monstros do andar ou contra o Boss.
    /// </summary>
    public void StartBattle(List<EnemyData> enemyEncounter)
    {
        if (PartyManager.Instance == null)
        {
            Debug.LogError("PartyManager não encontrado na cena!");
            return;
        }

        // 1. Carrega os heróis vivos da guilda
        heroesInBattle.Clear();
        foreach (var hero in PartyManager.Instance.activeParty)
        {
            if (hero.IsAlive)
                heroesInBattle.Add(hero);
        }

        if (heroesInBattle.Count == 0)
        {
            Debug.LogError("Todos os heróis estão mortos! Fim de jogo.");
            return;
        }

        // 2. Instancia os monstros
        enemiesInBattle.Clear();
        foreach (var eData in enemyEncounter)
        {
            if (eData != null)
                enemiesInBattle.Add(new EnemyInstance(eData));
        }

        defendingHeroes.Clear();
        currentTurnRound = 1;
        currentHeroIndex = 0;

        Debug.Log($"⚔️ [BATALHA INICIADA] {heroesInBattle.Count} Heróis vs {enemiesInBattle.Count} Monstros!");

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.battleRootPanel.SetActive(true);
            BattleHUD.Instance.RefreshBattleArena(heroesInBattle, enemiesInBattle);
        }

        StartHeroTurn();
    }

    private void StartHeroTurn()
    {
        // Pula heróis caídos
        while (currentHeroIndex < heroesInBattle.Count && !heroesInBattle[currentHeroIndex].IsAlive)
        {
            currentHeroIndex++;
        }

        // Se todos os heróis agiram nesta rodada, passa para os monstros
        if (currentHeroIndex >= heroesInBattle.Count)
        {
            StartCoroutine(ExecuteEnemyTurns());
            return;
        }

        currentState = BattleState.HeroTurn;
        HeroInstance actingHero = CurrentHero;
        Debug.Log($"👉 [SUA VEZ] É a vez de {actingHero.heroName} ({actingHero.heroClass})! Escolha sua ação: [Atacar], [Defender] ou [Habilidade].");

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.SetHeroTurnUI(actingHero);
        }
    }

    #region Ações do Jogador (Comandos Estilo Final Fantasy)

    /// <summary>
    /// Executa um ataque físico básico contra o monstro selecionado.
    /// </summary>
    public void PlayerAttack(int targetEnemyIndex)
    {
        if (currentState != BattleState.HeroTurn || CurrentHero == null) return;

        if (targetEnemyIndex < 0 || targetEnemyIndex >= enemiesInBattle.Count || !enemiesInBattle[targetEnemyIndex].IsAlive)
        {
            Debug.LogWarning("Alvo inválido!");
            return;
        }

        HeroInstance attacker = CurrentHero;
        EnemyInstance target = enemiesInBattle[targetEnemyIndex];

        // Cálculo de dano com chance de acerto crítico (Destreza)
        int attackPower = attacker.GetPhysicalAttack();
        bool isCrit = Random.Range(0f, 100f) <= attacker.GetCritRate();
        if (isCrit)
        {
            attackPower = Mathf.RoundToInt(attackPower * 1.75f);
            Debug.Log($"⚡ GOLPE CRÍTICO!");
        }

        Debug.Log($"🗡️ {attacker.heroName} desferiu um ataque contra {target.enemyName}!");
        target.TakeDamage(attackPower);

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateAllStats();
            BattleHUD.Instance.combatLogText.text = isCrit 
                ? $"⚡ <b>CRÍTICO!</b> {attacker.heroName} causou {attackPower} de dano a {target.enemyName}!"
                : $"🗡️ {attacker.heroName} causou {attackPower} de dano a {target.enemyName}!";
        }

        if (!target.IsAlive)
        {
            Debug.Log($"💀 {target.enemyName} foi abatido!");
        }

        // Verifica vitória
        if (CheckVictoryCondition())
        {
            OnVictory();
            return;
        }

        // Passa para o próximo herói da rodada
        currentHeroIndex++;
        StartHeroTurn();
    }

    /// <summary>
    /// Assume postura defensiva até a próxima rodada (reduz o dano sofrido em 50%).
    /// </summary>
    public void PlayerDefend()
    {
        if (currentState != BattleState.HeroTurn || CurrentHero == null) return;

        HeroInstance defender = CurrentHero;
        defendingHeroes.Add(defender);
        Debug.Log($"🛡️ {defender.heroName} assumiu postura defensiva! Dano sofrido reduzido pela metade.");

        currentHeroIndex++;
        StartHeroTurn();
    }

    #endregion

    #region Turno dos Monstros (IA de Batalha)

    private IEnumerator ExecuteEnemyTurns()
    {
        currentState = BattleState.EnemyTurn;
        Debug.Log($"👹 [TURNO DOS MONSTROS] As criaturas da masmorra se preparam para atacar!");

        yield return new WaitForSeconds(0.6f);

        foreach (var enemy in enemiesInBattle)
        {
            if (!enemy.IsAlive) continue;

            // Escolhe um herói vivo aleatório para atacar
            List<HeroInstance> livingHeroes = heroesInBattle.FindAll(h => h.IsAlive);
            if (livingHeroes.Count == 0) break;

            HeroInstance targetHero = livingHeroes[Random.Range(0, livingHeroes.Count)];

            int rawDamage = enemy.data.attack;
            // Se o herói estiver defendendo, corta o dano pela metade
            if (defendingHeroes.Contains(targetHero))
            {
                rawDamage = Mathf.RoundToInt(rawDamage * 0.5f);
                Debug.Log($"🛡️ {targetHero.heroName} bloqueou parte do golpe com seu escudo!");
            }

            Debug.Log($"💢 {enemy.enemyName} atacou {targetHero.heroName}!");
            targetHero.TakeDamage(rawDamage);

            if (BattleHUD.Instance != null)
            {
                BattleHUD.Instance.UpdateAllStats();
                BattleHUD.Instance.combatLogText.text = $"💢 {enemy.enemyName} atacou <b>{targetHero.heroName}</b> causando {rawDamage} de dano!";
            }

            if (!targetHero.IsAlive)
            {
                Debug.Log($"⚠️ PERIGO: {targetHero.heroName} caiu em combate!");
            }

            yield return new WaitForSeconds(0.8f);

            // Verifica derrota
            if (CheckDefeatCondition())
            {
                OnDefeat();
                yield break;
            }
        }

        // Prepara nova rodada
        currentTurnRound++;
        currentHeroIndex = 0;
        defendingHeroes.Clear();

        Debug.Log($"🔄 [NOVA RODADA] Rodada {currentTurnRound} se iniciando!");
        StartHeroTurn();
    }

    #endregion

    #region Condições de Vitória e Derrota

    private bool CheckVictoryCondition()
    {
        foreach (var e in enemiesInBattle)
        {
            if (e.IsAlive) return false;
        }
        return true;
    }

    private bool CheckDefeatCondition()
    {
        foreach (var h in heroesInBattle)
        {
            if (h.IsAlive) return false;
        }
        return true;
    }

    private void OnVictory()
    {
        currentState = BattleState.Victory;
        Debug.Log("🏆 VITÓRIA NA ARENA! Todos os monstros foram aniquilados!");

        int totalXP = 0;
        int totalGold = 0;
        List<ItemData> lootDropped = new List<ItemData>();

        foreach (var enemy in enemiesInBattle)
        {
            totalXP += enemy.data.xpReward;
            totalGold += enemy.data.goldReward;

            if (enemy.CheckLootDrop(out ItemData drop))
            {
                lootDropped.Add(drop);
            }
        }

        // Concede os espólios à Guilda
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.DistributeXP(totalXP);
            PartyManager.Instance.AddGold(totalGold);

            foreach (var item in lootDropped)
            {
                PartyManager.Instance.AddItemToInventory(item);
                Debug.Log($"✨ DROP RARO OBTIDO: {item.itemName}!");
            }
        }

        // Notifica o DungeonManager para continuar a exploração
        if (DungeonManager.Instance != null)
        {
            bool wasBossFight = enemiesInBattle.Exists(e => e.data.isBoss);
            if (wasBossFight)
            {
                DungeonManager.Instance.DefeatFloorBoss();
            }
            else
            {
                DungeonManager.Instance.GenerateDoors();
            }
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowVictory(totalXP, totalGold, lootDropped);
        }
    }

    private void OnDefeat()
    {
        currentState = BattleState.Defeat;
        Debug.LogError("☠️ DERROTA ESMAGADORA! Todo o grupo de aventureiros foi aniquilado na dungeon.");
    }

    #endregion

    #region Testes Rápidos no Inspector

    [ContextMenu("Iniciar Batalha de Teste")]
    public void TestStartBattle()
    {
        if (commonFloorEnemies != null && commonFloorEnemies.Length > 0)
        {
            List<EnemyData> encounter = new List<EnemyData>();
            encounter.Add(commonFloorEnemies[Random.Range(0, commonFloorEnemies.Length)]);
            encounter.Add(commonFloorEnemies[Random.Range(0, commonFloorEnemies.Length)]);
            StartBattle(encounter);
        }
    }

    [ContextMenu("Atacar Monstro 0")]
    public void TestAttackMonster0() => PlayerAttack(0);

    [ContextMenu("Atacar Monstro 1")]
    public void TestAttackMonster1() => PlayerAttack(1);

    [ContextMenu("Defender")]
    public void TestDefend() => PlayerDefend();

    #endregion
}
