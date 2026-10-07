using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Gerenciador visual da interface de combate e pós-combate (estilo Final Fantasy clássico e Pick Me Up!).
/// Controla o posicionamento em escada dos heróis na direita, 2 a 6 monstros na esquerda,
/// a barra inferior de comandos e cartões dos 4 heróis, seleção clicável no campo ou na HUD,
/// uso de poções (6 por herói), e a tela pós-combate com administração de atributos e escolha de portas.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    public static BattleHUD Instance { get; private set; }

    [Header("Elementos de Interface")]
    public Canvas battleCanvas;
    public GameObject battleRootPanel;

    [Header("Áreas do Campo de Batalha (Arena)")]
    public RectTransform battlefieldArea;
    public RectTransform heroesFieldContainer;
    public RectTransform enemiesFieldContainer;

    [Header("Barra Inferior (Estilo Final Fantasy / Imagem 3)")]
    public RectTransform bottomBarPanel;
    public RectTransform commandBoxPanel;
    public RectTransform heroCardsRowPanel;

    [Header("Textos do Topo e Log")]
    public TextMeshProUGUI turnBannerText;
    public TextMeshProUGUI combatLogText;

    [Header("Botões de Ação do Herói Ativo")]
    public TextMeshProUGUI commandHeaderTitle;
    public Button attackBtn;
    public Button potionBtn;
    public TextMeshProUGUI potionBtnText;
    public Button defendBtn;
    public Button endTurnBtn;

    [Header("Modo de Seleção de Alvo")]
    public GameObject targetModePanel;
    public TextMeshProUGUI targetModePromptText;
    public Button cancelTargetBtn;
    private List<Button> dynamicTargetButtons = new List<Button>();

    [Header("Painéis de Pós-Combate")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victorySummaryText;
    public Button adminPartyBtn;
    public Button nextRoomBtn;
    public Button retreatBtn;

    [Header("Modal de Administração do Grupo (Level Up & Cura)")]
    public GameObject partyAdminModal;
    public Transform adminCardsContainer;
    public Button closeAdminModalBtn;

    [Header("Modal de Escolha de Portas da Próxima Sala")]
    public GameObject doorSelectionModal;
    public Transform doorCardsContainer;
    public Button closeDoorsModalBtn;

    [Header("Painel de Derrota")]
    public GameObject defeatPanel;
    public Button restartFloorBtn;

    // Listas internas de referências visuais
    private List<HeroFieldDisplay> heroFieldDisplays = new List<HeroFieldDisplay>();
    private List<HeroBottomCardDisplay> heroBottomCardDisplays = new List<HeroBottomCardDisplay>();
    private List<EnemyFieldDisplay> enemyFieldDisplays = new List<EnemyFieldDisplay>();

    private bool isTargetingMode = false;
    public bool IsTargetingMode => isTargetingMode;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureEventSystem();
    }

    void Start()
    {
        EnsureEventSystem();
        BuildHUDStructureIfNotExisting();
    }

    /// <summary>
    /// Garante que exista um EventSystem com suporte ao novo Input System na cena para capturar cliques.
    /// </summary>
    public void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            var module = esObj.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            Debug.Log("[BattleHUD] EventSystem e InputSystemUIInputModule ativos.");
        }
    }

    /// <summary>
    /// Cria a estrutura gráfica completa no Canvas se ainda não tiver sido criada.
    /// </summary>
    public void BuildHUDStructureIfNotExisting()
    {
        EnsureEventSystem();

        if (battleCanvas == null)
        {
            battleCanvas = FindAnyObjectByType<Canvas>();
            if (battleCanvas == null)
            {
                GameObject canvasObj = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                battleCanvas = canvasObj.GetComponent<Canvas>();
                battleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        if (battleCanvas.GetComponent<GraphicRaycaster>() == null)
            battleCanvas.gameObject.AddComponent<GraphicRaycaster>();

        CanvasScaler cs = battleCanvas.GetComponent<CanvasScaler>();
        if (cs == null) cs = battleCanvas.gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        if (battleRootPanel != null) return;

        // 1. Painel Raiz de Batalha (Fundo Geral)
        battleRootPanel = CreateUIObject("BattleRootPanel", battleCanvas.transform, Vector2.zero, Vector2.one);
        Image rootBg = battleRootPanel.AddComponent<Image>();
        rootBg.color = new Color(0.06f, 0.08f, 0.12f, 1f);

        // 2. Banner de Turno no Topo
        GameObject bannerObj = CreateUIObject("TurnBanner", battleRootPanel.transform, new Vector2(0.20f, 0.92f), new Vector2(0.80f, 0.98f));
        turnBannerText = bannerObj.AddComponent<TextMeshProUGUI>();
        turnBannerText.fontSize = 26;
        turnBannerText.fontStyle = FontStyles.Bold;
        turnBannerText.alignment = TextAlignmentOptions.Center;
        turnBannerText.color = new Color(1f, 0.85f, 0.35f);
        turnBannerText.text = "=== ARENA DE COMBATE ===";

        // 3. Log de Combate (Logo abaixo do banner)
        GameObject logObj = CreateUIObject("CombatLog", battleRootPanel.transform, new Vector2(0.15f, 0.87f), new Vector2(0.85f, 0.92f));
        combatLogText = logObj.AddComponent<TextMeshProUGUI>();
        combatLogText.fontSize = 18;
        combatLogText.alignment = TextAlignmentOptions.Center;
        combatLogText.color = new Color(0.9f, 0.9f, 0.95f);
        combatLogText.text = "A batalha começou! Selecione seu herói para agir.";

        // 4. Área do Campo de Batalha (Arena - 70% superior)
        GameObject bfObj = CreateUIObject("BattlefieldArea", battleRootPanel.transform, new Vector2(0.02f, 0.25f), new Vector2(0.98f, 0.86f));
        battlefieldArea = bfObj.GetComponent<RectTransform>();

        // Container dos Inimigos (Lado Esquerdo do Campo)
        GameObject efObj = CreateUIObject("EnemiesFieldContainer", battlefieldArea.transform, new Vector2(0.0f, 0.0f), new Vector2(0.50f, 1.0f));
        enemiesFieldContainer = efObj.GetComponent<RectTransform>();

        // Container dos Heróis (Lado Direito do Campo)
        GameObject hfObj = CreateUIObject("HeroesFieldContainer", battlefieldArea.transform, new Vector2(0.50f, 0.0f), new Vector2(1.0f, 1.0f));
        heroesFieldContainer = hfObj.GetComponent<RectTransform>();

        // 5. Barra Inferior (Bottom Bar - Estilo Imagem 3)
        GameObject bbObj = CreateUIObject("BottomBarPanel", battleRootPanel.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.23f));
        bottomBarPanel = bbObj.GetComponent<RectTransform>();
        Image bbBg = bbObj.AddComponent<Image>();
        bbBg.color = new Color(0.10f, 0.12f, 0.18f, 0.95f);

        // 5A. Caixa de Comandos (Lado Esquerdo da barra inferior)
        GameObject cbObj = CreateUIObject("CommandBoxPanel", bottomBarPanel.transform, new Vector2(0.01f, 0.05f), new Vector2(0.28f, 0.95f));
        commandBoxPanel = cbObj.GetComponent<RectTransform>();
        Image cbBg = cbObj.AddComponent<Image>();
        cbBg.color = new Color(0.15f, 0.18f, 0.25f, 0.95f);

        // Título do Herói Ativo nos comandos
        GameObject cbTitleObj = CreateUIObject("CommandTitle", commandBoxPanel.transform, new Vector2(0.04f, 0.72f), new Vector2(0.96f, 0.95f));
        commandHeaderTitle = cbTitleObj.AddComponent<TextMeshProUGUI>();
        commandHeaderTitle.fontSize = 17;
        commandHeaderTitle.fontStyle = FontStyles.Bold;
        commandHeaderTitle.alignment = TextAlignmentOptions.Center;
        commandHeaderTitle.color = new Color(1f, 0.85f, 0.4f);
        commandHeaderTitle.text = "COMANDOS";

        // Botões de Ação na Caixa de Comandos
        attackBtn = CreateButton(commandBoxPanel, "AttackBtn", "[ ATACAR ]", new Vector2(0.04f, 0.38f), new Vector2(0.48f, 0.68f), new Color(0.65f, 0.18f, 0.20f));
        attackBtn.onClick.AddListener(OnClickAttack);

        potionBtn = CreateButton(commandBoxPanel, "PotionBtn", "POÇÃO (6/6)", new Vector2(0.52f, 0.38f), new Vector2(0.96f, 0.68f), new Color(0.18f, 0.58f, 0.32f));
        potionBtnText = potionBtn.GetComponentInChildren<TextMeshProUGUI>();
        potionBtn.onClick.AddListener(OnClickUsePotion);

        defendBtn = CreateButton(commandBoxPanel, "DefendBtn", "[ DEFENDER ]", new Vector2(0.04f, 0.05f), new Vector2(0.48f, 0.34f), new Color(0.20f, 0.38f, 0.65f));
        defendBtn.onClick.AddListener(OnClickDefend);

        endTurnBtn = CreateButton(commandBoxPanel, "EndTurnBtn", "[ ENCERRAR ]", new Vector2(0.52f, 0.05f), new Vector2(0.96f, 0.34f), new Color(0.35f, 0.35f, 0.40f));
        endTurnBtn.onClick.AddListener(OnClickEndTurn);

        // Painel de Modo de Seleção de Alvo (sobreposto na caixa de comandos)
        GameObject tmpObj = CreateUIObject("TargetModePanel", commandBoxPanel.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));
        targetModePanel = tmpObj;
        Image tmpBg = tmpObj.AddComponent<Image>();
        tmpBg.color = new Color(0.12f, 0.14f, 0.20f, 0.98f);

        GameObject promptObj = CreateUIObject("TargetPrompt", targetModePanel.transform, new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.95f));
        targetModePromptText = promptObj.AddComponent<TextMeshProUGUI>();
        targetModePromptText.fontSize = 15;
        targetModePromptText.alignment = TextAlignmentOptions.Center;
        targetModePromptText.color = new Color(1f, 0.45f, 0.45f);
        targetModePromptText.text = "CLIQUE NO MONSTRO NO CAMPO OU ABAIXO:";

        cancelTargetBtn = CreateButton(targetModePanel.transform, "CancelTargetBtn", "[ CANCELAR ]", new Vector2(0.10f, 0.08f), new Vector2(0.90f, 0.48f), new Color(0.40f, 0.40f, 0.45f));
        cancelTargetBtn.onClick.AddListener(CancelTargetingMode);
        targetModePanel.SetActive(false);

        // 5B. Fileira de 4 Cartões de Heróis (Lado Direito da barra inferior)
        GameObject hcrObj = CreateUIObject("HeroCardsRow", bottomBarPanel.transform, new Vector2(0.30f, 0.05f), new Vector2(0.99f, 0.95f));
        heroCardsRowPanel = hcrObj.GetComponent<RectTransform>();
        HorizontalLayoutGroup hlg = hcrObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // 6. Modais Pós-Combate (Vitória, Administração, Escolha de Portas, Derrota)
        BuildPostCombatPanels();
    }

    private void BuildPostCombatPanels()
    {
        // Painel de Vitória
        victoryPanel = CreateUIObject("VictoryPanel", battleRootPanel.transform, new Vector2(0.25f, 0.20f), new Vector2(0.75f, 0.80f));
        Image vBg = victoryPanel.AddComponent<Image>();
        vBg.color = new Color(0.09f, 0.11f, 0.16f, 0.98f);

        GameObject vtObj = CreateUIObject("VictoryTitle", victoryPanel.transform, new Vector2(0.10f, 0.80f), new Vector2(0.90f, 0.96f));
        TextMeshProUGUI vtTmp = vtObj.AddComponent<TextMeshProUGUI>();
        vtTmp.text = "=== SALA LIMPA COM SUCESSO! ===";
        vtTmp.fontSize = 28;
        vtTmp.fontStyle = FontStyles.Bold;
        vtTmp.alignment = TextAlignmentOptions.Center;
        vtTmp.color = new Color(1f, 0.85f, 0.3f);

        GameObject vsObj = CreateUIObject("VictorySummary", victoryPanel.transform, new Vector2(0.10f, 0.45f), new Vector2(0.90f, 0.78f));
        victorySummaryText = vsObj.AddComponent<TextMeshProUGUI>();
        victorySummaryText.fontSize = 19;
        victorySummaryText.alignment = TextAlignmentOptions.Center;
        victorySummaryText.color = Color.white;

        // 3 Botões Principais no Painel de Vitória:
        adminPartyBtn = CreateButton(victoryPanel.transform, "AdminPartyBtn", "[ ADMINISTRAR GRUPO ]", new Vector2(0.15f, 0.28f), new Vector2(0.85f, 0.42f), new Color(0.22f, 0.48f, 0.75f));
        adminPartyBtn.onClick.AddListener(OpenPartyAdminModal);

        nextRoomBtn = CreateButton(victoryPanel.transform, "NextRoomBtn", "[ ESCOLHER PRÓXIMA SALA >> ]", new Vector2(0.15f, 0.14f), new Vector2(0.85f, 0.26f), new Color(0.18f, 0.60f, 0.30f));
        nextRoomBtn.onClick.AddListener(OpenDoorSelectionModal);

        retreatBtn = CreateButton(victoryPanel.transform, "RetreatBtn", "[ RETORNAR À GUILDA ]", new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.12f), new Color(0.55f, 0.35f, 0.15f));
        retreatBtn.onClick.AddListener(OnClickRetreatFromDungeon);

        victoryPanel.SetActive(false);

        // Modal de Administração do Grupo
        partyAdminModal = CreateUIObject("PartyAdminModal", battleRootPanel.transform, new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.90f));
        Image admBg = partyAdminModal.AddComponent<Image>();
        admBg.color = new Color(0.08f, 0.10f, 0.14f, 0.99f);

        GameObject admTitleObj = CreateUIObject("AdminTitle", partyAdminModal.transform, new Vector2(0.10f, 0.88f), new Vector2(0.90f, 0.98f));
        TextMeshProUGUI admTitleTmp = admTitleObj.AddComponent<TextMeshProUGUI>();
        admTitleTmp.text = "=== ADMINISTRAÇÃO DO GRUPO (PONTOS & POÇÕES) ===";
        admTitleTmp.fontSize = 24;
        admTitleTmp.fontStyle = FontStyles.Bold;
        admTitleTmp.alignment = TextAlignmentOptions.Center;
        admTitleTmp.color = new Color(1f, 0.85f, 0.35f);

        GameObject cardsContObj = CreateUIObject("AdminCardsContainer", partyAdminModal.transform, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.86f));
        adminCardsContainer = cardsContObj.transform;
        HorizontalLayoutGroup admHlg = cardsContObj.AddComponent<HorizontalLayoutGroup>();
        admHlg.spacing = 15;
        admHlg.childControlWidth = true;
        admHlg.childControlHeight = true;
        admHlg.childForceExpandWidth = true;
        admHlg.childForceExpandHeight = true;

        closeAdminModalBtn = CreateButton(partyAdminModal.transform, "CloseAdminBtn", "[ CONCLUIR E VOLTAR ]", new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.10f), new Color(0.35f, 0.40f, 0.50f));
        closeAdminModalBtn.onClick.AddListener(ClosePartyAdminModal);
        partyAdminModal.SetActive(false);

        // Modal de Escolha de Portas
        doorSelectionModal = CreateUIObject("DoorSelectionModal", battleRootPanel.transform, new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.82f));
        Image doorBg = doorSelectionModal.AddComponent<Image>();
        doorBg.color = new Color(0.08f, 0.10f, 0.15f, 0.98f);

        GameObject doorTitleObj = CreateUIObject("DoorTitle", doorSelectionModal.transform, new Vector2(0.10f, 0.82f), new Vector2(0.90f, 0.96f));
        TextMeshProUGUI doorTitleTmp = doorTitleObj.AddComponent<TextMeshProUGUI>();
        doorTitleTmp.text = "=== ESCOLHA SEU PRÓXIMO DESTINO ===";
        doorTitleTmp.fontSize = 26;
        doorTitleTmp.fontStyle = FontStyles.Bold;
        doorTitleTmp.alignment = TextAlignmentOptions.Center;
        doorTitleTmp.color = new Color(1f, 0.85f, 0.35f);

        GameObject doorCardsObj = CreateUIObject("DoorCardsContainer", doorSelectionModal.transform, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.78f));
        doorCardsContainer = doorCardsObj.transform;
        HorizontalLayoutGroup doorHlg = doorCardsObj.AddComponent<HorizontalLayoutGroup>();
        doorHlg.spacing = 20;
        doorHlg.childControlWidth = true;
        doorHlg.childControlHeight = true;
        doorHlg.childForceExpandWidth = true;
        doorHlg.childForceExpandHeight = true;

        closeDoorsModalBtn = CreateButton(doorSelectionModal.transform, "CloseDoorsBtn", "[ VOLTAR ]", new Vector2(0.40f, 0.04f), new Vector2(0.60f, 0.14f), new Color(0.40f, 0.40f, 0.45f));
        closeDoorsModalBtn.onClick.AddListener(() => doorSelectionModal.SetActive(false));
        doorSelectionModal.SetActive(false);

        // Painel de Derrota
        defeatPanel = CreateUIObject("DefeatPanel", battleRootPanel.transform, new Vector2(0.25f, 0.30f), new Vector2(0.75f, 0.70f));
        Image defBg = defeatPanel.AddComponent<Image>();
        defBg.color = new Color(0.30f, 0.08f, 0.10f, 0.98f);

        GameObject defTitleObj = CreateUIObject("DefeatTitle", defeatPanel.transform, new Vector2(0.10f, 0.60f), new Vector2(0.90f, 0.90f));
        TextMeshProUGUI defTitleTmp = defTitleObj.AddComponent<TextMeshProUGUI>();
        defTitleTmp.text = "=== GRUPO DERROTADO! ===\nTodos os heróis caíram na masmorra.";
        defTitleTmp.fontSize = 24;
        defTitleTmp.fontStyle = FontStyles.Bold;
        defTitleTmp.alignment = TextAlignmentOptions.Center;
        defTitleTmp.color = Color.white;

        restartFloorBtn = CreateButton(defeatPanel.transform, "RestartFloorBtn", "[ REINICIAR ANDAR ]", new Vector2(0.25f, 0.15f), new Vector2(0.75f, 0.45f), new Color(0.75f, 0.20f, 0.25f));
        restartFloorBtn.onClick.AddListener(() => {
            defeatPanel.SetActive(false);
            if (PartyManager.Instance != null)
            {
                foreach (var h in PartyManager.Instance.activeParty)
                {
                    h.currentHP = h.GetMaxHP();
                    h.healingPotions = 6;
                }
            }
            if (DungeonManager.Instance != null)
                DungeonManager.Instance.StartFloor(DungeonManager.Instance.currentFloor);
        });
        defeatPanel.SetActive(false);
    }

    /// <summary>
    /// Inicializa e reconstrói o campo de batalha para um novo combate.
    /// Posiciona os heróis em escada na direita e os 2 a 6 monstros na esquerda.
    /// </summary>
    public void RefreshBattleArena(List<HeroInstance> heroes, List<EnemyInstance> enemies)
    {
        BuildHUDStructureIfNotExisting();

        victoryPanel.SetActive(false);
        partyAdminModal.SetActive(false);
        doorSelectionModal.SetActive(false);
        defeatPanel.SetActive(false);
        targetModePanel.SetActive(false);
        isTargetingMode = false;

        // Limpa instâncias anteriores
        foreach (Transform child in heroesFieldContainer) Destroy(child.gameObject);
        foreach (Transform child in enemiesFieldContainer) Destroy(child.gameObject);
        foreach (Transform child in heroCardsRowPanel) Destroy(child.gameObject);

        heroFieldDisplays.Clear();
        heroBottomCardDisplays.Clear();
        enemyFieldDisplays.Clear();

        // 1. Posiciona os Heróis na Arena em Escada (Lado Direito - Estilo Final Fantasy Clássico)
        // Posições com leve deslocamento em escada:
        // Herói 0 (topo-direita): X 0.55, Y 0.75
        // Herói 1 (segundo):      X 0.45, Y 0.51
        // Herói 2 (terceiro):     X 0.35, Y 0.27
        // Herói 3 (quarto):       X 0.25, Y 0.03
        float[,] heroPosRatios = new float[,] {
            { 0.48f, 0.72f },
            { 0.38f, 0.48f },
            { 0.28f, 0.24f },
            { 0.18f, 0.01f }
        };

        for (int i = 0; i < heroes.Count; i++)
        {
            HeroInstance hero = heroes[i];
            float posX = heroPosRatios[i % 4, 0];
            float posY = heroPosRatios[i % 4, 1];

            HeroFieldDisplay hfd = CreateHeroFieldEntity(hero, i, posX, posY);
            heroFieldDisplays.Add(hfd);

            // Cria o Cartão correspondente na barra inferior (Imagem 3)
            HeroBottomCardDisplay hbcd = CreateHeroBottomCard(hero, i);
            heroBottomCardDisplays.Add(hbcd);
        }

        // 2. Posiciona os Monstros na Arena (Lado Esquerdo - 2 a 6 Monstros)
        // Se for 1 Boss: posiciona no centro da esquerda com tamanho maior!
        if (enemies.Count == 1 && enemies[0].data.isBoss)
        {
            EnemyFieldDisplay efd = CreateEnemyFieldEntity(enemies[0], 0, 0.25f, 0.30f, 320, 160, true);
            enemyFieldDisplays.Add(efd);
        }
        else
        {
            // Grade de 2 colunas x 3 linhas
            // Coluna de Trás (Esquerda): X 0.08 | Coluna da Frente (Direita): X 0.52
            // Linhas Y: 0.68 (topo), 0.36 (meio), 0.04 (baixo)
            float[,] enemySlotPositions = new float[,] {
                { 0.52f, 0.68f }, // Slot 0: Frente Topo
                { 0.52f, 0.36f }, // Slot 1: Frente Meio
                { 0.52f, 0.04f }, // Slot 2: Frente Baixo
                { 0.08f, 0.68f }, // Slot 3: Trás Topo
                { 0.08f, 0.36f }, // Slot 4: Trás Meio
                { 0.08f, 0.04f }  // Slot 5: Trás Baixo
            };

            for (int i = 0; i < enemies.Count; i++)
            {
                int slotIdx = i < 6 ? i : 5;
                float posX = enemySlotPositions[slotIdx, 0];
                float posY = enemySlotPositions[slotIdx, 1];

                EnemyFieldDisplay efd = CreateEnemyFieldEntity(enemies[i], i, posX, posY, 220, 95, false);
                enemyFieldDisplays.Add(efd);
            }
        }

        UpdateAllStats();
    }

    private HeroFieldDisplay CreateHeroFieldEntity(HeroInstance hero, int index, float xRatio, float yRatio)
    {
        GameObject heroObj = CreateUIObject($"HeroField_{hero.heroName}", heroesFieldContainer.transform, new Vector2(xRatio, yRatio), new Vector2(xRatio + 0.48f, yRatio + 0.24f));
        Image bg = heroObj.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.18f, 0.26f, 0.95f);

        Button btn = heroObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ConfigureButtonColors(btn, new Color(0.14f, 0.18f, 0.26f, 0.95f), new Color(0.25f, 0.35f, 0.55f));
        btn.onClick.AddListener(() => {
            if (BattleManager.Instance.currentState == BattleState.HeroTurn)
            {
                BattleManager.Instance.SelectHero(hero);
            }
        });

        // Nome e Estrelas
        GameObject nameObj = CreateUIObject("Name", heroObj.transform, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        string starBadge = hero.isGoldStar ? $"<color=#FFD700>[{hero.currentStars}★]</color>" : $"<color=#C0C0C0>[{hero.currentStars}★]</color>";
        nameTmp.text = $"{starBadge} <b>{hero.heroName}</b> <size=13><color=#90CAF9>[{hero.heroClass}]</color></size>";
        nameTmp.fontSize = 16;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.color = Color.white;
        nameTmp.raycastTarget = false;

        // Barra de Vida / Status
        GameObject hpObj = CreateUIObject("HPText", heroObj.transform, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.50f));
        TextMeshProUGUI hpTmp = hpObj.AddComponent<TextMeshProUGUI>();
        hpTmp.fontSize = 14;
        hpTmp.color = new Color(0.4f, 0.95f, 0.4f);
        hpTmp.raycastTarget = false;

        return new HeroFieldDisplay {
            hero = hero,
            container = heroObj,
            cardImage = bg,
            nameText = nameTmp,
            hpText = hpTmp
        };
    }

    private HeroBottomCardDisplay CreateHeroBottomCard(HeroInstance hero, int index)
    {
        GameObject cardObj = CreateUIObject($"BottomCard_{hero.heroName}", heroCardsRowPanel.transform, Vector2.zero, Vector2.one);
        Image bg = cardObj.AddComponent<Image>();
        bg.color = new Color(0.13f, 0.16f, 0.22f, 0.95f);

        Button btn = cardObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ConfigureButtonColors(btn, new Color(0.13f, 0.16f, 0.22f, 0.95f), new Color(0.24f, 0.32f, 0.48f));
        btn.onClick.AddListener(() => {
            if (BattleManager.Instance.currentState == BattleState.HeroTurn)
            {
                BattleManager.Instance.SelectHero(hero);
            }
        });

        // Topo: Nome, Estrelas e Classe
        GameObject titleObj = CreateUIObject("Title", cardObj.transform, new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.96f));
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = $"<b>{hero.heroName}</b> <size=12><color=#90CAF9>[{hero.heroClass}]</color></size>";
        titleTmp.fontSize = 15;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.raycastTarget = false;

        // Meio: HP e MP
        GameObject statObj = CreateUIObject("Stats", cardObj.transform, new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.68f));
        TextMeshProUGUI statTmp = statObj.AddComponent<TextMeshProUGUI>();
        statTmp.fontSize = 13;
        statTmp.alignment = TextAlignmentOptions.Center;
        statTmp.raycastTarget = false;

        // Fundo: Poções e Tag de Status ([PRONTO], [AGIU], [DEFESA])
        GameObject botObj = CreateUIObject("StatusTag", cardObj.transform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.32f));
        TextMeshProUGUI botTmp = botObj.AddComponent<TextMeshProUGUI>();
        botTmp.fontSize = 13;
        botTmp.alignment = TextAlignmentOptions.Center;
        botTmp.raycastTarget = false;

        return new HeroBottomCardDisplay {
            hero = hero,
            container = cardObj,
            bgImage = bg,
            titleText = titleTmp,
            statsText = statTmp,
            statusBadgeText = botTmp
        };
    }

    private EnemyFieldDisplay CreateEnemyFieldEntity(EnemyInstance enemy, int index, float xRatio, float yRatio, float w, float h, bool isBoss)
    {
        GameObject enemyObj = CreateUIObject($"EnemyField_{enemy.enemyName}_{index}", enemiesFieldContainer.transform, new Vector2(xRatio, yRatio), new Vector2(xRatio + 0.40f, yRatio + 0.28f));
        Image bg = enemyObj.AddComponent<Image>();
        bg.color = isBoss ? new Color(0.48f, 0.12f, 0.15f, 0.98f) : new Color(0.32f, 0.12f, 0.15f, 0.95f);

        Button btn = enemyObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ConfigureButtonColors(btn, bg.color, new Color(0.65f, 0.22f, 0.26f));

        int targetIdx = index;
        btn.onClick.AddListener(() => {
            if (BattleManager.Instance.currentState == BattleState.HeroTurn)
            {
                ExecuteAttackOnTarget(targetIdx);
            }
        });

        // Nome do Inimigo
        GameObject nameObj = CreateUIObject("Name", enemyObj.transform, new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = isBoss ? $"<color=#FF5555>[CHEFE]</color> <b>{enemy.enemyName}</b>" : $"<color=#FFAAAA>[MONSTRO]</color> <b>{enemy.enemyName}</b>";
        nameTmp.fontSize = isBoss ? 18 : 15;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.color = Color.white;
        nameTmp.raycastTarget = false;

        // Barra de Vida
        GameObject hpObj = CreateUIObject("HPText", enemyObj.transform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.50f));
        TextMeshProUGUI hpTmp = hpObj.AddComponent<TextMeshProUGUI>();
        hpTmp.fontSize = isBoss ? 16 : 14;
        hpTmp.color = new Color(1f, 0.5f, 0.5f);
        hpTmp.raycastTarget = false;

        return new EnemyFieldDisplay {
            enemy = enemy,
            index = index,
            container = enemyObj,
            cardImage = bg,
            nameText = nameTmp,
            hpText = hpTmp
        };
    }

    /// <summary>
    /// Atualiza as estatísticas de vida, mana, poções e indicadores visuais de todos na tela.
    /// </summary>
    public void UpdateAllStats()
    {
        // 1. Heróis no Campo e Cartões Inferiores
        for (int i = 0; i < heroFieldDisplays.Count; i++)
        {
            var hfd = heroFieldDisplays[i];
            var hbcd = heroBottomCardDisplays[i];
            HeroInstance h = hfd.hero;

            if (h.IsAlive)
            {
                hfd.hpText.text = $"HP: {h.currentHP}/{h.GetMaxHP()} | MP: {h.currentMP}/{h.GetMaxMP()}";
                hbcd.statsText.text = $"<color=#66FF66>HP: {h.currentHP}/{h.GetMaxHP()}</color> | <color=#66CCFF>MP: {h.currentMP}/{h.GetMaxMP()}</color>";

                string stateTag;
                if (BattleManager.Instance.IsHeroDefending(h))
                    stateTag = "<color=#80D8FF>[DEFESA]</color>";
                else if (h.hasActedThisRound)
                    stateTag = "<color=#9E9E9E>[AGIU]</color>";
                else
                    stateTag = "<color=#69F0AE>[PRONTO]</color>";

                hbcd.statusBadgeText.text = $"Poções: {h.healingPotions}/6  {stateTag}";
            }
            else
            {
                hfd.hpText.text = "<color=red>[CAÍDO]</color>";
                hfd.cardImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

                hbcd.statsText.text = "<color=red>[CAÍDO EM COMBATE]</color>";
                hbcd.statusBadgeText.text = "Poções: 0/6";
                hbcd.bgImage.color = new Color(0.18f, 0.18f, 0.18f, 0.5f);
            }
        }

        // 2. Monstros no Campo
        for (int i = 0; i < enemyFieldDisplays.Count; i++)
        {
            var efd = enemyFieldDisplays[i];
            EnemyInstance e = efd.enemy;

            if (e.IsAlive)
            {
                efd.hpText.text = $"HP: {e.currentHP}/{e.maxHP}";
            }
            else
            {
                efd.hpText.text = "<color=#888888>[DERROTADO]</color>";
                efd.cardImage.color = new Color(0.15f, 0.15f, 0.15f, 0.4f);
            }
        }
    }

    /// <summary>
    /// Configura a UI para o herói selecionado na Fase do Jogador.
    /// </summary>
    public void SetHeroTurnUI(HeroInstance hero)
    {
        UpdateAllStats();

        if (turnBannerText != null)
            turnBannerText.text = $"=== FASE DO JOGADOR (RODADA {BattleManager.Instance.currentTurnRound}) ===";

        if (hero == null)
        {
            commandHeaderTitle.text = "SELECIONE UM HERÓI";
            attackBtn.interactable = false;
            potionBtn.interactable = false;
            defendBtn.interactable = false;
            return;
        }

        bool canAct = hero.IsAlive && !hero.hasActedThisRound;

        commandHeaderTitle.text = $"HERÓI: {hero.heroName.ToUpper()}";
        if (potionBtnText != null)
            potionBtnText.text = $"POÇÃO ({hero.healingPotions}/6)";

        if (combatLogText != null)
        {
            combatLogText.text = canAct 
                ? $"Vez de <b>{hero.heroName}</b>! Escolha uma ação: [Atacar], [Poção], [Defender] ou selecione outro herói."
                : $"<b>{hero.heroName}</b> já agiu nesta rodada. Selecione outro aventureiro!";
        }

        attackBtn.interactable = canAct;
        potionBtn.interactable = canAct && hero.healingPotions > 0;
        defendBtn.interactable = canAct;
        endTurnBtn.interactable = true;

        CancelTargetingMode();

        // Destaca visualmente o herói selecionado nos cartões e campo
        for (int i = 0; i < heroBottomCardDisplays.Count; i++)
        {
            var hbcd = heroBottomCardDisplays[i];
            var hfd = heroFieldDisplays[i];

            if (hbcd.hero == hero)
            {
                hbcd.bgImage.color = new Color(0.22f, 0.40f, 0.65f, 1f); // Destaque azul brilhante
                hfd.cardImage.color = new Color(0.25f, 0.45f, 0.70f, 1f);
            }
            else if (hbcd.hero.IsAlive)
            {
                hbcd.bgImage.color = hbcd.hero.hasActedThisRound 
                    ? new Color(0.10f, 0.12f, 0.16f, 0.85f) 
                    : new Color(0.14f, 0.17f, 0.24f, 0.95f);

                hfd.cardImage.color = hbcd.hero.hasActedThisRound
                    ? new Color(0.10f, 0.12f, 0.16f, 0.85f)
                    : new Color(0.14f, 0.18f, 0.26f, 0.95f);
            }
        }
    }

    /// <summary>
    /// Configura a UI para a Fase dos Monstros.
    /// </summary>
    public void SetEnemyTurnUI()
    {
        UpdateAllStats();

        if (turnBannerText != null)
            turnBannerText.text = "=== FASE DOS MONSTROS ===";

        commandHeaderTitle.text = "TURNO INIMIGO";
        attackBtn.interactable = false;
        potionBtn.interactable = false;
        defendBtn.interactable = false;
        endTurnBtn.interactable = false;
        CancelTargetingMode();
    }

    #region Comandos do Jogador

    public void OnClickAttack()
    {
        if (BattleManager.Instance.selectedHero == null) return;

        // Se só tiver 1 monstro vivo, ataca direto sem necessitar de segundo clique!
        int firstAlive = -1;
        int aliveCount = 0;
        for (int i = 0; i < enemyFieldDisplays.Count; i++)
        {
            if (enemyFieldDisplays[i].enemy != null && enemyFieldDisplays[i].enemy.IsAlive)
            {
                if (firstAlive == -1) firstAlive = i;
                aliveCount++;
            }
        }

        if (aliveCount == 1 && firstAlive != -1)
        {
            ExecuteAttackOnTarget(firstAlive);
            return;
        }

        // Ativa o modo de seleção de alvo
        EnterTargetingMode();
    }

    public void EnterTargetingMode()
    {
        isTargetingMode = true;
        if (targetModePanel != null) targetModePanel.SetActive(true);

        if (combatLogText != null)
            combatLogText.text = "<b>Escolha qual criatura atacar:</b> Clique no monstro no campo de batalha!";

        // Destaca os monstros vivos no campo com cor pulsante/vermelha
        for (int i = 0; i < enemyFieldDisplays.Count; i++)
        {
            if (enemyFieldDisplays[i].enemy.IsAlive)
            {
                enemyFieldDisplays[i].cardImage.color = new Color(0.70f, 0.20f, 0.25f, 1f);
            }
        }
    }

    public void CancelTargetingMode()
    {
        isTargetingMode = false;
        if (targetModePanel != null) targetModePanel.SetActive(false);

        // Restaura cores originais dos monstros vivos
        for (int i = 0; i < enemyFieldDisplays.Count; i++)
        {
            if (enemyFieldDisplays[i].enemy.IsAlive)
            {
                enemyFieldDisplays[i].cardImage.color = enemyFieldDisplays[i].enemy.data.isBoss 
                    ? new Color(0.48f, 0.12f, 0.15f, 0.98f) 
                    : new Color(0.32f, 0.12f, 0.15f, 0.95f);
            }
        }
    }

    public void ExecuteAttackOnTarget(int enemyIndex)
    {
        CancelTargetingMode();
        BattleManager.Instance.PlayerAttack(enemyIndex);
    }

    public void OnClickUsePotion()
    {
        CancelTargetingMode();
        BattleManager.Instance.PlayerUsePotion();
    }

    public void OnClickDefend()
    {
        CancelTargetingMode();
        BattleManager.Instance.PlayerDefend();
    }

    public void OnClickEndTurn()
    {
        CancelTargetingMode();
        BattleManager.Instance.EndPlayerPhaseManually();
    }

    #endregion

    #region Pós-Combate, Administração de Grupo e Escolha de Portas

    public void ShowVictory(int xp, int gold, List<ItemData> drops)
    {
        UpdateAllStats();
        if (victoryPanel != null) victoryPanel.SetActive(true);

        string dropsText = "";
        if (drops != null && drops.Count > 0)
        {
            dropsText = "\n<b>Itens Obtidos:</b> ";
            foreach (var d in drops) dropsText += $"[{d.itemName}] ";
        }

        // Verifica se algum herói subiu de nível e possui pontos para distribuir
        string levelUpNotice = "";
        int totalUnallocated = 0;
        if (PartyManager.Instance != null)
        {
            foreach (var h in PartyManager.Instance.activeParty)
            {
                if (h.unallocatedAttributePoints > 0)
                    totalUnallocated += h.unallocatedAttributePoints;
            }
        }

        if (totalUnallocated > 0)
        {
            levelUpNotice = $"\n\n<color=#FFD700><b>[LEVEL UP NO GRUPO!]</b></color>\nVocê tem {totalUnallocated} pontos de atributo para distribuir no menu [ADMINISTRAR GRUPO]!";
        }

        if (victorySummaryText != null)
        {
            victorySummaryText.text = $"<b>Recompensas da Câmara:</b>\n+{xp} XP para cada herói vivo\n+{gold} Moedas de Ouro{dropsText}{levelUpNotice}";
        }
    }

    public void ShowDefeat()
    {
        UpdateAllStats();
        if (defeatPanel != null) defeatPanel.SetActive(true);
    }

    public void OpenPartyAdminModal()
    {
        if (partyAdminModal == null) return;
        partyAdminModal.SetActive(true);
        RefreshAdminPartyCards();
    }

    public void ClosePartyAdminModal()
    {
        if (partyAdminModal != null) partyAdminModal.SetActive(false);
    }

    /// <summary>
    /// Constrói os 4 cartões de administração do grupo para distribuir pontos de atributo (STR, DEX, INT, VIT)
    /// e usar poções de cura fora de combate.
    /// </summary>
    public void RefreshAdminPartyCards()
    {
        if (adminCardsContainer == null || PartyManager.Instance == null) return;

        foreach (Transform child in adminCardsContainer) Destroy(child.gameObject);

        foreach (var hero in PartyManager.Instance.activeParty)
        {
            GameObject cardObj = CreateUIObject($"AdminCard_{hero.heroName}", adminCardsContainer, Vector2.zero, Vector2.one);
            Image bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.17f, 0.24f, 0.98f);

            // Nome e Classe
            GameObject titleObj = CreateUIObject("Title", cardObj.transform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f));
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = $"<b>{hero.heroName}</b> (Nv. {hero.level})\n<color=#90CAF9>[{hero.heroClass}]</color>";
            titleTmp.fontSize = 16;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;

            // Pontos Livres Disponíveis
            GameObject ptsObj = CreateUIObject("PointsText", cardObj.transform, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.84f));
            TextMeshProUGUI ptsTmp = ptsObj.AddComponent<TextMeshProUGUI>();
            ptsTmp.text = hero.unallocatedAttributePoints > 0 
                ? $"<color=#FFD700><b>Pontos Livres: {hero.unallocatedAttributePoints}</b></color>"
                : "<color=#888888>Sem pontos livres</color>";
            ptsTmp.fontSize = 14;
            ptsTmp.alignment = TextAlignmentOptions.Center;

            // Linhas de Atributos com botões [+]
            HeroAttributes attrs = hero.GetCurrentAttributes();
            CreateAttributeRow(cardObj.transform, "FORÇA (STR)", attrs.strength, hero, "STR", 0.58f);
            CreateAttributeRow(cardObj.transform, "DESTREZA (DEX)", attrs.dexterity, hero, "DEX", 0.44f);
            CreateAttributeRow(cardObj.transform, "INTELIGÊNCIA (INT)", attrs.intelligence, hero, "INT", 0.30f);
            CreateAttributeRow(cardObj.transform, "VITALIDADE (VIT)", attrs.vitality, hero, "VIT", 0.16f);

            // Botão de Usar Poção de Cura fora de combate
            int healVal = Mathf.RoundToInt(hero.GetMaxHP() * 0.35f) + 10;
            Button healBtn = CreateButton(cardObj.transform, "HealBtn", $"[ CURAR (+{healVal} HP) ] ({hero.healingPotions}/6)", new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.12f), new Color(0.18f, 0.55f, 0.30f));
            healBtn.interactable = hero.healingPotions > 0 && hero.currentHP < hero.GetMaxHP();
            healBtn.onClick.AddListener(() => {
                if (hero.UseHealingPotion(out int healed))
                {
                    RefreshAdminPartyCards();
                    UpdateAllStats();
                }
            });
        }
    }

    private void CreateAttributeRow(Transform parent, string label, int value, HeroInstance hero, string attrKey, float yAnchor)
    {
        GameObject rowObj = CreateUIObject($"AttrRow_{attrKey}", parent, new Vector2(0.06f, yAnchor), new Vector2(0.94f, yAnchor + 0.11f));
        
        GameObject lblObj = CreateUIObject("Label", rowObj.transform, new Vector2(0.0f, 0.0f), new Vector2(0.70f, 1.0f));
        TextMeshProUGUI lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
        lblTmp.text = $"{label}: <b>{value}</b>";
        lblTmp.fontSize = 13;
        lblTmp.alignment = TextAlignmentOptions.Left;
        lblTmp.color = Color.white;

        if (hero.unallocatedAttributePoints > 0)
        {
            Button addBtn = CreateButton(rowObj.transform, "AddBtn", "+", new Vector2(0.75f, 0.1f), new Vector2(0.98f, 0.9f), new Color(0.20f, 0.60f, 0.35f));
            addBtn.onClick.AddListener(() => {
                hero.AllocateAttributePoint(attrKey);
                RefreshAdminPartyCards();
                UpdateAllStats();
            });
        }
    }

    public void OpenDoorSelectionModal()
    {
        if (doorSelectionModal == null || DungeonManager.Instance == null) return;
        doorSelectionModal.SetActive(true);

        foreach (Transform child in doorCardsContainer) Destroy(child.gameObject);

        var choices = DungeonManager.Instance.currentDoorChoices;
        for (int i = 0; i < choices.Count; i++)
        {
            int doorIndex = i;
            var room = choices[i];

            GameObject cardObj = CreateUIObject($"DoorCard_{i}", doorCardsContainer, Vector2.zero, Vector2.one);
            Image bg = cardObj.AddComponent<Image>();
            
            Color doorColor = new Color(0.18f, 0.22f, 0.32f, 0.98f);
            if (room.roomType == RoomType.Boss) doorColor = new Color(0.55f, 0.15f, 0.18f, 0.98f);
            else if (room.roomType == RoomType.Treasure) doorColor = new Color(0.60f, 0.48f, 0.12f, 0.98f);
            else if (room.roomType == RoomType.Rest) doorColor = new Color(0.18f, 0.50f, 0.30f, 0.98f);
            else if (room.roomType == RoomType.Stairs) doorColor = new Color(0.25f, 0.40f, 0.70f, 0.98f);
            bg.color = doorColor;

            Button btn = cardObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            ConfigureButtonColors(btn, doorColor, doorColor * 1.25f);
            btn.onClick.AddListener(() => {
                doorSelectionModal.SetActive(false);
                victoryPanel.SetActive(false);
                DungeonManager.Instance.ChooseDoor(doorIndex);
            });

            // Ícone e Título da Porta
            GameObject titleObj = CreateUIObject("Title", cardObj.transform, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.95f));
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = $"<b>{room.roomTitle}</b>";
            titleTmp.fontSize = 20;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleTmp.raycastTarget = false;

            // Descrição da Porta
            GameObject descObj = CreateUIObject("Desc", cardObj.transform, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.50f));
            TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
            descTmp.text = room.description;
            descTmp.fontSize = 14;
            descTmp.alignment = TextAlignmentOptions.Center;
            descTmp.color = new Color(0.9f, 0.9f, 0.9f);
            descTmp.raycastTarget = false;
        }
    }

    public void OnClickRetreatFromDungeon()
    {
        Debug.Log("🏰 A Guilda recuou em segurança com todos os tesouros e experiência!");
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (battleRootPanel != null) battleRootPanel.SetActive(false);
    }

    #endregion

    #region Utilitários de Interface UGUI

    private GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return obj;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        GameObject btnObj = CreateUIObject(name, parent, anchorMin, anchorMax);
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        ConfigureButtonColors(btn, bgColor, bgColor * 1.3f);

        GameObject txtObj = CreateUIObject("Label", btnObj.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }

    private void ConfigureButtonColors(Button btn, Color normal, Color highlighted)
    {
        ColorBlock cb = btn.colors;
        cb.normalColor = normal;
        cb.highlightedColor = highlighted;
        cb.pressedColor = normal * 0.75f;
        cb.selectedColor = highlighted;
        cb.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        btn.colors = cb;
    }

    #endregion

    private class HeroFieldDisplay
    {
        public HeroInstance hero;
        public GameObject container;
        public Image cardImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
    }

    private class HeroBottomCardDisplay
    {
        public HeroInstance hero;
        public GameObject container;
        public Image bgImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI statsText;
        public TextMeshProUGUI statusBadgeText;
    }

    private class EnemyFieldDisplay
    {
        public EnemyInstance enemy;
        public int index;
        public GameObject container;
        public Image cardImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
    }
}
