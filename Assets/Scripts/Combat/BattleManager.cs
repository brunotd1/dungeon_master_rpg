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
    private static BattleManager _instance;
    public static BattleManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<BattleManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Configuração de Teste Automático")]
    [Tooltip("Se ativado, inicia uma batalha automaticamente ao dar Play!")]
    public bool autoStartBattleOnPlay = true;

    [Header("Estado do Combate")]
    public BattleState currentState = BattleState.Idle;
    public int currentTurnRound = 1;

    [Header("Combatentes na Arena")]
    public List<HeroInstance> heroesInBattle = new List<HeroInstance>();
    public List<EnemyInstance> enemiesInBattle = new List<EnemyInstance>();

    [Header("Monstros Padrão para Batalhas (Configuração)")]
    public EnemyData[] commonFloorEnemies;
    public EnemyData floorBossEnemy;

    [Header("Turno e Seleção")]
    public HeroInstance selectedHero = null;
    public HeroInstance CurrentHero => selectedHero;

    // Registra quais heróis estão em postura defensiva nesta rodada
    private HashSet<HeroInstance> defendingHeroes = new HashSet<HeroInstance>();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        EnsureDefaultEnemies();
    }

    /// <summary>
    /// Garante que mesmo que o projeto seja aberto pela primeira vez sem referências configuradas no Inspector,
    /// existam monstros funcionais para o jogo iniciar sem erros.
    /// </summary>
    public void EnsureDefaultEnemies()
    {
        if (commonFloorEnemies == null || commonFloorEnemies.Length == 0)
        {
            EnemyData goblin = ScriptableObject.CreateInstance<EnemyData>();
            goblin.enemyName = "Goblin Lanceiro";
            goblin.maxHP = 45;
            goblin.attack = 10;
            goblin.defense = 3;
            goblin.speed = 12;
            goblin.xpReward = 25;
            goblin.goldReward = 15;

            EnemyData skeleton = ScriptableObject.CreateInstance<EnemyData>();
            skeleton.enemyName = "Esqueleto Guerreiro";
            skeleton.maxHP = 60;
            skeleton.attack = 14;
            skeleton.defense = 6;
            skeleton.speed = 8;
            skeleton.xpReward = 35;
            skeleton.goldReward = 20;

            commonFloorEnemies = new EnemyData[] { goblin, skeleton };
        }

        if (floorBossEnemy == null)
        {
            EnemyData boss = ScriptableObject.CreateInstance<EnemyData>();
            boss.enemyName = "Guardião de Pedra (Chefe)";
            boss.maxHP = 220;
            boss.attack = 22;
            boss.defense = 12;
            boss.speed = 10;
            boss.xpReward = 200;
            boss.goldReward = 150;
            boss.isBoss = true;
            floorBossEnemy = boss;
        }
    }

    void Start()
    {
        if (autoStartBattleOnPlay)
        {
            StartCoroutine(AutoStartBattleRoutine());
        }
    }

    private IEnumerator AutoStartBattleRoutine()
    {
        yield return null; // Aguarda o primeiro frame completo para inicialização de todos os Singletons
        TestStartBattle();
    }

    /// <summary>
    /// Inicia uma batalha na arena contra monstros do andar ou contra o Boss.
    /// </summary>
    public void StartBattle(List<EnemyData> enemyEncounter)
    {
        if (PartyManager.Instance == null)
        {
            GameObject pmObj = new GameObject("PartyManager", typeof(PartyManager));
            Debug.Log("[BattleManager] PartyManager criado dinamicamente.");
        }

        if (PartyManager.Instance.activeParty.Count == 0)
        {
            PartyManager.Instance.InitializeParty();
        }

        // 1. Carrega os heróis vivos da guilda
        heroesInBattle.Clear();
        foreach (var hero in PartyManager.Instance.activeParty)
        {
            if (hero.IsAlive)
            {
                hero.hasActedThisRound = false;
                heroesInBattle.Add(hero);
            }
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
        selectedHero = null;

        Debug.Log($"⚔️ [BATALHA INICIADA] {heroesInBattle.Count} Heróis vs {enemiesInBattle.Count} Monstros!");

        if (BattleHUD.Instance == null)
        {
            GameObject hudObj = new GameObject("BattleHUD", typeof(BattleHUD));
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.RefreshBattleArena(heroesInBattle, enemiesInBattle);
        }

        StartPlayerPhase();
    }

    /// <summary>
    /// Inicia a Fase do Jogador na rodada. O jogador escolhe seus heróis livremente na ordem que desejar.
    /// </summary>
    public void StartPlayerPhase()
    {
        currentState = BattleState.HeroTurn;

        foreach (var h in heroesInBattle)
        {
            h.hasActedThisRound = false;
        }
        defendingHeroes.Clear();

        Debug.Log($"👑 [FASE DO JOGADOR - RODADA {currentTurnRound}] Escolha qualquer herói pronto para agir!");

        // Auto-seleciona o primeiro herói vivo disponível
        AutoSelectNextReadyHero();
    }

    /// <summary>
    /// Seleciona o herói indicado pelo jogador (ao clicar no personagem no campo ou na barra inferior).
    /// </summary>
    public void SelectHero(HeroInstance hero)
    {
        if (currentState != BattleState.HeroTurn) return;
        if (hero == null || !hero.IsAlive) return;

        selectedHero = hero;
        Debug.Log($"👉 Herói selecionado: {hero.heroName} ({(hero.hasActedThisRound ? "Já agiu nesta rodada" : "Pronto para agir")})");

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.SetHeroTurnUI(selectedHero);
        }
    }

    /// <summary>
    /// Auto-seleciona o próximo herói vivo que ainda não agiu nesta rodada.
    /// </summary>
    public void AutoSelectNextReadyHero()
    {
        HeroInstance nextReady = heroesInBattle.Find(h => h.IsAlive && !h.hasActedThisRound);
        selectedHero = nextReady;

        if (selectedHero != null)
        {
            if (BattleHUD.Instance != null)
            {
                BattleHUD.Instance.SetHeroTurnUI(selectedHero);
            }
        }
        else
        {
            // Se nenhum herói pode agir, transiciona para os inimigos
            StartCoroutine(ExecuteEnemyTurns());
        }
    }

    /// <summary>
    /// Verifica se todos os heróis vivos já realizaram sua ação nesta rodada.
    /// </summary>
    private void CheckPhaseCompletion()
    {
        if (CheckVictoryCondition())
        {
            OnVictory();
            return;
        }

        bool anyHeroReady = heroesInBattle.Exists(h => h.IsAlive && !h.hasActedThisRound);
        if (!anyHeroReady)
        {
            Debug.Log("⏳ Todos os heróis agiram nesta rodada! Iniciando o turno dos monstros...");
            StartCoroutine(ExecuteEnemyTurns());
        }
        else
        {
            AutoSelectNextReadyHero();
        }
    }

    #region Ações do Jogador (Comandos Estilo Final Fantasy)

    /// <summary>
    /// Executa um ataque físico básico contra o monstro selecionado com o herói ativo.
    /// </summary>
    public void PlayerAttack(int targetEnemyIndex)
    {
        if (currentState != BattleState.HeroTurn || selectedHero == null) return;
        if (selectedHero.hasActedThisRound)
        {
            Debug.LogWarning($"{selectedHero.heroName} já agiu nesta rodada! Selecione outro herói.");
            return;
        }

        if (targetEnemyIndex < 0 || targetEnemyIndex >= enemiesInBattle.Count || !enemiesInBattle[targetEnemyIndex].IsAlive)
        {
            Debug.LogWarning("Alvo inválido!");
            return;
        }

        HeroInstance attacker = selectedHero;
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

        attacker.hasActedThisRound = true;

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateAllStats();
            BattleHUD.Instance.combatLogText.text = isCrit 
                ? $"[CRÍTICO!] <b>{attacker.heroName}</b> causou {attackPower} de dano a {target.enemyName}!"
                : $"<b>{attacker.heroName}</b> causou {attackPower} de dano a {target.enemyName}!";
        }

        if (!target.IsAlive)
        {
            Debug.Log($"💀 {target.enemyName} foi abatido!");
        }

        CheckPhaseCompletion();
    }

    /// <summary>
    /// O herói selecionado consome uma Poção de Cura (cura 35% Max HP + 10).
    /// </summary>
    public void PlayerUsePotion()
    {
        if (currentState != BattleState.HeroTurn || selectedHero == null) return;
        if (selectedHero.hasActedThisRound)
        {
            Debug.LogWarning($"{selectedHero.heroName} já agiu nesta rodada!");
            return;
        }

        if (selectedHero.healingPotions <= 0)
        {
            Debug.LogWarning($"{selectedHero.heroName} não possui mais poções de cura!");
            if (BattleHUD.Instance != null)
                BattleHUD.Instance.combatLogText.text = $"[AVISO] {selectedHero.heroName} não tem mais poções de cura!";
            return;
        }

        if (selectedHero.UseHealingPotion(out int amountHealed))
        {
            selectedHero.hasActedThisRound = true;

            if (BattleHUD.Instance != null)
            {
                BattleHUD.Instance.UpdateAllStats();
                BattleHUD.Instance.combatLogText.text = $"[POÇÃO] <b>{selectedHero.heroName}</b> recuperou {amountHealed} HP! (Restam: {selectedHero.healingPotions}/6)";
            }

            CheckPhaseCompletion();
        }
    }

    /// <summary>
    /// Assume postura defensiva até a próxima rodada (reduz o dano sofrido em 50%).
    /// </summary>
    public void PlayerDefend()
    {
        if (currentState != BattleState.HeroTurn || selectedHero == null) return;
        if (selectedHero.hasActedThisRound)
        {
            Debug.LogWarning($"{selectedHero.heroName} já agiu nesta rodada!");
            return;
        }

        HeroInstance defender = selectedHero;
        defendingHeroes.Add(defender);
        defender.hasActedThisRound = true;
        Debug.Log($"🛡️ {defender.heroName} assumiu postura defensiva! Dano sofrido reduzido pela metade.");

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateAllStats();
            BattleHUD.Instance.combatLogText.text = $"[DEFESA] <b>{defender.heroName}</b> assumiu postura defensiva! (-50% dano sofrido)";
        }

        CheckPhaseCompletion();
    }

    /// <summary>
    /// Permite ao jogador encerrar a fase do jogador manualmente, mesmo se restarem heróis sem agir.
    /// </summary>
    public void EndPlayerPhaseManually()
    {
        if (currentState != BattleState.HeroTurn) return;
        Debug.Log("⏩ O jogador optou por encerrar a fase do grupo!");
        StartCoroutine(ExecuteEnemyTurns());
    }

    public bool IsHeroDefending(HeroInstance hero) => defendingHeroes.Contains(hero);

    #endregion

    #region Turno dos Monstros (IA de Batalha)

    private IEnumerator ExecuteEnemyTurns()
    {
        currentState = BattleState.EnemyTurn;
        selectedHero = null;
        Debug.Log($"👹 [TURNO DOS MONSTROS] As criaturas da masmorra se preparam para atacar!");

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.SetEnemyTurnUI();
        }

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
                Debug.Log($"🛡️ {targetHero.heroName} bloqueou parte do golpe com sua postura defensiva!");
            }

            Debug.Log($"💢 {enemy.enemyName} atacou {targetHero.heroName}!");
            targetHero.TakeDamage(rawDamage);

            if (BattleHUD.Instance != null)
            {
                BattleHUD.Instance.UpdateAllStats();
                BattleHUD.Instance.combatLogText.text = defendingHeroes.Contains(targetHero)
                    ? $"[BLOQUEIO] {enemy.enemyName} atacou <b>{targetHero.heroName}</b> causando {rawDamage} de dano! (-50%)"
                    : $"{enemy.enemyName} atacou <b>{targetHero.heroName}</b> causando {rawDamage} de dano!";
            }

            if (!targetHero.IsAlive)
            {
                Debug.Log($"⚠️ PERIGO: {targetHero.heroName} caiu em combate!");
            }

            yield return new WaitForSeconds(0.7f);

            // Verifica derrota
            if (CheckDefeatCondition())
            {
                OnDefeat();
                yield break;
            }
        }

        // Prepara nova rodada
        currentTurnRound++;
        Debug.Log($"🔄 [NOVA RODADA] Rodada {currentTurnRound} se iniciando!");
        StartPlayerPhase();
    }

    #endregion

    #region Condições de Vitória e Derrota

    public bool CheckVictoryCondition()
    {
        foreach (var e in enemiesInBattle)
        {
            if (e.IsAlive) return false;
        }
        return true;
    }

    public bool CheckDefeatCondition()
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

        // Notifica o DungeonManager que a sala foi limpa e gera as portas para o jogador escolher
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

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowDefeat();
        }
    }

    #endregion

    #region Testes Rápidos no Inspector

    [ContextMenu("Iniciar Batalha de Teste (2 a 6 Inimigos)")]
    public void TestStartBattle()
    {
        if (commonFloorEnemies != null && commonFloorEnemies.Length > 0)
        {
            List<EnemyData> encounter = new List<EnemyData>();
            int enemyCount = Random.Range(2, 7); // Mínimo 2, máximo 6 monstros!
            for (int i = 0; i < enemyCount; i++)
            {
                encounter.Add(commonFloorEnemies[Random.Range(0, commonFloorEnemies.Length)]);
            }
            StartBattle(encounter);
        }
    }

    [ContextMenu("Atacar Monstro 0")]
    public void TestAttackMonster0() => PlayerAttack(0);

    [ContextMenu("Atacar Monstro 1")]
    public void TestAttackMonster1() => PlayerAttack(1);

    [ContextMenu("Usar Poção de Cura")]
    public void TestUsePotion() => PlayerUsePotion();

    [ContextMenu("Defender")]
    public void TestDefend() => PlayerDefend();

    #endregion
}
