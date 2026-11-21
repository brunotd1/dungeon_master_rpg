using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O Mestre da Masmorra (DungeonManager).
/// Controla o avanço pelos 100 andares da torre, rastreamento de andares liberados (maxUnlockedFloor),
/// geração de portas e salas, aparição do Boss do andar e salvamento automático de progresso.
/// </summary>
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    [Header("Progresso da Torre (Pick Me Up!)")]
    [Tooltip("Andar atual em que a guilda se encontra (1 a 100)")]
    public int currentFloor = 1;

    [Tooltip("Maior andar já desbloqueado pela Guilda (todos os andares de 1 até este valor estão liberados)")]
    public int maxUnlockedFloor = 1;

    [Tooltip("Quantidade de salas concluídas neste andar")]
    public int clearedRoomsThisFloor = 0;

    [Tooltip("Salas necessárias para a porta do Boss surgir")]
    public int roomsRequiredForBoss = 4;

    [Tooltip("O Boss deste andar já foi derrotado?")]
    public bool isFloorBossDefeated = false;

    [Header("Portas Disponíveis para Escolha")]
    public List<DungeonRoom> currentDoorChoices = new List<DungeonRoom>();

    /// <summary>
    /// Retorna o nível máximo permitido para os heróis no andar especificado (Andar 1 = Nv. 10).
    /// </summary>
    public int GetMaxHeroLevelForFloor(int floor = -1)
    {
        int targetFloor = floor > 0 ? floor : currentFloor;
        return Mathf.Clamp(targetFloor * 10, 10, 100);
    }

    /// <summary>
    /// Retorna o nível mínimo de herói exigido para desbloquear a sala do Chefe do andar (Andar 1 = Nv. 5).
    /// </summary>
    public int GetRequiredHeroLevelForBoss(int floor = -1)
    {
        int targetFloor = floor > 0 ? floor : currentFloor;
        return (Mathf.Max(1, targetFloor) - 1) * 10 + 5;
    }

    /// <summary>
    /// Retorna o nível do Chefe do andar especificado (Andar 1 = Nv. 7).
    /// </summary>
    public int GetFloorBossLevel(int floor = -1)
    {
        int targetFloor = floor > 0 ? floor : currentFloor;
        return (Mathf.Max(1, targetFloor) - 1) * 10 + 7;
    }

    /// <summary>
    /// Verifica se pelo menos 2 heróis da Party ativa atingiram o nível necessário para o Boss aparecer (Nv. 5 no 1º Andar).
    /// </summary>
    public bool AreHeroesReadyForFloorBoss(out int qualifiedHeroesCount, out int requiredHeroesCount)
    {
        qualifiedHeroesCount = 0;
        if (PartyManager.Instance == null || PartyManager.Instance.activeParty == null || PartyManager.Instance.activeParty.Count == 0)
        {
            requiredHeroesCount = 2;
            return false;
        }

        int reqHeroLvl = GetRequiredHeroLevelForBoss();
        foreach (var hero in PartyManager.Instance.activeParty)
        {
            if (hero != null && hero.level >= reqHeroLvl)
            {
                qualifiedHeroesCount++;
            }
        }

        requiredHeroesCount = Mathf.Min(2, PartyManager.Instance.activeParty.Count);
        return qualifiedHeroesCount >= requiredHeroesCount;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (maxUnlockedFloor < currentFloor)
            maxUnlockedFloor = currentFloor;

        GenerateDoors();
    }

    /// <summary>
    /// Inicia um andar da torre (e garante que ele esteja registrado nos andares liberados).
    /// </summary>
    public void StartFloor(int floorNumber)
    {
        currentFloor = Mathf.Max(1, floorNumber);
        if (currentFloor > maxUnlockedFloor)
        {
            maxUnlockedFloor = currentFloor;
        }

        clearedRoomsThisFloor = 0;
        isFloorBossDefeated = false;

        Debug.Log($"🏰 [TORRE - ANDAR {currentFloor}] A Guilda entrou no Andar {currentFloor}! (Andares Liberados: 1 ao {maxUnlockedFloor})");
        GenerateDoors();
        SaveSystem.AutoSave($"Entrada no Andar {currentFloor}");
    }

    /// <summary>
    /// Permite viajar diretamente para qualquer andar já liberado pela Guilda e iniciar o combate nele.
    /// </summary>
    public void TravelToUnlockedFloor(int targetFloor)
    {
        int clamped = Mathf.Clamp(targetFloor, 1, Mathf.Max(1, maxUnlockedFloor));
        StartFloor(clamped);

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.TestStartBattle();
        }
    }

    /// <summary>
    /// Retorna a quantidade balanceada de monstros comuns de acordo com o progresso de salas no andar.
    /// </summary>
    public int GetScaledEnemyCountForCurrentRoom()
    {
        if (clearedRoomsThisFloor <= 2) return Random.Range(2, 4);
        if (clearedRoomsThisFloor <= 5) return Random.Range(2, 5);
        return Random.Range(3, 6);
    }

    /// <summary>
    /// Gera 2 a 3 opções de portas para a próxima sala com base no estado do andar.
    /// </summary>
    public void GenerateDoors()
    {
        currentDoorChoices.Clear();

        if (isFloorBossDefeated)
        {
            currentDoorChoices.Add(new DungeonRoom(RoomType.Stairs, currentFloor, clearedRoomsThisFloor));
            currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));

            float postBossRoll = Random.value;
            if (postBossRoll < 0.14f)
                currentDoorChoices.Add(new DungeonRoom(RoomType.Treasure, currentFloor, clearedRoomsThisFloor));
            else if (postBossRoll < 0.40f)
                currentDoorChoices.Add(new DungeonRoom(RoomType.Rest, currentFloor, clearedRoomsThisFloor));
            else
                currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));

            Debug.Log($"🚪 [ESCOLHA DE PORTAS - PÓS-BOSS] Escadaria para o Andar {currentFloor + 1} liberada! Ou continue no andar para farmar XP e itens!");
        }
        else
        {
            if (AreHeroesReadyForFloorBoss(out int qualCount, out int reqCount))
            {
                currentDoorChoices.Add(new DungeonRoom(RoomType.Boss, currentFloor, clearedRoomsThisFloor));
                currentDoorChoices.Add(new DungeonRoom(RoomType.Rest, currentFloor, clearedRoomsThisFloor));
                currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));

                Debug.Log($"⚠️ [ALERTA DE CHEFE] {qualCount}/{reqCount} heróis no Nv. {GetRequiredHeroLevelForBoss()}+! A sala do Guardião (Nv. {GetFloorBossLevel()}) do Andar {currentFloor} foi revelada!");
            }
            else
            {
                currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));

                if (clearedRoomsThisFloor >= 2 && Random.value < 0.14f)
                    currentDoorChoices.Add(new DungeonRoom(RoomType.Treasure, currentFloor, clearedRoomsThisFloor));
                else
                    currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));

                if (clearedRoomsThisFloor >= 1 && Random.value < 0.22f)
                    currentDoorChoices.Add(new DungeonRoom(RoomType.Rest, currentFloor, clearedRoomsThisFloor));
                else
                    currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));

                Debug.Log($"🚪 [ESCOLHA DE PORTAS] {currentDoorChoices.Count} caminhos à frente no Andar {currentFloor}. (Boss exige {reqCount} heróis no Nv. {GetRequiredHeroLevelForBoss()})");
            }
        }
    }

    /// <summary>
    /// O jogador escolhe uma das portas (0, 1 ou 2) para entrar.
    /// Só é possível escolher portas com a sala atual completamente limpa.
    /// </summary>
    public void ChooseDoor(int doorIndex)
    {
        if (BattleManager.Instance != null && (BattleManager.Instance.currentState == BattleState.HeroTurn || BattleManager.Instance.currentState == BattleState.EnemyTurn))
        {
            Debug.LogWarning("⚠️ Você precisa derrotar todos os monstros na câmara presente antes de escolher a próxima porta!");
            return;
        }

        if (doorIndex < 0 || doorIndex >= currentDoorChoices.Count)
        {
            Debug.LogWarning("Porta inválida selecionada!");
            return;
        }

        DungeonRoom selectedRoom = currentDoorChoices[doorIndex];
        currentDoorChoices.Clear();
        Debug.Log($"🚶 A Guilda atravessou a porta: [{selectedRoom.roomTitle}]");

        ProcessRoom(selectedRoom);
    }

    private void ProcessRoom(DungeonRoom room)
    {
        clearedRoomsThisFloor++;

        switch (room.roomType)
        {
            case RoomType.Stairs:
                AscendToNextFloor();
                return;

            case RoomType.Boss:
                Debug.Log($"👹 [GUARDIÃO DO ANDAR] A Guilda adentra a câmara do Chefe do Andar {currentFloor}!");
                if (BattleManager.Instance != null && BattleManager.Instance.floorBossEnemy != null)
                {
                    List<EnemyData> bossEncounter = new List<EnemyData> { BattleManager.Instance.floorBossEnemy };
                    BattleManager.Instance.StartBattle(bossEncounter);
                    return;
                }
                else
                {
                    DefeatFloorBoss();
                    return;
                }

            case RoomType.Combat:
                Debug.Log($"⚔️ [EMBOSCADA] Inimigos avistados na câmara!");
                if (BattleManager.Instance != null && BattleManager.Instance.commonFloorEnemies != null && BattleManager.Instance.commonFloorEnemies.Length > 0)
                {
                    List<EnemyData> encounter = new List<EnemyData>();
                    int enemyCount = GetScaledEnemyCountForCurrentRoom();
                    for (int i = 0; i < enemyCount; i++)
                    {
                        encounter.Add(BattleManager.Instance.commonFloorEnemies[Random.Range(0, BattleManager.Instance.commonFloorEnemies.Length)]);
                    }
                    BattleManager.Instance.StartBattle(encounter);
                    return;
                }
                else
                {
                    int xpReward = 40 * currentFloor;
                    int goldReward = Random.Range(15, 35) * currentFloor;
                    PartyManager.Instance?.DistributeXP(xpReward);
                    PartyManager.Instance?.AddGold(goldReward);
                }
                break;

            case RoomType.Treasure:
                Debug.Log($"🎁 Baú aberto! Ouro e equipamentos enviados para o Depósito da Guilda!");
                int chestGold = Random.Range(35, 70) * currentFloor;
                List<ItemData> chestDrops = new List<ItemData>();
                if (PartyManager.Instance != null)
                {
                    PartyManager.Instance.AddGold(chestGold);
                    ItemData chestLoot = ItemGenerator.GenerateRandomDungeonDrop(currentFloor, -1, isTreasureChest: true, isBossDrop: false);
                    PartyManager.Instance.AddItemToInventory(chestLoot);
                    if (chestLoot != null) chestDrops.Add(chestLoot);
                }
                GenerateDoors();
                SaveSystem.AutoSave("Bau de Tesouro Coletado");
                if (BattleHUD.Instance != null)
                {
                    BattleHUD.Instance.UpdateDungeonBackground(RoomType.Treasure, currentFloor);
                    BattleHUD.Instance.ShowVictory(0, chestGold, chestDrops);
                }
                return;

            case RoomType.Rest:
                Debug.Log($"🏕️ O grupo descansou na fogueira! Curando 35% de HP e MP de todos os heróis!");
                if (PartyManager.Instance != null)
                {
                    foreach (var hero in PartyManager.Instance.activeParty)
                    {
                        hero.Heal(Mathf.RoundToInt(hero.GetMaxHP() * 0.35f));
                        hero.currentMP = Mathf.Min(hero.GetMaxMP(), hero.currentMP + Mathf.RoundToInt(hero.GetMaxMP() * 0.35f));
                    }
                }
                GenerateDoors();
                SaveSystem.AutoSave("Descanso na Fogueira");
                if (BattleHUD.Instance != null)
                {
                    BattleHUD.Instance.UpdateDungeonBackground(RoomType.Rest, currentFloor);
                    BattleHUD.Instance.ShowVictory(0, 0, null);
                }
                return;
        }

        GenerateDoors();
        SaveSystem.AutoSave("Sala Concluida");
    }

    /// <summary>
    /// Marca o Boss do andar como derrotado e libera o próximo andar na lista de Andares Liberados!
    /// </summary>
    [ContextMenu("Derrotar Boss do Andar (Desbloquear Escadas)")]
    public void DefeatFloorBoss()
    {
        isFloorBossDefeated = true;
        if (currentFloor + 1 > maxUnlockedFloor)
        {
            maxUnlockedFloor = currentFloor + 1;
        }

        Debug.Log($"🏆 VITÓRIA ÉPICA! O Guardião do Andar {currentFloor} foi destruído!");
        Debug.Log($"🪜 O Andar {currentFloor + 1} foi LIBERADO permanentemente no seu Save!");

        GenerateDoors();
        SaveSystem.AutoSave($"Boss do Andar {currentFloor} Derrotado");
    }

    /// <summary>
    /// Sobe a escadaria e entra no próximo andar da masmorra.
    /// </summary>
    [ContextMenu("Subir para o Próximo Andar")]
    public void AscendToNextFloor()
    {
        int nextFloor = currentFloor + 1;
        if (nextFloor > maxUnlockedFloor)
        {
            maxUnlockedFloor = nextFloor;
        }

        Debug.Log($"🪜 [SUBINDO AS ESCADAS] A Guilda sobe os degraus de pedra e adentra o Andar {nextFloor}!");
        StartFloor(nextFloor);

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.TestStartBattle();
        }
    }

    #region Métodos de Teste Rápido no Inspector

    [ContextMenu("Entrar na Porta 0")]
    public void ChooseDoor0() => ChooseDoor(0);

    [ContextMenu("Entrar na Porta 1")]
    public void ChooseDoor1() => ChooseDoor(1);

    [ContextMenu("Entrar na Porta 2")]
    public void ChooseDoor2() => ChooseDoor(2);

    #endregion
}
