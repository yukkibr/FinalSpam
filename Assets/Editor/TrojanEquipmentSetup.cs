#if UNITY_EDITOR
using System;
using FinalSpam.Enemies;
using UnityEditor;
using UnityEngine;

namespace FinalSpam.EditorTools
{
    public static class TrojanEquipmentSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Enemies/SoldadoTroia.prefab";
        private const string SwordPrefab = "Assets/Prefabs/Weapons/EspadaTroia.prefab";
        private const string ShieldPrefab = "Assets/Prefabs/Weapons/EscudoTroia.prefab";

        [MenuItem("FinalSpam/Inimigos/Reparar equipamento do Soldado de Troia", priority=120)]
        public static void Repair()
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform left = Find(prefab.transform, "HAND_L");
                Transform right = Find(prefab.transform, "HAND_R");
                if (left == null || right == null)
                    throw new Exception("Os ossos HAND_L e HAND_R nao foram encontrados.");

                Transform shield = EnsureEquipment(prefab.transform, left, "EscudoTroia", ShieldPrefab);
                Transform sword = EnsureEquipment(prefab.transform, right, "EspadaTroia", SwordPrefab);

                // O novo FBX traz o centro da malha do escudo recuado
                // e a face larga deitada. Girar sua superficie para frente.
                shield.localPosition = new Vector3(0f, 0.44f, 0.22f);
                shield.localRotation = Quaternion.Euler(180f, 0f, 0f);
                shield.localScale = Vector3.one;

                // A espada deve ter o cabo no punho e a lamina para cima.
                // Seu pivot antigo ficava ~0.42u a frente e a ponta para baixo.
                sword.localPosition = new Vector3(0f, 0.02f, -0.42f);
                sword.localRotation = Quaternion.Euler(90f, 0f, 0f) *
                                      Quaternion.Euler(0f, 180f, 0f);
                sword.localScale = Vector3.one;

                AlignEquipment(prefab.transform, shield, sword);

                EnemyProceduralAnimator animator = prefab.GetComponent<EnemyProceduralAnimator>();
                if (animator == null || animator.enemyKind != EnemyProceduralAnimator.EnemyKind.TrojanSoldier)
                    throw new Exception("Animador do Soldado de Troia nao encontrado.");
                animator.trojanLeftArm = left;
                animator.trojanRightArm = right;
                animator.AutoAssignBones();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                if (saved == null) throw new Exception("Falha ao salvar prefab.");
                Debug.Log("[TROJAN-SETUP] SAVED sword=HAND_R shield=HAND_L prefab=" + PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            AssetDatabase.SaveAssets();
            Validate();
        }

        public static void Validate()
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform left = Find(prefab.transform, "HAND_L");
                Transform right = Find(prefab.transform, "HAND_R");
                Transform shield = Find(prefab.transform, "EscudoTroia");
                Transform sword = Find(prefab.transform, "EspadaTroia");
                if (left == null || right == null || shield == null || sword == null ||
                    sword.parent != right || shield.parent != left)
                    throw new Exception("Espada e escudo nao estao nos ossos corretos.");

                EnemyProceduralAnimator a = prefab.GetComponent<EnemyProceduralAnimator>();
                if (a == null) throw new Exception("Missing EnemyProceduralAnimator");
                if (a.trojanLeftArm != left || a.trojanRightArm != right)
                    throw new Exception("Animated bones do not match equipment mount bones");

                a.ResetAnimationState();
                a.TriggerAttack();
                if (!a.IsAttacking) throw new Exception("Attack trigger not working");
                a.TriggerDefense();
                if (!a.IsDefending) throw new Exception("Defense trigger not working");
                a.ResetAnimationState();
                a.SetDefense(true);
                if (!a.IsDefending) throw new Exception("Hold defense not working");
                a.SetDefense(false);
                if (a.IsDefending) throw new Exception("Released defense remains on");
                a.ResetAnimationState();

                Debug.Log("[TROJAN-VALIDATION] RIGHT sword local="+sword.localPosition+
                    " rotation="+sword.localRotation);
                Debug.Log("[TROJAN-VALIDATION] LEFT shield local="+shield.localPosition+
                    " rotation="+shield.localRotation);
                Debug.Log("[TROJAN-VALIDATION] PASS: sword RIGHT, shield LEFT, attack and defense API.");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        // Orientacao medida pela malha do FBX: o escudo tem espessura
        // no eixo Z da mao, e a espada se estende no eixo Y da mao.
        // Reorientar em relacao ao CORPO, nao em relacao a rotacao do punho.
        public static void AlignEquipment(Transform character, Transform shield, Transform sword)
        {
            Transform left = shield.parent;
            Transform right = sword.parent;

            // Mantem a parte visivel do escudo na frente do tronco.
            Vector3 currentNormal = left.TransformDirection(Vector3.forward);
            Quaternion shieldCorrection = Quaternion.FromToRotation(
                currentNormal, character.forward);
            shield.rotation = shieldCorrection * shield.rotation;

            Renderer[] renderers = shield.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new Exception("Escudo sem MeshRenderer.");
            Bounds shieldBounds = renderers[0].bounds;
            for (int i=1; i<renderers.Length; i++)
                shieldBounds.Encapsulate(renderers[i].bounds);
            Vector3 targetShieldCenter = left.position +
                character.up * 0.14f + character.forward * 0.20f;
            shield.position += targetShieldCenter - shieldBounds.center;

            // Evita que a espada aponte para as costas por causa dos
            // eixos locais diferentes do bone HAND_R.
            Vector3 desiredBlade = (character.up * 0.88f +
                character.forward * 0.35f).normalized;
            Vector3 currentBlade = right.TransformDirection(Vector3.up);
            Vector3 desiredGrip = right.TransformPoint(new Vector3(0f, -0.06f, 0f));
            Vector3 swordLocalGrip = sword.InverseTransformPoint(desiredGrip);
            Quaternion swordCorrection = Quaternion.FromToRotation(
                currentBlade, desiredBlade);
            sword.rotation = swordCorrection * sword.rotation;
            // Rotacionar em torno do cabo, mantendo o punho preso na mao.
            sword.position += desiredGrip - sword.TransformPoint(swordLocalGrip);
        }

        private static Transform EnsureEquipment(Transform root, Transform parent,
            string name, string path)
        {
            Transform mount=Find(root,name);
            if (mount == null)
            {
                GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source == null) throw new Exception("Prefab ausente: "+path);
                GameObject instance=PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (instance == null) throw new Exception("Falha ao instanciar: "+path);
                mount=instance.transform;
                mount.name=name;
            }
            if (mount.parent != parent)
                mount.SetParent(parent,false);
            return mount;
        }

        private static Transform Find(Transform parent, string n)
        {
            foreach (Transform tr in parent.GetComponentsInChildren<Transform>(true))
                if (string.Equals(tr.name,n,StringComparison.OrdinalIgnoreCase))
                    return tr;
            return null;
        }
    }
}
#endif
