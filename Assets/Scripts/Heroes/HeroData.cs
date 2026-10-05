using UnityEngine;

/// <summary>
/// Modelo ScriptableObject para os heróis do jogo (inspirado no sistema de estrelas e classes de Pick Me Up!).
/// Guarda as características base, classe, estrelas, atributos e sprites de cada aventureiro.
/// </summary>
[CreateAssetMenu(fileName = "NovoHeroi", menuName = "Dungeon Master RPG/Novo Heroi")]
public class HeroData : ScriptableObject
{
    [Header("Identidade do Herói")]
    [Tooltip("Nome do aventureiro")]
    public string heroName = "Novo Heroi";

    [TextArea(2, 4)]
    [Tooltip("História ou biografia do herói")]
    public string biography = "Um aventureiro convocado para explorar a dungeon.";

    [Tooltip("Classe do herói")]
    public HeroClassType heroClass = HeroClassType.Knight;

    [Range(1, 5)]
    [Tooltip("Classificação em Estrelas (1★ a 5★ - Define o nível máximo do herói)")]
    public int stars = 1;

    [Tooltip("Herói de Estrela Dourada (Heróis especiais/únicos com atributos superiores e mais skills - Estilo Han Yslat/Loki)")]
    public bool isGoldStar = false;

    /// <summary>
    /// Calcula o nível máximo baseado nas estrelas:
    /// 1★ = Nível 20 | 2★ = Nível 40 | 3★ = Nível 60 | 4★ = Nível 80 | 5★ = Nível 100
    /// </summary>
    public static int GetMaxLevelForStars(int starRank)
    {
        return Mathf.Clamp(starRank, 1, 5) * 20;
    }

    [Header("Visual")]
    [Tooltip("Retrato / Avatar do personagem para a interface")]
    public Sprite portrait;

    [Tooltip("Sprite do herói em batalha (visão lateral estilo Final Fantasy)")]
    public Sprite battleSprite;

    [Header("Atributos Base (Nível 1)")]
    public HeroAttributes baseAttributes = new HeroAttributes(10, 10, 10, 10);

    [Header("Crescimento por Nível")]
    [Tooltip("Pontos de atributos ganhos automaticamente a cada subida de nível")]
    public HeroAttributes growthPerLevel = new HeroAttributes(2, 2, 1, 2);

    #region Fórmulas de Atributos Derivados (Estilo RPG Clássico & Souls)

    /// <summary>
    /// Vida Máxima (HP): Baseada principalmente na Vitalidade.
    /// </summary>
    public int CalculateMaxHP(int level, HeroAttributes currentAttrs)
    {
        return currentAttrs.vitality * 12 + (level * 15);
    }

    /// <summary>
    /// Mana Máxima (MP): Baseada na Inteligência (usada para magias e habilidades).
    /// </summary>
    public int CalculateMaxMP(int level, HeroAttributes currentAttrs)
    {
        return currentAttrs.intelligence * 8 + (level * 5);
    }

    /// <summary>
    /// Poder de Ataque Físico: Influenciado fortemente pela Força e um pouco pela Destreza.
    /// </summary>
    public int CalculatePhysicalAttack(HeroAttributes currentAttrs)
    {
        return Mathf.RoundToInt(currentAttrs.strength * 2.2f + currentAttrs.dexterity * 0.8f);
    }

    /// <summary>
    /// Defesa Física: Reduz o dano físico sofrido. Baseada na Força e Vitalidade.
    /// </summary>
    public int CalculatePhysicalDefense(HeroAttributes currentAttrs)
    {
        return Mathf.RoundToInt(currentAttrs.vitality * 1.5f + currentAttrs.strength * 0.8f);
    }

    /// <summary>
    /// Poder Mágico: Escala diretamente com a Inteligência.
    /// </summary>
    public int CalculateMagicAttack(HeroAttributes currentAttrs)
    {
        return Mathf.RoundToInt(currentAttrs.intelligence * 2.8f);
    }

    /// <summary>
    /// Velocidade / Iniciativa: Define a ordem de agir no combate (estilo Final Fantasy).
    /// </summary>
    public int CalculateSpeed(HeroAttributes currentAttrs)
    {
        return Mathf.RoundToInt(currentAttrs.dexterity * 1.6f);
    }

    /// <summary>
    /// Chance de Golpe Crítico (%): Baseada na Destreza.
    /// </summary>
    public float CalculateCritRate(HeroAttributes currentAttrs)
    {
        return Mathf.Clamp(currentAttrs.dexterity * 0.5f, 5f, 60f);
    }

    #endregion
}
