#if UNITY_EDITOR
using FinalSpam.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalSpam.EditorTools
{
    /// <summary>
    /// Sincroniza o eixo Y com a instancia da cena ja aberta no Editor.
    /// O arquivo da cena e o prefab foram migrados para Y na atualizacao.
    /// </summary>
    [InitializeOnLoad]
    public static class DdosTurbineAxisSync
    {
        private const string ScenePath = "Assets/Scenes/Teste Scene.unity";

        static DdosTurbineAxisSync()
        {
            EditorApplication.delayCall += Apply;
        }

        [MenuItem("FinalSpam/Inimigos/Corrigir turbina DDoS (eixo Y)", priority = 110)]
        public static void Apply()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Apply;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            bool wasDirty = scene.isDirty;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "AVIAO DDOS")
                    continue;

                EnemyProceduralAnimator animator = root.GetComponent<EnemyProceduralAnimator>();
                if (animator == null ||
                    animator.enemyKind != EnemyProceduralAnimator.EnemyKind.DdosPlane ||
                    animator.turbineSpinAxis == EnemyProceduralAnimator.BoneAxis.Y)
                    continue;

                Undo.RecordObject(animator, "Corrigir eixo da turbina DDoS");
                animator.turbineSpinAxis = EnemyProceduralAnimator.BoneAxis.Y;
                EditorUtility.SetDirty(animator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                EditorSceneManager.MarkSceneDirty(scene);

                // Preserva outras edicoes nao salvas do usuario.
                // Se a cena ja estava suja, deixa o salvamento para ele.
                if (!wasDirty)
                    EditorSceneManager.SaveScene(scene);

                Debug.Log("[FinalSpam] Turbina do aviao DDoS ajustada para eixo Y.");
            }
        }
    }
}
#endif
