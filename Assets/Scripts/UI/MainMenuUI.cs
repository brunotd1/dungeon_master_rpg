using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu Principal do Jogo (Tela de Titulo com botoes compactos e transparentes sobre a arte de fundo).
/// Permite ao jogador:
/// 1. [ CONTINUAR JOGO ] (Carrega automaticamente o save mais recente e entra na Sede da Guilda)
/// 2. [ INICIAR NOVO JOGO ] (Cria nova campanha no Slot livre ou escolhido e entra na Sede da Guilda)
/// 3. [ CARREGAR SAVE ] (Visualiza e escolhe qualquer um dos 4 Slots de Save com detalhes completos)
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private static MainMenuUI _instance;
    public static MainMenuUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<MainMenuUI>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MainMenuUI", typeof(MainMenuUI));
                    _instance = go.GetComponent<MainMenuUI>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Referencias de UI")]
    public Canvas targetCanvas;
    public GameObject mainMenuRootPanel;
    public Image backgroundArtImage;
    public GameObject titleScreenView;
    public GameObject saveSlotsView;

    private static Sprite _cachedMainMenuBgSprite;
    private bool isSelectingSlotForNewGame = false;
    public bool IsMenuOpen => mainMenuRootPanel != null && mainMenuRootPanel.activeSelf;

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
        OpenMainMenu();
    }

    public static Sprite LoadBackgroundSprite(string resourceSubPath, string pngFileName)
    {
        Sprite spr = Resources.Load<Sprite>(resourceSubPath);
        if (spr != null) return spr;

        string fullPath = Path.Combine(Application.dataPath, "Resources", "Sprites", "Backgrounds", pngFileName);
        if (!File.Exists(fullPath))
        {
            fullPath = Path.Combine(Application.dataPath, "Sprites", "Backgrounds", pngFileName);
        }

        if (File.Exists(fullPath))
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (ImageConversion.LoadImage(tex, bytes))
            {
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }
        return null;
    }

    public void OpenMainMenu()
    {
        BuildUIStructure();
        if (mainMenuRootPanel == null) return;

        mainMenuRootPanel.SetActive(true);
        mainMenuRootPanel.transform.SetAsLastSibling();
        ShowTitleScreenView();
    }

    public void CloseMainMenu()
    {
        if (mainMenuRootPanel != null)
        {
            mainMenuRootPanel.SetActive(false);
        }
    }

    private void BuildUIStructure()
    {
        if (mainMenuRootPanel != null) return;

        if (targetCanvas == null)
        {
            if (BattleHUD.Instance != null)
            {
                BattleHUD.Instance.BuildHUDStructureIfNotExisting();
                targetCanvas = BattleHUD.Instance.battleCanvas;
            }
            if (targetCanvas == null)
            {
                targetCanvas = FindAnyObjectByType<Canvas>();
            }
        }

        if (targetCanvas == null) return;

        // 1. Painel Raiz em Tela Cheia
        mainMenuRootPanel = CreateUIObject("MainMenuRootPanel", targetCanvas.transform, Vector2.zero, Vector2.one);
        Image rootBg = mainMenuRootPanel.AddComponent<Image>();
        rootBg.color = new Color(0.02f, 0.03f, 0.06f, 1.0f);

        // 2. Imagem de Fundo Dedicada do Menu Principal (100% visivel e nitida)
        GameObject bgArtObj = CreateUIObject("MainMenuBackgroundArt", mainMenuRootPanel.transform, Vector2.zero, Vector2.one);
        backgroundArtImage = bgArtObj.AddComponent<Image>();
        if (_cachedMainMenuBgSprite == null)
        {
            _cachedMainMenuBgSprite = LoadBackgroundSprite("Sprites/Backgrounds/bg_main_menu", "bg_main_menu.png");
            if (_cachedMainMenuBgSprite == null)
                _cachedMainMenuBgSprite = LoadBackgroundSprite("Sprites/Backgrounds/bg_catacombs", "bg_catacombs.png");
        }

        if (_cachedMainMenuBgSprite != null)
        {
            backgroundArtImage.sprite = _cachedMainMenuBgSprite;
            backgroundArtImage.color = Color.white;
            backgroundArtImage.preserveAspect = false;
        }
        else
        {
            backgroundArtImage.color = new Color(0.05f, 0.07f, 0.12f, 1f);
        }

        titleScreenView = CreateUIObject("TitleScreenView", mainMenuRootPanel.transform, Vector2.zero, Vector2.one);
        saveSlotsView = CreateUIObject("SaveSlotsView", mainMenuRootPanel.transform, Vector2.zero, Vector2.one);
        saveSlotsView.SetActive(false);
    }

    #region 1. Tela Inicial Limpa com Botoes Menores e Transparentes

    public void ShowTitleScreenView()
    {
        titleScreenView.SetActive(true);
        saveSlotsView.SetActive(false);

        if (backgroundArtImage != null)
        {
            backgroundArtImage.color = Color.white;
        }

        foreach (Transform child in titleScreenView.transform) Destroy(child.gameObject);

        // 1. Titulo Flutuante no Topo (Sem caixa de fundo para nao cobrir a Lua nem a Torre!)
        GameObject titleTxtObj = CreateUIObject("GameTitleText", titleScreenView.transform, new Vector2(0.15f, 0.80f), new Vector2(0.85f, 0.96f));
        CreateCrispText(titleTxtObj,
            "<size=40><color=#ffd166><b>DUNGEON MASTER RPG</b></color></size>\n" +
            "<size=16><color=#e2e8f0>A TORRE DOS ESQUECIDOS  &  A GUILDA DOS AVENTUREIROS</color></size>",
            30f, Color.white, TextAlignmentOptions.Center, true);

        // 2. Resumo Discreto do Ultimo Save (Flutuante logo acima dos botoes, com fundo super leve)
        GameSaveData recentSave = SaveSystem.GetMostRecentSave();
        int saveCount = SaveSystem.GetExistingSaveCount();

        GameObject summaryPill = CreateGlassBox(
            "SaveSummaryPill",
            titleScreenView.transform,
            new Vector2(0.32f, 0.30f),
            new Vector2(0.68f, 0.37f),
            new Color(0.02f, 0.03f, 0.06f, 0.42f),
            new Color(0.85f, 0.68f, 0.30f, 0.55f),
            1f
        );

        GameObject sumTxtObj = CreateUIObject("SummaryText", summaryPill.transform, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f));
        if (recentSave != null)
        {
            int totalHeroes = (recentSave.heroes != null ? recentSave.heroes.Count : 0) + (recentSave.reserveHeroes != null ? recentSave.reserveHeroes.Count : 0);
            int depositCount = recentSave.guildDeposit != null ? recentSave.guildDeposit.Count : 0;

            string summaryStr =
                $"<color=#2ecc71><b>Save Slot {recentSave.slotIndex}</b></color>  |  " +
                $"Andar <color=#ffd166><b>{recentSave.currentFloor}</b></color> (Liberados: <color=#4cc9f0><b>1-{recentSave.maxUnlockedFloor}</b></color>)  |  " +
                $"Herois: <b>{totalHeroes}</b>  |  <color=#ffd166><b>{recentSave.goldCoins} G</b></color>  |  Armazem: <b>{depositCount}</b>";

            CreateCrispText(sumTxtObj, summaryStr, 14f, Color.white, TextAlignmentOptions.Center, true);
        }
        else
        {
            CreateCrispText(sumTxtObj, "<color=#ffd166>Nenhum Save ativo</color> — Clique em <b>Iniciar Novo Jogo</b> para entrar na Guilda!", 14f, Color.white, TextAlignmentOptions.Center, true);
        }

        // 3. Botoes Compactos e Transparentes na parte inferior central (x: 0.36 a 0.64)
        // Botao 1: CONTINUAR JOGO
        string continueLabel = recentSave != null
            ? $"CONTINUAR JOGO (SLOT {recentSave.slotIndex})"
            : "CONTINUAR JOGO";
        Button continueBtn = CreateGlassButton(
            titleScreenView.transform,
            "ContinueBtn",
            continueLabel,
            new Vector2(0.36f, 0.21f),
            new Vector2(0.64f, 0.28f),
            () => {
                if (recentSave != null)
                {
                    LoadSlotAndEnterGame(recentSave.slotIndex);
                }
            },
            recentSave != null ? new Color(0.06f, 0.28f, 0.16f, 0.48f) : new Color(0.08f, 0.09f, 0.12f, 0.30f),
            recentSave != null ? new Color(0.35f, 0.88f, 0.55f, 0.80f) : new Color(0.40f, 0.45f, 0.52f, 0.40f),
            16.5f
        );
        continueBtn.interactable = (recentSave != null);

        // Botao 2: INICIAR NOVO JOGO
        CreateGlassButton(
            titleScreenView.transform,
            "NewGameBtn",
            "INICIAR NOVO JOGO",
            new Vector2(0.36f, 0.13f),
            new Vector2(0.64f, 0.20f),
            () => {
                if (!SaveSystem.HasAnySave())
                {
                    StartNewGameInSlotAndEnter(1);
                }
                else
                {
                    isSelectingSlotForNewGame = true;
                    ShowSaveSlotsView();
                }
            },
            new Color(0.06f, 0.16f, 0.32f, 0.48f),
            new Color(0.45f, 0.75f, 1.00f, 0.80f),
            16.5f
        );

        // Botao 3: CARREGAR SAVE / SLOTS
        string loadLabel = saveCount > 0
            ? $"CARREGAR SAVE ({saveCount})"
            : "SLOTS DE SAVE";

        CreateGlassButton(
            titleScreenView.transform,
            "LoadSaveBtn",
            loadLabel,
            new Vector2(0.36f, 0.05f),
            new Vector2(0.64f, 0.12f),
            () => {
                isSelectingSlotForNewGame = false;
                ShowSaveSlotsView();
            },
            new Color(0.22f, 0.14f, 0.05f, 0.48f),
            new Color(0.95f, 0.76f, 0.32f, 0.80f),
            16f
        );
    }

    #endregion

    #region 2. Tela de Selecao e Gerenciamento de Slots de Save (Transparente sobre o Fundo)

    public void ShowSaveSlotsView()
    {
        titleScreenView.SetActive(false);
        saveSlotsView.SetActive(true);

        foreach (Transform child in saveSlotsView.transform) Destroy(child.gameObject);

        GameObject box = CreateGlassBox(
            "SlotsContainerBox",
            saveSlotsView.transform,
            new Vector2(0.12f, 0.08f),
            new Vector2(0.88f, 0.92f),
            new Color(0.02f, 0.03f, 0.07f, 0.58f),
            new Color(0.90f, 0.72f, 0.30f, 0.85f),
            1.5f
        );

        // Cabecalho
        GameObject hdrObj = CreateUIObject("Header", box.transform, new Vector2(0.03f, 0.88f), new Vector2(0.74f, 0.98f));
        string headerTitle = isSelectingSlotForNewGame
            ? "ESCOLHA UM SLOT PARA INICIAR O NOVO JOGO"
            : "CARREGAR JOGO / SLOTS DE SAVE DA GUILDA";
        CreateCrispText(hdrObj, headerTitle, 22f, new Color(1.00f, 0.85f, 0.32f), TextAlignmentOptions.MidlineLeft, true);

        // Botao Voltar ao Menu Principal
        CreateGlassButton(
            box.transform,
            "BackToTitleBtn",
            "VOLTAR AO MENU [X]",
            new Vector2(0.76f, 0.89f),
            new Vector2(0.97f, 0.97f),
            ShowTitleScreenView,
            new Color(0.38f, 0.10f, 0.12f, 0.58f),
            new Color(0.95f, 0.45f, 0.45f, 0.85f),
            15.5f
        );

        // Lista dos 4 Slots de Save
        float slotHeight = 0.82f / SaveSystem.MAX_SAVE_SLOTS;
        for (int s = 1; s <= SaveSystem.MAX_SAVE_SLOTS; s++)
        {
            int slotNumber = s;
            float yMax = 0.86f - ((s - 1) * slotHeight) - 0.01f;
            float yMin = 0.86f - (s * slotHeight) + 0.01f;

            GameSaveData slotData = SaveSystem.ReadSaveSlotHeader(slotNumber);
            BuildSaveSlotCard(box.transform, slotNumber, slotData, new Vector2(0.03f, yMin), new Vector2(0.97f, yMax));
        }
    }

    private void BuildSaveSlotCard(Transform parent, int slotNumber, GameSaveData data, Vector2 anchorMin, Vector2 anchorMax)
    {
        bool hasData = (data != null);
        bool isCurrentSlot = (SaveSystem.CurrentActiveSlot == slotNumber && hasData);

        Color cardColor = hasData
            ? (isCurrentSlot ? new Color(0.06f, 0.14f, 0.24f, 0.56f) : new Color(0.04f, 0.07f, 0.14f, 0.50f))
            : new Color(0.03f, 0.04f, 0.08f, 0.40f);

        Color borderColor = isCurrentSlot
            ? new Color(1.00f, 0.84f, 0.28f, 0.90f)
            : (hasData ? new Color(0.42f, 0.65f, 0.92f, 0.75f) : new Color(0.35f, 0.42f, 0.55f, 0.50f));

        GameObject cardObj = CreateGlassBox($"SaveSlot_{slotNumber}", parent, anchorMin, anchorMax, cardColor, borderColor, 1.5f);

        GameObject infoObj = CreateUIObject("SlotInfo", cardObj.transform, new Vector2(0.02f, 0.06f), new Vector2(0.64f, 0.94f));

        if (hasData)
        {
            int depositCount = data.guildDeposit != null ? data.guildDeposit.Count : 0;
            int reserveCount = data.reserveHeroes != null ? data.reserveHeroes.Count : 0;
            List<string> heroDescriptions = new List<string>();
            if (data.heroes != null)
            {
                for (int i = 0; i < data.heroes.Count; i++)
                {
                    var h = data.heroes[i];
                    string shortN = h.heroName.Split(',')[0].Trim();
                    string classN = ItemData.FormatClassName((HeroClassType)h.heroClass);
                    heroDescriptions.Add($"<color=#ffd166>[{h.currentStars}*]</color> <b>{shortN}</b> ({classN} Nv.{h.level})");
                }
            }
            string heroesLine = heroDescriptions.Count > 0 ? string.Join("  |  ", heroDescriptions) : "Sem herois";
            string activeTag = isCurrentSlot ? " <color=#2ecc71>[ULTIMO JOGADO]</color>" : "";

            string slotText =
                $"<size=18><color=#ffd166><b>SLOT {slotNumber}</b></color>{activeTag}   -   <color=#cbd5e1>{data.lastSavedDate}</color></size>\n" +
                $"Andar: <color=#2ecc71><b>{data.currentFloor}</b></color> (Liberados: <color=#4cc9f0><b>1-{data.maxUnlockedFloor}</b></color>)  |  Ouro: <color=#ffd166><b>{data.goldCoins} G</b></color>  |  Armazem: <color=#2ecc71><b>{depositCount} Itens</b></color>  |  Reserva: <b>{reserveCount}</b>\n" +
                $"<size=14.5>Party:  {heroesLine}</size>";

            CreateCrispText(infoObj, slotText, 15.5f, Color.white, TextAlignmentOptions.MidlineLeft, true);

            if (isSelectingSlotForNewGame)
            {
                CreateGlassButton(cardObj.transform, "OverwriteNewBtn", "SOBRESCREVER COM NOVO JOGO",
                    new Vector2(0.66f, 0.20f), new Vector2(0.98f, 0.80f), () => {
                        StartNewGameInSlotAndEnter(slotNumber);
                    }, new Color(0.45f, 0.22f, 0.06f, 0.60f), new Color(0.95f, 0.68f, 0.25f, 0.85f), 15.5f);
            }
            else
            {
                CreateGlassButton(cardObj.transform, "LoadSlotBtn", $"CARREGAR SLOT {slotNumber}",
                    new Vector2(0.66f, 0.20f), new Vector2(0.86f, 0.80f), () => {
                        LoadSlotAndEnterGame(slotNumber);
                    }, new Color(0.08f, 0.36f, 0.20f, 0.60f), new Color(0.35f, 0.90f, 0.55f, 0.85f), 16f);

                CreateGlassButton(cardObj.transform, "DeleteSlotBtn", "APAGAR",
                    new Vector2(0.87f, 0.20f), new Vector2(0.98f, 0.80f), () => {
                        SaveSystem.DeleteSaveSlot(slotNumber);
                        ShowSaveSlotsView();
                    }, new Color(0.38f, 0.10f, 0.12f, 0.60f), new Color(0.95f, 0.45f, 0.45f, 0.85f), 15f);
            }
        }
        else
        {
            string emptyText =
                $"<size=18><color=#94a3b8><b>SLOT {slotNumber}  -  [ VAZIO ]</b></color></size>\n" +
                "<color=#cbd5e1>Disponivel para iniciar uma nova campanha na Guilda!</color>";
            CreateCrispText(infoObj, emptyText, 15.5f, new Color(0.85f, 0.90f, 0.96f), TextAlignmentOptions.MidlineLeft, true);

            CreateGlassButton(cardObj.transform, "NewGameHereBtn", $"NOVO JOGO NO SLOT {slotNumber}",
                new Vector2(0.66f, 0.20f), new Vector2(0.98f, 0.80f), () => {
                    StartNewGameInSlotAndEnter(slotNumber);
                }, new Color(0.08f, 0.24f, 0.46f, 0.58f), new Color(0.45f, 0.75f, 1.00f, 0.85f), 16f);
        }
    }

    #endregion

    #region 3. Transicao para a Sede da Guilda (Novo Jogo, Continuar ou Carregar Save)

    private void StartNewGameInSlotAndEnter(int slotNumber)
    {
        SaveSystem.StartNewGameInSlot(slotNumber);
        CloseMainMenu();

        if (PartyMenuUI.Instance != null)
        {
            PartyMenuUI.Instance.EnterGuildHub();
        }
    }

    private void LoadSlotAndEnterGame(int slotNumber)
    {
        if (SaveSystem.LoadGame(slotNumber))
        {
            CloseMainMenu();
            if (PartyMenuUI.Instance != null)
            {
                PartyMenuUI.Instance.EnterGuildHub();
            }
        }
    }

    #endregion

    #region 4. Utilitarios de Vidro Transparente Real (Sem Outline solido no Image)

    public static GameObject CreateGlassBox(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color glassBgColor, Color borderColor, float borderThickness = 1.5f)
    {
        GameObject box = CreateUIObject(name, parent, anchorMin, anchorMax);
        Image bg = box.AddComponent<Image>();
        bg.color = glassBgColor;

        AddHollowBorder(box.transform, borderColor, borderThickness);
        return box;
    }

    public static void AddHollowBorder(Transform parent, Color color, float px = 1.5f)
    {
        // Top
        GameObject top = new GameObject("BorderTop", typeof(RectTransform), typeof(Image));
        top.transform.SetParent(parent, false);
        RectTransform rtT = top.GetComponent<RectTransform>();
        rtT.anchorMin = new Vector2(0f, 1f);
        rtT.anchorMax = new Vector2(1f, 1f);
        rtT.offsetMin = new Vector2(0f, -px);
        rtT.offsetMax = Vector2.zero;
        Image imgT = top.GetComponent<Image>();
        imgT.color = color;
        imgT.raycastTarget = false;

        // Bottom
        GameObject bot = new GameObject("BorderBottom", typeof(RectTransform), typeof(Image));
        bot.transform.SetParent(parent, false);
        RectTransform rtB = bot.GetComponent<RectTransform>();
        rtB.anchorMin = new Vector2(0f, 0f);
        rtB.anchorMax = new Vector2(1f, 0f);
        rtB.offsetMin = Vector2.zero;
        rtB.offsetMax = new Vector2(0f, px);
        Image imgB = bot.GetComponent<Image>();
        imgB.color = color;
        imgB.raycastTarget = false;

        // Left
        GameObject left = new GameObject("BorderLeft", typeof(RectTransform), typeof(Image));
        left.transform.SetParent(parent, false);
        RectTransform rtL = left.GetComponent<RectTransform>();
        rtL.anchorMin = new Vector2(0f, 0f);
        rtL.anchorMax = new Vector2(0f, 1f);
        rtL.offsetMin = new Vector2(0f, px);
        rtL.offsetMax = new Vector2(px, -px);
        Image imgL = left.GetComponent<Image>();
        imgL.color = color;
        imgL.raycastTarget = false;

        // Right
        GameObject right = new GameObject("BorderRight", typeof(RectTransform), typeof(Image));
        right.transform.SetParent(parent, false);
        RectTransform rtR = right.GetComponent<RectTransform>();
        rtR.anchorMin = new Vector2(1f, 0f);
        rtR.anchorMax = new Vector2(1f, 1f);
        rtR.offsetMin = new Vector2(-px, px);
        rtR.offsetMax = new Vector2(0f, -px);
        Image imgR = right.GetComponent<Image>();
        imgR.color = color;
        imgR.raycastTarget = false;
    }

    private Button CreateGlassButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action, Color glassColor, Color borderColor, float fontSize)
    {
        GameObject btnObj = CreateUIObject(name, parent, anchorMin, anchorMax);
        Image bg = btnObj.AddComponent<Image>();
        bg.color = glassColor;

        AddHollowBorder(btnObj.transform, borderColor, 1.5f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1.25f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1.0f);
        cb.selectedColor = new Color(1.15f, 1.15f, 1.15f, 1.15f);
        cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        GameObject txtObj = CreateUIObject("Label", btnObj.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
        TextMeshProUGUI tmp = CreateCrispText(txtObj, label, fontSize, Color.white, TextAlignmentOptions.Center, true);
        tmp.raycastTarget = false;

        return btn;
    }

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

    private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
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
