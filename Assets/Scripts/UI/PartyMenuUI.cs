using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sede da Guilda e Menu de Aventureiros com Fundo Dedicado da Taverna/Salao da Guilda (bg_guild_hall).
/// Design atualizado:
/// - Botoes compactos e realmente transparentes (vidro escuro com bordas finas vazadas).
/// - O painel direito (Party, Equipamento, Armazem, Atributos, Salvar) inicia FECHADO para exibir o Salao da Guilda por inteiro,
///   abrindo apenas quando o jogador clica no botao/aba correspondente (e podendo ser fechado a qualquer momento).
/// </summary>
public class PartyMenuUI : MonoBehaviour
{
    private static PartyMenuUI _instance;
    public static PartyMenuUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<PartyMenuUI>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PartyMenuUI", typeof(PartyMenuUI));
                    _instance = go.GetComponent<PartyMenuUI>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    private class InventoryStack
    {
        public string stackKey;
        public ItemData sampleItem;
        public List<ItemData> instances = new List<ItemData>();
        public int Count => instances.Count;
    }

    public const int TOTAL_BACKPACK_SLOTS = 24;
    public const int MAX_STACK_PER_SLOT = 64;

    [Header("Estrutura Raiz e Plano de Fundo da Guilda")]
    public Canvas targetCanvas;
    public GameObject menuRootPanel;
    public Image guildBackgroundImage;
    private static Sprite _cachedGuildBgSprite;

    [Header("Painel de Navegacao Compacto e Transparente (Esquerda)")]
    public RectTransform navBox;
    public RectTransform infoBox;
    public TextMeshProUGUI navTitleText;
    public Button navPartyBtn;
    public Button navEquipBtn;
    public Button navInventoryBtn;
    public Button navStatusBtn;
    public Button navSaveTowerBtn;
    public Button navCloseBtn;
    public TextMeshProUGUI navCloseBtnLabel;
    public TextMeshProUGUI locationText;
    public TextMeshProUGUI goldText;

    [Header("Painel de Conteudo Sob Demanda (Direita)")]
    public RectTransform contentBox;
    public GameObject partyListView;
    public GameObject equipView;
    public GameObject inventoryView;
    public GameObject statusView;
    public GameObject saveTowerView;

    [Header("Estado Interno")]
    public bool isInGuildHub = false;
    public bool isContentWindowOpen = false;
    public HeroInstance selectedHero;
    public ItemType selectedEquipSlot = ItemType.Weapon;
    private int selectedBackpackSlotIndex = 0;
    private string saveFeedbackMessage = "";
    private string partyFeedbackMessage = "";

