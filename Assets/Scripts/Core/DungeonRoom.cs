using UnityEngine;

/// <summary>
/// Tipos de salas encontradas durante a exploração da masmorra.
/// </summary>
public enum RoomType
{
    Combat,     // Batalha comum contra monstros do andar
    Treasure,   // Sala de baú com ouro e equipamentos
    Rest,       // Fogueira / Santuário para recuperar HP e MP
    Boss,       // Sala do Guardião do Andar (o chefe)
    Stairs      // Escadaria para o próximo andar (liberada após matar o Boss)
}

/// <summary>
/// Representa uma porta / sala sorteada para o jogador escolher.
/// </summary>
[System.Serializable]
public class DungeonRoom
{
    public RoomType roomType;
    public string roomTitle;
    [TextArea(2, 3)]
    public string description;
    public int floor;

    public DungeonRoom(RoomType type, int floorNumber, int roomNumber)
    {
        roomType = type;
        floor = floorNumber;

        switch (type)
        {
            case RoomType.Combat:
                roomTitle = $"⚔️ Batalha no Andar {floor}";
                description = "Monstros hostis rondam esta câmara. Prepare-se para a luta!";
                break;
            case RoomType.Treasure:
                roomTitle = $"🎁 Câmara do Tesouro";
                description = "Um baú reforçado brilha na penumbra. Há espólios valiosos aqui.";
                break;
            case RoomType.Rest:
                roomTitle = $"🏕️ Fogueira de Descanso";
                description = "Um refúgio seguro para a guilda curar ferimentos e recuperar mana.";
                break;
            case RoomType.Boss:
                roomTitle = $"👹 Guardião do Andar {floor}";
                description = "Uma presença esmagadora emana desta porta. O Chefe do Andar aguarda!";
                break;
            case RoomType.Stairs:
                roomTitle = $"🪜 Escadaria para o Andar {floor + 1}";
                description = "As escadas que levam para o andar superior da torre estão livres!";
                break;
        }
    }
}
