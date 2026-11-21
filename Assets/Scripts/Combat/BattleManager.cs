using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [System.NonSerialized] public HeroInstance selectedHero = null;
    public HeroInstance CurrentHero => selectedHero;
    [System.NonSerialized] public HeroSkill pendingSkill = null;

    // Registra quais heróis estão em postura defensiva nesta rodada
    private HashSet<HeroInstance> defendingHeroes = new HashSet<HeroInstance>();

    // Cooldowns das habilidades do Boss Necromante
    private int bossSummonCooldown = 0;
    private int bossHealCooldown = 0;

    [Header("Auto-Run (Estilo Taskbar Heroes)")]
    public bool isAutoRunEnabled = false;
    private Coroutine autoHeroActionRoutine;
    private Coroutine autoNextRoomRoutine;

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
            boss.enemyName = "Necromante das Catacumbas";
            boss.maxHP = 646;
            boss.attack = 91;
            boss.defense = 35;
            boss.speed = 11;
            boss.xpReward = 860;
            boss.goldReward = 195;
            boss.isBoss = true;
            floorBossEnemy = boss;
        }
    }

    private EnemyData GetSkeletonMinionTemplate()
    {
        if (commonFloorEnemies != null)
        {
            foreach (var e in commonFloorEnemies)
            {
                if (e != null && !string.IsNullOrEmpty(e.enemyName) && e.enemyName.ToLower().Contains("esqueleto"))
                {
                    return e;
                }
            }
        }

        EnemyData skeleton = ScriptableObject.CreateInstance<EnemyData>();
        skeleton.enemyName = "Esqueleto Guerreiro";
        skeleton.maxHP = 104;
        skeleton.attack = 42;
        skeleton.defense = 13;
        skeleton.speed = 7;
        skeleton.xpReward = 45;
        skeleton.goldReward = 12;
        skeleton.isBoss = false;
        return skeleton;
    }

    void Start()
    {
        if (autoStartBattleOnPlay)
        {
            StartCoroutine(AutoStartBattleRoutine());
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            bool mainMenuOpen = MainMenuUI.Instance != null && MainMenuUI.Instance.IsMenuOpen;
            bool inGuildHub = PartyMenuUI.Instance != null && PartyMenuUI.Instance.isInGuildHub &&
                              PartyMenuUI.Instance.menuRootPanel != null && PartyMenuUI.Instance.menuRootPanel.activeSelf;
            if (!mainMenuOpen && !inGuildHub &&
                BattleHUD.Instance != null && BattleHUD.Instance.battleRootPanel != null && BattleHUD.Instance.battleRootPanel.activeInHierarchy)
            {
                ToggleAutoRun();
            }
        }
    }

    private IEnumerator AutoStartBattleRoutine()
    {
        yield return null; // Aguarda o primeiro frame completo para inicialização de todos os Singletons
        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.BuildHUDStructureIfNotExisting();
        }
        if (MainMenuUI.Instance != null)
        {
            MainMenuUI.Instance.OpenMainMenu();
        }
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

        // 2. Instancia os monstros com nível calibrado para o andar atual (Comuns 1-6 no 1º andar; Boss Nv. 7)
        enemiesInBattle.Clear();
        int floor = DungeonManager.Instance != null ? DungeonManager.Instance.currentFloor : 1;
        int clearedRooms = DungeonManager.Instance != null ? DungeonManager.Instance.clearedRoomsThisFloor : 0;
        int bossLevel = DungeonManager.Instance != null ? DungeonManager.Instance.GetFloorBossLevel(floor) : ((floor - 1) * 10 + 7);
        int minCommonLvl = (Mathf.Max(1, floor) - 1) * 10 + 1;
        int maxCommonLvl = (Mathf.Max(1, floor) - 1) * 10 + 6;

        int partyAvgLvl = minCommonLvl;
        if (heroesInBattle.Count > 0)
        {
            int sumLvl = 0;
            foreach (var h in heroesInBattle) sumLvl += h.level;
            partyAvgLvl = Mathf.RoundToInt((float)sumLvl / heroesInBattle.Count);
        }

        int progressionTarget = minCommonLvl + Mathf.Max(0, (clearedRooms - 1) / 2);
        int baseCommonLvl = Mathf.Clamp(Mathf.Max(partyAvgLvl, progressionTarget), minCommonLvl, maxCommonLvl);

        foreach (var eData in enemyEncounter)
        {
            if (eData == null) continue;
            int enemyLevel = eData.isBoss
                ? bossLevel
                : Mathf.Clamp(baseCommonLvl + Random.Range(-1, 2), minCommonLvl, maxCommonLvl);
            enemiesInBattle.Add(new EnemyInstance(eData, enemyLevel));
        }

        defendingHeroes.Clear();
        currentTurnRound = 1;
        bossSummonCooldown = 0;
        bossHealCooldown = 0;
        selectedHero = null;

        if (autoHeroActionRoutine != null)
        {
            StopCoroutine(autoHeroActionRoutine);
            autoHeroActionRoutine = null;
        }
        if (autoNextRoomRoutine != null)
        {
            StopCoroutine(autoNextRoomRoutine);
            autoNextRoomRoutine = null;
        }

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

        if (isAutoRunEnabled && !selectedHero.hasActedThisRound)
        {
            TriggerAutoRunForCurrentHero();
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

            if (isAutoRunEnabled)
            {
                TriggerAutoRunForCurrentHero();
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
    /// Prepara a execução de uma habilidade ou ataque específico.
    /// </summary>
    public void SelectSkillToCast(HeroSkill skill)
    {
        if (currentState != BattleState.HeroTurn || selectedHero == null) return;
        if (selectedHero.hasActedThisRound) return;

        if (skill == null || string.IsNullOrEmpty(skill.skillName) || skill.damageMultiplier <= 0f)
        {
            if (selectedHero.availableSkills == null || selectedHero.availableSkills.Count == 0)
                selectedHero.InitializeDefaultSkills();

            skill = (selectedHero.availableSkills != null && selectedHero.availableSkills.Count > 0)
                ? selectedHero.availableSkills[0]
                : new HeroSkill("Ataque Básico", "Golpe físico padrão.", 0, 1.0f);
        }

        if (selectedHero.currentMP < skill.mpCost)
        {
            Debug.LogWarning($"[MANA INSUFICIENTE] {selectedHero.heroName} não tem MP suficiente para {skill.skillName} ({selectedHero.currentMP}/{skill.mpCost} MP)!");
            if (BattleHUD.Instance != null)
                BattleHUD.Instance.combatLogText.text = $"[AVISO] {selectedHero.heroName} não possui Mana suficiente para {skill.skillName}!";
            return;
        }

        pendingSkill = skill;
        Debug.Log($"🎯 Habilidade selecionada: {skill.skillName} (Custo: {skill.mpCost} MP). Escolha o alvo no campo de batalha!");
    }

    /// <summary>
    /// Executa o ataque ou habilidade pendente contra o monstro alvo.
    /// </summary>
    public void PlayerExecuteSkillOn(int targetEnemyIndex, HeroSkill skillToUse = null)
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

        HeroSkill skill = skillToUse ?? pendingSkill;
        if (skill == null || string.IsNullOrEmpty(skill.skillName) || skill.damageMultiplier <= 0f)
        {
            if (selectedHero.availableSkills == null || selectedHero.availableSkills.Count == 0)
                selectedHero.InitializeDefaultSkills();

            skill = (selectedHero.availableSkills != null && selectedHero.availableSkills.Count > 0)
                ? selectedHero.availableSkills[0]
                : new HeroSkill("Ataque Básico", "Golpe físico padrão.", 0, 1.0f);
        }

        if (selectedHero.currentMP < skill.mpCost)
        {
            Debug.LogWarning($"[MANA INSUFICIENTE] {selectedHero.heroName} não tem MP suficiente para {skill.skillName}!");
            if (BattleHUD.Instance != null)
                BattleHUD.Instance.combatLogText.text = $"[AVISO] Mana insuficiente para {skill.skillName}!";
            return;
        }

        HeroInstance attacker = selectedHero;
        EnemyInstance target = enemiesInBattle[targetEnemyIndex];
        CancelPendingAutoHeroAction();

        // Consome MP
        if (skill.mpCost > 0)
        {
            attacker.currentMP = Mathf.Max(0, attacker.currentMP - skill.mpCost);
        }

        // Cálculo de Dano (Físico ou Mágico com multiplicador de skill)
        int basePower = skill.isMagic ? attacker.GetMagicAttack() : attacker.GetPhysicalAttack();
        int attackPower = Mathf.RoundToInt(basePower * skill.damageMultiplier);

        // Cálculo de Crítico
        float critChance = attacker.GetCritRate() + skill.critBonus;
        bool isCrit = Random.Range(0f, 100f) <= critChance;
        if (isCrit)
        {
            attackPower = Mathf.RoundToInt(attackPower * 1.75f);
            Debug.Log($"⚡ GOLPE CRÍTICO COM {skill.skillName}!");
        }

        Debug.Log($"🗡️ {attacker.heroName} usou {skill.skillName} contra {target.enemyName}!");
        target.TakeDamage(attackPower, skill.isMagic);

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.PlayHeroAttackAnimation(attacker, targetEnemyIndex, skill.skillName);
        }

        attacker.hasActedThisRound = true;
        pendingSkill = null;

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateAllStats();
            BattleHUD.Instance.combatLogText.text = isCrit 
                ? $"[CRÍTICO!] <b>{attacker.heroName}</b> usou <b>{skill.skillName}</b> causando {attackPower} de dano a {target.enemyName}!"
                : $"<b>{attacker.heroName}</b> usou <b>{skill.skillName}</b> causando {attackPower} de dano a {target.enemyName}!";
        }

        if (!target.IsAlive)
        {
            Debug.Log($"💀 {target.enemyName} foi abatido!");
        }

        CheckPhaseCompletion();
    }

    /// <summary>
    /// Atalho para ataque físico básico contra o monstro (compatibilidade com botões diretos e testes).
    /// </summary>
    public void PlayerAttack(int targetEnemyIndex)
    {
        PlayerExecuteSkillOn(targetEnemyIndex, null);
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
            CancelPendingAutoHeroAction();
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
        CancelPendingAutoHeroAction();
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
        if (autoHeroActionRoutine != null)
        {
            StopCoroutine(autoHeroActionRoutine);
            autoHeroActionRoutine = null;
        }
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

        for (int eIdx = 0; eIdx < enemiesInBattle.Count; eIdx++)
        {
            EnemyInstance enemy = enemiesInBattle[eIdx];
            if (enemy == null || !enemy.IsAlive) continue;

            List<HeroInstance> livingHeroes = heroesInBattle.FindAll(h => h.IsAlive);
            if (livingHeroes.Count == 0) break;

            bool isBossUnit = enemy.data != null && enemy.data.isBoss;
            if (isBossUnit)
            {
                if (bossSummonCooldown > 0) bossSummonCooldown--;
                if (bossHealCooldown > 0) bossHealCooldown--;

                int livingMinions = enemiesInBattle.FindAll(e => e != null && e.IsAlive && (e.data == null || !e.data.isBoss)).Count;

                // Skill 1 do Boss Necromante: [ INVOCAÇÃO NECROMÂNTICA ] - Invoca 2 Esqueletos Guerreiros
                if (livingMinions == 0 && bossSummonCooldown <= 0)
                {
                    enemiesInBattle.RemoveAll(e => e != null && !e.IsAlive && e.isSummonedMinion);
                    eIdx = enemiesInBattle.IndexOf(enemy);

                    int minionLevel = Mathf.Max(1, enemy.level - 2);
                    EnemyData skelTemplate = GetSkeletonMinionTemplate();
                    enemiesInBattle.Add(new EnemyInstance(skelTemplate, minionLevel, summonedMinion: true));
                    enemiesInBattle.Add(new EnemyInstance(skelTemplate, minionLevel, summonedMinion: true));
                    bossSummonCooldown = 3;

                    if (BattleHUD.Instance != null)
                    {
                        BattleHUD.Instance.RebuildEnemiesDisplay(enemiesInBattle);
                        BattleHUD.Instance.UpdateAllStats();
                        BattleHUD.Instance.PlayBossSummonAnimation(enemy);
                        if (BattleHUD.Instance.combatLogText != null)
                        {
                            BattleHUD.Instance.combatLogText.text =
                                $"[SKILL DO CHEFE - INVOCAÇÃO NECROMÂNTICA] <b>{enemy.enemyName}</b> invocou <b>2 Esqueletos Guerreiros (Nv.{minionLevel})</b> para auxiliá-lo!";
                        }
                    }

                    yield return new WaitForSeconds(0.85f);

                    livingHeroes = heroesInBattle.FindAll(h => h.IsAlive);
                    if (livingHeroes.Count > 0)
                    {
                        if (BattleHUD.Instance != null) BattleHUD.Instance.PlayBossAttackAnimation(enemy);
                        HeroInstance orbTarget = livingHeroes[Random.Range(0, livingHeroes.Count)];
                        int orbRaw = Mathf.RoundToInt(enemy.attack * 0.75f);
                        ApplyEnemyStrikeToHero(
                            orbTarget,
                            orbRaw,
                            isMagic: true,
                            logMessage: $"[ORBE SOMBRIO] <b>{enemy.enemyName}</b> atingiu <b>{orbTarget.heroName}</b> após erguer seus mortos-vivos!"
                        );
                        yield return new WaitForSeconds(0.65f);
                        if (CheckDefeatCondition())
                        {
                            OnDefeat();
                            yield break;
                        }
                    }
                    continue;
                }

                // Skill 2 do Boss Necromante: [ REGENERAÇÃO SOMBRIA ] - Regenera HP e drena alma de um herói
                if (enemy.currentHP < Mathf.RoundToInt(enemy.maxHP * 0.80f) && bossHealCooldown <= 0)
                {
                    int healAmount = Mathf.RoundToInt(enemy.maxHP * 0.16f) + (livingMinions * 22);
                    int healed = enemy.Heal(healAmount);
                    bossHealCooldown = 2;

                    if (BattleHUD.Instance != null) BattleHUD.Instance.PlayBossAttackAnimation(enemy);
                    HeroInstance drainTarget = livingHeroes[Random.Range(0, livingHeroes.Count)];
                    int drainRaw = Mathf.RoundToInt(enemy.attack * 0.82f);
                    ApplyEnemyStrikeToHero(
                        drainTarget,
                        drainRaw,
                        isMagic: true,
                        logMessage: $"[SKILL DO CHEFE - REGENERAÇÃO SOMBRIA] <b>{enemy.enemyName}</b> drenou a alma de <b>{drainTarget.heroName}</b> e regenerou <color=#2ecc71><b>+{healed} HP</b></color>!"
                    );

                    yield return new WaitForSeconds(0.80f);
                    if (CheckDefeatCondition())
                    {
                        OnDefeat();
                        yield break;
                    }
                    continue;
                }

                // Ataque Padrão do Necromante: Magia Negra + chance de Onda Necromântica em 2º alvo
                if (BattleHUD.Instance != null) BattleHUD.Instance.PlayBossAttackAnimation(enemy);
                HeroInstance primaryTarget = livingHeroes[Random.Range(0, livingHeroes.Count)];
                ApplyEnemyStrikeToHero(
                    primaryTarget,
                    enemy.attack,
                    isMagic: true,
                    logMessage: defendingHeroes.Contains(primaryTarget)
                        ? $"[BLOQUEIO] {enemy.enemyName} disparou Magia Negra em <b>{primaryTarget.heroName}</b>! (-50%)"
                        : $"[MAGIA NEGRA] <b>{enemy.enemyName} (Nv.{enemy.level})</b> atacou <b>{primaryTarget.heroName}</b>!"
                );

                yield return new WaitForSeconds(0.65f);
                if (CheckDefeatCondition())
                {
                    OnDefeat();
                    yield break;
                }

                livingHeroes = heroesInBattle.FindAll(h => h.IsAlive);
                if (livingHeroes.Count > 1 && Random.value < 0.40f)
                {
                    if (BattleHUD.Instance != null) BattleHUD.Instance.PlayBossAttackAnimation(enemy);
                    List<HeroInstance> otherTargets = livingHeroes.FindAll(h => h != primaryTarget);
                    HeroInstance secondTarget = otherTargets[Random.Range(0, otherTargets.Count)];
                    int waveRaw = Mathf.RoundToInt(enemy.attack * 0.70f);
                    ApplyEnemyStrikeToHero(
                        secondTarget,
                        waveRaw,
                        isMagic: true,
                        logMessage: $"[ONDA NECROMÂNTICA] <b>{enemy.enemyName}</b> atingiu também <b>{secondTarget.heroName}</b>!"
                    );

                    yield return new WaitForSeconds(0.55f);
                    if (CheckDefeatCondition())
                    {
                        OnDefeat();
                        yield break;
                    }
                }
                continue;
            }

            // Monstros comuns e esqueletos invocados
            HeroInstance targetHero = livingHeroes[Random.Range(0, livingHeroes.Count)];
            Debug.Log($"💢 {enemy.enemyName} (Nv.{enemy.level}) atacou {targetHero.heroName}!");
            ApplyEnemyStrikeToHero(
                targetHero,
                enemy.attack,
                isMagic: false,
                logMessage: defendingHeroes.Contains(targetHero)
                    ? $"[BLOQUEIO] {enemy.enemyName} atacou <b>{targetHero.heroName}</b>! (-50%)"
                    : $"{enemy.enemyName} (Nv.{enemy.level}) atacou <b>{targetHero.heroName}</b>!"
            );

            yield return new WaitForSeconds(0.65f);
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

    private void ApplyEnemyStrikeToHero(HeroInstance targetHero, int rawDamage, bool isMagic, string logMessage)
    {
        if (targetHero == null || !targetHero.IsAlive) return;

        int adjustedDamage = rawDamage;
        if (defendingHeroes.Contains(targetHero))
        {
            adjustedDamage = Mathf.RoundToInt(rawDamage * 0.5f);
            Debug.Log($"🛡️ {targetHero.heroName} bloqueou parte do golpe com sua postura defensiva!");
        }

        targetHero.TakeDamage(adjustedDamage, isMagic);

        if (BattleHUD.Instance != null)
        {
            if (targetHero.IsAlive)
                BattleHUD.Instance.PlayHeroHurtAnimation(targetHero);
            else
                BattleHUD.Instance.PlayHeroDefeatAnimation(targetHero);

            BattleHUD.Instance.UpdateAllStats();
            if (BattleHUD.Instance.combatLogText != null)
                BattleHUD.Instance.combatLogText.text = logMessage;
        }

        if (!targetHero.IsAlive)
        {
            Debug.Log($"⚠️ PERIGO: {targetHero.heroName} caiu em combate!");
        }
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
        int maxEnemyLevel = 1;
        bool wasBossFight = enemiesInBattle.Exists(e => e.data != null && e.data.isBoss);
        List<ItemData> lootDropped = new List<ItemData>();

        foreach (var enemy in enemiesInBattle)
        {
            totalXP += enemy.xpReward;
            totalGold += enemy.goldReward;
            if (enemy.level > maxEnemyLevel)
                maxEnemyLevel = enemy.level;
        }

        int currentFloor = DungeonManager.Instance != null ? DungeonManager.Instance.currentFloor : 1;

        // Controle rígido de quantidade de drops por sala:
        // No máximo 1 equipamento por sala normal (chance calibrada) + chance separada de 1 Poção de Vida.
        if (wasBossFight)
        {
            int bossLvl = DungeonManager.Instance != null ? DungeonManager.Instance.GetFloorBossLevel(currentFloor) : maxEnemyLevel;
            ItemData bossEquip = ItemGenerator.GenerateRandomDungeonDrop(currentFloor, bossLvl, isTreasureChest: false, isBossDrop: true);
            if (bossEquip != null) lootDropped.Add(bossEquip);
        }
        else
        {
            float equipDropChance = Mathf.Clamp(0.16f + (enemiesInBattle.Count * 0.025f), 0.18f, 0.32f);
            if (Random.value <= equipDropChance)
            {
                ItemData equipDrop = ItemGenerator.GenerateRandomDungeonDrop(currentFloor, maxEnemyLevel, isTreasureChest: false, isBossDrop: false);
                if (equipDrop != null) lootDropped.Add(equipDrop);
            }

            if (Random.value <= 0.14f)
            {
                lootDropped.Add(ItemGenerator.CreateHealingPotion());
            }
        }

        int xpPerHero = totalXP;
        // Concede os espólios à Guilda
        if (PartyManager.Instance != null)
        {
            xpPerHero = PartyManager.Instance.DistributeXP(totalXP);
            PartyManager.Instance.AddGold(totalGold);

            foreach (var item in lootDropped)
            {
                PartyManager.Instance.AddItemToInventory(item);
                Debug.Log($"✨ DROP OBTIDO: {item.itemName} ({item.rarity})!");
            }
        }

        // Notifica o DungeonManager que a sala foi limpa e gera as portas para o jogador escolher
        if (DungeonManager.Instance != null)
        {
            if (wasBossFight)
            {
                DungeonManager.Instance.DefeatFloorBoss();
            }
            else
            {
                DungeonManager.Instance.GenerateDoors();
                SaveSystem.AutoSave("Vitoria na Sala");
            }
        }
        else
        {
            SaveSystem.AutoSave("Vitoria na Sala");
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowVictory(xpPerHero, totalGold, lootDropped);
        }
    }

    private void OnDefeat()
    {
        currentState = BattleState.Defeat;
        SetAutoRun(false);
        Debug.LogError("☠️ DERROTA ESMAGADORA! Todo o grupo de aventureiros foi aniquilado na dungeon.");

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowDefeat();
        }
    }

    #endregion

    #region Sistema de Auto-Run (Estilo Taskbar Heroes)

    /// <summary>
    /// Alterna entre controle manual e Auto-Run contínuo de combate e salas.
    /// </summary>
    public void ToggleAutoRun()
    {
        SetAutoRun(!isAutoRunEnabled);
    }

    /// <summary>
    /// Ativa ou desativa o Auto-Run, gerenciando corrotinas de ação e avanço automático.
    /// </summary>
    public void SetAutoRun(bool enabled)
    {
        if (isAutoRunEnabled == enabled)
        {
            if (BattleHUD.Instance != null) BattleHUD.Instance.UpdateAutoRunButtonUI(isAutoRunEnabled);
            return;
        }

        isAutoRunEnabled = enabled;

        if (!isAutoRunEnabled)
        {
            if (autoHeroActionRoutine != null)
            {
                StopCoroutine(autoHeroActionRoutine);
                autoHeroActionRoutine = null;
            }
            if (autoNextRoomRoutine != null)
            {
                StopCoroutine(autoNextRoomRoutine);
                autoNextRoomRoutine = null;
            }
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateAutoRunButtonUI(isAutoRunEnabled);
            if (BattleHUD.Instance.combatLogText != null)
            {
                BattleHUD.Instance.combatLogText.text = isAutoRunEnabled
                    ? "<b><color=#2ecc71>[AUTO-RUN ATIVADO]</color></b> Os heróis jogarão e explorarão o andar automaticamente!"
                    : "<b><color=#ffd166>[AUTO-RUN DESATIVADO]</color></b> Controle manual restaurado.";
            }
        }

        if (isAutoRunEnabled)
        {
            if (currentState == BattleState.HeroTurn)
            {
                if (BattleHUD.Instance != null)
                {
                    BattleHUD.Instance.CancelTargetingMode();
                    BattleHUD.Instance.ShowHeroStatusView();
                }
                TriggerAutoRunForCurrentHero();
            }
            else if (IsPostRoomScreenOpen())
            {
                TriggerAutoAdvanceNextRoom(0.45f);
            }
        }
        else if (currentState == BattleState.HeroTurn && selectedHero != null && BattleHUD.Instance != null)
        {
            BattleHUD.Instance.SetHeroTurnUI(selectedHero);
        }
    }

    private bool IsPostRoomScreenOpen()
    {
        if (currentState == BattleState.Victory) return true;
        if (BattleHUD.Instance == null) return false;
        bool victoryOpen = BattleHUD.Instance.victoryPanel != null && BattleHUD.Instance.victoryPanel.activeSelf;
        bool doorsOpen = BattleHUD.Instance.doorSelectionModal != null && BattleHUD.Instance.doorSelectionModal.activeSelf;
        return victoryOpen || doorsOpen;
    }

    private bool IsOverlayMenuBlockingAutoRun()
    {
        if (MainMenuUI.Instance != null && MainMenuUI.Instance.mainMenuRootPanel != null && MainMenuUI.Instance.mainMenuRootPanel.activeSelf)
            return true;
        if (PartyMenuUI.Instance != null && PartyMenuUI.Instance.menuRootPanel != null && PartyMenuUI.Instance.menuRootPanel.activeSelf)
            return true;
        if (BattleHUD.Instance != null && BattleHUD.Instance.partyAdminModal != null && BattleHUD.Instance.partyAdminModal.activeSelf)
            return true;
        return false;
    }

    private void CancelPendingAutoHeroAction()
    {
        if (autoHeroActionRoutine != null)
        {
            StopCoroutine(autoHeroActionRoutine);
            autoHeroActionRoutine = null;
        }
    }

    public void TriggerAutoRunForCurrentHero()
    {
        if (!isAutoRunEnabled || currentState != BattleState.HeroTurn) return;
        if (selectedHero == null || !selectedHero.IsAlive || selectedHero.hasActedThisRound) return;

        if (autoHeroActionRoutine != null)
        {
            StopCoroutine(autoHeroActionRoutine);
        }
        autoHeroActionRoutine = StartCoroutine(AutoHeroActionCoroutine());
    }

    private IEnumerator AutoHeroActionCoroutine()
    {
        yield return new WaitForSeconds(0.48f);

        while (isAutoRunEnabled && IsOverlayMenuBlockingAutoRun())
        {
            yield return null;
        }

        if (!isAutoRunEnabled || currentState != BattleState.HeroTurn || selectedHero == null || !selectedHero.IsAlive || selectedHero.hasActedThisRound)
        {
            autoHeroActionRoutine = null;
            yield break;
        }

        HeroInstance actingHero = selectedHero;
        bool isBossFight = enemiesInBattle.Exists(e => e != null && e.IsAlive && e.data != null && e.data.isBoss);

        // 1º — Auto-Poção de Cura
        int maxHP = Mathf.Max(1, actingHero.GetMaxHP());
        float hpRatio = (float)actingHero.currentHP / maxHP;
        float healThreshold = isBossFight ? 0.55f : 0.45f;
        if (hpRatio <= healThreshold && actingHero.healingPotions > 0)
        {
            autoHeroActionRoutine = null;
            PlayerUsePotion();
            yield break;
        }

        // 2º — Escolha de Habilidade
        if (actingHero.availableSkills == null || actingHero.availableSkills.Count == 0)
        {
            actingHero.InitializeDefaultSkills();
        }

        HeroSkill basicSkill = null;
        HeroSkill bestSkill = null;
        float bestMult = 0f;

        if (actingHero.availableSkills != null)
        {
            foreach (var s in actingHero.availableSkills)
            {
                if (s == null || s.damageMultiplier <= 0f) continue;
                if (s.mpCost == 0 && (basicSkill == null || s.damageMultiplier >= basicSkill.damageMultiplier))
                {
                    basicSkill = s;
                }
                if (actingHero.currentMP >= s.mpCost && s.damageMultiplier > bestMult)
                {
                    bestMult = s.damageMultiplier;
                    bestSkill = s;
                }
            }
        }

        if (basicSkill == null)
        {
            basicSkill = new HeroSkill("Ataque Básico", "Golpe físico padrão.", 0, 1.0f);
        }

        int maxMP = Mathf.Max(1, actingHero.GetMaxMP());
        float mpRatio = (float)actingHero.currentMP / maxMP;
        bool preferSkill = bestSkill != null && bestSkill.mpCost > 0 && (isBossFight || mpRatio >= 0.35f);

        // 3º — Escolha Inteligente de Alvo
        int basicKillIdx = FindKillableEnemyIndex(actingHero, basicSkill, isBossFight);
        HeroSkill chosenSkill;
        int targetIndex;

        if (basicKillIdx >= 0)
        {
            chosenSkill = basicSkill;
            targetIndex = basicKillIdx;
        }
        else
        {
            chosenSkill = preferSkill ? bestSkill : basicSkill;
            targetIndex = ChooseBestTargetEnemyIndex(actingHero, chosenSkill, isBossFight);
        }

        if (targetIndex < 0)
        {
            autoHeroActionRoutine = null;
            yield break;
        }

        autoHeroActionRoutine = null;
        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.CancelTargetingMode();
            BattleHUD.Instance.ShowHeroStatusView();
        }
        PlayerExecuteSkillOn(targetIndex, chosenSkill);
    }

    private int EstimateDamageAgainst(HeroInstance hero, HeroSkill skill, EnemyInstance enemy)
    {
        if (hero == null || skill == null || enemy == null) return 0;
        int basePower = skill.isMagic ? hero.GetMagicAttack() : hero.GetPhysicalAttack();
        int raw = Mathf.RoundToInt(basePower * skill.damageMultiplier);
        int effectiveDef = skill.isMagic ? Mathf.RoundToInt(enemy.defense * 0.65f) : enemy.defense;
        int flatReduction = Mathf.RoundToInt(effectiveDef * 0.40f);
        float pctMultiplier = 100f / (100f + effectiveDef * 1.10f);
        int reduced = Mathf.RoundToInt(Mathf.Max(raw * 0.25f, raw - flatReduction) * pctMultiplier);
        return Mathf.Max(2, reduced);
    }

    private bool HasLivingMinions()
    {
        for (int i = 0; i < enemiesInBattle.Count; i++)
        {
            EnemyInstance e = enemiesInBattle[i];
            if (e != null && e.IsAlive && (e.data == null || !e.data.isBoss))
                return true;
        }
        return false;
    }

    private int FindKillableEnemyIndex(HeroInstance hero, HeroSkill skill, bool isBossFight)
    {
        bool minionsAlive = isBossFight && HasLivingMinions();
        int bestIdx = -1;
        int highestKillableHp = -1;
        for (int i = 0; i < enemiesInBattle.Count; i++)
        {
            EnemyInstance e = enemiesInBattle[i];
            if (e == null || !e.IsAlive) continue;
            if (minionsAlive && e.data != null && e.data.isBoss) continue;

            int est = EstimateDamageAgainst(hero, skill, e);
            if (e.currentHP <= est && e.currentHP > highestKillableHp)
            {
                highestKillableHp = e.currentHP;
                bestIdx = i;
            }
        }
        return bestIdx;
    }

    private int ChooseBestTargetEnemyIndex(HeroInstance hero, HeroSkill skill, bool isBossFight)
    {
        int killIdx = FindKillableEnemyIndex(hero, skill, isBossFight);
        if (killIdx >= 0) return killIdx;

        bool minionsAlive = isBossFight && HasLivingMinions();
        int bestIdx = -1;
        int lowestHP = int.MaxValue;

        for (int i = 0; i < enemiesInBattle.Count; i++)
        {
            EnemyInstance e = enemiesInBattle[i];
            if (e == null || !e.IsAlive) continue;
            if (minionsAlive && e.data != null && e.data.isBoss) continue;

            if (e.currentHP < lowestHP)
            {
                lowestHP = e.currentHP;
                bestIdx = i;
            }
        }

        if (bestIdx >= 0) return bestIdx;

        for (int i = 0; i < enemiesInBattle.Count; i++)
        {
            if (enemiesInBattle[i] != null && enemiesInBattle[i].IsAlive)
                return i;
        }
        return -1;
    }

    public void TriggerAutoAdvanceNextRoom(float delay = 1.15f)
    {
        if (!isAutoRunEnabled) return;
        if (autoNextRoomRoutine != null)
        {
            StopCoroutine(autoNextRoomRoutine);
        }
        autoNextRoomRoutine = StartCoroutine(AutoAdvanceNextRoomCoroutine(delay));
    }

    private IEnumerator AutoAdvanceNextRoomCoroutine(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        while (isAutoRunEnabled && IsOverlayMenuBlockingAutoRun())
        {
            yield return null;
        }

        if (!isAutoRunEnabled || DungeonManager.Instance == null)
        {
            autoNextRoomRoutine = null;
            yield break;
        }

        if (currentState == BattleState.HeroTurn || currentState == BattleState.EnemyTurn || currentState == BattleState.Defeat)
        {
            autoNextRoomRoutine = null;
            yield break;
        }

        var choices = DungeonManager.Instance.currentDoorChoices;
        if (choices == null || choices.Count == 0)
        {
            DungeonManager.Instance.GenerateDoors();
            choices = DungeonManager.Instance.currentDoorChoices;
        }

        if (choices == null || choices.Count == 0)
        {
            autoNextRoomRoutine = null;
            yield break;
        }

        int chosenDoorIdx = ChooseBestAutoRunDoorIndex(choices);
        autoNextRoomRoutine = null;

        if (BattleHUD.Instance != null)
        {
            if (BattleHUD.Instance.doorSelectionModal != null) BattleHUD.Instance.doorSelectionModal.SetActive(false);
            if (BattleHUD.Instance.victoryPanel != null) BattleHUD.Instance.victoryPanel.SetActive(false);
        }

        DungeonManager.Instance.ChooseDoor(chosenDoorIdx);
    }

    private int ChooseBestAutoRunDoorIndex(List<DungeonRoom> choices)
    {
        bool anyHeroHurt = false;
        float sumHpRatio = 0f;
        int aliveCount = 0;

        if (PartyManager.Instance != null && PartyManager.Instance.activeParty != null)
        {
            foreach (var h in PartyManager.Instance.activeParty)
            {
                if (h != null && h.IsAlive)
                {
                    float ratio = (float)h.currentHP / Mathf.Max(1, h.GetMaxHP());
                    sumHpRatio += ratio;
                    aliveCount++;
                    if (ratio < 0.68f) anyHeroHurt = true;
                }
            }
        }

        float avgHpRatio = aliveCount > 0 ? (sumHpRatio / aliveCount) : 1f;
        bool needsRest = anyHeroHurt || avgHpRatio < 0.75f;
        bool healthyForBoss = avgHpRatio >= 0.70f && aliveCount >= 2;

        int restIdx = -1;
        int treasureIdx = -1;
        int bossIdx = -1;
        int combatIdx = -1;
        int nonStairsFallback = -1;

        for (int i = 0; i < choices.Count; i++)
        {
            RoomType rt = choices[i].roomType;
            if (rt == RoomType.Rest && restIdx < 0) restIdx = i;
            else if (rt == RoomType.Treasure && treasureIdx < 0) treasureIdx = i;
            else if (rt == RoomType.Boss && bossIdx < 0) bossIdx = i;
            else if (rt == RoomType.Combat && combatIdx < 0) combatIdx = i;

            if (rt != RoomType.Stairs && nonStairsFallback < 0)
                nonStairsFallback = i;
        }

        if (needsRest && restIdx >= 0) return restIdx;
        if (treasureIdx >= 0) return treasureIdx;
        if (bossIdx >= 0 && (healthyForBoss || combatIdx < 0)) return bossIdx;
        if (combatIdx >= 0) return combatIdx;
        if (nonStairsFallback >= 0) return nonStairsFallback;
        return 0;
    }

    #endregion

    #region Testes Rápidos no Inspector

    [ContextMenu("Iniciar Batalha de Teste (2 a 6 Inimigos)")]
    public void TestStartBattle()
    {
        if (commonFloorEnemies != null && commonFloorEnemies.Length > 0)
        {
            List<EnemyData> encounter = new List<EnemyData>();
            int enemyCount = DungeonManager.Instance != null
                ? DungeonManager.Instance.GetScaledEnemyCountForCurrentRoom()
                : Random.Range(2, 4);
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
