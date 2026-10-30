#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FinalSpam.Enemies;

public static class FinalSpamEnemySpawnTools
{
    private const string Directory = "Assets/Prefabs/Gameplay";

    [MenuItem("GameObject/FinalSpam/SpawnPoint - Soldado de Troia", false, 10)]
    public static void CreateTrojanInScene()
    {
        CreateInScene("Soldado de Troia", "Assets/Prefabs/Enemies/SoldadoTroia.prefab", false);
    }

    [MenuItem("GameObject/FinalSpam/SpawnPoint - Aviao DDoS", false, 11)]
    public static void CreatePlaneInScene()
    {
        CreateInScene("Aviao DDoS", "Assets/Prefabs/Enemies/AviaoDDOS.prefab", true);
    }

    [MenuItem("FinalSpam/Inimigos/Gerar Prefabs de SpawnPoints", false, 30)]
    public static void CreateSpawnPrefabs()
    {
        EnsureDirectory();
        MakePrefab("SpawnPoint_SoldadoTroia", "Assets/Prefabs/Enemies/SoldadoTroia.prefab", false);
        MakePrefab("SpawnPoint_AviaoDDOS", "Assets/Prefabs/Enemies/AviaoDDOS.prefab", true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FinalSpam] Prefabs de SpawnPoints criados em " + Directory);
    }

    private static void CreateInScene(string label, string assetPath, bool aircraft)
    {
        GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (enemy == null)
        {
            Debug.LogError("[FinalSpam] Prefab de inimigo nao encontrado: " + assetPath);
            return;
        }
        GameObject marker = new GameObject("SpawnPoint | " + label);
        Undo.RegisterCreatedObjectUndo(marker, "Criar spawnpoint de inimigo");
        var sp = marker.AddComponent<EnemySpawnPoint>();
        sp.enemyPrefab = enemy;
        sp.maxAlive = 1;
        sp.spawnInterval = aircraft ? 14f : 6f;
        sp.enableChaseForSpawnedSoldiers = true;
        sp.enableFlightForSpawnedAircraft = true;
        if (SceneView.lastActiveSceneView != null)
            marker.transform.position = SceneView.lastActiveSceneView.pivot +
                (aircraft ? Vector3.up * 8f : Vector3.zero);
        Selection.activeGameObject = marker;
        EditorGUIUtility.PingObject(marker);
        EditorSceneManager.MarkSceneDirty(marker.scene);
    }

    private static void EnsureDirectory()
    {
        if (!AssetDatabase.IsValidFolder(Directory))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Gameplay");
    }

    private static void MakePrefab(string name, string enemyPath, bool aircraft)
    {
        string savePath = Directory + "/" + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(savePath) != null) return;
        GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPath);
        if (enemy == null) throw new System.Exception("Prefab missing: " + enemyPath);
        GameObject marker = new GameObject(name);
        try
        {
            var sp = marker.AddComponent<EnemySpawnPoint>();
            sp.enemyPrefab = enemy;
            sp.maxAlive = 1;
            sp.spawnInterval = aircraft ? 14f : 6f;
            sp.enableChaseForSpawnedSoldiers = true;
            sp.enableFlightForSpawnedAircraft = true;
            PrefabUtility.SaveAsPrefabAsset(marker, savePath);
        }
        finally { Object.DestroyImmediate(marker); }
    }
}
#endif

#if UNITY_EDITOR
/// <summary>Opcional: alinha o eixo azul dos canos ao nariz do aviao.
/// Nunca move os Transform nem e executado automaticamente.</summary>
public static class FinalSpamMuzzleOrientationTools
{
    [MenuItem("FinalSpam/Inimigos/Orientar eixos dos MuzzlePoints para frente", false, 45)]
    public static void AlignMuzzleAxes()
    {
        const string path = "Assets/Prefabs/Enemies/AviaoDDOS.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var anim = root.GetComponent<FinalSpam.Enemies.EnemyProceduralAnimator>();
            if (anim == null || anim.ddosMuzzleLeft == null ||
                anim.ddosMuzzleRight == null)
            {
                Debug.LogError("[FinalSpam] MuzzlePoints do aviao nao configurados.");
                return;
            }
            // Altera apenas a direcao dos eixos (rotacao); posicao intacta.
            var direction = Quaternion.LookRotation(root.transform.forward,
                root.transform.up);
            anim.ddosMuzzleLeft.rotation = direction;
            anim.ddosMuzzleRight.rotation = direction;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[FinalSpam] Eixos +Z dos dois MuzzlePoints apontam para " +
                "a frente. Posicoes preservadas.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
#endif
