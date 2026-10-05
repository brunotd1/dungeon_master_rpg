using UnityEngine;

/// <summary>
/// Define as classes e funções disponíveis para os aventureiros da Guilda:
/// - Knight (Cavaleiro): Balanceado, bom dano e defesa média.
/// - HeavyKnight (Cavaleiro Pesado): Tanque do time, focado em alta vida, defesa e proteção.
/// - Mage (Mago): Especialista em dano mágico e controle elemental à distância (usa MP).
/// - Rogue (Ladino): Focado em velocidade, ataques críticos com adagas ou arco.
/// </summary>
public enum HeroClassType
{
    Knight,
    HeavyKnight,
    Mage,
    Rogue
}

/// <summary>
/// Atributos primários dos heróis (inspirado em Dark Souls e RPGs clássicos).
/// Cada atributo influencia diretamente as capacidades de combate e quais itens podem ser equipados.
/// </summary>
[System.Serializable]
public struct HeroAttributes
{
    [Tooltip("Força: Aumenta dano físico e permite usar armas pesadas e armaduras de placas")]
    public int strength;

    [Tooltip("Destreza: Aumenta velocidade, acerto crítico e permite usar adagas e arcos")]
    public int dexterity;

    [Tooltip("Inteligência: Aumenta dano mágico, mana máxima e permite usar cajados e feitiços")]
    public int intelligence;

    [Tooltip("Vitalidade: Aumenta a vida máxima (HP) e resistência a dano")]
    public int vitality;

    public HeroAttributes(int str, int dex, int intel, int vit)
    {
        strength = str;
        dexterity = dex;
        intelligence = intel;
        vitality = vit;
    }
}
