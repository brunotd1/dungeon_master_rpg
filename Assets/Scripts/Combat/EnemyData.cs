using UnityEngine;

/// <summary>
/// Modelo ScriptableObject para os monstros e chefes da masmorra.
/// Define os atributos de combate, recompensas de XP, moedas de ouro e chances de drop de itens.
/// </summary>
[CreateAssetMenu(fileName = "NovoInimigo", menuName = "Dungeon Master RPG/Novo Inimigo")]
public class EnemyData : ScriptableObject
{
    [Header("Identificação do Monstro")]
    public string enemyName = "Monstro da Masmorra";
    [TextArea(2, 3)]
    public string description = "Uma criatura hostil que habita os andares da torre.";
    public bool isBoss = false;

    [Header("Visual")]
    [Tooltip("Sprite do monstro em batalha (visão lateral)")]
    public Sprite battleSprite;

    [Header("Atributos de Combate")]
    [Tooltip("Vida máxima")]
    public int maxHP = 40;

    [Tooltip("Poder de ataque físico")]
    public int attack = 12;

    [Tooltip("Defesa física (reduz o dano sofrido)")]
    public int defense = 3;

    [Tooltip("Velocidade / Iniciativa (define a ordem do turno)")]
    public int speed = 8;

    [Header("Espólios e Recompensas")]
    [Tooltip("XP concedido ao grupo quando derrotado")]
    public int xpReward = 35;

    [Tooltip("Moedas de ouro concedidas")]
    public int goldReward = 20;

    [Header("Tabela de Drop (Loot)")]
    [Tooltip("Item que este monstro pode dropar")]
    public ItemData possibleDrop;

    [Range(0f, 1f)]
    [Tooltip("Chance de dropar o item (ex: 0.25 = 25% de chance)")]
    public float dropChance = 0.25f;
}
