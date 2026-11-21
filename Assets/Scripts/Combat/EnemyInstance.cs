using UnityEngine;

/// <summary>
/// Instância viva de um monstro durante uma batalha da masmorra.
/// Escala HP, Ataque, Defesa, Velocidade e recompensas pelo nível do monstro e do andar,
/// suporta lacaios invocados pelo Boss Necromante, regeneração de HP e mitigação híbrida de dano.
/// </summary>
[System.Serializable]
public class EnemyInstance
{
    public EnemyData data;
    public string enemyName;
    public int level = 1;
    public bool isSummonedMinion = false;
    public int currentHP;
    public int maxHP;
    public int attack;
    public int defense;
    public int speed;
    public int xpReward;
    public int goldReward;

    public bool IsAlive => currentHP > 0;

    public EnemyInstance(EnemyData enemyData, int enemyLevel = 1, bool summonedMinion = false)
    {
        data = enemyData;
        enemyName = enemyData != null ? enemyData.enemyName : "Monstro";
        level = Mathf.Max(1, enemyLevel);
        isSummonedMinion = summonedMinion;

        bool isBoss = enemyData != null && enemyData.isBoss;
        if (isBoss)
        {
            maxHP = 380 + (level * 38);              // Boss Nv. 7 = 646 HP
            attack = 42 + (level * 7);               // Boss Nv. 7 = 91 ATQ
            defense = 14 + (level * 3);              // Boss Nv. 7 = 35 DEF
            speed = 9 + (level / 2);
            xpReward = 440 + (level * 60);           // Boss Nv. 7 = 860 XP total
            goldReward = 90 + (level * 15);          // Boss Nv. 7 = 195 Ouro
        }
        else if (isSummonedMinion)
        {
            maxHP = 55 + (level * 15);               // Nv. 5 = 130 HP
            attack = 26 + (level * 6);               // Nv. 5 = 56 ATQ
            defense = 8 + (level * 2);               // Nv. 5 = 18 DEF
            speed = 7 + (level / 2);
            xpReward = 20 + (level * 5);
            goldReward = 5 + (level * 2);
        }
        else
        {
            bool isTanky = (!string.IsNullOrEmpty(enemyName) && enemyName.ToLower().Contains("esqueleto"))
                           || (enemyData != null && enemyData.maxHP >= 48 && !enemyName.ToLower().Contains("goblin"));

            if (isTanky)
            {
                maxHP = 82 + (level * 22);           // Nv.1: 104 | Nv.3: 148 | Nv.5: 192
                attack = 35 + (level * 7);           // Nv.1: 42  | Nv.3: 56  | Nv.5: 70
                defense = 10 + (level * 3);          // Nv.1: 13  | Nv.3: 19  | Nv.5: 25
                speed = 6 + (level / 2);
                xpReward = 80 + (level * 26);        // Nv.1: 106 | Nv.3: 158 | Nv.5: 210
                goldReward = 8 + (level * 4);
            }
            else
            {
                maxHP = 65 + (level * 18);           // Nv.1: 83  | Nv.3: 119 | Nv.5: 155
                attack = 30 + (level * 6);           // Nv.1: 36  | Nv.3: 48  | Nv.5: 60
                defense = 7 + (level * 2);           // Nv.1: 9   | Nv.3: 13  | Nv.5: 17
                speed = 9 + (level / 2);
                xpReward = 65 + (level * 22);        // Nv.1: 87  | Nv.3: 131 | Nv.5: 175
                goldReward = 6 + (level * 3);
            }
        }

        currentHP = maxHP;
    }

    /// <summary>
    /// Regenera HP do monstro até o limite máximo e retorna quanto foi curado.
    /// </summary>
    public int Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return 0;
        int prevHP = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        return currentHP - prevHP;
    }

    /// <summary>
    /// Aplica dano ao monstro usando mitigação híbrida (parcela plana + redução percentual).
    /// </summary>
    public int TakeDamage(int rawDamage, bool isMagic = false)
    {
        int effectiveDef = isMagic ? Mathf.RoundToInt(defense * 0.65f) : defense;
        int flatReduction = Mathf.RoundToInt(effectiveDef * 0.40f);
        float pctMultiplier = 100f / (100f + effectiveDef * 1.10f);
        int reduced = Mathf.RoundToInt(Mathf.Max(rawDamage * 0.25f, rawDamage - flatReduction) * pctMultiplier);
        int finalDamage = Mathf.Max(2, reduced);

        currentHP -= finalDamage;
        if (currentHP < 0) currentHP = 0;

        Debug.Log($"💥 {enemyName} (Nv.{level}) sofreu {finalDamage} de dano {(isMagic ? "mágico" : "físico")}! (HP restante: {currentHP}/{maxHP})");
        return finalDamage;
    }

    /// <summary>
    /// Sorteia se o monstro derrubou algum item após morrer (quando consultado individualmente).
    /// </summary>
    public bool CheckLootDrop(out ItemData item)
    {
        item = null;
        float chance = (data != null && data.dropChance > 0f) ? data.dropChance : 0.20f;
        if (Random.value <= chance)
        {
            if (data != null && data.possibleDrop != null && data.possibleDrop.GetIcon() != null)
            {
                item = data.possibleDrop;
            }
            else
            {
                int floor = DungeonManager.Instance != null ? DungeonManager.Instance.currentFloor : 1;
                bool isBoss = data != null && data.isBoss;
                item = ItemGenerator.GenerateRandomDungeonDrop(floor, level, isTreasureChest: false, isBossDrop: isBoss);
            }
            return item != null;
        }
        return false;
    }
}
