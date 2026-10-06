using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O Mestre da Masmorra (DungeonManager).
/// Controla o avanço pelos 100 andares da torre, geração de portas e salas,
/// aparição do Boss do andar e a mecânica de permanência para grind pós-boss (Pick Me Up!).
/// </summary>
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    [Header("Progresso da Torre (Pick Me Up!)")]
    [Tooltip("Andar atual em que a guilda se encontra (1 a 100)")]
    public int currentFloor = 1;

    [Tooltip("Quantidade de salas concluídas neste andar")]
    public int clearedRoomsThisFloor = 0;

    [Tooltip("Salas necessárias para a porta do Boss surgir")]
    public int roomsRequiredForBoss = 4;

    [Tooltip("O Boss deste andar já foi derrotado?")]
    public bool isFloorBossDefeated = false;

    [Header("Portas Disponíveis para Escolha")]
    public List<DungeonRoom> currentDoorChoices = new List<DungeonRoom>();

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
        StartFloor(currentFloor);
    }

    /// <summary>
    /// Inicia um novo andar da torre.
    /// </summary>
    public void StartFloor(int floorNumber)
    {
        currentFloor = floorNumber;
        clearedRoomsThisFloor = 0;
        isFloorBossDefeated = false;

        Debug.Log($"🏰 [TORRE - ANDAR {currentFloor}] A Guilda entrou em um novo território desconhecido!");
        GenerateDoors();
    }

    /// <summary>
    /// Gera 2 a 3 opções de portas para a próxima sala com base no estado do andar.
    /// </summary>
    public void GenerateDoors()
    {
        currentDoorChoices.Clear();

        if (isFloorBossDefeated)
        {
            // O Boss já morreu! A Escadaria fica permanentemente disponível como opção para o jogador subir quando quiser!
            currentDoorChoices.Add(new DungeonRoom(RoomType.Stairs, currentFloor, clearedRoomsThisFloor));

            // Outras opções para quem quer continuar grindando no andar
            currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));
            currentDoorChoices.Add(new DungeonRoom(RoomType.Treasure, currentFloor, clearedRoomsThisFloor));

            Debug.Log($"🚪 [ESCOLHA DE PORTAS - PÓS-BOSS] Escadaria para o Andar {currentFloor + 1} liberada! Ou continue no andar para farmar XP e itens!");
        }
        else
        {
            // O Boss ainda está vivo
            if (clearedRoomsThisFloor >= roomsRequiredForBoss)
            {
                // Chegou na hora do Boss! A porta do Chefe aparece como opção prioritária
                currentDoorChoices.Add(new DungeonRoom(RoomType.Boss, currentFloor, clearedRoomsThisFloor));
                currentDoorChoices.Add(new DungeonRoom(RoomType.Rest, currentFloor, clearedRoomsThisFloor));
                currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));

                Debug.Log($"⚠️ [ALERTA DE CHEFE] O Guardião do Andar {currentFloor} foi localizado!");
            }
            else
            {
                // Salas normais de progressão (Combate, Baú, Descanso)
                currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));

                float roll = Random.value;
                if (roll < 0.45f)
                    currentDoorChoices.Add(new DungeonRoom(RoomType.Treasure, currentFloor, clearedRoomsThisFloor));
                else
                    currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));

                if (Random.value < 0.35f)
                    currentDoorChoices.Add(new DungeonRoom(RoomType.Rest, currentFloor, clearedRoomsThisFloor));
                else
                    currentDoorChoices.Add(new DungeonRoom(RoomType.Combat, currentFloor, clearedRoomsThisFloor));

                Debug.Log($"🚪 [ESCOLHA DE PORTAS] {currentDoorChoices.Count} caminhos à frente no Andar {currentFloor}.");
            }
        }
    }

    /// <summary>
    /// O jogador escolhe uma das portas (0, 1 ou 2) para entrar.
    /// </summary>
    public void ChooseDoor(int doorIndex)
    {
        if (doorIndex < 0 || doorIndex >= currentDoorChoices.Count)
        {
            Debug.LogWarning("Porta inválida selecionada!");
            return;
        }

        DungeonRoom selectedRoom = currentDoorChoices[doorIndex];
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
                Debug.Log($"⚔️ [COMBATE CONTRA O CHEFE] Batalha decisiva contra o Guardião do Andar {currentFloor}!");
                // Simulação da vitória do Boss para teste (posteriormente ligado ao BattleManager)
                DefeatFloorBoss();
                break;

            case RoomType.Combat:
                Debug.Log($"⚔️ Monstros derrotados! Concedendo recompensas...");
                if (PartyManager.Instance != null)
                {
                    int xpReward = 40 * currentFloor;
                    int goldReward = Random.Range(15, 35) * currentFloor;
                    PartyManager.Instance.DistributeXP(xpReward);
                    PartyManager.Instance.AddGold(goldReward);
                }
                break;

            case RoomType.Treasure:
                Debug.Log($"🎁 Baú aberto! Ouro e tesouros obtidos!");
                if (PartyManager.Instance != null)
                {
                    PartyManager.Instance.AddGold(Random.Range(50, 120) * currentFloor);
                }
                break;

            case RoomType.Rest:
                Debug.Log($"🏕️ O grupo descansou na fogueira! Curando 35% de HP e MP de todos os heróis!");
                if (PartyManager.Instance != null)
                {
                    foreach (var hero in PartyManager.Instance.activeParty)
                    {
                        hero.Heal(Mathf.RoundToInt(hero.GetMaxHP() * 0.35f));
                    }
                }
                break;
        }

        // Gera as próximas opções de portas após a sala ser concluída
        GenerateDoors();
    }

    /// <summary>
    /// Marca o Boss do andar como derrotado e libera as escadarias para o próximo andar.
    /// </summary>
    [ContextMenu("Derrotar Boss do Andar (Desbloquear Escadas)")]
    public void DefeatFloorBoss()
    {
        isFloorBossDefeated = true;
        Debug.Log($"🏆 VITÓRIA ÉPICA! O Guardião do Andar {currentFloor} foi destruído!");
        Debug.Log($"🪜 A Escadaria para o Andar {currentFloor + 1} agora está aberta! Você pode subir ou continuar farmando no andar!");

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.DistributeXP(250 * currentFloor);
            PartyManager.Instance.AddGold(300 * currentFloor);
        }

        GenerateDoors();
    }

    /// <summary>
    /// Sobe a escadaria e entra no próximo andar da masmorra.
    /// </summary>
    [ContextMenu("Subir para o Próximo Andar")]
    public void AscendToNextFloor()
    {
        int nextFloor = currentFloor + 1;
        Debug.Log($"🪜 [SUBINDO AS ESCADAS] A Guilda sobe os degraus de pedra e adentra o Andar {nextFloor}!");
        StartFloor(nextFloor);
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
