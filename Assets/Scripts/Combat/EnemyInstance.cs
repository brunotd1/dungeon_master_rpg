using UnityEngine;

/// <summary>
/// Instância viva de um monstro durante uma batalha da masmorra.
/// Controla o HP atual, dano sofrido e o sorteio de itens derrubados (drops) ao morrer.
/// </summary>
[System.Serializable]
public class EnemyInstance
{
    public EnemyData data;
    public string enemyName;
    public int currentHP;
    public int maxHP;

    public bool IsAlive => currentHP > 0;

    public EnemyInstance(EnemyData enemyData)
    {
        data = enemyData;
        enemyName = enemyData.enemyName;
        maxHP = enemyData.maxHP;
        currentHP = maxHP;
    }

    /// <summary>
    /// Aplica dano ao monstro considerando sua defesa (física ou mágica).
    /// </summary>
    public int TakeDamage(int rawDamage, bool isMagic = false)
    {
        int effectiveDef = isMagic ? Mathf.RoundToInt(data.defense * 0.7f) : data.defense;
        int finalDamage = Mathf.Max(1, rawDamage - effectiveDef);
        currentHP -= finalDamage;
        if (currentHP < 0) currentHP = 0;

        Debug.Log($"💥 {enemyName} sofreu {finalDamage} de dano {(isMagic ? "mágico" : "físico")}! (HP restante: {currentHP}/{maxHP})");
        return finalDamage;
    }

    /// <summary>
    /// Sorteia se o monstro derrubou algum item raro após morrer.
    /// </summary>
    public bool CheckLootDrop(out ItemData item)
    {
        item = null;
        if (data.possibleDrop != null && Random.value <= data.dropChance)
        {
            item = data.possibleDrop;
            return true;
        }
        return false;
    }
}
