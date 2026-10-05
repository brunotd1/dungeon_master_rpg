using UnityEngine;

/// <summary>
/// Gerador Procedural / Gacha de Aventureiros (Fiel ao universo de Pick Me Up!).
/// Cria heróis aleatórios com nomes, títulos medievais, classes e rolagens de atributos únicos.
/// Suporta o sorteio de Heróis de Estrelas Douradas com bônus de status!
/// </summary>
public static class HeroGenerator
{
    private static readonly string[] FirstNames = {
        "Han", "Loki", "Aron", "Boran", "Velvet", "Kael", "Cedric", "Rowan",
        "Eldrin", "Garrick", "Theron", "Mira", "Lyra", "Selene", "Vael", "Darius"
    };

    private static readonly string[] Nicknames = {
        "o Destemido", "Muralha de Ferro", "Lâmina da Noite", "Olhos de Cinza",
        "o Silencioso", "Coração de Leão", "o Renegado", "Mão de Prata", "o Errante", "Sombra Veloz"
    };

    /// <summary>
    /// Gera um aventureiro completamente aleatório para a guilda.
    /// </summary>
    public static HeroInstance GenerateRandomHero(int startingStars = 1, float goldStarChance = 0.05f)
    {
        // 1. Sorteia Classe
        HeroClassType randomClass = (HeroClassType)Random.Range(0, 4);

        // 2. Sorteia Nome e Título
        string randomName = $"{FirstNames[Random.Range(0, FirstNames.Length)]}, {Nicknames[Random.Range(0, Nicknames.Length)]}";

        // 3. Sorteia se é Estrela Dourada (Pick Me Up!)
        bool isGold = Random.value < goldStarChance;

        // 4. Cria arquétipo em memória com atributos rolados aleatoriamente
        HeroData generatedData = ScriptableObject.CreateInstance<HeroData>();
        generatedData.heroName = randomName;
        generatedData.heroClass = randomClass;
        generatedData.stars = startingStars;
        generatedData.isGoldStar = isGold;

        // 5. Rola atributos base com variação aleatória de acordo com a classe
        generatedData.baseAttributes = RollClassAttributes(randomClass, isGold);
        generatedData.growthPerLevel = GetClassGrowth(randomClass);

        HeroInstance instance = new HeroInstance(generatedData, startingLevel: 1);

        string starBadge = isGold ? $"🌟 {startingStars}★ (DOURADA)" : $"⭐ {startingStars}★";
        Debug.Log($"📜 [CONVOCAÇÃO DA GUILDA] Novo Herói: {instance.heroName} | {randomClass} | {starBadge} | FOR: {generatedData.baseAttributes.strength} DES: {generatedData.baseAttributes.dexterity} INT: {generatedData.baseAttributes.intelligence} VIT: {generatedData.baseAttributes.vitality}");

        return instance;
    }

    private static HeroAttributes RollClassAttributes(HeroClassType heroClass, bool isGoldStar)
    {
        int str = 10, dex = 10, intel = 10, vit = 10;

        switch (heroClass)
        {
            case HeroClassType.Knight:
                str = Random.Range(12, 16);
                dex = Random.Range(9, 12);
                intel = Random.Range(5, 8);
                vit = Random.Range(11, 15);
                break;

            case HeroClassType.HeavyKnight:
                str = Random.Range(13, 17);
                dex = Random.Range(5, 8);
                intel = Random.Range(4, 7);
                vit = Random.Range(15, 20);
                break;

            case HeroClassType.Mage:
                str = Random.Range(4, 7);
                dex = Random.Range(7, 10);
                intel = Random.Range(15, 20);
                vit = Random.Range(7, 11);
                break;

            case HeroClassType.Rogue:
                str = Random.Range(7, 10);
                dex = Random.Range(15, 20);
                intel = Random.Range(6, 9);
                vit = Random.Range(8, 12);
                break;
        }

        // Heróis com Estrela Dourada ganham bônus de status inicial (+25%)
        if (isGoldStar)
        {
            str = Mathf.RoundToInt(str * 1.25f);
            dex = Mathf.RoundToInt(dex * 1.25f);
            intel = Mathf.RoundToInt(intel * 1.25f);
            vit = Mathf.RoundToInt(vit * 1.25f);
        }

        return new HeroAttributes(str, dex, intel, vit);
    }

    private static HeroAttributes GetClassGrowth(HeroClassType heroClass)
    {
        return heroClass switch
        {
            HeroClassType.Knight => new HeroAttributes(3, 2, 1, 3),
            HeroClassType.HeavyKnight => new HeroAttributes(3, 1, 1, 4),
            HeroClassType.Mage => new HeroAttributes(1, 1, 4, 2),
            HeroClassType.Rogue => new HeroAttributes(2, 4, 1, 2),
            _ => new HeroAttributes(2, 2, 2, 2)
        };
    }
}
