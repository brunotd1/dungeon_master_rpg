# ⚔️ Dungeon Master RPG (Inspirado em Pick Me Up! Infinite Gacha)

RPG de exploração de masmorras e gerenciamento de guilda desenvolvido em Unity (URP 2D).

---

## 🚀 Como Abrir e Rodar o Projeto no Unity (Guia Rápido)
1. **Versão recomendada:** Unity 6 (versão `6000.0` ou superior com suporte a URP 2D).
2. **Clonar o Repositório:**
   ```bash
   git clone https://github.com/brunotd1/dungeon_master_rpg.git
   ```
3. **Adicionar no Unity Hub:**
   - Abra o **Unity Hub**;
   - Clique em **Add** (ou *Adicionar*) -> **Add project from disk**;
   - Selecione a pasta raiz `dungeon_master_rpg` que foi clonada;
   - Abra o projeto.
4. **Abrir a Cena de Jogo:**
   - No painel *Project* do Unity, acesse: `Assets/Scenes/SampleScene.unity` e dê duplo clique.
   - *(O script `AutoSceneBootstrap` também carrega esta cena automaticamente se o Unity abrir numa cena vazia).*
5. **Jogar:**
   - Aperte o botão **Play** no topo do Unity. O combate se iniciará imediatamente com a interface de batalha interativa!

---

## 📌 1. Visão Geral
* **Gênero:** RPG Dungeon Crawler / Guild Master com Combate Clássico por Turnos.
* **Inspiração Principal:** *Pick Me Up! Infinite Gacha* (torre de 100 andares, gacha de heróis, estrelas, level cap, permadeath e grind pós-boss).
* **Sistema de Combate:** Visão lateral clássica (estilo Final Fantasy), 4 aventureiros perfilados contra os monstros.
* **Sistema de Equipamento:** 6 slots de equipamentos com requisitos de atributos mínimos estilo *Dark Souls*.

---

## 🛡️ 2. As Classes e Atributos
* **Classes:**
  * **Cavaleiro (Knight):** Equilibrado, dano físico constante e resistência média.
  * **Cavaleiro Pesado (HeavyKnight):** Tanque do time, focado em alta Vida (HP) e Defesa.
  * **Mago (Mage):** Especialista em dano mágico e controle elemental (consome MP).
  * **Ladino (Rogue):** Focado em velocidade de turno, adagas/arco e alta chance de crítico.

* **Atributos Primários (Estilo Souls):**
  * `Força (STR)`: Escala ataque físico e libera armas pesadas/placas.
  * `Destreza (DEX)`: Aumenta iniciativa, velocidade e chance de crítico.
  * `Inteligência (INT)`: Escala poder mágico e capacidade máxima de Mana.
  * `Vitalidade (VIT)`: Determina a Vida Máxima (HP) e resistência a dano.

---

## ⭐ 3. Sistema de Estrelas e Level Cap (Pick Me Up!)
O limite de nível é estritamente travado pelas estrelas do aventureiro:
* **1★:** Limite Nível **20**
* **2★:** Limite Nível **40**
* **3★:** Limite Nível **60**
* **4★:** Limite Nível **80**
* **5★:** Limite Nível **100**
* **Estrelas Douradas (Gold Stars):** Heróis especiais/raros que nascem com +25% em todos os status e habilidades exclusivas.

---

## 🏰 4. A Masmorra e Escolha de Portas
* **Progresso por Andares (1 a 100):** Cada andar possui um Chefe (Guardião do Andar).
* **Portas Dinâmicas:** A cada sala limpa, o jogador escolhe entre 2 ou 3 portas (Combate, Baú de Tesouro, Descanso, Boss).
* **Mecânica de Grind Pós-Boss:** 
  * Derrotar o Boss do andar **libera a Escadaria para o Próximo Andar**.
  * A Escadaria torna-se uma das opções de portas, permitindo que o jogador decida se sobe imediatamente ou continua explorando salas para farmar XP e itens antes de avançar!

---

## 🏗️ 5. Arquitetura de Scripts
* `HeroClass.cs`: Enums de classes e struct de atributos primários.
* `HeroData.cs`: ScriptableObject com fórmulas matemáticas de combate e crescimento.
* `HeroInstance.cs`: O herói vivo com XP, HP, MP, 6 slots equipados e promoção de estrelas.
* `HeroGenerator.cs`: Gerador procedural de heróis com nomes medievais e rolagens aleatórias.
* `ItemData.cs`: Equipamentos com requisitos de atributos estilo Dark Souls (`CanBeEquippedBy`).
* `PartyManager.cs`: Gerenciador do grupo de 4 heróis, bolsa de ouro e divisão de XP.
* `DungeonRoom.cs`: Definição dos tipos de salas da masmorra.
* `DungeonManager.cs`: Gerenciador dos andares, gerador de portas e controle do boss.
* `EnemyData.cs` & `EnemyInstance.cs`: Modelos de monstros, chefes de andar, cálculo de dano e tabela de loot.
* `BattleManager.cs`: Motor de combate por turnos JRPG (iniciativa por velocidade, turnos de heróis e IA dos monstros, postura de defesa e cálculo de dano físico/crítico).
* `BattleHUD.cs`: Interface gráfica dinâmica (estilo Final Fantasy), cartões dos heróis e monstros com HP/MP, banners de rodada e menu interativo de seleção de alvos com cliques de mouse.

---

## 🎯 6. Status Atual e Próximos Passos
* ✅ **Mundo e Classes:** 4 classes implementadas com atributos, estrelas, level caps e requisitos estilo Souls.
* ✅ **Masmorra e Exploração:** Lógica de 100 andares com portas dinâmicas e grind pós-chefe.
* ✅ **Combate por Turnos Completo:** Arena visual interativa em tempo real com seleção de alvos por clique e botões de ação.
* 🔜 **Próximos Passos (Amanhã):**
  * Habilidades e magias ativas de cada classe (Golpe Pesado, Provocação/Taunt, Bola de Fogo, Ataque Furtivo).
  * Interface visual de seleção de portas da masmorra (porta de combate, baú, descanso).
  * Tela de inventário e equipamentos com verificação dos atributos dos heróis.
