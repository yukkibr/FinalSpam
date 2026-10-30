#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FinalSpam.Enemies;

/// <summary>
/// Ativado depois da importacao para ligar o Rigidbody/voo ao prefab DDoS
/// SEM reabrir nem salvar a cena do usuario. Tambem gera 2 prefabs de spawn.
/// </summary>
public static class FinalSpamCombatAutoSetup
{
    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.delayCall += ConfigureWhenReady;
    }

    private static void ConfigureWhenReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ConfigureWhenReady;
            return;
        }
        Configure();
    }

    [MenuItem("FinalSpam/Inimigos/Instalar voo e spawns nos prefabs", false, 25)]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        const string path = "Assets/Prefabs/Enemies/AviaoDDOS.prefab";
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) return;

        GameObject plane = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var anim = plane.GetComponent<EnemyProceduralAnimator>();
            if (anim == null) return;
            Transform left = anim.ddosMuzzleLeft;
            Transform right = anim.ddosMuzzleRight;
            Transform bomb = anim.ddosMiniNukeDropPoint;
            Vector3 l = left != null ? left.localPosition : Vector3.zero;
            Vector3 r = right != null ? right.localPosition : Vector3.zero;
            Vector3 b = bomb != null ? bomb.localPosition : Vector3.zero;
            Quaternion lr = left != null ? left.localRotation : Quaternion.identity;
            Quaternion rr = right != null ? right.localRotation : Quaternion.identity;
            Quaternion br = bomb != null ? bomb.localRotation : Quaternion.identity;
            if (plane.GetComponent<DdosAircraftFlight>() == null)
            {
                var weapons = plane.GetComponent<DdosAircraftWeapons>();
                if (weapons != null)
                {
                    weapons.fireAlongAircraftForward = true;
                    weapons.muzzleHitRadius = 0.65f;
                }
                plane.AddComponent<DdosAircraftFlight>();
                if (left != null && (left.localPosition != l || left.localRotation != lr) ||
                    right != null && (right.localPosition != r || right.localRotation != rr) ||
                    bomb != null && (bomb.localPosition != b || bomb.localRotation != br))
                    throw new System.Exception("Um marker se moveu: instalacao cancelada");
                PrefabUtility.SaveAsPrefabAsset(plane, path);
                Debug.Log("[FinalSpam] Voo Rigidbody do DDOS instalado no prefab. MuzzlePoints intactos.");
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(plane); }

        FinalSpamEnemySpawnTools.CreateSpawnPrefabs();
    }
}
#endif
