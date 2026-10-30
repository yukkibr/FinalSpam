#if UNITY_EDITOR
using System;
using System.IO;
using FinalSpam.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalSpam.EditorTools
{
    /// <summary>
    /// Instala animacoes procedurais nos tres FBX existentes e gera prefabs
    /// separados para o futuro sistema de spawn. Operacao idempotente.
    /// </summary>
    [InitializeOnLoad]
    public static class FinalSpamEnemySetup
    {
        private const string ScenePath = "Assets/Scenes/Teste Scene.unity";
        private const string PrefabsFolder = "Assets/Prefabs/Enemies";
        private const string InstallVersion = "EnemyProceduralV1";
        private static bool running;

        private struct EnemySpec
        {
            public string sceneName;
            public string sourcePath;
            public string prefabPath;
            public EnemyProceduralAnimator.EnemyKind kind;

            public EnemySpec(string sceneName, string modelName, string prefabName,
                EnemyProceduralAnimator.EnemyKind kind)
            {
                this.sceneName = sceneName;
                sourcePath = "Assets/Models/" + modelName + ".fbx";
                prefabPath = PrefabsFolder + "/" + prefabName + ".prefab";
                this.kind = kind;
            }
        }

        private static readonly EnemySpec[] enemies =
        {
            new EnemySpec("Soldado de troia", "Soldado de troia", "SoldadoTroia",
                EnemyProceduralAnimator.EnemyKind.TrojanSoldier),
            new EnemySpec("VirusTorre", "VirusTorre", "VirusTorre",
                EnemyProceduralAnimator.EnemyKind.TowerVirus),
            new EnemySpec("AVIAO DDOS", "AVIAO DDOS", "AviaoDDOS",
                EnemyProceduralAnimator.EnemyKind.DdosPlane)
        };

        static FinalSpamEnemySetup()
        {
            EditorApplication.delayCall += TryInitialSetup;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static string InstallationKey
        {
            get { return "FinalSpam." + InstallVersion + "." + Application.dataPath; }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path != ScenePath || EditorPrefs.GetBool(InstallationKey, false)) return;
            EditorApplication.delayCall += TryInitialSetup;
        }

        private static void TryInitialSetup()
        {
            if (EditorPrefs.GetBool(InstallationKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryInitialSetup;
                return;
            }

            // Nao abre/fecha cenas do usuario nem altera configuracoes de build.
            // Se a cena de teste nao estiver aberta, o menu manual continua disponivel.
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;

            try
            {
                int installed = Install(scene);
                EditorPrefs.SetBool(InstallationKey, true);
                Debug.Log("[FinalSpam] Animacoes procedurais instaladas: "
                    + installed + " inimigos configurados. Prefabs em " + PrefabsFolder);
            }
            catch (Exception ex)
            {
                Debug.LogError("[FinalSpam] Falha ao instalar animacoes: " + ex);
            }
        }

        [MenuItem("FinalSpam/Inimigos/Instalar animacoes e criar prefabs", priority = 100)]
        public static void InstallFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[FinalSpam] Saia do Play Mode antes de instalar.");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[FinalSpam] Abra Assets/Scenes/Teste Scene.unity e repita.");
                return;
            }

            try
            {
                int installed = Install(scene);
                EditorPrefs.SetBool(InstallationKey, true);
                EditorUtility.DisplayDialog("FinalSpam",
                    "Animacoes prontas! " + installed
                    + " inimigos configurados na cena de teste.\n"
                    + "Prefabs: " + PrefabsFolder, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("FinalSpam", ex.Message, "OK");
            }
        }

        private static int Install(Scene scene)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabsFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

            foreach (EnemySpec spec in enemies)
                EnsureEnemyPrefab(spec);

            int count = 0;
            bool changed = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (EnemySpec spec in enemies)
                {
                    if (root.name != spec.sceneName) continue;

                    EnemyProceduralAnimator animator = root.GetComponent<EnemyProceduralAnimator>();
                    if (animator == null)
                    {
                        animator = Undo.AddComponent<EnemyProceduralAnimator>(root);
                        changed = true;
                    }

                    // Tambem repara setup parcial, sem destruir ajustes personalizados.
                    if (animator.modelRoot == null)
                    {
                        animator.modelRoot = root.transform;
                        changed = true;
                    }
                    if (animator.enemyKind != spec.kind)
                    {
                        animator.enemyKind = spec.kind;
                        changed = true;
                    }

                    animator.AutoAssignBones();
                    EditorUtility.SetDirty(animator);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                    count++;
                    break;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            return count;
        }

        private static void EnsureEnemyPrefab(EnemySpec spec)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.prefabPath);
            if (prefab != null)
            {
                // Nao sobrescreve prefabs que o usuario ja personalizou.
                return;
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.sourcePath);
            if (model == null)
                throw new FileNotFoundException("Modelo 3D nao encontrado: " + spec.sourcePath);

            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("Nao foi possivel instanciar " + spec.sourcePath);

                instance.name = spec.sceneName;
                EnemyProceduralAnimator animator =
                    instance.GetComponent<EnemyProceduralAnimator>() ??
                    instance.AddComponent<EnemyProceduralAnimator>();
                animator.enemyKind = spec.kind;
                animator.modelRoot = instance.transform;
                animator.AutoAssignBones();

                bool success;
                PrefabUtility.SaveAsPrefabAsset(instance, spec.prefabPath, out success);
                if (!success)
                    throw new IOException("Falha ao salvar prefab: " + spec.prefabPath);
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
#endif
