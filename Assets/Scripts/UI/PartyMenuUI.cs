using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu da Guilda e Aventureiros no estilo clássico JRPG / RPG Maker.
/// Inclui Mochila em Grade de Slots Quadrados estilo Minecraft com agrupamento (stacking) de itens idênticos.
/// Tipografia de alta visibilidade com fontes ampliadas, negrito, contorno preto e sombra projetada em todos os números e textos.
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

    /// <summary>
    /// Representa uma pilha (stack) de itens idênticos que ocupam um único slot quadrado na mochila.
    /// </summary>
    private class InventoryStack
    {
        public string stackKey;
        public ItemData sampleItem;
        public List<ItemData> instances = new List<ItemData>();
        public int Count => instances.Count;
    }

    public const int TOTAL_BACKPACK_SLOTS = 24; // 8 colunas x 3 linhas de slots quadrados
    public const int MAX_STACK_PER_SLOT = 64;   // Limite por slot estilo Minecraft

    [Header("Estrutura Raiz")]
    public Canvas targetCanvas;
    public GameObject menuRootPanel;

    [Header("Painel de Navegação (Esquerda)")]
    public RectTransform navBox;
    public RectTransform infoBox;
    public Button navPartyBtn;
    public Button navEquipBtn;
    public Button navInventoryBtn;
    public Button navStatusBtn;
    public Button navCloseBtn;
    public TextMeshProUGUI locationText;
    public TextMeshProUGUI goldText;

    [Header("Painel de Conteúdo (Direita)")]
    public RectTransform contentBox;
    public GameObject partyListView;
    public GameObject equipView;
    public GameObject inventoryView;
    public GameObject statusView;

    [Header("Estado Interno")]
    public HeroInstance selectedHero;
    public ItemType selectedEquipSlot = ItemType.Weapon;
    private int selectedBackpackSlotIndex = 0;

    private enum MenuTab { Party, Equip, Inventory, Status }
    private MenuTab currentTab = MenuTab.Party;

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
        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleMenu();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuRootPanel != null && menuRootPanel.activeSelf)
            {
                CloseMenu();
            }
        }
    }

    public void ToggleMenu()
    {
        if (menuRootPanel == null) BuildUIStructure();

        if (menuRootPanel.activeSelf)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu(HeroInstance initialHero = null)
    {
        BuildUIStructure();
        if (initialHero != null)
        {
            selectedHero = initialHero;
        }
        else if (PartyManager.Instance != null && PartyManager.Instance.activeParty.Count > 0)
        {
            if (selectedHero == null || !PartyManager.Instance.activeParty.Contains(selectedHero))
                selectedHero = PartyManager.Instance.activeParty[0];
        }

        menuRootPanel.SetActive(true);
        menuRootPanel.transform.SetAsLastSibling();
        SwitchTab(MenuTab.Party);
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
            if (selectedHero != null)
            {
                BattleHUD.Instance.RefreshHeroStatusPanel(selectedHero);
            }
        }
    }

    #region Agrupamento de Itens em Slots (Stacking Estilo Minecraft)

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

    #region Construção da Interface (Estilo RPG Maker com Alta Legibilidade)

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
        rootBg.color = new Color(0.03f, 0.04f, 0.07f, 0.97f);

        // 2. Coluna Esquerda: Caixa de Navegação
        GameObject navObj = CreateFramedBox("NavBox", menuRootPanel.transform, new Vector2(0.02f, 0.22f), new Vector2(0.24f, 0.98f));
        navBox = navObj.GetComponent<RectTransform>();

        GameObject titleObj = CreateUIObject("NavTitle", navBox.transform, new Vector2(0.04f, 0.87f), new Vector2(0.96f, 0.98f));
        TextMeshProUGUI titleTmp = CreateCrispText(titleObj, "=== GUILDA ===", 25f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.Center, true);

        navPartyBtn = CreateNavButton(navBox.transform, "NavParty", "GRUPO", new Vector2(0.07f, 0.72f), new Vector2(0.93f, 0.84f), () => SwitchTab(MenuTab.Party));
        navEquipBtn = CreateNavButton(navBox.transform, "NavEquip", "EQUIPAMENTO", new Vector2(0.07f, 0.58f), new Vector2(0.93f, 0.70f), () => SwitchTab(MenuTab.Equip));
        navInventoryBtn = CreateNavButton(navBox.transform, "NavInv", "MOCHILA", new Vector2(0.07f, 0.44f), new Vector2(0.93f, 0.56f), () => SwitchTab(MenuTab.Inventory));
        navStatusBtn = CreateNavButton(navBox.transform, "NavStatus", "ATRIBUTOS", new Vector2(0.07f, 0.30f), new Vector2(0.93f, 0.42f), () => SwitchTab(MenuTab.Status));
        navCloseBtn = CreateNavButton(navBox.transform, "NavClose", "VOLTAR [X]", new Vector2(0.07f, 0.07f), new Vector2(0.93f, 0.21f), CloseMenu, new Color(0.60f, 0.18f, 0.20f));

        // 3. Coluna Esquerda Inferior: Local e Ouro
        GameObject infoObj = CreateFramedBox("InfoBox", menuRootPanel.transform, new Vector2(0.02f, 0.02f), new Vector2(0.24f, 0.19f));
        infoBox = infoObj.GetComponent<RectTransform>();

        GameObject locObj = CreateUIObject("LocationText", infoBox.transform, new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.94f));
        locationText = CreateCrispText(locObj, "Masmorra dos Esquecidos\n<size=15><color=#d0d8e8>Andar 1 - Exploracao</color></size>", 18f, new Color(0.35f, 0.92f, 0.60f), TextAlignmentOptions.MidlineLeft, true);

        GameObject goldObj = CreateUIObject("GoldText", infoBox.transform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.46f));
        goldText = CreateCrispText(goldObj, "OURO: 0 G", 21f, new Color(1.00f, 0.86f, 0.25f), TextAlignmentOptions.MidlineLeft, true);

        // 4. Painel Direito Principal
        GameObject contentObj = CreateFramedBox("ContentBox", menuRootPanel.transform, new Vector2(0.26f, 0.02f), new Vector2(0.98f, 0.98f));
        contentBox = contentObj.GetComponent<RectTransform>();

        partyListView = CreateUIObject("PartyListView", contentBox.transform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));
        equipView = CreateUIObject("EquipView", contentBox.transform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));
        inventoryView = CreateUIObject("InventoryView", contentBox.transform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));
        statusView = CreateUIObject("StatusView", contentBox.transform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));

        menuRootPanel.SetActive(false);
    }

    private GameObject CreateFramedBox(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject box = CreateUIObject(name, parent, anchorMin, anchorMax);
        Image bg = box.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.09f, 0.14f, 0.98f);

        Outline outline = box.AddComponent<Outline>();
        outline.effectColor = new Color(0.30f, 0.52f, 0.80f, 0.92f);
        outline.effectDistance = new Vector2(2, -2);

        return box;
    }

    private Button CreateNavButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action, Color? baseColor = null, float fontSize = 18.5f)
    {
        Color color = baseColor ?? new Color(0.14f, 0.21f, 0.34f, 0.98f);
        GameObject btnObj = CreateUIObject(name, parent, anchorMin, anchorMax);
        Image bg = btnObj.AddComponent<Image>();
        bg.color = color;

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.38f, 0.60f, 0.90f, 0.88f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = color * 1.35f;
        cb.pressedColor = color * 0.8f;
        cb.selectedColor = color * 1.2f;
        btn.colors = cb;
        btn.onClick.AddListener(action);

        GameObject txtObj = CreateUIObject("Label", btnObj.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
        TextMeshProUGUI tmp = CreateCrispText(txtObj, label, fontSize, Color.white, TextAlignmentOptions.Center, true);
        tmp.raycastTarget = false;

        return btn;
    }

    #endregion

    #region Navegação entre Abas

    private void SwitchTab(MenuTab tab)
    {
        currentTab = tab;

        if (PartyManager.Instance != null)
        {
            goldText.text = $"OURO: {PartyManager.Instance.goldCoins} G";
            if (DungeonManager.Instance != null)
            {
                locationText.text = $"Masmorra dos Esquecidos\n<size=15><color=#d0d8e8>Andar {DungeonManager.Instance.currentFloor} - Exploracao</color></size>";
            }
        }

        partyListView.SetActive(tab == MenuTab.Party);
        equipView.SetActive(tab == MenuTab.Equip);
        inventoryView.SetActive(tab == MenuTab.Inventory);
        statusView.SetActive(tab == MenuTab.Status);

        HighlightNavButton(navPartyBtn, tab == MenuTab.Party);
        HighlightNavButton(navEquipBtn, tab == MenuTab.Equip);
        HighlightNavButton(navInventoryBtn, tab == MenuTab.Inventory);
        HighlightNavButton(navStatusBtn, tab == MenuTab.Status);

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
        }
    }

    private void HighlightNavButton(Button btn, bool isActive)
    {
        if (btn == null) return;
        Image bg = btn.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = isActive 
                ? new Color(0.18f, 0.54f, 0.68f, 1f) 
                : new Color(0.14f, 0.21f, 0.34f, 0.98f);
        }
    }

    #endregion

    #region Visualização 1: Lista da Equipe (Party List)

    private void BuildPartyListView()
    {
        foreach (Transform child in partyListView.transform) Destroy(child.gameObject);

        if (PartyManager.Instance == null || PartyManager.Instance.activeParty.Count == 0) return;

        var party = PartyManager.Instance.activeParty;
        int count = party.Count;
        float rowHeight = 0.96f / Mathf.Max(count, 4);

        for (int i = 0; i < count; i++)
        {
            HeroInstance hero = party[i];
            float yMin = 0.98f - ((i + 1) * rowHeight);
            float yMax = 0.98f - (i * rowHeight) - 0.015f;

            BuildHeroRow(hero, new Vector2(0.01f, yMin), new Vector2(0.99f, yMax));
        }
    }

    private void BuildHeroRow(HeroInstance hero, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject rowObj = CreateUIObject($"Row_{hero.heroName}", partyListView.transform, anchorMin, anchorMax);
        Image rowBg = rowObj.AddComponent<Image>();
        rowBg.color = new Color(0.09f, 0.12f, 0.18f, 0.96f);

        Outline outline = rowObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.24f, 0.38f, 0.58f, 0.88f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button rowBtn = rowObj.AddComponent<Button>();
        rowBtn.targetGraphic = rowBg;
        Color normal = rowBg.color;
        ColorBlock cb = rowBtn.colors;
        cb.normalColor = normal;
        cb.highlightedColor = new Color(0.16f, 0.25f, 0.40f, 1f);
        cb.pressedColor = new Color(0.12f, 0.18f, 0.28f, 1f);
        rowBtn.colors = cb;
        rowBtn.onClick.AddListener(() => {
            selectedHero = hero;
            SwitchTab(MenuTab.Equip);
        });

        // Avatar do Herói
        GameObject spriteBox = CreateUIObject("HeroSpriteBox", rowObj.transform, new Vector2(0.01f, 0.05f), new Vector2(0.13f, 0.95f));
        Image spriteImg = spriteBox.AddComponent<Image>();
        spriteImg.sprite = HeroGenerator.GetClassSprite(hero.heroClass);
        spriteImg.preserveAspect = true;
        spriteImg.raycastTarget = false;

        // Coluna de Identificação, Classe e Atributos (Mais espaçosa e com letras grandes)
        GameObject infoObj = CreateUIObject("HeroInfo", rowObj.transform, new Vector2(0.14f, 0.04f), new Vector2(0.46f, 0.96f));
        
        GameObject nameObj = CreateUIObject("Name", infoObj.transform, new Vector2(0.00f, 0.54f), new Vector2(1.00f, 0.98f));
        string starText = hero.isGoldStar ? $"[{hero.currentStars} Estrela Ouro]" : $"[{hero.currentStars} Estrela]";
        CreateCrispText(nameObj, $"<b>{hero.heroName}</b> <color=#ffd166>{starText}</color>", 20.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        GameObject classObj = CreateUIObject("Class", infoObj.transform, new Vector2(0.00f, 0.04f), new Vector2(1.00f, 0.54f));
        HeroAttributes ha = hero.GetCurrentAttributes();
        string classAndAttrs =
            $"Classe: <color=#ffffff><b>{ItemData.FormatClassName(hero.heroClass)}</b></color>  |  <b>Nv. {hero.level}</b>\n" +
            $"<color=#4cc9f0>FOR:</color> <color=#ffd166><b>{ha.strength}</b></color>   " +
            $"<color=#4cc9f0>DES:</color> <color=#ffd166><b>{ha.dexterity}</b></color>   " +
            $"<color=#4cc9f0>INT:</color> <color=#ffd166><b>{ha.intelligence}</b></color>   " +
            $"<color=#4cc9f0>VIT:</color> <color=#ffd166><b>{ha.vitality}</b></color>";
        TextMeshProUGUI classTmp = CreateCrispText(classObj, classAndAttrs, 16.5f, new Color(0.86f, 0.92f, 1.00f), TextAlignmentOptions.MidlineLeft, true);
        classTmp.lineSpacing = 4f;

        // Barras de HP e MP com moldura escura e números grandes em destaque
        GameObject barsObj = CreateUIObject("HeroBars", rowObj.transform, new Vector2(0.47f, 0.08f), new Vector2(0.72f, 0.92f));

        CreateVitalsBar(barsObj.transform, "HPBar", "HP", hero.currentHP, hero.GetMaxHP(), 
            new Color(0.82f, 0.34f, 0.12f, 0.96f), new Vector2(0.00f, 0.54f), new Vector2(1.00f, 0.92f));

        CreateVitalsBar(barsObj.transform, "MPBar", "MP", hero.currentMP, hero.GetMaxMP(), 
            new Color(0.14f, 0.44f, 0.82f, 0.96f), new Vector2(0.00f, 0.08f), new Vector2(1.00f, 0.46f));

        // Preview de Equipamento à Direita
        GameObject equipPreviewObj = CreateUIObject("EquipPreview", rowObj.transform, new Vector2(0.73f, 0.06f), new Vector2(0.99f, 0.94f));
        BuildRowEquipPreview(equipPreviewObj.transform, hero);
    }

    private void CreateVitalsBar(Transform parent, string name, string label, int current, int max, Color fillColor, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject barObj = CreateUIObject(name, parent, anchorMin, anchorMax);
        
        Image bgImg = barObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.07f, 0.11f, 0.98f);

        Outline barOutline = barObj.AddComponent<Outline>();
        barOutline.effectColor = new Color(0.28f, 0.40f, 0.58f, 0.90f);
        barOutline.effectDistance = new Vector2(1.5f, -1.5f);

        float fillRatio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        GameObject fillObj = CreateUIObject("Fill", barObj.transform, new Vector2(0.01f, 0.06f), new Vector2(0.01f + 0.98f * fillRatio, 0.94f));
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = fillColor;

        GameObject txtObj = CreateUIObject("Label", barObj.transform, new Vector2(0.04f, 0.00f), new Vector2(0.96f, 1.00f));
        TextMeshProUGUI tmp = CreateCrispText(txtObj, $"{label}:  {current} / {max}", 18.5f, Color.white, TextAlignmentOptions.Center, true);
        tmp.raycastTarget = false;
    }

    private void BuildRowEquipPreview(Transform parent, HeroInstance hero)
    {
        Sprite wpIcon = hero.equippedWeapon != null ? hero.equippedWeapon.GetIcon() : null;
        string wpName = hero.equippedWeapon != null ? hero.equippedWeapon.itemName : "Desarmado";

        Sprite chestIcon = hero.equippedChest != null ? hero.equippedChest.GetIcon() : null;
        string chestName = hero.equippedChest != null ? hero.equippedChest.itemName : "Sem Armadura";

        GameObject wpObj = CreateUIObject("WpRow", parent, new Vector2(0.00f, 0.52f), new Vector2(1.00f, 0.98f));
        if (wpIcon != null)
        {
            GameObject iconObj = CreateUIObject("Icon", wpObj.transform, new Vector2(0.00f, 0.04f), new Vector2(0.20f, 0.96f));
            Image img = iconObj.AddComponent<Image>();
            img.sprite = wpIcon;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        GameObject wpTxtObj = CreateUIObject("Text", wpObj.transform, new Vector2(0.22f, 0.00f), new Vector2(1.00f, 1.00f));
        TextMeshProUGUI wpTmp = CreateCrispText(wpTxtObj, $"<color=#ffd166>[Arma]</color> <b>{wpName}</b>", 16f, Color.white, TextAlignmentOptions.MidlineLeft, true);
        wpTmp.raycastTarget = false;

        GameObject chObj = CreateUIObject("ChestRow", parent, new Vector2(0.00f, 0.02f), new Vector2(1.00f, 0.48f));
        if (chestIcon != null)
        {
            GameObject iconObj = CreateUIObject("Icon", chObj.transform, new Vector2(0.00f, 0.04f), new Vector2(0.20f, 0.96f));
            Image img = iconObj.AddComponent<Image>();
            img.sprite = chestIcon;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        GameObject chTxtObj = CreateUIObject("Text", chObj.transform, new Vector2(0.22f, 0.00f), new Vector2(1.00f, 1.00f));
        TextMeshProUGUI chTmp = CreateCrispText(chTxtObj, $"<color=#72efdd>[Peitoral]</color> <b>{chestName}</b>", 16f, Color.white, TextAlignmentOptions.MidlineLeft, true);
        chTmp.raycastTarget = false;
    }

    #endregion

    #region Visualização 2: Equipamento (Compatibilidade de Classes & Atributos)

    private void BuildEquipView()
    {
        foreach (Transform child in equipView.transform) Destroy(child.gameObject);

        if (selectedHero == null && PartyManager.Instance != null && PartyManager.Instance.activeParty.Count > 0)
            selectedHero = PartyManager.Instance.activeParty[0];

        if (selectedHero == null) return;

        GameObject headerObj = CreateUIObject("EquipHeader", equipView.transform, new Vector2(0.02f, 0.86f), new Vector2(0.98f, 0.98f));
        BuildHeroSelectorHeader(headerObj.transform);

        GameObject slotsBox = CreateUIObject("SlotsBox", equipView.transform, new Vector2(0.02f, 0.02f), new Vector2(0.49f, 0.84f));
        BuildEquipmentSlots(slotsBox.transform);

        GameObject inventorySideBox = CreateUIObject("InventorySideBox", equipView.transform, new Vector2(0.51f, 0.02f), new Vector2(0.98f, 0.84f));
        BuildCompatibleInventory(inventorySideBox.transform);
    }

    private void BuildHeroSelectorHeader(Transform parent)
    {
        GameObject avObj = CreateUIObject("Avatar", parent, new Vector2(0.00f, 0.04f), new Vector2(0.08f, 0.96f));
        Image avImg = avObj.AddComponent<Image>();
        avImg.sprite = HeroGenerator.GetClassSprite(selectedHero.heroClass);
        avImg.preserveAspect = true;

        HeroAttributes ha = selectedHero.GetCurrentAttributes();
        GameObject titleObj = CreateUIObject("Title", parent, new Vector2(0.09f, 0.00f), new Vector2(0.60f, 1.00f));
        string hdrText =
            $"<b><size=20>{selectedHero.heroName}</size></b>  <color=#a8dadc>({ItemData.FormatClassName(selectedHero.heroClass)} - Nv. {selectedHero.level})</color>\n" +
            $"<color=#ffd166>FOR: <b>{ha.strength}</b>  |  DES: <b>{ha.dexterity}</b>  |  INT: <b>{ha.intelligence}</b>  |  VIT: <b>{ha.vitality}</b>   |   HP: <b>{selectedHero.currentHP}/{selectedHero.GetMaxHP()}</b></color>";
        CreateCrispText(titleObj, hdrText, 16.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        var party = PartyManager.Instance.activeParty;
        float btnStartX = 0.61f;
        float btnW = 0.09f;
        for (int i = 0; i < party.Count; i++)
        {
            HeroInstance h = party[i];
            float x1 = btnStartX + (i * (btnW + 0.008f));
            float x2 = x1 + btnW;
            Color c = (h == selectedHero) ? new Color(0.18f, 0.56f, 0.68f) : new Color(0.18f, 0.22f, 0.32f);
            string shortName = h.heroName.Split(',')[0].Trim();
            CreateNavButton(parent, $"SelectHero_{i}", shortName, new Vector2(x1, 0.12f), new Vector2(x2, 0.88f), () => {
                selectedHero = h;
                if (currentTab == MenuTab.Equip) BuildEquipView();
                else if (currentTab == MenuTab.Inventory) BuildInventoryView();
                else if (currentTab == MenuTab.Status) BuildStatusView();
            }, c, 16.5f);
        }
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

        float slotHeight = 0.96f / 6f;

        for (int i = 0; i < 6; i++)
        {
            ItemType slotType = slots[i];
            string slotLabel = slotNames[i];
            float yMin = 0.98f - ((i + 1) * slotHeight);
            float yMax = 0.98f - (i * slotHeight) - 0.015f;

            ItemData equippedItem = GetEquippedItem(selectedHero, slotType);
            bool isSlotSelected = (selectedEquipSlot == slotType);

            GameObject slotObj = CreateUIObject($"Slot_{slotType}", parent, new Vector2(0.00f, yMin), new Vector2(1.00f, yMax));
            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = isSlotSelected 
                ? new Color(0.18f, 0.34f, 0.48f, 0.98f) 
                : new Color(0.10f, 0.14f, 0.21f, 0.96f);

            Outline outline = slotObj.AddComponent<Outline>();
            outline.effectColor = isSlotSelected ? new Color(0.35f, 0.78f, 1.00f, 1f) : new Color(0.25f, 0.38f, 0.54f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button slotBtn = slotObj.AddComponent<Button>();
            slotBtn.targetGraphic = slotBg;
            slotBtn.onClick.AddListener(() => {
                selectedEquipSlot = slotType;
                BuildEquipView();
            });

            GameObject frameObj = CreateUIObject("IconFrame", slotObj.transform, new Vector2(0.02f, 0.08f), new Vector2(0.17f, 0.92f));
            Image frameBg = frameObj.AddComponent<Image>();
            frameBg.color = new Color(0.05f, 0.07f, 0.11f, 0.95f);
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
            string classCompat = equippedItem != null ? $"<size=14><color=#cbd5e1>({equippedItem.GetAllowedClassesText()})</color></size>" : "";
            string slotInfoText = $"<color=#ffd166><b>{slotLabel}</b></color> {classCompat}\n{itemNameStr}  <color=#2ecc71><b>{statStr}</b></color>";
            TextMeshProUGUI tmp = CreateCrispText(txtObj, slotInfoText, 16f, Color.white, TextAlignmentOptions.MidlineLeft, true);
            tmp.raycastTarget = false;

            if (equippedItem != null)
            {
                CreateNavButton(slotObj.transform, "UnequipBtn", "Guardar", 
                    new Vector2(0.77f, 0.14f), new Vector2(0.98f, 0.86f), () => {
                        selectedHero.UnequipItem(slotType);
                        PartyManager.Instance.AddItemToInventory(equippedItem);
                        BuildEquipView();
                    }, new Color(0.55f, 0.20f, 0.22f), 16.5f);
            }
        }
    }

    private void BuildCompatibleInventory(Transform parent)
    {
        GameObject hdrObj = CreateUIObject("SideHeader", parent, new Vector2(0.00f, 0.90f), new Vector2(1.00f, 1.00f));
        CreateCrispText(hdrObj, $"EQUIPAMENTOS NA MOCHILA ({FormatSlotTypeName(selectedEquipSlot)}):", 18f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.MidlineLeft, true);

        GameObject listObj = CreateUIObject("CompatibleList", parent, new Vector2(0.00f, 0.02f), new Vector2(1.00f, 0.88f));
        
        List<InventoryStack> slotStacks = BuildInventoryStacks(selectedEquipSlot);

        if (slotStacks.Count == 0)
        {
            GameObject emptyObj = CreateUIObject("EmptyText", listObj.transform, new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.80f));
            string emptyMsg = $"Nenhum equipamento do tipo <b>{FormatSlotTypeName(selectedEquipSlot)}</b> guardado na mochila.\n\n" +
                              "<size=16><color=#cbd5e1>Dica: Os monstros derrotados na masmorra podem dropar armas, armaduras e pocoes para sua mochila!</color></size>";
            CreateCrispText(emptyObj, emptyMsg, 18f, new Color(0.88f, 0.92f, 0.98f), TextAlignmentOptions.Center, true);
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
        GameObject cardObj = CreateUIObject($"Item_{item.itemName}", parent, anchorMin, anchorMax);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.11f, 0.15f, 0.22f, 0.96f);

        Outline outline = cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.28f, 0.42f, 0.60f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject frameObj = CreateUIObject("IconFrame", cardObj.transform, new Vector2(0.02f, 0.08f), new Vector2(0.17f, 0.92f));
        Image frameBg = frameObj.AddComponent<Image>();
        frameBg.color = new Color(0.05f, 0.07f, 0.11f, 0.95f);

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
            $"<b><size=17>{item.itemName}</size></b>{qtyTag} <color=#ffd166>[{item.GetAllowedClassesText()}]</color>\n" +
            $"<color=#2ecc71><b>{stats}</b></color>   <color=#cbd5e1>({reqDetails})</color>\n" +
            $"<color={reqColor}><b>{reqStr}</b></color>";
        CreateCrispText(txtObj, cardInfo, 15f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        Button equipBtn = CreateNavButton(cardObj.transform, "EquipBtn", canEquip ? "Equipar" : "Bloqueado", 
            new Vector2(0.76f, 0.14f), new Vector2(0.98f, 0.86f), () => {
                string msg;
                ItemData instanceToEquip = stack.instances[0];
                ItemData previous = GetEquippedItem(selectedHero, instanceToEquip.itemType);
                if (selectedHero.TryEquipItem(instanceToEquip, out msg))
                {
                    PartyManager.Instance.inventory.Remove(instanceToEquip);
                    if (previous != null) PartyManager.Instance.AddItemToInventory(previous);
                    BuildEquipView();
                }
            }, canEquip ? new Color(0.18f, 0.56f, 0.35f) : new Color(0.35f, 0.20f, 0.22f), 16.5f);
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

    #region Visualização 3: Mochila em Grade de Slots Quadrados (Estilo Minecraft com Stacking)

    private void BuildInventoryView()
    {
        foreach (Transform child in inventoryView.transform) Destroy(child.gameObject);

        if (selectedHero == null && PartyManager.Instance != null && PartyManager.Instance.activeParty.Count > 0)
            selectedHero = PartyManager.Instance.activeParty[0];

        // 1. Topo: Seletor de Herói
        GameObject hdrObj = CreateUIObject("InvHeader", inventoryView.transform, new Vector2(0.02f, 0.86f), new Vector2(0.98f, 0.98f));
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

        // 2. Subtítulo da Grade
        GameObject subHdrObj = CreateUIObject("SubHeader", inventoryView.transform, new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.85f));
        CreateCrispText(subHdrObj, $"MOCHILA DA GUILDA  ({stacks.Count} / {TOTAL_BACKPACK_SLOTS} Slots Ocupados)  -  Itens identicos agrupam no mesmo slot!", 17.5f, new Color(1.00f, 0.85f, 0.35f), TextAlignmentOptions.MidlineLeft, true);

        // 3. Container da Grade de 24 Slots Quadrados (8 colunas x 3 linhas)
        GameObject gridBox = CreateUIObject("BackpackGridBox", inventoryView.transform, new Vector2(0.03f, 0.29f), new Vector2(0.97f, 0.77f));
        Image gridBg = gridBox.AddComponent<Image>();
        gridBg.color = new Color(0.05f, 0.07f, 0.11f, 0.96f);
        Outline gridOutline = gridBox.AddComponent<Outline>();
        gridOutline.effectColor = new Color(0.25f, 0.40f, 0.60f, 0.88f);
        gridOutline.effectDistance = new Vector2(1.5f, -1.5f);

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

        // 4. Painel Inferior de Inspeção e Ação do Slot Selecionado
        GameObject inspectorBox = CreateUIObject("SelectedSlotInspector", inventoryView.transform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.27f));
        InventoryStack selectedStack = (selectedBackpackSlotIndex >= 0 && selectedBackpackSlotIndex < stacks.Count)
            ? stacks[selectedBackpackSlotIndex]
            : null;
        BuildSelectedStackInspector(inspectorBox.transform, selectedStack);
    }

    private void BuildSquareBackpackSlot(Transform parent, int slotIndex, InventoryStack stack, Vector2 anchorMin, Vector2 anchorMax)
    {
        bool isOccupied = (stack != null && stack.Count > 0);
        bool isSelected = (isOccupied && slotIndex == selectedBackpackSlotIndex);

        GameObject slotObj = CreateUIObject($"Slot_{slotIndex}", parent, anchorMin, anchorMax);
        Image slotBg = slotObj.AddComponent<Image>();

        if (isSelected)
        {
            slotBg.color = new Color(0.20f, 0.38f, 0.56f, 1f);
        }
        else if (isOccupied)
        {
            slotBg.color = new Color(0.11f, 0.16f, 0.25f, 0.98f);
        }
        else
        {
            slotBg.color = new Color(0.07f, 0.09f, 0.14f, 0.88f);
        }

        Outline outline = slotObj.AddComponent<Outline>();
        if (isSelected)
        {
            outline.effectColor = new Color(1.00f, 0.86f, 0.28f, 1f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
        }
        else if (isOccupied)
        {
            outline.effectColor = GetRarityBorderColor(stack.sampleItem.rarity);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        else
        {
            outline.effectColor = new Color(0.20f, 0.28f, 0.40f, 0.75f);
            outline.effectDistance = new Vector2(1, -1);
        }

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
        badgeBg.color = new Color(0.03f, 0.04f, 0.07f, 0.92f);
        badgeBg.raycastTarget = false;

        GameObject txtObj = CreateUIObject("CountText", badgeBgObj.transform, Vector2.zero, Vector2.one);
        Color numColor = count > 1 ? new Color(1.00f, 0.90f, 0.30f) : Color.white;
        TextMeshProUGUI tmp = CreateCrispText(txtObj, count.ToString(), 18f, numColor, TextAlignmentOptions.Center, true);
        tmp.raycastTarget = false;
    }

    private void BuildSelectedStackInspector(Transform parent, InventoryStack stack)
    {
        Image bg = parent.gameObject.AddComponent<Image>();
        bg.color = new Color(0.09f, 0.12f, 0.19f, 0.98f);

        Outline outline = parent.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.30f, 0.48f, 0.72f, 0.92f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        if (stack == null || stack.sampleItem == null)
        {
            GameObject emptyObj = CreateUIObject("EmptyInspector", parent, Vector2.zero, Vector2.one);
            CreateCrispText(emptyObj, "Selecione um slot ocupado na grade acima para inspecionar ou usar o item.", 18f, new Color(0.80f, 0.86f, 0.94f), TextAlignmentOptions.Center, true);
            return;
        }

        ItemData item = stack.sampleItem;

        // 1. Moldura + Ícone em destaque à esquerda
        GameObject frameObj = CreateUIObject("IconFrame", parent, new Vector2(0.02f, 0.10f), new Vector2(0.13f, 0.90f));
        Image frameBg = frameObj.AddComponent<Image>();
        frameBg.color = new Color(0.05f, 0.07f, 0.11f, 0.96f);

        Sprite ic = item.GetIcon();
        if (ic != null)
        {
            GameObject icObj = CreateUIObject("Icon", frameObj.transform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
            Image icImg = icObj.AddComponent<Image>();
            icImg.sprite = ic;
            icImg.preserveAspect = true;
        }

        // 2. Informações completas do Item e da Pilha (Centro)
        GameObject infoObj = CreateUIObject("ItemDetails", parent, new Vector2(0.15f, 0.06f), new Vector2(0.72f, 0.94f));

        string stats = GetItemStatsShort(item);
        string classText = item.itemType == ItemType.Consumable ? "Todas as Classes" : item.GetAllowedClassesText();
        string reqDetails = item.itemType == ItemType.Consumable ? "" : $"  |  {GetItemRequirementsShort(item)}";

        string detailsText =
            $"<b><size=20>{item.itemName}</size></b>   <color=#ffd166><b>Quantidade no Slot: x{stack.Count}</b></color>   <color=#cbd5e1>[{FormatSlotTypeName(item.itemType)}]</color>\n" +
            $"<color=#2ecc71><b>{stats}</b></color>   |   Classes: <b>{classText}</b>{reqDetails}\n" +
            $"<size=15><color=#dce4f0>{item.description}</color>   <color=#ffd166>(Valor unitario: {item.goldValue} G)</color></size>";
        CreateCrispText(infoObj, detailsText, 16.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        // 3. Botões de Ação à direita
        if (item.itemType == ItemType.Consumable && selectedHero != null)
        {
            bool needsHeal = selectedHero.IsAlive && selectedHero.currentHP < selectedHero.GetMaxHP();
            string btnLabel = needsHeal ? $"Usar em {selectedHero.heroName.Split(',')[0]}" : "HP Ja Cheio";

            Button useBtn = CreateNavButton(parent, "UsePotionBtn", btnLabel,
                new Vector2(0.74f, 0.52f), new Vector2(0.98f, 0.90f), () => {
                    int healAmount = Mathf.Max(15, Mathf.RoundToInt(selectedHero.GetMaxHP() * (item.healHP / 100f)));
                    selectedHero.Heal(healAmount);
                    PartyManager.Instance.inventory.Remove(stack.instances[0]);
                    BuildInventoryView();
                }, needsHeal ? new Color(0.18f, 0.58f, 0.32f) : new Color(0.28f, 0.30f, 0.35f), 17f);
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
                        BuildInventoryView();
                    }
                }, canEquip ? new Color(0.18f, 0.52f, 0.65f) : new Color(0.32f, 0.22f, 0.24f), 17f);
            eqBtn.interactable = canEquip;
        }

        CreateNavButton(parent, "DiscardOneBtn", "Descartar 1",
            new Vector2(0.74f, 0.10f), new Vector2(0.98f, 0.44f), () => {
                PartyManager.Instance.inventory.Remove(stack.instances[0]);
                BuildInventoryView();
            }, new Color(0.50f, 0.20f, 0.22f), 16.5f);
    }

    private Color GetRarityBorderColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return new Color(0.20f, 0.78f, 0.42f, 0.92f);
            case ItemRarity.Rare:     return new Color(0.28f, 0.58f, 0.98f, 0.96f);
            case ItemRarity.Epic:     return new Color(0.68f, 0.32f, 0.95f, 0.96f);
            case ItemRarity.Legendary:return new Color(1.00f, 0.82f, 0.22f, 1.00f);
            default:                  return new Color(0.32f, 0.45f, 0.65f, 0.88f);
        }
    }

    #endregion

    #region Visualização 4: Status e Atributos Dark Souls

    private void BuildStatusView()
    {
        foreach (Transform child in statusView.transform) Destroy(child.gameObject);

        if (selectedHero == null && PartyManager.Instance != null && PartyManager.Instance.activeParty.Count > 0)
            selectedHero = PartyManager.Instance.activeParty[0];

        if (selectedHero == null) return;

        GameObject headerObj = CreateUIObject("StatusHeader", statusView.transform, new Vector2(0.02f, 0.86f), new Vector2(0.98f, 0.98f));
        BuildHeroSelectorHeader(headerObj.transform);

        GameObject attrBox = CreateUIObject("AttrBox", statusView.transform, new Vector2(0.02f, 0.02f), new Vector2(0.48f, 0.84f));
        Image aBg = attrBox.AddComponent<Image>();
        aBg.color = new Color(0.09f, 0.12f, 0.18f, 0.96f);
        Outline aOut = attrBox.AddComponent<Outline>();
        aOut.effectColor = new Color(0.28f, 0.44f, 0.65f, 0.85f);

        GameObject aTitle = CreateUIObject("Title", attrBox.transform, new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.98f));
        CreateCrispText(aTitle, $"ATRIBUTOS PRIMARIOS (Pontos Livres: <color=#ffd166><b>{selectedHero.unallocatedAttributePoints}</b></color>)", 19.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        HeroAttributes attrs = selectedHero.GetCurrentAttributes();
        CreateAttributeRow(attrBox.transform, "FORCA (STR):", attrs.strength, 0.69f, () => {
            if (selectedHero.AllocateAttributePoint("STR")) BuildStatusView();
        });
        CreateAttributeRow(attrBox.transform, "DESTREZA (DEX):", attrs.dexterity, 0.51f, () => {
            if (selectedHero.AllocateAttributePoint("DEX")) BuildStatusView();
        });
        CreateAttributeRow(attrBox.transform, "INTELIGENCIA (INT):", attrs.intelligence, 0.33f, () => {
            if (selectedHero.AllocateAttributePoint("INT")) BuildStatusView();
        });
        CreateAttributeRow(attrBox.transform, "VITALIDADE (VIT):", attrs.vitality, 0.15f, () => {
            if (selectedHero.AllocateAttributePoint("VIT")) BuildStatusView();
        });

        GameObject statsBox = CreateUIObject("StatsBox", statusView.transform, new Vector2(0.50f, 0.02f), new Vector2(0.98f, 0.84f));
        Image sBg = statsBox.AddComponent<Image>();
        sBg.color = new Color(0.09f, 0.12f, 0.18f, 0.96f);
        Outline sOut = statsBox.AddComponent<Outline>();
        sOut.effectColor = new Color(0.28f, 0.44f, 0.65f, 0.85f);

        GameObject sTitle = CreateUIObject("Title", statsBox.transform, new Vector2(0.06f, 0.87f), new Vector2(0.94f, 0.98f));
        CreateCrispText(sTitle, "ESTATISTICAS DERIVADAS DE COMBATE", 19.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        GameObject sBody = CreateUIObject("Body", statsBox.transform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.84f));
        string derivedText =
            $"- Vida Maxima (HP):   <b><color=#ff9f1c>{selectedHero.currentHP} / {selectedHero.GetMaxHP()}</color></b>\n" +
            $"- Mana Maxima (MP):   <b><color=#4cc9f0>{selectedHero.currentMP} / {selectedHero.GetMaxMP()}</color></b>\n\n" +
            $"- Ataque Fisico:       <b><color=#ff6b6b>{selectedHero.GetPhysicalAttack()}</color></b>\n" +
            $"- Defesa Fisica:       <b><color=#4ea8de>{selectedHero.GetPhysicalDefense()}</color></b>\n" +
            $"- Ataque Magico:      <b><color=#c77dff>{selectedHero.GetMagicAttack()}</color></b>\n" +
            $"- Velocidade / Turno:  <b><color=#2ecc71>{selectedHero.GetSpeed()}</color></b>\n" +
            $"- Taxa de Critico:     <b><color=#ffd166>{selectedHero.GetCritRate():F1}%</color></b>";
        TextMeshProUGUI sbTmp = CreateCrispText(sBody, derivedText, 19f, Color.white, TextAlignmentOptions.TopLeft, true);
        sbTmp.lineSpacing = 14f;
    }

    private void CreateAttributeRow(Transform parent, string label, int value, float yAnchor, UnityEngine.Events.UnityAction onPlus)
    {
        GameObject row = CreateUIObject("AttrRow", parent, new Vector2(0.06f, yAnchor), new Vector2(0.94f, yAnchor + 0.14f));
        
        GameObject txtObj = CreateUIObject("Text", row.transform, new Vector2(0.00f, 0.00f), new Vector2(0.72f, 1.00f));
        CreateCrispText(txtObj, $"{label}   <size=22><color=#ffd166><b>{value}</b></color></size>", 19f, Color.white, TextAlignmentOptions.MidlineLeft, true);

        Button plusBtn = CreateNavButton(row.transform, "PlusBtn", "[ + ]", 
            new Vector2(0.75f, 0.10f), new Vector2(0.98f, 0.90f), onPlus, new Color(0.18f, 0.58f, 0.35f), 20f);
        plusBtn.interactable = selectedHero.unallocatedAttributePoints > 0;
    }

    #endregion

    #region Utilitários de Criação de UI e Texto de Alta Nitidez

    private TextMeshProUGUI CreateCrispText(GameObject obj, string content, float fontSize, Color color, TextAlignmentOptions alignment, bool bold = true)
    {
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.outlineWidth = 0.20f;
        tmp.outlineColor = new Color32(0, 0, 0, 235);

        Shadow sh = obj.GetComponent<Shadow>();
        if (sh == null) sh = obj.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.90f);
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
