using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Garante que o jogo inicialize com 100% de sucesso em qualquer máquina recém-clonada do GitHub.
/// Se o Unity for aberto numa cena vazia (padrão de novos clones), abre automaticamente a SampleScene.
/// Se o Play for pressionado em qualquer cena, auto-instancia os gerenciadores essenciais caso não existam.
/// </summary>
public static class AutoSceneBootstrap
{
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            // Se a cena ativa no editor estiver vazia/sem caminho salvo (cena Untitled padrão do Unity ao clonar)
            var activeScene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path) || activeScene.name == "Untitled")
            {
                string targetScene = "Assets/Scenes/SampleScene.unity";
                if (System.IO.File.Exists(targetScene))
                {
                    Debug.Log($"[AutoSceneBootstrap] Detectada cena vazia. Carregando automaticamente: {targetScene}");
                    EditorSceneManager.OpenScene(targetScene);
                }
            }
        };
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeCore()
    {
        // 1. Garante uma Câmera Principal na cena se nenhuma existir
        if (Camera.main == null && Object.FindAnyObjectByType<Camera>() == null)
        {
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camObj.tag = "MainCamera";
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
            Debug.Log("[AutoSceneBootstrap] Câmera 2D criada dinamicamente.");
        }

        // 2. Garante o EventSystem com InputSystemUIInputModule
        EventSystem es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = esObj.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            Debug.Log("[AutoSceneBootstrap] EventSystem e InputSystemUIInputModule criados dinamicamente.");
        }

        // 3. Garante o PartyManager
        if (PartyManager.Instance == null && Object.FindAnyObjectByType<PartyManager>() == null)
        {
            GameObject pmObj = new GameObject("PartyManager", typeof(PartyManager));
            Debug.Log("[AutoSceneBootstrap] PartyManager criado dinamicamente.");
        }

        // 4. Garante o DungeonManager
        if (DungeonManager.Instance == null && Object.FindAnyObjectByType<DungeonManager>() == null)
        {
            GameObject dmObj = new GameObject("DungeonManager", typeof(DungeonManager));
            Debug.Log("[AutoSceneBootstrap] DungeonManager criado dinamicamente.");
        }

        // 5. Garante o BattleHUD
        if (BattleHUD.Instance == null && Object.FindAnyObjectByType<BattleHUD>() == null)
        {
            GameObject hudObj = new GameObject("BattleHUD", typeof(BattleHUD));
            Debug.Log("[AutoSceneBootstrap] BattleHUD criado dinamicamente.");
        }

        // 6. Garante o BattleManager
        if (BattleManager.Instance == null && Object.FindAnyObjectByType<BattleManager>() == null)
        {
            GameObject bmObj = new GameObject("BattleManager", typeof(BattleManager));
            Debug.Log("[AutoSceneBootstrap] BattleManager criado dinamicamente.");
        }
    }
}
