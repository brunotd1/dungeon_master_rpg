using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla a interface gráfica da batalha estilo Final Fantasy.
/// Exibe os 4 heróis e monstros lado a lado, barras de HP/MP,
/// menu de comandos (Atacar, Habilidade, Defender) e mensagens de ação em tempo real.
/// </summary>
public class BattleUI : MonoBehaviour
{
    public static BattleUI Instance { get; private set; }

    [Header("Painel Principal de Combate")]
    public GameObject battlePanel;

    [Header("Textos de Turno e Narração")]
    public TextMeshProUGUI turnAnnouncementText;
    public TextMeshProUGUI actionLogText;

    [Header("Menu de Comandos do Herói")]
    public GameObject actionCommandPanel;
    public Button attackButton;
    public Button defendButton;

    [Header("Seleção de Alvo (Monstros)")]
    public GameObject targetSelectPanel;
    public Transform targetButtonContainer;
    public GameObject targetButtonPrefab; // Botão para clicar no monstro que quer atacar

    [Header("Status dos 4 Heróis na Tela")]
    public Transform heroPartyContainer;
    // Cada herói tem seu display com Nome, HP e MP

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
    /// Chamado pelo BattleManager para atualizar a interface no início do turno do herói.
    /// </summary>
    public void ShowHeroTurn(HeroInstance hero)
    {
        if (turnAnnouncementText != null)
            turnAnnouncementText.text = $"⚔️ Vez de {hero.heroName} ({hero.heroClass})";

        if (actionCommandPanel != null)
            actionCommandPanel.SetActive(true);

        if (targetSelectPanel != null)
            targetSelectPanel.SetActive(false);
    }

    /// <summary>
    /// Chamado quando o jogador clica no botão [Atacar]: abre as opções de monstros para escolher o alvo.
    /// </summary>
    public void OnClickAttack()
    {
        if (actionCommandPanel != null)
            actionCommandPanel.SetActive(false);

        if (targetSelectPanel != null)
        {
            targetSelectPanel.SetActive(true);
            PopulateTargetButtons();
        }
    }

    /// <summary>
    /// Gera botões para selecionar qual monstro vivo atacar.
    /// </summary>
    private void PopulateTargetButtons()
    {
        if (targetButtonContainer == null || BattleManager.Instance == null) return;

        // Limpa botões antigos
        foreach (Transform child in targetButtonContainer)
        {
            Destroy(child.gameObject);
        }

        var enemies = BattleManager.Instance.enemiesInBattle;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].IsAlive)
            {
                int enemyIndex = i;
                GameObject btnObj = new GameObject($"Target_{enemies[i].enemyName}", typeof(RectTransform), typeof(Button), typeof(Image));
                btnObj.transform.SetParent(targetButtonContainer, false);

                // Texto do botão
                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(btnObj.transform, false);
                TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
                tmp.text = $"{enemies[i].enemyName} (HP: {enemies[i].currentHP}/{enemies[i].maxHP})";
                tmp.fontSize = 14;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;

                // Ação ao clicar: ataca o monstro selecionado
                Button btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() => {
                    targetSelectPanel.SetActive(false);
                    BattleManager.Instance.PlayerAttack(enemyIndex);
                });
            }
        }
    }

    /// <summary>
    /// Chamado quando o jogador clica no botão [Defender].
    /// </summary>
    public void OnClickDefend()
    {
        if (actionCommandPanel != null)
            actionCommandPanel.SetActive(false);

        if (targetSelectPanel != null)
            targetSelectPanel.SetActive(false);

        BattleManager.Instance.PlayerDefend();
    }

    /// <summary>
    /// Exibe mensagem de ação no log.
    /// </summary>
    public void SetLog(string message)
    {
        if (actionLogText != null)
            actionLogText.text = message;
    }
}
