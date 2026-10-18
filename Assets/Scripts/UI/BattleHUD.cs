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
/// a barra inferior de comandos, a aba dinâmica de status detalhado e seleção de ataques/habilidades do herói,
/// uso de poções (6 por herói), e a tela pós-combate com administração de atributos e escolha de portas.
/// Todas as fontes e textos foram padronizados na cor branca para máxima nitidez e leitura.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    private static BattleHUD _instance;
    public static BattleHUD Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<BattleHUD>();
                if (_instance == null)
                {
                    GameObject hudObj = new GameObject("BattleHUD", typeof(BattleHUD));
                    _instance = hudObj.GetComponent<BattleHUD>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

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

    [Header("Painel Detalhado do Herói Selecionado (Substitui os cartões)")]
    public RectTransform heroDetailsPanel;
    public Transform heroTabsContainer;
    public GameObject heroStatusView;
    public GameObject heroAttacksView;
    public Transform skillCardsContainer;
    public Button backToStatusBtn;
    public TextMeshProUGUI attacksViewTitle;

    [Header("Textos de Status do Herói Selecionado")]
    public Image statusHeroAvatarImage;
    public TextMeshProUGUI statusHeroTitleText;
    public TextMeshProUGUI statusVitalsText;
    public TextMeshProUGUI statusAttributesText;
    public TextMeshProUGUI statusCombatStatsText;

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
    private List<HeroTabDisplay> heroTabDisplays = new List<HeroTabDisplay>();
    private List<EnemyFieldDisplay> enemyFieldDisplays = new List<EnemyFieldDisplay>();

    private bool isTargetingMode = false;
    public bool IsTargetingMode => isTargetingMode;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
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
        EventSystem es = FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
            var module = esObj.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            Debug.Log("[BattleHUD] EventSystem e InputSystemUIInputModule criados dinamicamente.");
        }
        else
        {
            var oldModule = es.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                DestroyImmediate(oldModule);
            }

            var module = es.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                module = es.gameObject.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }
            else if (module.actionsAsset == null)
            {
                module.AssignDefaultActions();
            }
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

        // Garante no runtime que o material de fonte padrão seja branco puro
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null && defaultFont.material != null)
        {
            defaultFont.material.SetColor("_FaceColor", Color.white);
        }

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
        turnBannerText.color = Color.white;
        turnBannerText.text = "=== ARENA DE COMBATE ===";

        // 3. Log de Combate (Logo abaixo do banner)
        GameObject logObj = CreateUIObject("CombatLog", battleRootPanel.transform, new Vector2(0.15f, 0.87f), new Vector2(0.85f, 0.92f));
        combatLogText = logObj.AddComponent<TextMeshProUGUI>();
        combatLogText.fontSize = 18;
        combatLogText.alignment = TextAlignmentOptions.Center;
        combatLogText.color = Color.white;
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
        commandHeaderTitle.color = Color.white;
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
        targetModePromptText.fontSize = 14;
        targetModePromptText.alignment = TextAlignmentOptions.Center;
        targetModePromptText.color = Color.white;
        targetModePromptText.text = "CLIQUE NO MONSTRO NO CAMPO:";

        cancelTargetBtn = CreateButton(targetModePanel.transform, "CancelTargetBtn", "[ CANCELAR ]", new Vector2(0.10f, 0.08f), new Vector2(0.90f, 0.48f), new Color(0.40f, 0.40f, 0.45f));
        cancelTargetBtn.onClick.AddListener(CancelTargetingMode);
        targetModePanel.SetActive(false);

        // 5B. Painel Detalhado do Herói Selecionado & Habilidades (Lado Direito da barra inferior)
        GameObject hdObj = CreateUIObject("HeroDetailsPanel", bottomBarPanel.transform, new Vector2(0.30f, 0.05f), new Vector2(0.99f, 0.95f));
        heroDetailsPanel = hdObj.GetComponent<RectTransform>();
        Image hdBg = hdObj.AddComponent<Image>();
        hdBg.color = new Color(0.12f, 0.15f, 0.22f, 0.98f);

        // Barra de Abas dos Heróis (Topo do painel direito)
        GameObject tabsObj = CreateUIObject("HeroTabsRow", heroDetailsPanel.transform, new Vector2(0.01f, 0.80f), new Vector2(0.99f, 0.98f));
        heroTabsContainer = tabsObj.transform;
        HorizontalLayoutGroup tabsHlg = tabsObj.AddComponent<HorizontalLayoutGroup>();
        tabsHlg.spacing = 8;
        tabsHlg.childControlWidth = true;
        tabsHlg.childControlHeight = true;
        tabsHlg.childForceExpandWidth = true;
        tabsHlg.childForceExpandHeight = true;

        // Visualização 1: Status Detalhado do Herói (Vitals, Atributos Primários e Estatísticas de Combate)
        heroStatusView = CreateUIObject("HeroStatusView", heroDetailsPanel.transform, new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.78f));

        // Coluna 1: Avatar, Nome, Nível, Vitais (HP/MP/Poções)
        GameObject c1Obj = CreateUIObject("Col1_Vitals", heroStatusView.transform, new Vector2(0.00f, 0.00f), new Vector2(0.33f, 1.00f));
        
        GameObject c1AvatarObj = CreateUIObject("HeroAvatar", c1Obj.transform, new Vector2(0.02f, 0.08f), new Vector2(0.32f, 0.92f));
        statusHeroAvatarImage = c1AvatarObj.AddComponent<Image>();
        statusHeroAvatarImage.preserveAspect = true;
        statusHeroAvatarImage.raycastTarget = false;

        GameObject c1TitleObj = CreateUIObject("HeroTitle", c1Obj.transform, new Vector2(0.34f, 0.60f), new Vector2(0.98f, 0.98f));
        statusHeroTitleText = c1TitleObj.AddComponent<TextMeshProUGUI>();
        statusHeroTitleText.fontSize = 16;
        statusHeroTitleText.fontStyle = FontStyles.Bold;
        statusHeroTitleText.color = Color.white;

        GameObject c1VitalsObj = CreateUIObject("HeroVitals", c1Obj.transform, new Vector2(0.34f, 0.02f), new Vector2(0.98f, 0.58f));
        statusVitalsText = c1VitalsObj.AddComponent<TextMeshProUGUI>();
        statusVitalsText.fontSize = 14;
        statusVitalsText.color = Color.white;

        // Coluna 2: Atributos Primários (STR, DEX, INT, VIT)
        GameObject c2Obj = CreateUIObject("Col2_Attributes", heroStatusView.transform, new Vector2(0.34f, 0.00f), new Vector2(0.66f, 1.00f));
        GameObject c2TextObj = CreateUIObject("AttrText", c2Obj.transform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.98f));
        statusAttributesText = c2TextObj.AddComponent<TextMeshProUGUI>();
        statusAttributesText.fontSize = 15;
        statusAttributesText.color = Color.white;

        // Coluna 3: Estatísticas de Combate Derivadas (ATQ, DEF, MATQ, VEL, CRIT)
        GameObject c3Obj = CreateUIObject("Col3_CombatStats", heroStatusView.transform, new Vector2(0.67f, 0.00f), new Vector2(1.00f, 1.00f));
        GameObject c3TextObj = CreateUIObject("StatsText", c3Obj.transform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.98f));
        statusCombatStatsText = c3TextObj.AddComponent<TextMeshProUGUI>();
        statusCombatStatsText.fontSize = 15;
        statusCombatStatsText.color = Color.white;

        // Visualização 2: Lista de Ataques e Habilidades
        heroAttacksView = CreateUIObject("HeroAttacksView", heroDetailsPanel.transform, new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.78f));

        // Topo da aba de ataques: Título e Botão Voltar
        GameObject atkTopObj = CreateUIObject("AttacksHeader", heroAttacksView.transform, new Vector2(0.00f, 0.74f), new Vector2(1.00f, 1.00f));
        GameObject atkTitleObj = CreateUIObject("AttacksTitle", atkTopObj.transform, new Vector2(0.01f, 0.00f), new Vector2(0.70f, 1.00f));
        attacksViewTitle = atkTitleObj.AddComponent<TextMeshProUGUI>();
        attacksViewTitle.fontSize = 14;
        attacksViewTitle.fontStyle = FontStyles.Bold;
        attacksViewTitle.color = Color.white;
        attacksViewTitle.alignment = TextAlignmentOptions.MidlineLeft;
        attacksViewTitle.text = "ESCOLHA O ATAQUE OU HABILIDADE:";

        backToStatusBtn = CreateButton(atkTopObj.transform, "BackToStatusBtn", "[ VOLTAR AOS STATUS ]", new Vector2(0.72f, 0.05f), new Vector2(0.99f, 0.95f), new Color(0.35f, 0.38f, 0.45f));
        backToStatusBtn.onClick.AddListener(ShowHeroStatusView);

        // Container dos Cartões de Habilidades
        GameObject skillsContObj = CreateUIObject("SkillsContainer", heroAttacksView.transform, new Vector2(0.00f, 0.02f), new Vector2(1.00f, 0.70f));
        skillCardsContainer = skillsContObj.transform;
        HorizontalLayoutGroup skillsHlg = skillsContObj.AddComponent<HorizontalLayoutGroup>();
        skillsHlg.spacing = 10;
        skillsHlg.childControlWidth = true;
        skillsHlg.childControlHeight = true;
        skillsHlg.childForceExpandWidth = true;
        skillsHlg.childForceExpandHeight = true;

        heroAttacksView.SetActive(false);

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
        vtTmp.color = Color.white;

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
        admTitleTmp.color = Color.white;

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
        doorTitleTmp.color = Color.white;

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
        if (battleRootPanel != null) battleRootPanel.SetActive(true);

        victoryPanel.SetActive(false);
        partyAdminModal.SetActive(false);
        doorSelectionModal.SetActive(false);
        defeatPanel.SetActive(false);
        targetModePanel.SetActive(false);
        isTargetingMode = false;

        // Limpa instâncias anteriores
        foreach (Transform child in heroesFieldContainer) Destroy(child.gameObject);
        foreach (Transform child in enemiesFieldContainer) Destroy(child.gameObject);
        foreach (Transform child in heroTabsContainer) Destroy(child.gameObject);

        heroFieldDisplays.Clear();
        heroTabDisplays.Clear();
        enemyFieldDisplays.Clear();

        // 1. Posiciona os Heróis na Arena em Escada (Lado Direito - Estilo Final Fantasy Clássico)
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

            // Cria a aba correspondente no topo do painel detalhado
            HeroTabDisplay htd = CreateHeroTab(hero, i);
            heroTabDisplays.Add(htd);
        }

        // 2. Posiciona os Monstros na Arena (Lado Esquerdo - 2 a 6 Monstros)
        if (enemies.Count == 1 && enemies[0].data.isBoss)
        {
            EnemyFieldDisplay efd = CreateEnemyFieldEntity(enemies[0], 0, 0.25f, 0.30f, 320, 160, true);
            enemyFieldDisplays.Add(efd);
        }
        else
        {
            // Grade de 2 colunas x 3 linhas
            // Coluna da Frente (Direita): X 0.52 | Coluna de Trás (Esquerda): X 0.08
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

        // Avatar do Herói (visão lateral / arte em pixel)
        GameObject avatarObj = CreateUIObject("Avatar", heroObj.transform, new Vector2(0.02f, 0.06f), new Vector2(0.36f, 0.94f));
        Image avatarImg = avatarObj.AddComponent<Image>();
        avatarImg.preserveAspect = true;
        avatarImg.raycastTarget = false;
        Sprite spr = GetHeroSprite(hero);
        if (spr != null)
        {
            avatarImg.sprite = spr;
            avatarImg.color = Color.white;
        }
        else
        {
            avatarImg.color = Color.clear;
        }

        // Nome e Estrelas
        GameObject nameObj = CreateUIObject("Name", heroObj.transform, new Vector2(0.38f, 0.52f), new Vector2(0.98f, 0.95f));
        TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        string starBadge = hero.isGoldStar ? $"[{hero.currentStars}* Ouro]" : $"[{hero.currentStars}*]";
        nameTmp.text = $"{starBadge} <b>{hero.heroName}</b>\n<size=12>[{hero.heroClass}]</size>";
        nameTmp.fontSize = 15;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.color = Color.white;
        nameTmp.raycastTarget = false;

        // Barra de Vida / Status
        GameObject hpObj = CreateUIObject("HPText", heroObj.transform, new Vector2(0.38f, 0.08f), new Vector2(0.98f, 0.48f));
        TextMeshProUGUI hpTmp = hpObj.AddComponent<TextMeshProUGUI>();
        hpTmp.fontSize = 13;
        hpTmp.color = Color.white;
        hpTmp.raycastTarget = false;

        return new HeroFieldDisplay {
            hero = hero,
            container = heroObj,
            cardImage = bg,
            avatarImage = avatarImg,
            nameText = nameTmp,
            hpText = hpTmp
        };
    }

    private HeroTabDisplay CreateHeroTab(HeroInstance hero, int index)
    {
        GameObject tabObj = CreateUIObject($"HeroTab_{hero.heroName}", heroTabsContainer, Vector2.zero, Vector2.one);
        Image bg = tabObj.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.20f, 0.28f, 0.95f);

        Button btn = tabObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ConfigureButtonColors(btn, bg.color, new Color(0.28f, 0.38f, 0.55f));
        btn.onClick.AddListener(() => {
            if (BattleManager.Instance.currentState == BattleState.HeroTurn)
            {
                BattleManager.Instance.SelectHero(hero);
            }
        });

        GameObject txtObj = CreateUIObject("TabText", tabObj.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return new HeroTabDisplay {
            hero = hero,
            tabObj = tabObj,
            tabBg = bg,
            tabText = tmp,
            tabButton = btn
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

        // Avatar do Monstro / Chefe
        GameObject avatarObj = CreateUIObject("Avatar", enemyObj.transform, new Vector2(0.02f, 0.06f), new Vector2(0.36f, 0.94f));
        Image avatarImg = avatarObj.AddComponent<Image>();
        avatarImg.preserveAspect = true;
        avatarImg.raycastTarget = false;
        Sprite spr = GetEnemySprite(enemy);
        if (spr != null)
        {
            avatarImg.sprite = spr;
            avatarImg.color = Color.white;
        }
        else
        {
            avatarImg.color = Color.clear;
        }

        // Nome do Inimigo
        GameObject nameObj = CreateUIObject("Name", enemyObj.transform, new Vector2(0.38f, 0.50f), new Vector2(0.98f, 0.95f));
        TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = isBoss ? $"[CHEFE] <b>{enemy.enemyName}</b>" : $"<b>{enemy.enemyName}</b>";
        nameTmp.fontSize = isBoss ? 16 : 14;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.color = Color.white;
        nameTmp.raycastTarget = false;

        // Barra de Vida
        GameObject hpObj = CreateUIObject("HPText", enemyObj.transform, new Vector2(0.38f, 0.08f), new Vector2(0.98f, 0.48f));
        TextMeshProUGUI hpTmp = hpObj.AddComponent<TextMeshProUGUI>();
        hpTmp.fontSize = isBoss ? 15 : 13;
        hpTmp.color = Color.white;
        hpTmp.raycastTarget = false;

        return new EnemyFieldDisplay {
            enemy = enemy,
            index = index,
            container = enemyObj,
            cardImage = bg,
            avatarImage = avatarImg,
            nameText = nameTmp,
            hpText = hpTmp
        };
    }

    /// <summary>
    /// Atualiza as estatísticas de vida, mana, poções e indicadores visuais de todos na tela.
    /// </summary>
    public void UpdateAllStats()
    {
        // 1. Heróis no Campo e Abas Superiores
        for (int i = 0; i < heroFieldDisplays.Count; i++)
        {
            var hfd = heroFieldDisplays[i];
            HeroInstance h = hfd.hero;

            hfd.hpText.color = Color.white;
            if (h.IsAlive)
            {
                hfd.hpText.text = $"HP: {h.currentHP}/{h.GetMaxHP()} | MP: {h.currentMP}/{h.GetMaxMP()}";
                if (hfd.avatarImage != null) hfd.avatarImage.color = Color.white;
            }
            else
            {
                hfd.hpText.text = "[CAÍDO]";
                hfd.cardImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                if (hfd.avatarImage != null) hfd.avatarImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }

        for (int i = 0; i < heroTabDisplays.Count; i++)
        {
            var htd = heroTabDisplays[i];
            HeroInstance h = htd.hero;

            htd.tabText.color = Color.white;
            string shortName = h.heroName.Split(',')[0].Trim();
            if (h.IsAlive)
            {
                string tag;
                if (BattleManager.Instance.IsHeroDefending(h))
                    tag = "[DEF]";
                else if (h.hasActedThisRound)
                    tag = "[AGIU]";
                else
                    tag = "[OK]";

                htd.tabText.text = $"<b>{shortName}</b> [{h.heroClass}] {tag}";
            }
            else
            {
                htd.tabText.text = $"<b>{shortName}</b> [CAÍDO]";
                htd.tabBg.color = new Color(0.2f, 0.15f, 0.15f, 0.5f);
            }
        }

        // 2. Monstros no Campo
        for (int i = 0; i < enemyFieldDisplays.Count; i++)
        {
            var efd = enemyFieldDisplays[i];
            EnemyInstance e = efd.enemy;

            efd.hpText.color = Color.white;
            if (e.IsAlive)
            {
                efd.hpText.text = $"HP: {e.currentHP}/{e.maxHP}";
                if (efd.avatarImage != null) efd.avatarImage.color = Color.white;
            }
            else
            {
                efd.hpText.text = "[DERROTADO]";
                efd.cardImage.color = new Color(0.15f, 0.15f, 0.15f, 0.4f);
                if (efd.avatarImage != null) efd.avatarImage.color = new Color(0.25f, 0.25f, 0.25f, 0.3f);
            }
        }

        // 3. Atualiza os dados detalhados do herói atualmente selecionado
        RefreshHeroStatusPanel(BattleManager.Instance != null ? BattleManager.Instance.selectedHero : null);
    }

    /// <summary>
    /// Atualiza as 3 colunas de status (Vitais, Atributos Primários e Estatísticas de Combate) do herói ativo com texto branco.
    /// </summary>
    public void RefreshHeroStatusPanel(HeroInstance hero)
    {
        if (statusHeroTitleText == null) return;

        statusHeroTitleText.color = Color.white;
        statusVitalsText.color = Color.white;
        statusAttributesText.color = Color.white;
        statusCombatStatsText.color = Color.white;

        if (statusHeroAvatarImage != null)
        {
            if (hero != null)
            {
                Sprite spr = GetHeroSprite(hero);
                if (spr != null)
                {
                    statusHeroAvatarImage.sprite = spr;
                    statusHeroAvatarImage.color = hero.IsAlive ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.6f);
                }
                else
                {
                    statusHeroAvatarImage.color = Color.clear;
                }
            }
            else
            {
                statusHeroAvatarImage.color = Color.clear;
            }
        }

        if (hero == null)
        {
            statusHeroTitleText.text = "<b>Nenhum aventureiro selecionado.</b>";
            statusVitalsText.text = "Clique em um herói no campo de batalha ou nas abas acima.";
            statusAttributesText.text = "";
            statusCombatStatsText.text = "";
            return;
        }

        if (hero.availableSkills == null || hero.availableSkills.Count == 0)
        {
            hero.InitializeDefaultSkills();
        }

        string starBadge = hero.isGoldStar ? $"[{hero.currentStars}* Ouro]" : $"[{hero.currentStars}*]";
        statusHeroTitleText.text = $"{starBadge} <b>{hero.heroName}</b> (Nv. {hero.level})\nClasse: {hero.heroClass}";

        if (hero.IsAlive)
        {
            string stateTag;
            if (BattleManager.Instance.IsHeroDefending(hero))
                stateTag = "[EM DEFESA]";
            else if (hero.hasActedThisRound)
                stateTag = "[JÁ AGIU]";
            else
                stateTag = "[PRONTO PARA AGIR]";

            statusVitalsText.text =
                $"<b>HP:</b> {hero.currentHP} / {hero.GetMaxHP()}\n" +
                $"<b>MP:</b> {hero.currentMP} / {hero.GetMaxMP()}\n" +
                $"<b>Poções de Cura:</b> {hero.healingPotions}/6\n" +
                $"<b>Estado:</b> {stateTag}";

            HeroAttributes attrs = hero.GetCurrentAttributes();
            string freePointsStr = hero.unallocatedAttributePoints > 0
                ? $"\n<b>Pontos Livres:</b> +{hero.unallocatedAttributePoints}"
                : "";

            statusAttributesText.text =
                "<b>ATRIBUTOS PRIMÁRIOS</b>\n" +
                $"FORÇA (STR): <b>{attrs.strength}</b>\n" +
                $"DESTREZA (DEX): <b>{attrs.dexterity}</b>\n" +
                $"INTELIGÊNCIA (INT): <b>{attrs.intelligence}</b>\n" +
                $"VITALIDADE (VIT): <b>{attrs.vitality}</b>" +
                freePointsStr;

            statusCombatStatsText.text =
                "<b>ESTATÍSTICAS DE COMBATE</b>\n" +
                $"ATQ FÍSICO: <b>{hero.GetPhysicalAttack()}</b>\n" +
                $"DEF FÍSICA: <b>{hero.GetPhysicalDefense()}</b>\n" +
                $"ATQ MÁGICO: <b>{hero.GetMagicAttack()}</b>\n" +
                $"VELOCIDADE: <b>{hero.GetSpeed()}</b>\n" +
                $"TAXA CRÍTICA: <b>{hero.GetCritRate():F1}%</b>";
        }
        else
        {
            statusVitalsText.text = "<b>[CAÍDO EM COMBATE]</b>\nEste herói foi nocauteado e não pode agir.";
            statusAttributesText.text = "";
            statusCombatStatsText.text = "";
        }
    }

    /// <summary>
    /// Exibe a visualização de status do herói e oculta a lista de habilidades.
    /// </summary>
    public void ShowHeroStatusView()
    {
        if (heroStatusView != null) heroStatusView.SetActive(true);
        if (heroAttacksView != null) heroAttacksView.SetActive(false);
        RefreshHeroStatusPanel(BattleManager.Instance != null ? BattleManager.Instance.selectedHero : null);
    }

    /// <summary>
    /// Exibe a visualização de ataques e habilidades disponíveis do herói ativo.
    /// </summary>
    public void ShowHeroAttacksView()
    {
        HeroInstance hero = BattleManager.Instance != null ? BattleManager.Instance.selectedHero : null;
        if (hero == null || !hero.IsAlive || hero.hasActedThisRound) return;

        if (hero.availableSkills == null || hero.availableSkills.Count == 0)
        {
            hero.InitializeDefaultSkills();
        }

        if (heroStatusView != null) heroStatusView.SetActive(false);
        if (heroAttacksView != null) heroAttacksView.SetActive(true);

        if (attacksViewTitle != null)
        {
            attacksViewTitle.color = Color.white;
            attacksViewTitle.text = $"HABILIDADES & ATAQUES DE <b>{hero.heroName.ToUpper()}</b> (MP: {hero.currentMP}/{hero.GetMaxMP()}):";
        }

        // Limpa cartões de habilidades anteriores
        foreach (Transform child in skillCardsContainer) Destroy(child.gameObject);

        // Cria um cartão para cada habilidade do herói
        for (int i = 0; i < hero.availableSkills.Count; i++)
        {
            HeroSkill skill = hero.availableSkills[i];
            CreateSkillCard(hero, skill);
        }
    }

    private void CreateSkillCard(HeroInstance hero, HeroSkill skill)
    {
        GameObject cardObj = CreateUIObject($"SkillCard_{skill.skillName}", skillCardsContainer, Vector2.zero, Vector2.one);
        Image bg = cardObj.AddComponent<Image>();

        bool hasMp = hero.currentMP >= skill.mpCost;
        Color normalColor = hasMp 
            ? (skill.isMagic ? new Color(0.18f, 0.28f, 0.48f, 0.98f) : new Color(0.35f, 0.20f, 0.22f, 0.98f))
            : new Color(0.20f, 0.20f, 0.22f, 0.80f);

        bg.color = normalColor;

        Button btn = cardObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ConfigureButtonColors(btn, normalColor, normalColor * 1.3f);
        btn.interactable = hasMp;

        btn.onClick.AddListener(() => {
            OnSelectSkillToExecute(skill);
        });

        // Título / Nome da Habilidade
        GameObject titleObj = CreateUIObject("Title", cardObj.transform, new Vector2(0.04f, 0.65f), new Vector2(0.96f, 0.96f));
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.fontSize = 15;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.text = $"<b>{skill.skillName}</b>";
        titleTmp.raycastTarget = false;

        // Custo e Tipo
        GameObject costObj = CreateUIObject("Cost", cardObj.transform, new Vector2(0.04f, 0.38f), new Vector2(0.96f, 0.65f));
        TextMeshProUGUI costTmp = costObj.AddComponent<TextMeshProUGUI>();
        costTmp.fontSize = 13;
        costTmp.alignment = TextAlignmentOptions.Center;
        costTmp.color = Color.white;
        costTmp.raycastTarget = false;

        string costStr = skill.mpCost > 0 ? $"Custo: {skill.mpCost} MP" : "Sem custo (0 MP)";
        string typeStr = skill.isMagic ? "[Mágico]" : "[Físico]";
        string mpWarning = hasMp ? "" : " [SEM MP]";
        costTmp.text = $"{costStr} | {typeStr}{mpWarning}";

        // Descrição e Dano
        GameObject descObj = CreateUIObject("Desc", cardObj.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.38f));
        TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
        descTmp.fontSize = 12;
        descTmp.alignment = TextAlignmentOptions.Center;
        descTmp.color = Color.white;
        descTmp.raycastTarget = false;

        string critBonusStr = skill.critBonus > 0 ? $" (+{skill.critBonus}% Crítico)" : "";
        descTmp.text = $"Dano: {Mathf.RoundToInt(skill.damageMultiplier * 100)}%{critBonusStr}\n<size=11>{skill.description}</size>";
    }

    private void OnSelectSkillToExecute(HeroSkill skill)
    {
        if (BattleManager.Instance.selectedHero == null) return;
        BattleManager.Instance.SelectSkillToCast(skill);

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

        // Ativa o modo de seleção de alvo no campo
        EnterTargetingMode(skill);
    }

    /// <summary>
    /// Configura a UI para o herói selecionado na Fase do Jogador.
    /// </summary>
    public void SetHeroTurnUI(HeroInstance hero)
    {
        UpdateAllStats();

        if (turnBannerText != null)
        {
            turnBannerText.color = Color.white;
            turnBannerText.text = $"=== FASE DO JOGADOR (RODADA {BattleManager.Instance.currentTurnRound}) ===";
        }

        ShowHeroStatusView();

        if (hero == null)
        {
            commandHeaderTitle.color = Color.white;
            commandHeaderTitle.text = "SELECIONE UM HERÓI";
            attackBtn.interactable = false;
            potionBtn.interactable = false;
            defendBtn.interactable = false;
            return;
        }

        bool canAct = hero.IsAlive && !hero.hasActedThisRound;

        commandHeaderTitle.color = Color.white;
        commandHeaderTitle.text = $"HERÓI: {hero.heroName.ToUpper()}";
        if (potionBtnText != null)
        {
            potionBtnText.color = Color.white;
            potionBtnText.text = $"POÇÃO ({hero.healingPotions}/6)";
        }

        if (combatLogText != null)
        {
            combatLogText.color = Color.white;
            combatLogText.text = canAct 
                ? $"Vez de <b>{hero.heroName}</b>! Escolha uma ação: [Atacar], [Poção], [Defender] ou selecione outro herói."
                : $"<b>{hero.heroName}</b> já agiu nesta rodada. Selecione outro aventureiro!";
        }

        attackBtn.interactable = canAct;
        potionBtn.interactable = canAct && hero.healingPotions > 0;
        defendBtn.interactable = canAct;
        endTurnBtn.interactable = true;

        CancelTargetingMode();

        // Destaca visualmente o herói selecionado na aba e no campo
        for (int i = 0; i < heroTabDisplays.Count; i++)
        {
            var htd = heroTabDisplays[i];
            var hfd = (i < heroFieldDisplays.Count) ? heroFieldDisplays[i] : null;

            if (htd.hero == hero)
            {
                htd.tabBg.color = new Color(0.25f, 0.48f, 0.78f, 1f); // Destaque azul brilhante
                if (hfd != null) hfd.cardImage.color = new Color(0.25f, 0.45f, 0.70f, 1f);
            }
            else if (htd.hero.IsAlive)
            {
                htd.tabBg.color = htd.hero.hasActedThisRound 
                    ? new Color(0.12f, 0.14f, 0.18f, 0.85f) 
                    : new Color(0.16f, 0.20f, 0.28f, 0.95f);

                if (hfd != null)
                {
                    hfd.cardImage.color = htd.hero.hasActedThisRound
                        ? new Color(0.10f, 0.12f, 0.16f, 0.85f)
                        : new Color(0.14f, 0.18f, 0.26f, 0.95f);
                }
            }
        }
    }

    /// <summary>
    /// Configura a UI para a Fase dos Monstros.
    /// </summary>
    public void SetEnemyTurnUI()
    {
        UpdateAllStats();
        ShowHeroStatusView();

        if (turnBannerText != null)
        {
            turnBannerText.color = Color.white;
            turnBannerText.text = "=== FASE DOS MONSTROS ===";
        }

        commandHeaderTitle.color = Color.white;
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
        if (!BattleManager.Instance.selectedHero.IsAlive || BattleManager.Instance.selectedHero.hasActedThisRound) return;

        // Abre a aba com todos os ataques e habilidades daquele aventureiro
        ShowHeroAttacksView();

        if (combatLogText != null)
        {
            combatLogText.color = Color.white;
            combatLogText.text = $"Escolha qual habilidade ou ataque <b>{BattleManager.Instance.selectedHero.heroName}</b> deve utilizar:";
        }
    }

    public void EnterTargetingMode(HeroSkill skill = null)
    {
        isTargetingMode = true;
        if (targetModePanel != null) targetModePanel.SetActive(true);

        string skillName = skill != null ? skill.skillName : (BattleManager.Instance.pendingSkill != null ? BattleManager.Instance.pendingSkill.skillName : "Ataque Básico");
        if (targetModePromptText != null)
        {
            targetModePromptText.color = Color.white;
            targetModePromptText.text = $"[{skillName.ToUpper()}] SELECIONADO!\nCLIQUE NO MONSTRO ALVO:";
        }

        if (combatLogText != null)
        {
            combatLogText.color = Color.white;
            combatLogText.text = $"<b>{skillName}:</b> Clique no monstro que deseja atingir no campo de batalha!";
        }

        // Destaca os monstros vivos no campo com cor pulsante/vermelha
        for (int i = 0; i < enemyFieldDisplays.Count; i++)
        {
            if (enemyFieldDisplays[i].enemy.IsAlive)
            {
                enemyFieldDisplays[i].cardImage.color = new Color(0.75f, 0.20f, 0.25f, 1f);
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
        BattleManager.Instance.PlayerExecuteSkillOn(enemyIndex);
        ShowHeroStatusView();
    }

    public void OnClickUsePotion()
    {
        CancelTargetingMode();
        ShowHeroStatusView();
        BattleManager.Instance.PlayerUsePotion();
    }

    public void OnClickDefend()
    {
        CancelTargetingMode();
        ShowHeroStatusView();
        BattleManager.Instance.PlayerDefend();
    }

    public void OnClickEndTurn()
    {
        CancelTargetingMode();
        ShowHeroStatusView();
        BattleManager.Instance.EndPlayerPhaseManually();
    }

    #endregion

    #region Pós-Combate, Administração de Grupo e Escolha de Portas

    public void ShowVictory(int xp, int gold, List<ItemData> drops)
    {
        UpdateAllStats();
        ShowHeroStatusView();
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
            levelUpNotice = $"\n\n<b>[LEVEL UP NO GRUPO!]</b>\nVocê tem {totalUnallocated} pontos de atributo para distribuir no menu [ADMINISTRAR GRUPO]!";
        }

        if (victorySummaryText != null)
        {
            victorySummaryText.color = Color.white;
            victorySummaryText.text = $"<b>Recompensas da Câmara:</b>\n+{xp} XP para cada herói vivo\n+{gold} Moedas de Ouro{dropsText}{levelUpNotice}";
        }
    }

    public void ShowDefeat()
    {
        UpdateAllStats();
        ShowHeroStatusView();
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
            titleTmp.text = $"<b>{hero.heroName}</b> (Nv. {hero.level})\n[{hero.heroClass}]";
            titleTmp.fontSize = 16;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;

            // Pontos Livres Disponíveis
            GameObject ptsObj = CreateUIObject("PointsText", cardObj.transform, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.84f));
            TextMeshProUGUI ptsTmp = ptsObj.AddComponent<TextMeshProUGUI>();
            ptsTmp.text = hero.unallocatedAttributePoints > 0 
                ? $"<b>Pontos Livres: {hero.unallocatedAttributePoints}</b>"
                : "Sem pontos livres";
            ptsTmp.fontSize = 14;
            ptsTmp.alignment = TextAlignmentOptions.Center;
            ptsTmp.color = Color.white;

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
            descTmp.color = Color.white;
            descTmp.raycastTarget = false;
        }
    }

    public void OnClickRetreatFromDungeon()
    {
        Debug.Log("A Guilda recuou em segurança com todos os tesouros e experiência!");
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
        tmp.fontSize = 15;
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

    public static Sprite GetHeroSprite(HeroInstance hero)
    {
        if (hero == null) return null;
        if (hero.heroData != null && hero.heroData.battleSprite != null)
            return hero.heroData.battleSprite;
        return HeroGenerator.GetClassSprite(hero.heroClass);
    }

    public static Sprite GetEnemySprite(EnemyInstance enemy)
    {
        if (enemy != null && enemy.data != null && enemy.data.battleSprite != null)
            return enemy.data.battleSprite;

        string eName = enemy != null && !string.IsNullOrEmpty(enemy.enemyName) ? enemy.enemyName.ToLower() : "";
        string spriteName = "esqueleto";
        if (eName.Contains("esqueleto")) spriteName = "esqueleto";
        else if (eName.Contains("goblin")) spriteName = "goblin_lanceiro";
        else if (eName.Contains("necromante") || eName.Contains("guardiao") || eName.Contains("guardião") || (enemy != null && enemy.data != null && enemy.data.isBoss)) spriteName = "necromante";

        Sprite loaded = Resources.Load<Sprite>($"Sprites/Enemies/{spriteName}");
        if (loaded != null) return loaded;

        return null;
    }

    #endregion

    private class HeroFieldDisplay
    {
        public HeroInstance hero;
        public GameObject container;
        public Image cardImage;
        public Image avatarImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
    }

    private class HeroTabDisplay
    {
        public HeroInstance hero;
        public GameObject tabObj;
        public Image tabBg;
        public TextMeshProUGUI tabText;
        public Button tabButton;
    }

    private class EnemyFieldDisplay
    {
        public EnemyInstance enemy;
        public int index;
        public GameObject container;
        public Image cardImage;
        public Image avatarImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
    }
}
