using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Gerenciador visual completo da interface de batalha estilo Final Fantasy.
/// Constrói dinamicamente os painéis dos 4 heróis (lado direito), dos monstros (lado esquerdo),
/// as barras de HP/MP, menus de ação e a tela de vitória com espólios.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    public static BattleHUD Instance { get; private set; }

    [Header("Elementos de Interface")]
    public Canvas battleCanvas;
    public GameObject battleRootPanel;

    [Header("Painéis de Posicionamento")]
    public RectTransform heroesContainer;  // Lado direito (os 4 heróis)
    public RectTransform enemiesContainer; // Lado esquerdo (os monstros)
    public RectTransform actionMenuPanel;  // Barra inferior de comandos

    [Header("Textos Principais")]
    public TextMeshProUGUI turnBannerText;
    public TextMeshProUGUI combatLogText;

    [Header("Botões de Ação")]
    public Button attackBtn;
    public Button defendBtn;

    [Header("Seleção Direta de Alvo")]
    public GameObject targetSelectionBar;
    private List<Button> dynamicTargetButtons = new List<Button>();

    [Header("Painel de Vitória / Espólios")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victorySummaryText;
    public Button continueExplorationBtn;

    // Listas internas de elementos visuais criados
    private List<HeroDisplayUI> heroDisplays = new List<HeroDisplayUI>();
    private List<EnemyDisplayUI> enemyDisplays = new List<EnemyDisplayUI>();

    private bool isTargetingEnemy = false;

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
    /// Garante que exista um EventSystem com suporte ao novo Input System na cena, permitindo cliques do mouse na UI.
    /// </summary>
    public void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            var module = esObj.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            Debug.Log("[BattleHUD] EventSystem e InputSystemUIInputModule criados para capturar cliques do mouse!");
        }
    }

    /// <summary>
    /// Constrói a estrutura visual na tela caso ainda não exista no Canvas.
    /// </summary>
    public void BuildHUDStructureIfNotExisting()
    {
        EnsureEventSystem();

        if (battleCanvas == null)
        {
            battleCanvas = FindFirstObjectByType<Canvas>();
            if (battleCanvas == null)
            {
                GameObject canvasObj = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                battleCanvas = canvasObj.GetComponent<Canvas>();
                battleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        // Garante componentes essenciais de renderização e raycasting no Canvas
        if (battleCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            battleCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler cs = battleCanvas.GetComponent<CanvasScaler>();
        if (cs == null) cs = battleCanvas.gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        if (battleRootPanel == null)
        {
            // Painel de Fundo da Arena
            battleRootPanel = new GameObject("BattleRootPanel", typeof(RectTransform), typeof(Image));
            battleRootPanel.transform.SetParent(battleCanvas.transform, false);
            RectTransform rootRT = battleRootPanel.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.sizeDelta = Vector2.zero;
            Image bgImg = battleRootPanel.GetComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f); // Azul escuro medieval

            // 1. Container dos Heróis (Lado Direito)
            GameObject heroesObj = new GameObject("HeroesPartyPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            heroesObj.transform.SetParent(battleRootPanel.transform, false);
            heroesContainer = heroesObj.GetComponent<RectTransform>();
            heroesContainer.anchorMin = new Vector2(0.65f, 0.25f);
            heroesContainer.anchorMax = new Vector2(0.95f, 0.85f);
            heroesContainer.offsetMin = Vector2.zero;
            heroesContainer.offsetMax = Vector2.zero;
            VerticalLayoutGroup vlgH = heroesObj.GetComponent<VerticalLayoutGroup>();
            vlgH.spacing = 15;
            vlgH.childControlHeight = false; // Preserva a altura de 110px de cada cartão!
            vlgH.childControlWidth = true;
            vlgH.childForceExpandHeight = false;

            // 2. Container dos Inimigos (Lado Esquerdo)
            GameObject enemiesObj = new GameObject("EnemiesPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            enemiesObj.transform.SetParent(battleRootPanel.transform, false);
            enemiesContainer = enemiesObj.GetComponent<RectTransform>();
            enemiesContainer.anchorMin = new Vector2(0.05f, 0.25f);
            enemiesContainer.anchorMax = new Vector2(0.40f, 0.85f);
            enemiesContainer.offsetMin = Vector2.zero;
            enemiesContainer.offsetMax = Vector2.zero;
            VerticalLayoutGroup vlgE = enemiesObj.GetComponent<VerticalLayoutGroup>();
            vlgE.spacing = 20;
            vlgE.childControlHeight = false; // Preserva a altura de 110px de cada cartão!
            vlgE.childControlWidth = true;
            vlgE.childForceExpandHeight = false;

            // 3. Barra Inferior de Comandos
            GameObject actionObj = new GameObject("ActionBarPanel", typeof(RectTransform), typeof(Image));
            actionObj.transform.SetParent(battleRootPanel.transform, false);
            actionMenuPanel = actionObj.GetComponent<RectTransform>();
            actionMenuPanel.anchorMin = new Vector2(0.05f, 0.04f);
            actionMenuPanel.anchorMax = new Vector2(0.95f, 0.20f);
            actionMenuPanel.offsetMin = Vector2.zero;
            actionMenuPanel.offsetMax = Vector2.zero;
            actionObj.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.20f, 0.90f);

            // Banner de Turno no Topo
            GameObject bannerObj = new GameObject("TurnBanner", typeof(RectTransform), typeof(TextMeshProUGUI));
            bannerObj.transform.SetParent(battleRootPanel.transform, false);
            RectTransform bannerRT = bannerObj.GetComponent<RectTransform>();
            bannerRT.anchorMin = new Vector2(0.2f, 0.88f);
            bannerRT.anchorMax = new Vector2(0.8f, 0.96f);
            bannerRT.offsetMin = Vector2.zero;
            bannerRT.offsetMax = Vector2.zero;
            turnBannerText = bannerObj.GetComponent<TextMeshProUGUI>();
            turnBannerText.fontSize = 28;
            turnBannerText.alignment = TextAlignmentOptions.Center;
            turnBannerText.color = new Color(1f, 0.85f, 0.4f); // Dourado
            turnBannerText.text = "=== ARENA DE BATALHA ===";

            // Log de Ação (dentro da barra de ação)
            GameObject logObj = new GameObject("CombatLog", typeof(RectTransform), typeof(TextMeshProUGUI));
            logObj.transform.SetParent(actionMenuPanel, false);
            RectTransform logRT = logObj.GetComponent<RectTransform>();
            logRT.anchorMin = new Vector2(0.02f, 0.1f);
            logRT.anchorMax = new Vector2(0.55f, 0.9f);
            logRT.offsetMin = Vector2.zero;
            logRT.offsetMax = Vector2.zero;
            combatLogText = logObj.GetComponent<TextMeshProUGUI>();
            combatLogText.fontSize = 18;
            combatLogText.alignment = TextAlignmentOptions.Left;
            combatLogText.color = Color.white;
            combatLogText.text = "A batalha começou! Escolha sua ação.";

            // Botão [ATACAR]
            attackBtn = CreateButton(actionMenuPanel, "AttackButton", "[ ATACAR ]", new Vector2(0.60f, 0.15f), new Vector2(0.78f, 0.85f), new Color(0.65f, 0.15f, 0.15f));
            attackBtn.onClick.AddListener(OnClickAttack);

            // Botão [DEFENDER]
            defendBtn = CreateButton(actionMenuPanel, "DefendButton", "[ DEFENDER ]", new Vector2(0.81f, 0.15f), new Vector2(0.98f, 0.85f), new Color(0.20f, 0.40f, 0.65f));
            defendBtn.onClick.AddListener(OnClickDefend);

            // Barra de Seleção Direta de Alvo (inicialmente desativada)
            GameObject targetBarObj = new GameObject("TargetSelectionBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            targetBarObj.transform.SetParent(actionMenuPanel, false);
            RectTransform targetRT = targetBarObj.GetComponent<RectTransform>();
            targetRT.anchorMin = new Vector2(0.55f, 0.15f);
            targetRT.anchorMax = new Vector2(0.98f, 0.85f);
            targetRT.offsetMin = Vector2.zero;
            targetRT.offsetMax = Vector2.zero;
            HorizontalLayoutGroup hlg = targetBarObj.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            targetSelectionBar = targetBarObj;
            targetSelectionBar.SetActive(false);

            // 4. Painel de Vitória
            CreateVictoryPanel();
        }
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor * 1.3f;
        cb.pressedColor = bgColor * 0.7f;
        cb.selectedColor = bgColor * 1.2f;
        btn.colors = cb;

        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRT = txtObj.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false; // Não bloqueia o clique no botão!

        return btn;
    }

    private void CreateVictoryPanel()
    {
        GameObject vObj = new GameObject("VictoryPanel", typeof(RectTransform), typeof(Image));
        vObj.transform.SetParent(battleRootPanel.transform, false);
        RectTransform vRT = vObj.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0.25f, 0.25f);
        vRT.anchorMax = new Vector2(0.75f, 0.75f);
        vRT.offsetMin = Vector2.zero;
        vRT.offsetMax = Vector2.zero;
        vObj.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 0.98f);
        victoryPanel = vObj;

        GameObject titleObj = new GameObject("VictoryTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(vObj.transform, false);
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.1f, 0.75f);
        titleRT.anchorMax = new Vector2(0.9f, 0.95f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "=== VITORIA NA MASMORRA! ===";
        titleTmp.fontSize = 28;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(1f, 0.85f, 0.2f);

        GameObject summaryObj = new GameObject("VictorySummary", typeof(RectTransform), typeof(TextMeshProUGUI));
        summaryObj.transform.SetParent(vObj.transform, false);
        RectTransform sumRT = summaryObj.GetComponent<RectTransform>();
        sumRT.anchorMin = new Vector2(0.1f, 0.30f);
        sumRT.anchorMax = new Vector2(0.9f, 0.70f);
        sumRT.offsetMin = Vector2.zero;
        sumRT.offsetMax = Vector2.zero;
        victorySummaryText = summaryObj.GetComponent<TextMeshProUGUI>();
        victorySummaryText.fontSize = 20;
        victorySummaryText.alignment = TextAlignmentOptions.Center;
        victorySummaryText.color = Color.white;

        continueExplorationBtn = CreateButton(vObj.transform, "ContinueBtn", "CONTINUAR EXPLORACAO >>", new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.24f), new Color(0.15f, 0.55f, 0.25f));
        continueExplorationBtn.onClick.AddListener(OnClickContinue);

        victoryPanel.SetActive(false);
    }

    /// <summary>
    /// Inicializa a arena visual ao começar a batalha.
    /// </summary>
    public void RefreshBattleArena(List<HeroInstance> heroes, List<EnemyInstance> enemies)
    {
        BuildHUDStructureIfNotExisting();
        victoryPanel.SetActive(false);
        isTargetingEnemy = false;

        // Limpa heróis anteriores
        foreach (Transform child in heroesContainer) Destroy(child.gameObject);
        heroDisplays.Clear();

        // Limpa inimigos anteriores
        foreach (Transform child in enemiesContainer) Destroy(child.gameObject);
        enemyDisplays.Clear();

        // 1. Cria os 4 Heróis na Direita
        for (int i = 0; i < heroes.Count; i++)
        {
            HeroDisplayUI hUI = CreateHeroCard(heroes[i], i);
            heroDisplays.Add(hUI);
        }

        // 2. Cria os Monstros na Esquerda
        for (int i = 0; i < enemies.Count; i++)
        {
            int enemyIndex = i;
            EnemyDisplayUI eUI = CreateEnemyCard(enemies[i], enemyIndex);
            enemyDisplays.Add(eUI);
        }

        UpdateAllStats();
    }

    private HeroDisplayUI CreateHeroCard(HeroInstance hero, int index)
    {
        GameObject cardObj = new GameObject($"HeroCard_{hero.heroName}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        cardObj.transform.SetParent(heroesContainer, false);
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 110);
        LayoutElement le = cardObj.GetComponent<LayoutElement>();
        le.preferredHeight = 110;
        le.minHeight = 110;
        Image bg = cardObj.GetComponent<Image>();
        bg.color = new Color(0.14f, 0.17f, 0.24f, 0.95f);

        // Nome e Classe
        GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(cardObj.transform, false);
        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.05f, 0.60f);
        nameRT.anchorMax = new Vector2(0.95f, 0.95f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;
        TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        string starBadge = hero.isGoldStar ? $"<color=#FFD700>[Ouro {hero.currentStars}*]</color>" : $"<color=#E0E0E0>[{hero.currentStars}*]</color>";
        nameTmp.text = $"{starBadge} {hero.heroName} <size=14><color=#90CAF9>[{hero.heroClass}]</color></size>";
        nameTmp.fontSize = 18;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white;
        nameTmp.raycastTarget = false;

        // HP Text
        GameObject hpObj = new GameObject("HPText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hpObj.transform.SetParent(cardObj.transform, false);
        RectTransform hpRT = hpObj.GetComponent<RectTransform>();
        hpRT.anchorMin = new Vector2(0.05f, 0.30f);
        hpRT.anchorMax = new Vector2(0.95f, 0.60f);
        hpRT.offsetMin = Vector2.zero;
        hpRT.offsetMax = Vector2.zero;
        TextMeshProUGUI hpTmp = hpObj.GetComponent<TextMeshProUGUI>();
        hpTmp.fontSize = 16;
        hpTmp.color = new Color(0.4f, 0.95f, 0.4f);
        hpTmp.raycastTarget = false;

        // MP Text
        GameObject mpObj = new GameObject("MPText", typeof(RectTransform), typeof(TextMeshProUGUI));
        mpObj.transform.SetParent(cardObj.transform, false);
        RectTransform mpRT = mpObj.GetComponent<RectTransform>();
        mpRT.anchorMin = new Vector2(0.05f, 0.05f);
        mpRT.anchorMax = new Vector2(0.95f, 0.30f);
        mpRT.offsetMin = Vector2.zero;
        mpRT.offsetMax = Vector2.zero;
        TextMeshProUGUI mpTmp = mpObj.GetComponent<TextMeshProUGUI>();
        mpTmp.fontSize = 15;
        mpTmp.color = new Color(0.4f, 0.8f, 1f);
        mpTmp.raycastTarget = false;

        return new HeroDisplayUI { hero = hero, cardImage = bg, hpText = hpTmp, mpText = mpTmp };
    }

    private EnemyDisplayUI CreateEnemyCard(EnemyInstance enemy, int index)
    {
        GameObject cardObj = new GameObject($"EnemyCard_{enemy.enemyName}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardObj.transform.SetParent(enemiesContainer, false);
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 110);
        LayoutElement le = cardObj.GetComponent<LayoutElement>();
        le.preferredHeight = 110;
        le.minHeight = 110;
        Image bg = cardObj.GetComponent<Image>();
        bg.color = new Color(0.38f, 0.14f, 0.16f, 0.95f);

        Button btn = cardObj.GetComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.38f, 0.14f, 0.16f, 0.95f);
        cb.highlightedColor = new Color(0.65f, 0.20f, 0.25f, 1f);
        cb.pressedColor = new Color(0.85f, 0.25f, 0.30f, 1f);
        btn.colors = cb;

        // Nome
        GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(cardObj.transform, false);
        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.05f, 0.50f);
        nameRT.anchorMax = new Vector2(0.95f, 0.95f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;
        TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        nameTmp.text = enemy.data.isBoss ? $"<color=#FF5555>[CHEFE]</color> {enemy.enemyName}" : $"<color=#FFAAAA>[MONSTRO]</color> {enemy.enemyName}";
        nameTmp.fontSize = 18;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white;
        nameTmp.raycastTarget = false;

        // HP Text
        GameObject hpObj = new GameObject("HPText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hpObj.transform.SetParent(cardObj.transform, false);
        RectTransform hpRT = hpObj.GetComponent<RectTransform>();
        hpRT.anchorMin = new Vector2(0.05f, 0.10f);
        hpRT.anchorMax = new Vector2(0.95f, 0.50f);
        hpRT.offsetMin = Vector2.zero;
        hpRT.offsetMax = Vector2.zero;
        TextMeshProUGUI hpTmp = hpObj.GetComponent<TextMeshProUGUI>();
        hpTmp.fontSize = 16;
        hpTmp.color = new Color(1f, 0.45f, 0.45f);
        hpTmp.raycastTarget = false;

        // Clique para atacar diretamente este monstro no campo
        int targetIdx = index;
        btn.onClick.AddListener(() => {
            if (BattleManager.Instance.currentState == BattleState.HeroTurn)
            {
                ExecuteAttackOn(targetIdx);
            }
        });

        return new EnemyDisplayUI { enemy = enemy, cardImage = bg, hpText = hpTmp };
    }

    /// <summary>
    /// Atualiza as vidas e manas de todos na tela.
    /// </summary>
    public void UpdateAllStats()
    {
        foreach (var h in heroDisplays)
        {
            if (h.hpText != null)
                h.hpText.text = $"HP: {h.hero.currentHP} / {h.hero.GetMaxHP()}";
            if (h.mpText != null)
                h.mpText.text = $"MP: {h.hero.currentMP} / {h.hero.GetMaxMP()}";

            if (!h.hero.IsAlive)
            {
                h.cardImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                h.hpText.text = "<color=red>[CAÍDO EM COMBATE]</color>";
            }
        }

        foreach (var e in enemyDisplays)
        {
            if (e.hpText != null)
                e.hpText.text = $"HP: {e.enemy.currentHP} / {e.enemy.maxHP}";

            if (!e.enemy.IsAlive)
            {
                e.cardImage.color = new Color(0.15f, 0.15f, 0.15f, 0.4f);
                e.hpText.text = "<color=#888888>[DERROTADO]</color>";
            }
        }
    }

    /// <summary>
    /// Destaca o herói que está agindo nesta rodada.
    /// </summary>
    public void SetHeroTurnUI(HeroInstance activeHero)
    {
        UpdateAllStats();

        if (turnBannerText != null)
            turnBannerText.text = $"TURNO DE: {activeHero.heroName.ToUpper()}";

        if (combatLogText != null)
            combatLogText.text = $"É o turno de <b>{activeHero.heroName}</b>! Escolha [ATACAR] ou [DEFENDER].";

        HideTargetSelection();

        // Destaca a moldura do herói ativo
        for (int i = 0; i < heroDisplays.Count; i++)
        {
            if (heroDisplays[i].hero == activeHero)
                heroDisplays[i].cardImage.color = new Color(0.25f, 0.45f, 0.70f, 1f); // Azul brilhante
            else if (heroDisplays[i].hero.IsAlive)
                heroDisplays[i].cardImage.color = new Color(0.14f, 0.17f, 0.24f, 0.95f);
        }

        attackBtn.interactable = true;
        defendBtn.interactable = true;
    }

    public void OnClickAttack()
    {
        int firstAlive = -1;
        int aliveCount = 0;
        for (int i = 0; i < enemyDisplays.Count; i++)
        {
            if (enemyDisplays[i].enemy != null && enemyDisplays[i].enemy.IsAlive)
            {
                if (firstAlive == -1) firstAlive = i;
                aliveCount++;
            }
        }

        // Se houver apenas 1 monstro vivo, ataca direto com um único clique!
        if (aliveCount == 1 && firstAlive != -1)
        {
            ExecuteAttackOn(firstAlive);
            return;
        }

        // Se houver mais de um, exibe os botões de seleção de alvo na barra e destaca os cartões
        ShowTargetSelection();
    }

    public void ShowTargetSelection()
    {
        isTargetingEnemy = true;
        combatLogText.text = ">> <b>Escolha qual monstro atacar:</b>";

        // Esconde botões de ação temporariamente
        attackBtn.gameObject.SetActive(false);
        defendBtn.gameObject.SetActive(false);

        // Limpa botões antigos de alvo
        foreach (var b in dynamicTargetButtons)
        {
            if (b != null) Destroy(b.gameObject);
        }
        dynamicTargetButtons.Clear();

        if (targetSelectionBar != null)
        {
            targetSelectionBar.SetActive(true);

            for (int i = 0; i < enemyDisplays.Count; i++)
            {
                int monsterIdx = i;
                var enemy = enemyDisplays[i].enemy;
                if (enemy != null && enemy.IsAlive)
                {
                    Button btn = CreateButton(targetSelectionBar.transform, $"TargetBtn_{i}", $"[ {enemy.enemyName} ]", Vector2.zero, Vector2.one, new Color(0.70f, 0.18f, 0.22f));
                    btn.onClick.AddListener(() => {
                        ExecuteAttackOn(monsterIdx);
                    });
                    dynamicTargetButtons.Add(btn);

                    // Destaque visual no campo
                    enemyDisplays[i].cardImage.color = new Color(0.70f, 0.20f, 0.25f, 1f);
                }
            }

            // Botão [CANCELAR]
            Button cancelBtn = CreateButton(targetSelectionBar.transform, "CancelTargetBtn", "[ CANCELAR ]", Vector2.zero, Vector2.one, new Color(0.35f, 0.35f, 0.40f));
            cancelBtn.onClick.AddListener(HideTargetSelection);
            dynamicTargetButtons.Add(cancelBtn);
        }
    }

    public void HideTargetSelection()
    {
        isTargetingEnemy = false;
        if (targetSelectionBar != null)
            targetSelectionBar.SetActive(false);

        if (attackBtn != null) attackBtn.gameObject.SetActive(true);
        if (defendBtn != null) defendBtn.gameObject.SetActive(true);

        // Restaura cores originais dos monstros vivos
        for (int i = 0; i < enemyDisplays.Count; i++)
        {
            if (enemyDisplays[i].enemy != null && enemyDisplays[i].enemy.IsAlive)
            {
                enemyDisplays[i].cardImage.color = new Color(0.38f, 0.14f, 0.16f, 0.95f);
            }
        }
    }

    public void ExecuteAttackOn(int enemyIndex)
    {
        HideTargetSelection();
        BattleManager.Instance.PlayerAttack(enemyIndex);
    }

    public void OnClickDefend()
    {
        HideTargetSelection();
        BattleManager.Instance.PlayerDefend();
    }

    public void ShowVictory(int xp, int gold, List<ItemData> drops)
    {
        UpdateAllStats();
        victoryPanel.SetActive(true);

        string dropsText = "";
        if (drops != null && drops.Count > 0)
        {
            dropsText = "\n<b>Drops Obtidos:</b> ";
            foreach (var d in drops) dropsText += $"[{d.itemName}] ";
        }

        victorySummaryText.text = $"<b>Recompensas da Batalha:</b>\n\n+{xp} XP para cada herói vivo\n+{gold} Moedas de Ouro{dropsText}";
    }

    public void OnClickContinue()
    {
        victoryPanel.SetActive(false);
        battleRootPanel.SetActive(false); // Oculta a arena e volta para a exploração de portas da dungeon!
    }

    private class HeroDisplayUI
    {
        public HeroInstance hero;
        public Image cardImage;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI mpText;
    }

    private class EnemyDisplayUI
    {
        public EnemyInstance enemy;
        public Image cardImage;
        public TextMeshProUGUI hpText;
    }
}
