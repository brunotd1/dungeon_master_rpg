using UnityEngine;

/// <summary>
/// Representa uma habilidade ou ataque que um herói pode executar em combate.
/// Suporta ataques físicos, feitiços mágicos, custos de MP, multiplicadores de dano e bônus de crítico.
/// </summary>
[System.Serializable]
public class HeroSkill
{
    [Tooltip("Nome do golpe ou habilidade")]
    public string skillName;

    [Tooltip("Descrição dos efeitos da habilidade")]
    public string description;

    [Tooltip("Custo de Mana (MP) para conjurar")]
    public int mpCost = 0;

    [Tooltip("Multiplicador sobre o ataque base do herói (1.0 = 100% de dano)")]
    public float damageMultiplier = 1.0f;

    [Tooltip("Se verdadeiro, escala com Ataque Mágico (INT). Se falso, escala com Ataque Físico (STR/DEX).")]
    public bool isMagic = false;

    [Tooltip("Chance adicional de acerto crítico")]
    public float critBonus = 0f;

    public HeroSkill(string name, string desc, int cost, float multiplier, bool magic = false, float crit = 0f)
    {
        skillName = name;
        description = desc;
        mpCost = cost;
        damageMultiplier = multiplier;
        isMagic = magic;
        critBonus = crit;
    }
}