    private enum MenuTab { None, Party, Equip, Inventory, Status, SaveAndTower }
    private MenuTab currentTab = MenuTab.None;

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
        BuildUIStructure();
    }

    void Update()
    {
        if (MainMenuUI.Instance != null && MainMenuUI.Instance.IsMenuOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuRootPanel != null && menuRootPanel.activeSelf)
            {
                // Se alguma janela da direita estiver aberta, ESC fecha primeiro a janela para mostrar o salao da guilda
                if (isContentWindowOpen)
                {
                    CloseContentWindow();
                }
                else if (!isInGuildHub)
                {
                    CloseMenu();
                }
            }
            return;
        }

        if (isInGuildHub)
            return;

        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (menuRootPanel == null) BuildUIStructure();

        if (menuRootPanel.activeSelf)
        {
            if (!isInGuildHub)
            {
                CloseMenu();
            }
        }
        else
        {
            OpenMenu();
        }
    }

    /// <summary>
    /// Entra na Sede da Guilda exibindo o Salao da Guilda completo e os botoes transparentes a esquerda.
    /// As janelas (Party, Equipamento, Armazem, etc.) so abrem quando o jogador clicar no botao respectivo!
    /// </summary>
    public void EnterGuildHub(HeroInstance initialHero = null)
    {
        BuildUIStructure();
        isInGuildHub = true;

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.RestAllHeroesAtGuild();
            if (initialHero != null)
            {
                selectedHero = initialHero;
            }
            else if (PartyManager.Instance.activeParty.Count > 0)
            {
                if (selectedHero == null)
                    selectedHero = PartyManager.Instance.activeParty[0];
            }
        }

        saveFeedbackMessage = "";
        partyFeedbackMessage = "Gerencie sua Esquadra de Expedicao (ate 4 herois) ou recrute novos aventureiros.";

        UpdateNavigationModeVisuals();
        RefreshSidebarInfo();
        menuRootPanel.SetActive(true);
        menuRootPanel.transform.SetAsLastSibling();

        // Inicia com a janela direita FECHADA para que o Salao da Guilda apareca inteiro!
        CloseContentWindow();
    }

    /// <summary>
    /// Abre o menu da Guilda durante a Dungeon.
    /// </summary>
    public void OpenMenu(HeroInstance initialHero = null)
    {
        BuildUIStructure();
        isInGuildHub = false;

        if (initialHero != null)
        {
            selectedHero = initialHero;
        }
        else if (PartyManager.Instance != null && PartyManager.Instance.activeParty.Count > 0)
        {
            if (selectedHero == null)
                selectedHero = PartyManager.Instance.activeParty[0];
        }

        saveFeedbackMessage = "";
        partyFeedbackMessage = "";

        UpdateNavigationModeVisuals();
        RefreshSidebarInfo();
        menuRootPanel.SetActive(true);
        menuRootPanel.transform.SetAsLastSibling();

        // Tambem inicia fechado ate o jogador clicar na aba desejada (ou abre Equip se clicou num heroi especifico)
        if (initialHero != null)
        {
            SwitchTab(MenuTab.Party);
        }
        else
        {
            CloseContentWindow();
        }
    }

    public void CloseContentWindow()
    {
        isContentWindowOpen = false;
        currentTab = MenuTab.None;
        if (contentBox != null)
        {
            contentBox.gameObject.SetActive(false);
        }
        RefreshNavHighlights();
    }

    public void CloseMenu()
    {
        if (menuRootPanel != null)
        {
            menuRootPanel.SetActive(false);
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateAllStats();
            if (selectedHero != null && PartyManager.Instance != null && PartyManager.Instance.activeParty.Contains(selectedHero))
            {
                BattleHUD.Instance.RefreshHeroStatusPanel(selectedHero);
            }
            else if (PartyManager.Instance != null && PartyManager.Instance.activeParty.Count > 0)
            {
                BattleHUD.Instance.RefreshHeroStatusPanel(PartyManager.Instance.activeParty[0]);
            }
        }
    }

    public void DepartToDungeonFromGuild(int? targetFloorOverride = null)
    {
        if (PartyManager.Instance == null || PartyManager.Instance.activeParty.Count == 0)
        {
            partyFeedbackMessage = "Escale pelo menos 1 heroi na Esquadra de Expedicao antes de partir para a Dungeon!";
            SwitchTab(MenuTab.Party);
            return;
        }

        isInGuildHub = false;
        isContentWindowOpen = false;
        CloseMenu();

        int floorToEnter = targetFloorOverride ?? (DungeonManager.Instance != null ? Mathf.Max(1, DungeonManager.Instance.currentFloor) : 1);
        if (DungeonManager.Instance != null)
        {
            DungeonManager.Instance.TravelToUnlockedFloor(floorToEnter);
        }
        else if (BattleManager.Instance != null)
        {
            BattleManager.Instance.TestStartBattle();
        }
    }

    private void OnClickNavCloseOrDepart()
    {
        if (isInGuildHub)
        {
            DepartToDungeonFromGuild();
        }
        else
        {
            CloseMenu();
        }
    }

    private void UpdateNavigationModeVisuals()
    {
        if (navTitleText != null)
        {
            navTitleText.text = isInGuildHub ? "SEDE DA GUILDA" : "MENU GUILDA";
        }

        if (guildBackgroundImage != null)
        {
            guildBackgroundImage.color = Color.white;
        }

        if (navCloseBtn != null)
        {
            Image bg = navCloseBtn.GetComponent<Image>();
            Color c = isInGuildHub
                ? new Color(0.06f, 0.32f, 0.18f, 0.55f)
                : new Color(0.38f, 0.10f, 0.12f, 0.55f);
            if (bg != null) bg.color = c;
        }

        if (navCloseBtnLabel != null)
        {
            navCloseBtnLabel.text = isInGuildHub ? "PARTIR P/ DUNGEON >>" : "VOLTAR A LUTA [X]";
        }
    }

    #region Agrupamento de Itens no Armazem da Guilda (Stacking Estilo Minecraft)

    private List<InventoryStack> BuildInventoryStacks(ItemType? filterType = null)
    {
        List<InventoryStack> stacks = new List<InventoryStack>();
        if (PartyManager.Instance == null || PartyManager.Instance.inventory == null)
            return stacks;

        foreach (var item in PartyManager.Instance.inventory)
        {
            if (item == null) continue;
            if (filterType.HasValue && item.itemType != filterType.Value) continue;

            string key = $"{item.itemType}_{item.itemName}";

            InventoryStack targetStack = null;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].stackKey == key && stacks[i].Count < MAX_STACK_PER_SLOT)
                {
                    targetStack = stacks[i];
                    break;
                }
            }

            if (targetStack == null)
            {
                targetStack = new InventoryStack
                {
                    stackKey = key,
                    sampleItem = item
                };
                stacks.Add(targetStack);
            }

            targetStack.instances.Add(item);
        }

        return stacks;
    }

    #endregion

    #region Construcao da Interface (Botoes Menores e Realmente Transparentes sobre o Salao da Guilda)

    public void BuildUIStructure()
    {
        if (menuRootPanel != null) return;

        if (targetCanvas == null)
        {
            targetCanvas = FindAnyObjectByType<Canvas>();
            if (targetCanvas == null && BattleHUD.Instance != null)
            {
                targetCanvas = BattleHUD.Instance.battleCanvas;
            }
        }

        if (targetCanvas == null) return;

        // 1. Painel Raiz
        menuRootPanel = CreateUIObject("PartyMenuRootPanel", targetCanvas.transform, Vector2.zero, Vector2.one);
        Image rootBg = menuRootPanel.AddComponent<Image>();
        rootBg.color = new Color(0.02f, 0.02f, 0.03f, 1.00f);

        // 2. Imagem de Fundo Dedicada do Interior da Sede da Guilda (100% nitida e iluminada)
        GameObject bgArtObj = CreateUIObject("GuildHallBackgroundArt", menuRootPanel.transform, Vector2.zero, Vector2.one);
        guildBackgroundImage = bgArtObj.AddComponent<Image>();
        if (_cachedGuildBgSprite == null)
        {
            _cachedGuildBgSprite = MainMenuUI.LoadBackgroundSprite("Sprites/Backgrounds/bg_guild_hall", "bg_guild_hall.png");
        }
        if (_cachedGuildBgSprite != null)
        {
            guildBackgroundImage.sprite = _cachedGuildBgSprite;
            guildBackgroundImage.color = Color.white;
            guildBackgroundImage.preserveAspect = false;
        }
        else
        {
            guildBackgroundImage.color = new Color(0.08f, 0.06f, 0.05f, 1.00f);
        }

        // 3. Coluna Esquerda Compacta e Transparente (x: 0.015 a 0.195)
        GameObject navObj = CreateFramedBox(
            "NavBox",
            menuRootPanel.transform,
            new Vector2(0.015f, 0.22f),
            new Vector2(0.195f, 0.96f),
            new Color(0.03f, 0.03f, 0.05f, 0.38f)
        );
        navBox = navObj.GetComponent<RectTransform>();

        GameObject titleObj = CreateUIObject("NavTitle", navBox.transform, new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.98f));
        navTitleText = CreateCrispText(titleObj, "SEDE DA GUILDA", 18.5f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.Center, true);

        navPartyBtn = CreateNavButton(navBox.transform, "NavParty", "PARTY / QUARTEL", new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.86f), () => ToggleOrSwitchTab(MenuTab.Party), null, 15f);
        navEquipBtn = CreateNavButton(navBox.transform, "NavEquip", "EQUIPAMENTO", new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.73f), () => ToggleOrSwitchTab(MenuTab.Equip), null, 15f);
        navInventoryBtn = CreateNavButton(navBox.transform, "NavInv", "ARMAZEM", new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.60f), () => ToggleOrSwitchTab(MenuTab.Inventory), null, 15f);
        navStatusBtn = CreateNavButton(navBox.transform, "NavStatus", "ATRIBUTOS", new Vector2(0.06f, 0.37f), new Vector2(0.94f, 0.47f), () => ToggleOrSwitchTab(MenuTab.Status), null, 15f);
        navSaveTowerBtn = CreateNavButton(navBox.transform, "NavSaveTower", "SALVAR / TORRE", new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.34f), () => ToggleOrSwitchTab(MenuTab.SaveAndTower), null, 15f);
        navCloseBtn = CreateNavButton(navBox.transform, "NavClose", "PARTIR P/ DUNGEON >>", new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.17f), OnClickNavCloseOrDepart, new Color(0.06f, 0.32f, 0.18f, 0.55f), 14.5f);
        navCloseBtnLabel = navCloseBtn.GetComponentInChildren<TextMeshProUGUI>();

        // 4. Caixa Inferior Esquerda Compacta e Transparente (Local, Andar e Ouro)
        GameObject infoObj = CreateFramedBox(
            "InfoBox",
            menuRootPanel.transform,
            new Vector2(0.015f, 0.04f),
            new Vector2(0.195f, 0.20f),
            new Color(0.03f, 0.03f, 0.05f, 0.38f)
        );
        infoBox = infoObj.GetComponent<RectTransform>();

        GameObject locObj = CreateUIObject("LocationText", infoObj.transform, new Vector2(0.06f, 0.44f), new Vector2(0.94f, 0.96f));
        locationText = CreateCrispText(locObj, "Sede da Guilda\n<size=13><color=#d0d8e8>Andar 1 (Liberados: 1)</color></size>", 15f, new Color(0.45f, 0.94f, 0.65f), TextAlignmentOptions.MidlineLeft, true);

        GameObject goldObj = CreateUIObject("GoldText", infoObj.transform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.42f));
        goldText = CreateCrispText(goldObj, "OURO: 0 G", 17f, new Color(1.00f, 0.86f, 0.25f), TextAlignmentOptions.MidlineLeft, true);

        // 5. Painel Direito Principal (Inicia FECHADO e usa vidro transparente real quando aberto)
        GameObject contentObj = CreateFramedBox(
            "ContentBox",
            menuRootPanel.transform,
            new Vector2(0.21f, 0.04f),
            new Vector2(0.98f, 0.96f),
            new Color(0.02f, 0.03f, 0.05f, 0.48f)
        );
        contentBox = contentObj.GetComponent<RectTransform>();

        partyListView = CreateUIObject("PartyListView", contentBox.transform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));
        equipView = CreateUIObject("EquipView", contentBox.transform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));
        inventoryView = CreateUIObject("InventoryView", contentBox.transform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));
        statusView = CreateUIObject("StatusView", contentBox.transform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));
        saveTowerView = CreateUIObject("SaveTowerView", contentBox.transform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));

        contentBox.gameObject.SetActive(false);
        menuRootPanel.SetActive(false);
    }

    /// <summary>
    /// Cria uma caixa com fundo de vidro transparente REAL (sem Outline solido no Image) e borda fina vazada.
    /// </summary>
    private GameObject CreateFramedBox(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color? customBgColor = null)
    {
        Color glassColor = customBgColor ?? new Color(0.03f, 0.04f, 0.06f, 0.44f);
        Color borderColor = new Color(0.88f, 0.70f, 0.30f, 0.72f);
        return MainMenuUI.CreateGlassBox(name, parent, anchorMin, anchorMax, glassColor, borderColor, 1.5f);
    }

    private Button CreateNavButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action, Color? baseColor = null, float fontSize = 15.5f)
    {
        Color color = baseColor ?? new Color(0.05f, 0.06f, 0.10f, 0.45f);
        GameObject btnObj = CreateUIObject(name, parent, anchorMin, anchorMax);
        Image bg = btnObj.AddComponent<Image>();
        bg.color = color;

        MainMenuUI.AddHollowBorder(btnObj.transform, new Color(0.88f, 0.70f, 0.32f, 0.70f), 1.2f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1.25f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1.0f);
        cb.selectedColor = new Color(1.15f, 1.15f, 1.15f, 1.15f);
        cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        GameObject txtObj = CreateUIObject("Label", btnObj.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
        TextMeshProUGUI tmp = CreateCrispText(txtObj, label, fontSize, Color.white, TextAlignmentOptions.Center, true);
        tmp.raycastTarget = false;

        return btn;
    }

    #endregion

    #region Navegacao entre Abas Sob Demanda

    private void ToggleOrSwitchTab(MenuTab tab)
    {
        if (isContentWindowOpen && currentTab == tab)
        {
            // Clicar novamente no botao ativo fecha a janela para mostrar o Salao da Guilda
            CloseContentWindow();
        }
        else
        {
            SwitchTab(tab);
        }
    }

    private void SwitchTab(MenuTab tab)
    {
        currentTab = tab;
        isContentWindowOpen = (tab != MenuTab.None);
        UpdateNavigationModeVisuals();
        RefreshSidebarInfo();

        if (contentBox != null)
        {
            contentBox.gameObject.SetActive(isContentWindowOpen);
        }

        if (!isContentWindowOpen)
        {
            RefreshNavHighlights();
            return;
        }

        partyListView.SetActive(tab == MenuTab.Party);
        equipView.SetActive(tab == MenuTab.Equip);
        inventoryView.SetActive(tab == MenuTab.Inventory);
        statusView.SetActive(tab == MenuTab.Status);
        saveTowerView.SetActive(tab == MenuTab.SaveAndTower);

        RefreshNavHighlights();

        switch (tab)
        {
            case MenuTab.Party:
                BuildPartyListView();
                break;
            case MenuTab.Equip:
                BuildEquipView();
                break;
            case MenuTab.Inventory:
                BuildInventoryView();
                break;
            case MenuTab.Status:
                BuildStatusView();
                break;
            case MenuTab.SaveAndTower:
                BuildSaveAndTowerView();
                break;
        }
    }

    private void RefreshNavHighlights()
    {
        HighlightNavButton(navPartyBtn, isContentWindowOpen && currentTab == MenuTab.Party);
        HighlightNavButton(navEquipBtn, isContentWindowOpen && currentTab == MenuTab.Equip);
        HighlightNavButton(navInventoryBtn, isContentWindowOpen && currentTab == MenuTab.Inventory);
        HighlightNavButton(navStatusBtn, isContentWindowOpen && currentTab == MenuTab.Status);
        HighlightNavButton(navSaveTowerBtn, isContentWindowOpen && currentTab == MenuTab.SaveAndTower);
    }

    private void RefreshSidebarInfo()
    {
        if (PartyManager.Instance != null && goldText != null)
        {
            goldText.text = $"OURO: {PartyManager.Instance.goldCoins} G";
        }
        if (locationText != null)
        {
            int curF = DungeonManager.Instance != null ? DungeonManager.Instance.currentFloor : 1;
            int maxF = DungeonManager.Instance != null ? DungeonManager.Instance.maxUnlockedFloor : 1;
            int slot = SaveSystem.CurrentActiveSlot;
            string placeName = isInGuildHub ? "Sede da Guilda" : $"Dungeon - Andar {curF}";
            locationText.text = $"{placeName}\n<size=13><color=#4cc9f0>Liberados: 1-{maxF}</color> | <color=#ffd166>Slot {slot}</color></size>";
        }
    }

    private void HighlightNavButton(Button btn, bool isActive)
    {
        if (btn == null) return;
        Image bg = btn.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = isActive 
                ? new Color(0.42f, 0.28f, 0.08f, 0.65f) 
                : new Color(0.05f, 0.06f, 0.10f, 0.42f);
        }
    }

    private void CreateCloseWindowButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        CreateNavButton(parent, "CloseWindowBtn", "FECHAR [X]", anchorMin, anchorMax, CloseContentWindow, new Color(0.38f, 0.10f, 0.12f, 0.55f), 14f);
    }

    #endregion

    #region Visualizacao 1: Gerenciamento de Party e Quartel da Guilda (Expedicao + Reserva)

    private void BuildPartyListView()
    {
        foreach (Transform child in partyListView.transform) Destroy(child.gameObject);

        if (PartyManager.Instance == null) return;

        var activeParty = PartyManager.Instance.activeParty;
        var reserveHeroes = PartyManager.Instance.reserveHeroes;

        // 1. Barra Superior de Controle da Guilda (Status, Recrutar Heroi e Fechar Janela)
        GameObject topBar = CreateFramedBox("GuildPartyTopBar", partyListView.transform, new Vector2(0.01f, 0.90f), new Vector2(0.99f, 0.99f), new Color(0.03f, 0.04f, 0.06f, 0.46f));

        GameObject msgObj = CreateUIObject("PartyBannerMsg", topBar.transform, new Vector2(0.02f, 0.06f), new Vector2(0.58f, 0.94f));
        string bannerText = !string.IsNullOrEmpty(partyFeedbackMessage)
            ? $"<color=#2ecc71>{partyFeedbackMessage}</color>"
            : "Escale os herois que irao para a Dungeon (ate 4 na Party) ou gerencie o Quartel.";
        CreateCrispText(msgObj, bannerText, 15f, new Color(0.95f, 0.96f, 1.00f), TextAlignmentOptions.MidlineLeft, true);

        // Botao Recrutar Novo Heroi (100 G)
        int recruitCost = 100;
        bool canAffordRecruit = PartyManager.Instance.goldCoins >= recruitCost;
        Button recruitBtn = CreateNavButton(topBar.transform, "RecruitHeroBtn", $"+ RECRUTAR HEROI ({recruitCost} G)",
            new Vector2(0.60f, 0.14f), new Vector2(0.83f, 0.86f), () => {
                HeroInstance recruited;
                string fb;
                if (PartyManager.Instance.RecruitNewHero(recruitCost, out recruited, out fb))
                {
                    selectedHero = recruited;
                    partyFeedbackMessage = fb;
                    SaveSystem.AutoSave("Novo Heroi Recrutado na Guilda");
                }
                else
                {
                    partyFeedbackMessage = fb;
                }
                RefreshSidebarInfo();
                BuildPartyListView();
            }, canAffordRecruit ? new Color(0.38f, 0.25f, 0.06f, 0.56f) : new Color(0.10f, 0.10f, 0.12f, 0.35f), 14f);
        recruitBtn.interactable = canAffordRecruit;

        // Botao Fechar Janela (Mostra o Salao da Guilda)
        CreateCloseWindowButton(topBar.transform, new Vector2(0.84f, 0.14f), new Vector2(0.99f, 0.86f));

        // 2. Coluna Esquerda: ESQUADRA DE EXPEDICAO (Herois que vao para a Dungeon - Max 4)
        GameObject activeBox = CreateFramedBox("ActivePartyColumn", partyListView.transform, new Vector2(0.01f, 0.01f), new Vector2(0.57f, 0.88f), new Color(0.02f, 0.03f, 0.05f, 0.38f));

        GameObject activeHdr = CreateUIObject("ActiveHeader", activeBox.transform, new Vector2(0.02f, 0.91f), new Vector2(0.98f, 0.99f));
        CreateCrispText(activeHdr,
            $"ESQUADRA DE EXPEDICAO — VAO P/ A DUNGEON ({activeParty.Count}/{PartyManager.MAX_ACTIVE_PARTY_SIZE})",
            16.5f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.MidlineLeft, true);

        float slotH = 0.88f / PartyManager.MAX_ACTIVE_PARTY_SIZE;
        for (int i = 0; i < PartyManager.MAX_ACTIVE_PARTY_SIZE; i++)
        {
            float yMax = 0.90f - (i * slotH) - 0.008f;
            float yMin = 0.90f - ((i + 1) * slotH) + 0.008f;

            if (i < activeParty.Count)
            {
                BuildActivePartyHeroCard(activeBox.transform, activeParty[i], i, new Vector2(0.02f, yMin), new Vector2(0.98f, yMax));
            }
            else
            {
                BuildEmptyPartySlotCard(activeBox.transform, i + 1, new Vector2(0.02f, yMin), new Vector2(0.98f, yMax));
            }
        }

        // 3. Coluna Direita: QUARTEL DA GUILDA (Herois na Reserva disponiveis para escalar)
        GameObject reserveBox = CreateFramedBox("ReserveHeroesColumn", partyListView.transform, new Vector2(0.58f, 0.01f), new Vector2(0.99f, 0.88f), new Color(0.02f, 0.03f, 0.05f, 0.38f));

        GameObject resHdr = CreateUIObject("ReserveHeader", reserveBox.transform, new Vector2(0.03f, 0.91f), new Vector2(0.97f, 0.99f));
        CreateCrispText(resHdr,
            $"QUARTEL DA GUILDA — RESERVA ({reserveHeroes.Count} Disponiveis)",
            16.5f, new Color(0.45f, 0.86f, 1.00f), TextAlignmentOptions.MidlineLeft, true);

        if (reserveHeroes.Count == 0)
        {
            GameObject emptyResObj = CreateUIObject("EmptyReserveMsg", reserveBox.transform, new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.80f));
            CreateCrispText(emptyResObj,
                "Nenhum heroi na Reserva no momento.\n\n<size=14.5><color=#dce4f0>Voce pode mover um heroi da Esquadra para a Reserva ou clicar em [ + RECRUTAR HEROI ] acima!</color></size>",
                16.5f, new Color(0.90f, 0.94f, 0.98f), TextAlignmentOptions.Center, true);
        }
        else
        {
            int displayCount = Mathf.Min(reserveHeroes.Count, 5);
            float resCardH = 0.88f / Mathf.Max(displayCount, 4);
            for (int r = 0; r < displayCount; r++)
            {
                float yMax = 0.90f - (r * resCardH) - 0.008f;
                float yMin = 0.90f - ((r + 1) * resCardH) + 0.008f;
                BuildReserveHeroCard(reserveBox.transform, reserveHeroes[r], new Vector2(0.02f, yMin), new Vector2(0.98f, yMax));
            }
        }
    }

    private void BuildActivePartyHeroCard(Transform parent, HeroInstance hero, int index, Vector2 anchorMin, Vector2 anchorMax)
    {
        bool isSelected = (selectedHero == hero);

        Color cardBgColor = isSelected ? new Color(0.18f, 0.13f, 0.05f, 0.54f) : new Color(0.03f, 0.04f, 0.07f, 0.44f);
        Color borderColor = isSelected ? new Color(1.00f, 0.84f, 0.28f, 0.92f) : new Color(0.75f, 0.58f, 0.26f, 0.65f);

        GameObject rowObj = MainMenuUI.CreateGlassBox($"ActiveHero_{index}_{hero.heroName}", parent, anchorMin, anchorMax, cardBgColor, borderColor, 1.2f);
        Image rowBg = rowObj.GetComponent<Image>();

        Button rowBtn = rowObj.AddComponent<Button>();
        rowBtn.targetGraphic = rowBg;
        rowBtn.onClick.AddListener(() => {
            selectedHero = hero;
            BuildPartyListView();
        });

        // Sprite do Heroi
        GameObject spriteBox = CreateUIObject("HeroSpriteBox", rowObj.transform, new Vector2(0.01f, 0.06f), new Vector2(0.14f, 0.94f));
        Image spriteImg = spriteBox.AddComponent<Image>();
        spriteImg.sprite = HeroGenerator.GetClassSprite(hero.heroClass);
        spriteImg.preserveAspect = true;
        spriteImg.raycastTarget = false;

        // Nome, Classe e Atributos
        GameObject infoObj = CreateUIObject("HeroInfo", rowObj.transform, new Vector2(0.15f, 0.04f), new Vector2(0.51f, 0.96f));
        HeroAttributes ha = hero.GetCurrentAttributes();
        string starText = hero.isGoldStar ? $"[{hero.currentStars}* Ouro]" : $"[{hero.currentStars}*]";
        string wpName = hero.equippedWeapon != null ? hero.equippedWeapon.itemName : "Desarmado";
        string chName = hero.equippedChest != null ? hero.equippedChest.itemName : "Sem Armadura";

        string infoText =
            $"<b><size=17>{hero.heroName}</size></b> <color=#ffd166>{starText}</color>\n" +
            $"<color=#ffffff><b>{ItemData.FormatClassName(hero.heroClass)}</b></color> Nv.{hero.level}  |  " +
            $"<color=#4cc9f0>FOR</color> {ha.strength} <color=#4cc9f0>DES</color> {ha.dexterity} <color=#4cc9f0>INT</color> {ha.intelligence} <color=#4cc9f0>VIT</color> {ha.vitality}\n" +
            $"<size=13><color=#ffd166>[Arma]</color> {wpName}  |  <color=#72efdd>[Armad.]</color> {chName}</size>";
        TextMeshProUGUI infoTmp = CreateCrispText(infoObj, infoText, 14f, new Color(0.94f, 0.96f, 1.00f), TextAlignmentOptions.MidlineLeft, true);
        infoTmp.raycastTarget = false;

        // Barras de HP e MP
        GameObject barsObj = CreateUIObject("HeroBars", rowObj.transform, new Vector2(0.52f, 0.10f), new Vector2(0.76f, 0.90f));
        CreateVitalsBar(barsObj.transform, "HPBar", "HP", hero.currentHP, hero.GetMaxHP(),
            new Color(0.78f, 0.30f, 0.10f, 0.72f), new Vector2(0.00f, 0.54f), new Vector2(1.00f, 0.94f));
        CreateVitalsBar(barsObj.transform, "MPBar", "MP", hero.currentMP, hero.GetMaxMP(),
            new Color(0.12f, 0.40f, 0.78f, 0.72f), new Vector2(0.00f, 0.06f), new Vector2(1.00f, 0.46f));

        // Botoes de Acao: [ EQUIPAR ] e [ -> RESERVA ]
        CreateNavButton(rowObj.transform, "EquipHeroBtn", "EQUIPAR",
            new Vector2(0.78f, 0.53f), new Vector2(0.98f, 0.90f), () => {
                selectedHero = hero;
                SwitchTab(MenuTab.Equip);
            }, new Color(0.08f, 0.26f, 0.46f, 0.55f), 13.5f);

        bool canMoveToReserve = PartyManager.Instance.activeParty.Count > 1;
        Button toResBtn = CreateNavButton(rowObj.transform, "MoveToReserveBtn", "-> RESERVA",
            new Vector2(0.78f, 0.10f), new Vector2(0.98f, 0.47f), () => {
                string fb;
                if (PartyManager.Instance.MoveHeroToReserve(hero, out fb))
                {
                    partyFeedbackMessage = fb;
                    if (selectedHero == hero && PartyManager.Instance.activeParty.Count > 0)
                        selectedHero = PartyManager.Instance.activeParty[0];
                    SaveSystem.AutoSave("Heroi Movido para a Reserva");
                    BuildPartyListView();
                }
                else
                {
                    partyFeedbackMessage = fb;
                    BuildPartyListView();
                }
            }, canMoveToReserve ? new Color(0.36f, 0.12f, 0.12f, 0.55f) : new Color(0.12f, 0.12f, 0.14f, 0.35f), 13.5f);
        toResBtn.interactable = canMoveToReserve;
    }

    private void BuildEmptyPartySlotCard(Transform parent, int slotNumber, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject rowObj = MainMenuUI.CreateGlassBox(
            $"EmptyPartySlot_{slotNumber}",
            parent,
            anchorMin,
            anchorMax,
            new Color(0.02f, 0.03f, 0.05f, 0.32f),
            new Color(0.55f, 0.44f, 0.24f, 0.45f),
            1f
        );

        GameObject txtObj = CreateUIObject("EmptySlotLabel", rowObj.transform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
        CreateCrispText(txtObj,
            $"<color=#ffd166>SLOT DE EXPEDICAO {slotNumber} — [ VAGA ABERTA ]</color>\n<size=13.5><color=#dce4f0>Clique em [ <- ESCALAR ] em um heroi do Quartel a direita para preencher!</color></size>",
            15f, Color.white, TextAlignmentOptions.Center, true);
    }

    private void BuildReserveHeroCard(Transform parent, HeroInstance reserveHero, Vector2 anchorMin, Vector2 anchorMax)
    {
        bool isSelected = (selectedHero == reserveHero);

        Color cardBgColor = isSelected ? new Color(0.18f, 0.13f, 0.05f, 0.54f) : new Color(0.03f, 0.04f, 0.07f, 0.44f);
        Color borderColor = isSelected ? new Color(1.00f, 0.84f, 0.28f, 0.92f) : new Color(0.45f, 0.68f, 0.90f, 0.60f);

        GameObject cardObj = MainMenuUI.CreateGlassBox($"ReserveHero_{reserveHero.heroName}", parent, anchorMin, anchorMax, cardBgColor, borderColor, 1.2f);

        // Sprite
        GameObject spriteBox = CreateUIObject("SpriteBox", cardObj.transform, new Vector2(0.02f, 0.06f), new Vector2(0.17f, 0.94f));
        Image spriteImg = spriteBox.AddComponent<Image>();
        spriteImg.sprite = HeroGenerator.GetClassSprite(reserveHero.heroClass);
        spriteImg.preserveAspect = true;
        spriteImg.raycastTarget = false;

        // Dados do Heroi na Reserva
        GameObject infoObj = CreateUIObject("Info", cardObj.transform, new Vector2(0.19f, 0.04f), new Vector2(0.71f, 0.96f));
        HeroAttributes ha = reserveHero.GetCurrentAttributes();
        string starText = reserveHero.isGoldStar ? $"[{reserveHero.currentStars}* Ouro]" : $"[{reserveHero.currentStars}*]";
        string wpName = reserveHero.equippedWeapon != null ? reserveHero.equippedWeapon.itemName : "Desarmado";

        string infoText =
            $"<b><size=16>{reserveHero.heroName}</size></b> <color=#ffd166>{starText}</color>\n" +
            $"<color=#a8dadc>{ItemData.FormatClassName(reserveHero.heroClass)} Nv.{reserveHero.level}</color>  |  HP {reserveHero.GetMaxHP()}\n" +
            $"<size=13>FOR {ha.strength} | DES {ha.dexterity} | INT {ha.intelligence} | VIT {ha.vitality}  (<color=#ffd166>{wpName}</color>)</size>";
        CreateCrispText(infoObj, infoText, 13.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        // Botao [ <- ESCALAR NA PARTY ]
        bool partyHasSpace = PartyManager.Instance.activeParty.Count < PartyManager.MAX_ACTIVE_PARTY_SIZE;
        string deployLabel = partyHasSpace ? "<- ESCALAR" : "<- TROCAR";

        CreateNavButton(cardObj.transform, "DeployBtn", deployLabel,
            new Vector2(0.73f, 0.53f), new Vector2(0.98f, 0.90f), () => {
                string fb;
                HeroInstance toReplace = null;
                if (!partyHasSpace)
                {
                    toReplace = (selectedHero != null && PartyManager.Instance.activeParty.Contains(selectedHero))
                        ? selectedHero
                        : PartyManager.Instance.activeParty[PartyManager.Instance.activeParty.Count - 1];
                }

                if (PartyManager.Instance.AssignReserveHeroToParty(reserveHero, toReplace, out fb))
                {
                    selectedHero = reserveHero;
                    partyFeedbackMessage = fb;
                    SaveSystem.AutoSave("Escala da Party Atualizada na Guilda");
                    BuildPartyListView();
                }
            }, new Color(0.06f, 0.34f, 0.18f, 0.55f), 13.5f);

        // Botao [ EQUIPAR ] no heroi da reserva
        CreateNavButton(cardObj.transform, "EquipReserveBtn", "EQUIPAR",
            new Vector2(0.73f, 0.10f), new Vector2(0.98f, 0.47f), () => {
                selectedHero = reserveHero;
                SwitchTab(MenuTab.Equip);
            }, new Color(0.08f, 0.26f, 0.46f, 0.55f), 13.5f);
    }

    private void CreateVitalsBar(Transform parent, string name, string label, int current, int max, Color fillColor, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject barObj = MainMenuUI.CreateGlassBox(
            name,
            parent,
            anchorMin,
            anchorMax,
            new Color(0.02f, 0.03f, 0.05f, 0.55f),
            new Color(0.72f, 0.56f, 0.28f, 0.65f),
            1f
        );

        float fillRatio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        GameObject fillObj = CreateUIObject("Fill", barObj.transform, new Vector2(0.01f, 0.06f), new Vector2(0.01f + 0.98f * fillRatio, 0.94f));
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = fillColor;

        GameObject txtObj = CreateUIObject("Label", barObj.transform, new Vector2(0.04f, 0.00f), new Vector2(0.96f, 1.00f));
        TextMeshProUGUI tmp = CreateCrispText(txtObj, $"{label}: {current}/{max}", 14.5f, Color.white, TextAlignmentOptions.Center, true);
        tmp.raycastTarget = false;
    }

    #endregion

    #region Visualizacao 2: Equipamento (Compatibilidade de Classes & Atributos)

    private void BuildEquipView()
    {
        foreach (Transform child in equipView.transform) Destroy(child.gameObject);

        if (selectedHero == null && PartyManager.Instance != null && PartyManager.Instance.activeParty.Count > 0)
            selectedHero = PartyManager.Instance.activeParty[0];

        if (selectedHero == null) return;

        GameObject headerObj = CreateFramedBox("EquipHeader", equipView.transform, new Vector2(0.01f, 0.86f), new Vector2(0.99f, 0.98f), new Color(0.03f, 0.04f, 0.06f, 0.46f));
        BuildHeroSelectorHeader(headerObj.transform);

        GameObject slotsBox = CreateUIObject("SlotsBox", equipView.transform, new Vector2(0.01f, 0.02f), new Vector2(0.49f, 0.84f));
        BuildEquipmentSlots(slotsBox.transform);

        GameObject inventorySideBox = CreateFramedBox("InventorySideBox", equipView.transform, new Vector2(0.51f, 0.02f), new Vector2(0.99f, 0.84f), new Color(0.02f, 0.03f, 0.05f, 0.40f));
        BuildCompatibleInventory(inventorySideBox.transform);
    }

    private void BuildHeroSelectorHeader(Transform parent)
    {
        GameObject avObj = CreateUIObject("Avatar", parent, new Vector2(0.01f, 0.06f), new Vector2(0.07f, 0.94f));
        Image avImg = avObj.AddComponent<Image>();
        avImg.sprite = HeroGenerator.GetClassSprite(selectedHero.heroClass);
        avImg.preserveAspect = true;

        bool isInActiveParty = PartyManager.Instance != null && PartyManager.Instance.activeParty.Contains(selectedHero);
        string statusTag = isInActiveParty ? "<color=#2ecc71>[PARTY]</color>" : "<color=#4cc9f0>[RESERVA]</color>";

        HeroAttributes ha = selectedHero.GetCurrentAttributes();
        GameObject titleObj = CreateUIObject("Title", parent, new Vector2(0.08f, 0.04f), new Vector2(0.47f, 0.96f));
        string hdrText =
            $"<b><size=17.5>{selectedHero.heroName}</size></b> {statusTag} <color=#a8dadc>({ItemData.FormatClassName(selectedHero.heroClass)} Nv.{selectedHero.level})</color>\n" +
            $"<color=#ffd166>FOR: <b>{ha.strength}</b> | DES: <b>{ha.dexterity}</b> | INT: <b>{ha.intelligence}</b> | VIT: <b>{ha.vitality}</b> | HP: <b>{selectedHero.currentHP}/{selectedHero.GetMaxHP()}</b></color>";
        CreateCrispText(titleObj, hdrText, 14f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        List<HeroInstance> allHeroes = PartyManager.Instance.GetAllGuildHeroes();
        int maxButtons = Mathf.Min(allHeroes.Count, 6);
        float btnStartX = 0.48f;
        float totalW = 0.40f;
        float btnW = (totalW / Mathf.Max(maxButtons, 1)) - 0.005f;

        for (int i = 0; i < maxButtons; i++)
        {
            HeroInstance h = allHeroes[i];
            bool isPartyMember = PartyManager.Instance.activeParty.Contains(h);
            float x1 = btnStartX + (i * (btnW + 0.005f));
            float x2 = x1 + btnW;
            Color c = (h == selectedHero)
                ? new Color(0.42f, 0.28f, 0.08f, 0.65f)
                : new Color(0.05f, 0.06f, 0.10f, 0.42f);
            string shortName = h.heroName.Split(',')[0].Trim();
            string tag = isPartyMember ? "<color=#2ecc71>[P]</color>" : "<color=#4cc9f0>[R]</color>";
            CreateNavButton(parent, $"SelectHero_{i}", $"{shortName}\n<size=10.5>{tag}</size>", new Vector2(x1, 0.12f), new Vector2(x2, 0.88f), () => {
                selectedHero = h;
                if (currentTab == MenuTab.Equip) BuildEquipView();
                else if (currentTab == MenuTab.Inventory) BuildInventoryView();
                else if (currentTab == MenuTab.Status) BuildStatusView();
            }, c, 12.5f);
        }

        CreateCloseWindowButton(parent, new Vector2(0.89f, 0.14f), new Vector2(0.99f, 0.86f));
    }

    private void BuildEquipmentSlots(Transform parent)
    {
        ItemType[] slots = new ItemType[] {
            ItemType.Weapon, ItemType.Helmet, ItemType.ChestArmor, 
            ItemType.Pants, ItemType.Boots, ItemType.Gloves
        };
        string[] slotNames = new string[] {
            "[Arma]", "[Capacete / Elmo]", "[Peitoral / Armadura]",
            "[Perneiras / Calcas]", "[Botas]", "[Manoplas / Luvas]"
        };

        float slotHeight = 0.98f / 6f;

        for (int i = 0; i < 6; i++)
        {
            ItemType slotType = slots[i];
            string slotLabel = slotNames[i];
            float yMin = 0.99f - ((i + 1) * slotHeight);
            float yMax = 0.99f - (i * slotHeight) - 0.012f;

            ItemData equippedItem = GetEquippedItem(selectedHero, slotType);
            bool isSlotSelected = (selectedEquipSlot == slotType);

            Color slotBgColor = isSlotSelected 
                ? new Color(0.24f, 0.16f, 0.06f, 0.56f) 
                : new Color(0.03f, 0.04f, 0.07f, 0.44f);
            Color slotBorderColor = isSlotSelected
                ? new Color(1.00f, 0.84f, 0.28f, 0.95f)
                : new Color(0.70f, 0.54f, 0.26f, 0.65f);

            GameObject slotObj = MainMenuUI.CreateGlassBox($"Slot_{slotType}", parent, new Vector2(0.00f, yMin), new Vector2(1.00f, yMax), slotBgColor, slotBorderColor, 1.2f);
            Image slotBg = slotObj.GetComponent<Image>();

            Button slotBtn = slotObj.AddComponent<Button>();
            slotBtn.targetGraphic = slotBg;
            slotBtn.onClick.AddListener(() => {
                selectedEquipSlot = slotType;
                BuildEquipView();
            });

            GameObject frameObj = CreateUIObject("IconFrame", slotObj.transform, new Vector2(0.02f, 0.08f), new Vector2(0.17f, 0.92f));
            Image frameBg = frameObj.AddComponent<Image>();
            frameBg.color = new Color(0.02f, 0.03f, 0.05f, 0.55f);
            frameBg.raycastTarget = false;

            Sprite iconSprite = equippedItem != null ? equippedItem.GetIcon() : null;
            if (iconSprite != null)
            {
                GameObject icObj = CreateUIObject("Icon", frameObj.transform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
                Image icImg = icObj.AddComponent<Image>();
                icImg.sprite = iconSprite;
                icImg.preserveAspect = true;
                icImg.raycastTarget = false;
            }

            GameObject txtObj = CreateUIObject("Info", slotObj.transform, new Vector2(0.19f, 0.04f), new Vector2(0.75f, 0.96f));
            string itemNameStr = equippedItem != null ? $"<b>{equippedItem.itemName}</b>" : "<color=#98a4b8>[Desequipado]</color>";
            string statStr = equippedItem != null ? GetItemStatsShort(equippedItem) : "";
            string classCompat = equippedItem != null ? $"<size=13.5><color=#cbd5e1>({equippedItem.GetAllowedClassesText()})</color></size>" : "";
            string slotInfoText = $"<color=#ffd166><b>{slotLabel}</b></color> {classCompat}\n{itemNameStr}  <color=#2ecc71><b>{statStr}</b></color>";
            TextMeshProUGUI tmp = CreateCrispText(txtObj, slotInfoText, 15.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);
            tmp.raycastTarget = false;

            if (equippedItem != null)
            {
                CreateNavButton(slotObj.transform, "UnequipBtn", "Guardar", 
                    new Vector2(0.77f, 0.14f), new Vector2(0.98f, 0.86f), () => {
                        selectedHero.UnequipItem(slotType);
                        PartyManager.Instance.AddItemToInventory(equippedItem);
                        SaveSystem.AutoSave("Equipamento Guardado no Armazem");
                        BuildEquipView();
                    }, new Color(0.38f, 0.12f, 0.14f, 0.55f), 15f);
            }
        }
    }

    private void BuildCompatibleInventory(Transform parent)
    {
        GameObject hdrObj = CreateUIObject("SideHeader", parent, new Vector2(0.03f, 0.90f), new Vector2(0.97f, 0.99f));
        CreateCrispText(hdrObj, $"ARMAZEM DA GUILDA ({FormatSlotTypeName(selectedEquipSlot)}):", 17.5f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.MidlineLeft, true);

        GameObject listObj = CreateUIObject("CompatibleList", parent, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.88f));
        
        List<InventoryStack> slotStacks = BuildInventoryStacks(selectedEquipSlot);

        if (slotStacks.Count == 0)
        {
            GameObject emptyObj = CreateUIObject("EmptyText", listObj.transform, new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.80f));
            string emptyMsg = $"Nenhum equipamento do tipo <b>{FormatSlotTypeName(selectedEquipSlot)}</b> guardado no Armazem.\n\n" +
                              "<size=15><color=#dce4f0>Dica: Voce pode desequipar itens de outros herois ou conquistar novos equipamentos na Dungeon!</color></size>";
            CreateCrispText(emptyObj, emptyMsg, 17f, new Color(0.92f, 0.94f, 0.98f), TextAlignmentOptions.Center, true);
            return;
        }

        float cardHeight = 0.98f / Mathf.Max(slotStacks.Count, 4);
        for (int i = 0; i < slotStacks.Count; i++)
        {
            InventoryStack stack = slotStacks[i];
            float yMin = 0.98f - ((i + 1) * cardHeight);
            float yMax = 0.98f - (i * cardHeight) - 0.015f;

            BuildInventoryEquipCard(listObj.transform, stack, new Vector2(0.00f, yMin), new Vector2(1.00f, yMax));
        }
    }

    private void BuildInventoryEquipCard(Transform parent, InventoryStack stack, Vector2 anchorMin, Vector2 anchorMax)
    {
        ItemData item = stack.sampleItem;
        GameObject cardObj = MainMenuUI.CreateGlassBox(
            $"Item_{item.itemName}",
            parent,
            anchorMin,
            anchorMax,
            new Color(0.03f, 0.04f, 0.07f, 0.46f),
            new Color(0.72f, 0.56f, 0.26f, 0.68f),
            1.2f
        );

        GameObject frameObj = CreateUIObject("IconFrame", cardObj.transform, new Vector2(0.02f, 0.08f), new Vector2(0.17f, 0.92f));
        Image frameBg = frameObj.AddComponent<Image>();
        frameBg.color = new Color(0.02f, 0.03f, 0.05f, 0.55f);

        Sprite ic = item.GetIcon();
        if (ic != null)
        {
            GameObject icObj = CreateUIObject("Icon", frameObj.transform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
            Image icImg = icObj.AddComponent<Image>();
            icImg.sprite = ic;
            icImg.preserveAspect = true;
        }

        if (stack.Count > 1)
        {
            CreateStackCountBadge(frameObj.transform, stack.Count);
        }

        GameObject txtObj = CreateUIObject("Text", cardObj.transform, new Vector2(0.19f, 0.04f), new Vector2(0.74f, 0.96f));

        string reqStr;
        bool canEquip = item.CanBeEquippedBy(selectedHero, out reqStr);
        string reqColor = canEquip ? "#2ecc71" : "#ff6b6b";
        string stats = GetItemStatsShort(item);
        string reqDetails = GetItemRequirementsShort(item);
        string qtyTag = stack.Count > 1 ? $" <color=#2ecc71><b>(x{stack.Count})</b></color>" : "";

        string cardInfo =
            $"<b><size=16.5>{item.itemName}</size></b>{qtyTag} <color=#ffd166>[{item.GetAllowedClassesText()}]</color>\n" +
            $"<color=#2ecc71><b>{stats}</b></color>   <color=#cbd5e1>({reqDetails})</color>\n" +
            $"<color={reqColor}><b>{reqStr}</b></color>";
        CreateCrispText(txtObj, cardInfo, 14.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        Button equipBtn = CreateNavButton(cardObj.transform, "EquipBtn", canEquip ? "Equipar" : "Bloqueado", 
            new Vector2(0.76f, 0.14f), new Vector2(0.98f, 0.86f), () => {
                string msg;
                ItemData instanceToEquip = stack.instances[0];
                ItemData previous = GetEquippedItem(selectedHero, instanceToEquip.itemType);
                if (selectedHero.TryEquipItem(instanceToEquip, out msg))
                {
                    PartyManager.Instance.inventory.Remove(instanceToEquip);
                    if (previous != null) PartyManager.Instance.AddItemToInventory(previous);
                    SaveSystem.AutoSave("Equipamento Vestido");
                    BuildEquipView();
                }
            }, canEquip ? new Color(0.06f, 0.36f, 0.20f, 0.58f) : new Color(0.22f, 0.12f, 0.12f, 0.45f), 15.5f);
        equipBtn.interactable = canEquip;
    }

    private ItemData GetEquippedItem(HeroInstance hero, ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon: return hero.equippedWeapon;
            case ItemType.Helmet: return hero.equippedHelmet;
            case ItemType.ChestArmor: return hero.equippedChest;
            case ItemType.Pants: return hero.equippedPants;
            case ItemType.Boots: return hero.equippedBoots;
            case ItemType.Gloves: return hero.equippedGloves;
            default: return null;
        }
    }

    private string FormatSlotTypeName(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon: return "Arma";
            case ItemType.Helmet: return "Capacete / Elmo";
            case ItemType.ChestArmor: return "Peitoral / Armadura";
            case ItemType.Pants: return "Perneiras / Calcas";
            case ItemType.Boots: return "Botas";
            case ItemType.Gloves: return "Manoplas / Luvas";
            case ItemType.Consumable: return "Consumivel";
            default: return type.ToString();
        }
    }

    private string GetItemStatsShort(ItemData item)
    {
        List<string> b = new List<string>();
        if (item.bonusAttack > 0) b.Add($"+{item.bonusAttack} ATQ");
        if (item.bonusDefense > 0) b.Add($"+{item.bonusDefense} DEF");
        if (item.bonusMagicAttack > 0) b.Add($"+{item.bonusMagicAttack} MATQ");
        if (item.bonusMaxHP > 0) b.Add($"+{item.bonusMaxHP} HP");
        if (item.bonusMaxMP > 0) b.Add($"+{item.bonusMaxMP} MP");
        if (item.bonusSpeed > 0) b.Add($"+{item.bonusSpeed} VEL");
        if (item.bonusCritRate > 0) b.Add($"+{item.bonusCritRate}% CRIT");
        if (item.healHP > 0) b.Add($"Cura {item.healHP}% HP");
        return string.Join(" | ", b);
    }

    private string GetItemRequirementsShort(ItemData item)
    {
        List<string> r = new List<string>();
        if (item.requiredStrength > 0) r.Add($"FOR {item.requiredStrength}");
        if (item.requiredDexterity > 0) r.Add($"DES {item.requiredDexterity}");
        if (item.requiredIntelligence > 0) r.Add($"INT {item.requiredIntelligence}");
        if (r.Count == 0) return "Livre";
        return "Req: " + string.Join(", ", r);
    }

    #endregion

    #region Visualizacao 3: Armazem / Deposito da Guilda em Grade de Slots Quadrados (Transparente)

    private void BuildInventoryView()
    {
        foreach (Transform child in inventoryView.transform) Destroy(child.gameObject);

        if (selectedHero == null && PartyManager.Instance != null && PartyManager.Instance.activeParty.Count > 0)
            selectedHero = PartyManager.Instance.activeParty[0];

        // 1. Topo: Seletor de Heroi
        GameObject hdrObj = CreateFramedBox("InvHeader", inventoryView.transform, new Vector2(0.01f, 0.86f), new Vector2(0.99f, 0.98f), new Color(0.03f, 0.04f, 0.06f, 0.46f));
        BuildHeroSelectorHeader(hdrObj.transform);

        List<InventoryStack> stacks = BuildInventoryStacks();

        if (selectedBackpackSlotIndex >= stacks.Count)
        {
            selectedBackpackSlotIndex = stacks.Count > 0 ? 0 : -1;
        }
        else if (selectedBackpackSlotIndex < 0 && stacks.Count > 0)
        {
            selectedBackpackSlotIndex = 0;
        }

        // 2. Subtitulo da Grade do Armazem da Guilda
        GameObject subHdrObj = CreateUIObject("SubHeader", inventoryView.transform, new Vector2(0.02f, 0.78f), new Vector2(0.98f, 0.85f));
        CreateCrispText(subHdrObj, $"ARMAZEM DA GUILDA  ({stacks.Count} / {TOTAL_BACKPACK_SLOTS} Slots Ocupados)  -  Equipamentos e itens adquiridos ficam guardados aqui!", 17f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.MidlineLeft, true);

        // 3. Container da Grade de 24 Slots Quadrados (8 colunas x 3 linhas)
        GameObject gridBox = CreateFramedBox("BackpackGridBox", inventoryView.transform, new Vector2(0.02f, 0.29f), new Vector2(0.98f, 0.77f), new Color(0.02f, 0.03f, 0.05f, 0.38f));

        int cols = 8;
        int rows = 3;
        float padX = 0.02f;
        float padY = 0.05f;
        float gapX = 0.012f;
        float gapY = 0.04f;
        float slotW = (1.0f - (2f * padX) - ((cols - 1) * gapX)) / cols;
        float slotH = (1.0f - (2f * padY) - ((rows - 1) * gapY)) / rows;

        for (int slotIdx = 0; slotIdx < TOTAL_BACKPACK_SLOTS; slotIdx++)
        {
            int r = slotIdx / cols;
            int c = slotIdx % cols;

            float xMin = padX + c * (slotW + gapX);
            float xMax = xMin + slotW;
            float yMax = 1.0f - padY - r * (slotH + gapY);
            float yMin = yMax - slotH;

            InventoryStack stack = (slotIdx < stacks.Count) ? stacks[slotIdx] : null;
            BuildSquareBackpackSlot(gridBox.transform, slotIdx, stack, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
        }

        // 4. Painel Inferior de Inspecao e Acao do Slot Selecionado
        GameObject inspectorBox = CreateFramedBox("SelectedSlotInspector", inventoryView.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.27f), new Color(0.03f, 0.04f, 0.06f, 0.48f));
        InventoryStack selectedStack = (selectedBackpackSlotIndex >= 0 && selectedBackpackSlotIndex < stacks.Count)
            ? stacks[selectedBackpackSlotIndex]
            : null;
        BuildSelectedStackInspector(inspectorBox.transform, selectedStack);
    }

    private void BuildSquareBackpackSlot(Transform parent, int slotIndex, InventoryStack stack, Vector2 anchorMin, Vector2 anchorMax)
    {
        bool isOccupied = (stack != null && stack.Count > 0);
        bool isSelected = (isOccupied && slotIndex == selectedBackpackSlotIndex);

        Color slotBgColor = isSelected
            ? new Color(0.26f, 0.18f, 0.06f, 0.62f)
            : (isOccupied ? new Color(0.04f, 0.05f, 0.09f, 0.50f) : new Color(0.02f, 0.03f, 0.05f, 0.30f));

        Color borderColor = isSelected
            ? new Color(1.00f, 0.86f, 0.28f, 0.98f)
            : (isOccupied ? GetRarityBorderColor(stack.sampleItem.rarity) : new Color(0.55f, 0.44f, 0.24f, 0.40f));

        GameObject slotObj = MainMenuUI.CreateGlassBox($"Slot_{slotIndex}", parent, anchorMin, anchorMax, slotBgColor, borderColor, isSelected ? 2f : 1.2f);
        Image slotBg = slotObj.GetComponent<Image>();

        if (!isOccupied) return;

        Button btn = slotObj.AddComponent<Button>();
        btn.targetGraphic = slotBg;
        int capturedIdx = slotIndex;
        btn.onClick.AddListener(() => {
            selectedBackpackSlotIndex = capturedIdx;
            BuildInventoryView();
        });

        Sprite ic = stack.sampleItem.GetIcon();
        if (ic != null)
        {
            GameObject icObj = CreateUIObject("Icon", slotObj.transform, new Vector2(0.10f, 0.14f), new Vector2(0.90f, 0.92f));
            Image icImg = icObj.AddComponent<Image>();
            icImg.sprite = ic;
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;
        }

        CreateStackCountBadge(slotObj.transform, stack.Count);
    }

    private void CreateStackCountBadge(Transform slotTransform, int count)
    {
        GameObject badgeBgObj = CreateUIObject("CountBadgeBg", slotTransform, new Vector2(0.42f, 0.02f), new Vector2(0.97f, 0.38f));
        Image badgeBg = badgeBgObj.AddComponent<Image>();
        badgeBg.color = new Color(0.02f, 0.02f, 0.04f, 0.78f);
        badgeBg.raycastTarget = false;

        GameObject txtObj = CreateUIObject("CountText", badgeBgObj.transform, Vector2.zero, Vector2.one);
        Color numColor = count > 1 ? new Color(1.00f, 0.90f, 0.30f) : Color.white;
        TextMeshProUGUI tmp = CreateCrispText(txtObj, count.ToString(), 17f, numColor, TextAlignmentOptions.Center, true);
        tmp.raycastTarget = false;
    }

    private void BuildSelectedStackInspector(Transform parent, InventoryStack stack)
    {
        if (stack == null || stack.sampleItem == null)
        {
            GameObject emptyObj = CreateUIObject("EmptyInspector", parent, Vector2.zero, Vector2.one);
            CreateCrispText(emptyObj, "Selecione um slot ocupado no Armazem da Guilda acima para inspecionar ou equipar o item.", 17f, new Color(0.90f, 0.94f, 0.98f), TextAlignmentOptions.Center, true);
            return;
        }

        ItemData item = stack.sampleItem;

        GameObject frameObj = CreateUIObject("IconFrame", parent, new Vector2(0.02f, 0.10f), new Vector2(0.13f, 0.90f));
        Image frameBg = frameObj.AddComponent<Image>();
        frameBg.color = new Color(0.02f, 0.03f, 0.05f, 0.55f);

        Sprite ic = item.GetIcon();
        if (ic != null)
        {
            GameObject icObj = CreateUIObject("Icon", frameObj.transform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
            Image icImg = icObj.AddComponent<Image>();
            icImg.sprite = ic;
            icImg.preserveAspect = true;
        }

        GameObject infoObj = CreateUIObject("ItemDetails", parent, new Vector2(0.15f, 0.06f), new Vector2(0.72f, 0.94f));

        string stats = GetItemStatsShort(item);
        string classText = item.itemType == ItemType.Consumable ? "Todas as Classes" : item.GetAllowedClassesText();
        string reqDetails = item.itemType == ItemType.Consumable ? "" : $"  |  {GetItemRequirementsShort(item)}";

        string detailsText =
            $"<b><size=19>{item.itemName}</size></b>   <color=#ffd166><b>Quantidade: x{stack.Count}</b></color>   <color=#cbd5e1>[{FormatSlotTypeName(item.itemType)}]</color>\n" +
            $"<color=#2ecc71><b>{stats}</b></color>   |   Classes: <b>{classText}</b>{reqDetails}\n" +
            $"<size=14.5><color=#dce4f0>{item.description}</color>   <color=#ffd166>(Valor: {item.goldValue} G)</color></size>";
        CreateCrispText(infoObj, detailsText, 16f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        if (item.itemType == ItemType.Consumable && selectedHero != null)
        {
            bool needsHeal = selectedHero.IsAlive && selectedHero.currentHP < selectedHero.GetMaxHP();
            string btnLabel = needsHeal ? $"Usar em {selectedHero.heroName.Split(',')[0]}" : "HP Ja Cheio";

            Button useBtn = CreateNavButton(parent, "UsePotionBtn", btnLabel,
                new Vector2(0.74f, 0.52f), new Vector2(0.98f, 0.90f), () => {
                    int healAmount = Mathf.Max(15, Mathf.RoundToInt(selectedHero.GetMaxHP() * (item.healHP / 100f)));
                    selectedHero.Heal(healAmount);
                    PartyManager.Instance.inventory.Remove(stack.instances[0]);
                    SaveSystem.AutoSave("Pocao Usada no Armazem");
                    BuildInventoryView();
                }, needsHeal ? new Color(0.06f, 0.36f, 0.20f, 0.58f) : new Color(0.14f, 0.14f, 0.18f, 0.40f), 16f);
            useBtn.interactable = needsHeal;
        }
        else if (item.itemType != ItemType.Consumable && selectedHero != null)
        {
            string reason;
            bool canEquip = item.CanBeEquippedBy(selectedHero, out reason);
            string btnLabel = canEquip ? $"Equipar em {selectedHero.heroName.Split(',')[0]}" : "Incompativel";

            Button eqBtn = CreateNavButton(parent, "EquipFromBagBtn", btnLabel,
                new Vector2(0.74f, 0.52f), new Vector2(0.98f, 0.90f), () => {
                    string msg;
                    ItemData toEquip = stack.instances[0];
                    ItemData previous = GetEquippedItem(selectedHero, toEquip.itemType);
                    if (selectedHero.TryEquipItem(toEquip, out msg))
                    {
                        PartyManager.Instance.inventory.Remove(toEquip);
                        if (previous != null) PartyManager.Instance.AddItemToInventory(previous);
                        SaveSystem.AutoSave("Equipamento do Armazem Vestido");
                        BuildInventoryView();
                    }
                }, canEquip ? new Color(0.08f, 0.30f, 0.48f, 0.58f) : new Color(0.22f, 0.12f, 0.14f, 0.45f), 16f);
            eqBtn.interactable = canEquip;
        }

        CreateNavButton(parent, "DiscardOneBtn", "Descartar 1",
            new Vector2(0.74f, 0.10f), new Vector2(0.98f, 0.44f), () => {
                PartyManager.Instance.inventory.Remove(stack.instances[0]);
                SaveSystem.AutoSave("Item Descartado");
                BuildInventoryView();
            }, new Color(0.38f, 0.12f, 0.14f, 0.55f), 15.5f);
    }

    private Color GetRarityBorderColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return new Color(0.20f, 0.78f, 0.42f, 0.85f);
            case ItemRarity.Rare:     return new Color(0.28f, 0.58f, 0.98f, 0.88f);
            case ItemRarity.Epic:     return new Color(0.68f, 0.32f, 0.95f, 0.88f);
            case ItemRarity.Legendary:return new Color(1.00f, 0.82f, 0.22f, 0.95f);
            default:                  return new Color(0.72f, 0.56f, 0.28f, 0.72f);
        }
    }

    #endregion

    #region Visualizacao 4: Status e Atributos Dark Souls

    private void BuildStatusView()
    {
        foreach (Transform child in statusView.transform) Destroy(child.gameObject);

        if (selectedHero == null && PartyManager.Instance != null && PartyManager.Instance.activeParty.Count > 0)
            selectedHero = PartyManager.Instance.activeParty[0];

        if (selectedHero == null) return;

        GameObject headerObj = CreateFramedBox("StatusHeader", statusView.transform, new Vector2(0.01f, 0.86f), new Vector2(0.99f, 0.98f), new Color(0.03f, 0.04f, 0.06f, 0.46f));
        BuildHeroSelectorHeader(headerObj.transform);

        GameObject attrBox = CreateFramedBox("AttrBox", statusView.transform, new Vector2(0.01f, 0.02f), new Vector2(0.49f, 0.84f), new Color(0.03f, 0.04f, 0.06f, 0.42f));

        GameObject aTitle = CreateUIObject("Title", attrBox.transform, new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.98f));
        CreateCrispText(aTitle, $"ATRIBUTOS PRIMARIOS (Pontos Livres: <color=#ffd166><b>{selectedHero.unallocatedAttributePoints}</b></color>)", 18.5f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.MidlineLeft, true);

        HeroAttributes attrs = selectedHero.GetCurrentAttributes();
        CreateAttributeRow(attrBox.transform, "FORCA (STR):", attrs.strength, 0.69f, () => {
            if (selectedHero.AllocateAttributePoint("STR")) { SaveSystem.AutoSave("Atributo STR Alocado"); BuildStatusView(); }
        });
        CreateAttributeRow(attrBox.transform, "DESTREZA (DEX):", attrs.dexterity, 0.51f, () => {
            if (selectedHero.AllocateAttributePoint("DEX")) { SaveSystem.AutoSave("Atributo DEX Alocado"); BuildStatusView(); }
        });
        CreateAttributeRow(attrBox.transform, "INTELIGENCIA (INT):", attrs.intelligence, 0.33f, () => {
            if (selectedHero.AllocateAttributePoint("INT")) { SaveSystem.AutoSave("Atributo INT Alocado"); BuildStatusView(); }
        });
        CreateAttributeRow(attrBox.transform, "VITALIDADE (VIT):", attrs.vitality, 0.15f, () => {
            if (selectedHero.AllocateAttributePoint("VIT")) { SaveSystem.AutoSave("Atributo VIT Alocado"); BuildStatusView(); }
        });

        GameObject statsBox = CreateFramedBox("StatsBox", statusView.transform, new Vector2(0.51f, 0.02f), new Vector2(0.99f, 0.84f), new Color(0.03f, 0.04f, 0.06f, 0.42f));

        GameObject sTitle = CreateUIObject("Title", statsBox.transform, new Vector2(0.06f, 0.87f), new Vector2(0.94f, 0.98f));
        CreateCrispText(sTitle, "ESTATISTICAS DERIVADAS DE COMBATE", 18.5f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.MidlineLeft, true);

        GameObject sBody = CreateUIObject("Body", statsBox.transform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.84f));
        string derivedText =
            $"- Vida Maxima (HP):   <b><color=#ff9f1c>{selectedHero.currentHP} / {selectedHero.GetMaxHP()}</color></b>\n" +
            $"- Mana Maxima (MP):   <b><color=#4cc9f0>{selectedHero.currentMP} / {selectedHero.GetMaxMP()}</color></b>\n\n" +
            $"- Ataque Fisico:       <b><color=#ff6b6b>{selectedHero.GetPhysicalAttack()}</color></b>\n" +
            $"- Defesa Fisica:       <b><color=#4ea8de>{selectedHero.GetPhysicalDefense()}</color></b>\n" +
            $"- Ataque Magico:      <b><color=#c77dff>{selectedHero.GetMagicAttack()}</color></b>\n" +
            $"- Velocidade / Turno:  <b><color=#2ecc71>{selectedHero.GetSpeed()}</color></b>\n" +
            $"- Taxa de Critico:     <b><color=#ffd166>{selectedHero.GetCritRate():F1}%</color></b>";
        TextMeshProUGUI sbTmp = CreateCrispText(sBody, derivedText, 18.5f, Color.white, TextAlignmentOptions.TopLeft, true);
        sbTmp.lineSpacing = 14f;
    }

    private void CreateAttributeRow(Transform parent, string label, int value, float yAnchor, UnityEngine.Events.UnityAction onPlus)
    {
        GameObject row = CreateUIObject("AttrRow", parent, new Vector2(0.06f, yAnchor), new Vector2(0.94f, yAnchor + 0.14f));
        
        GameObject txtObj = CreateUIObject("Text", row.transform, new Vector2(0.00f, 0.00f), new Vector2(0.72f, 1.00f));
        CreateCrispText(txtObj, $"{label}   <size=21><color=#ffd166><b>{value}</b></color></size>", 18f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        Button plusBtn = CreateNavButton(row.transform, "PlusBtn", "[ + ]", 
            new Vector2(0.75f, 0.10f), new Vector2(0.98f, 0.90f), onPlus, new Color(0.06f, 0.36f, 0.20f, 0.58f), 19f);
        plusBtn.interactable = selectedHero.unallocatedAttributePoints > 0;
    }

    #endregion

    #region Visualizacao 5: Salvar Progresso, Andares Liberados e Menu Principal

    private void BuildSaveAndTowerView()
    {
        foreach (Transform child in saveTowerView.transform) Destroy(child.gameObject);

        RefreshSidebarInfo();

        // 1. Secao Superior: Selecao Rapida de Andares Liberados (Andar 1 ate maxUnlockedFloor)
        GameObject floorsBox = CreateFramedBox("UnlockedFloorsBox", saveTowerView.transform, new Vector2(0.01f, 0.62f), new Vector2(0.99f, 0.98f), new Color(0.03f, 0.04f, 0.06f, 0.42f));

        int curFloor = DungeonManager.Instance != null ? DungeonManager.Instance.currentFloor : 1;
        int maxFloor = DungeonManager.Instance != null ? Mathf.Max(DungeonManager.Instance.maxUnlockedFloor, curFloor) : 1;

        GameObject flTitleObj = CreateUIObject("FloorsTitle", floorsBox.transform, new Vector2(0.02f, 0.72f), new Vector2(0.86f, 0.96f));
        CreateCrispText(flTitleObj,
            $"ANDARES DA DUNGEON LIBERADOS (<color=#2ecc71>Andar 1 ao {maxFloor}</color>)  -  Clique para partir:",
            17.5f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.MidlineLeft, true);

        CreateCloseWindowButton(floorsBox.transform, new Vector2(0.88f, 0.74f), new Vector2(0.98f, 0.94f));

        int totalFloorButtons = 10;
        int startFloor = Mathf.Max(1, maxFloor > 10 ? maxFloor - 9 : 1);
        float btnW = 0.94f / totalFloorButtons;

        for (int i = 0; i < totalFloorButtons; i++)
        {
            int floorNum = startFloor + i;
            bool isUnlocked = (floorNum <= maxFloor);
            bool isCurrent = (floorNum == curFloor);

            float xMin = 0.02f + i * (btnW + 0.004f);
            float xMax = xMin + btnW - 0.004f;

            Color btnColor = isCurrent
                ? new Color(0.06f, 0.36f, 0.20f, 0.58f)
                : (isUnlocked ? new Color(0.08f, 0.24f, 0.44f, 0.54f) : new Color(0.04f, 0.05f, 0.07f, 0.30f));

            string lbl = isUnlocked
                ? (isCurrent ? $"ANDAR {floorNum}\n<size=11><color=#ffd166>[ATUAL]</color></size>" : $"ANDAR {floorNum}\n<size=11><color=#2ecc71>[LIBERADO]</color></size>")
                : $"ANDAR {floorNum}\n<size=11><color=#94a3b8>[BLOQ.]</color></size>";

            Button fBtn = CreateNavButton(floorsBox.transform, $"FloorBtn_{floorNum}", lbl,
                new Vector2(xMin, 0.12f), new Vector2(xMax, 0.68f), () => {
                    DepartToDungeonFromGuild(floorNum);
                }, btnColor, 14.5f);
            fBtn.interactable = isUnlocked;
        }

        // 2. Secao Inferior: Slots de Save (Salvar / Carregar) + Botao de Menu Principal
        GameObject savesBox = CreateFramedBox("SavesManagementBox", saveTowerView.transform, new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.59f), new Color(0.03f, 0.04f, 0.06f, 0.42f));

        GameObject svHeaderObj = CreateUIObject("SavesHeader", savesBox.transform, new Vector2(0.02f, 0.84f), new Vector2(0.68f, 0.98f));
        string fb = string.IsNullOrEmpty(saveFeedbackMessage)
            ? $"SISTEMA DE SAVE (Slot Ativo: <color=#2ecc71>Slot {SaveSystem.CurrentActiveSlot}</color>)"
            : $"<color=#2ecc71>{saveFeedbackMessage}</color>";
        CreateCrispText(svHeaderObj, fb, 17.5f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.MidlineLeft, true);

        // Botao para abrir a Tela de Titulo / Menu Principal
        CreateNavButton(savesBox.transform, "OpenMainMenuBtn", "IR PARA O MENU PRINCIPAL",
            new Vector2(0.70f, 0.85f), new Vector2(0.98f, 0.97f), () => {
                SaveSystem.AutoSave("Saida para o Menu Principal");
                isInGuildHub = false;
                isContentWindowOpen = false;
                CloseMenu();
                if (MainMenuUI.Instance != null)
                {
                    MainMenuUI.Instance.OpenMainMenu();
                }
            }, new Color(0.38f, 0.24f, 0.08f, 0.58f), 15f);

        float rowH = 0.80f / SaveSystem.MAX_SAVE_SLOTS;
        for (int s = 1; s <= SaveSystem.MAX_SAVE_SLOTS; s++)
        {
            int slotNum = s;
            float yMax = 0.82f - ((s - 1) * rowH) - 0.01f;
            float yMin = 0.82f - (s * rowH) + 0.01f;

            GameSaveData sData = SaveSystem.ReadSaveSlotHeader(slotNum);
            BuildGuildSaveSlotRow(savesBox.transform, slotNum, sData, new Vector2(0.02f, yMin), new Vector2(0.98f, yMax));
        }
    }

    private void BuildGuildSaveSlotRow(Transform parent, int slotNum, GameSaveData data, Vector2 anchorMin, Vector2 anchorMax)
    {
        bool hasData = (data != null);
        bool isActive = (SaveSystem.CurrentActiveSlot == slotNum);

        Color bgColor = isActive ? new Color(0.16f, 0.12f, 0.05f, 0.52f) : new Color(0.03f, 0.04f, 0.07f, 0.40f);
        Color borderColor = isActive ? new Color(1.00f, 0.85f, 0.30f, 0.90f) : new Color(0.68f, 0.52f, 0.24f, 0.60f);

        GameObject rowObj = MainMenuUI.CreateGlassBox($"GuildSaveSlot_{slotNum}", parent, anchorMin, anchorMax, bgColor, borderColor, 1.2f);

        GameObject txtObj = CreateUIObject("Info", rowObj.transform, new Vector2(0.02f, 0.05f), new Vector2(0.62f, 0.95f));
        if (hasData)
        {
            int depCount = data.guildDeposit != null ? data.guildDeposit.Count : 0;
            int totalHeroes = (data.heroes != null ? data.heroes.Count : 0) + (data.reserveHeroes != null ? data.reserveHeroes.Count : 0);
            string activeBadge = isActive ? " <color=#2ecc71>[SLOT ATUAL]</color>" : "";
            string info =
                $"<color=#ffd166><b>SLOT {slotNum}</b></color>{activeBadge}  ({data.lastSavedDate})   |   " +
                $"Andar: <b>{data.currentFloor}</b> (Liberados: <b>1-{data.maxUnlockedFloor}</b>)   |   " +
                $"Herois: <b>{totalHeroes}</b>   |   Ouro: <color=#ffd166><b>{data.goldCoins} G</b></color>   |   Armazem: <color=#2ecc71><b>{depCount} itens</b></color>";
            CreateCrispText(txtObj, info, 14.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);
        }
        else
        {
            CreateCrispText(txtObj, $"<color=#94a3b8><b>SLOT {slotNum}  -  [ VAZIO ]</b></color>  (Clique em Salvar no Slot {slotNum} para gravar aqui)", 14.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);
        }

        // Botao Salvar Neste Slot
        CreateNavButton(rowObj.transform, "SaveHereBtn", $"SALVAR NO SLOT {slotNum}",
            new Vector2(0.64f, 0.14f), new Vector2(0.81f, 0.86f), () => {
                SaveSystem.SaveGame(slotNum, "Save Manual na Guilda");
                saveFeedbackMessage = $"Progresso, Herois e Armazem salvos com sucesso no SLOT {slotNum}!";
                BuildSaveAndTowerView();
            }, new Color(0.06f, 0.36f, 0.20f, 0.58f), 14.5f);

        // Botao Carregar Este Slot
        Button loadBtn = CreateNavButton(rowObj.transform, "LoadHereBtn", $"CARREGAR SLOT {slotNum}",
            new Vector2(0.82f, 0.14f), new Vector2(0.98f, 0.86f), () => {
                if (SaveSystem.LoadGame(slotNum))
                {
                    EnterGuildHub();
                    saveFeedbackMessage = $"Slot {slotNum} carregado com sucesso na Sede da Guilda!";
                    SwitchTab(MenuTab.SaveAndTower);
                }
            }, hasData ? new Color(0.08f, 0.26f, 0.48f, 0.58f) : new Color(0.14f, 0.14f, 0.18f, 0.35f), 14.5f);
        loadBtn.interactable = hasData;
    }

    #endregion

    #region Utilitarios de Criacao de UI e Texto de Alta Nitidez

    private TextMeshProUGUI CreateCrispText(GameObject obj, string content, float fontSize, Color color, TextAlignmentOptions alignment, bool bold = true)
    {
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.outlineWidth = 0.22f;
        tmp.outlineColor = new Color32(0, 0, 0, 240);

        Shadow sh = obj.GetComponent<Shadow>();
        if (sh == null) sh = obj.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.92f);
        sh.effectDistance = new Vector2(1.5f, -1.5f);

        return tmp;
    }

    private GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    #endregion
}
