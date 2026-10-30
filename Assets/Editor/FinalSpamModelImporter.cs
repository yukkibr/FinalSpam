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
    /// Rebuilds the prototype from the modeler's new textured FBX files.
    /// Safe to run from menu or Unity batchmode with -executeMethod.
    /// The script never deletes FBX/PNG sources or the preserved environment.
    /// </summary>
    public static class FinalSpamModelImporter
    {
        private const string ScenePath = "Assets/Scenes/FinalSpam - Modelos Atualizados.unity";
        private const string Models = "Assets/Models/";
        private const string Materials = "Assets/Materials/";
        private const string EnemyPrefabs = "Assets/Prefabs/Enemies/";
        private const string WeaponPrefabs = "Assets/Prefabs/Weapons/";
        private const string ProjectilePrefabs = "Assets/Prefabs/Projectiles/";
        private const string PropsPrefabs = "Assets/Prefabs/Props/";
        private const string PlayerPrefabs = "Assets/Prefabs/Characters/";
        private const string GroupName = "FinalSpam | Modelos Atualizados";

        [MenuItem("FinalSpam/Modelos/Reimportar e atualizar cena", priority=200)]
        public static void ImportAll()
        {
            try
            {
                if (!Application.isBatchMode &&
                    !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                Debug.Log("[FinalSpam-Import] START");
                EnsureDirectories();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                // Configure texture importer for non-color PBR maps.
                foreach (string key in new[] { "Escudo_de_Troia", "Espada_de_troia",
                    "PistolaHandyMan", "Soldado_De_Troia" })
                {
                    ConfigureLinearMap(Materials + key + "/MetallicSmoothness.png");
                }

                Material playerMat = CreateMaterial("Player", "Player", "Player Color.png");
                Material soldierMat = CreateMaterial("Soldado_De_Troia", "Trojan",
                    "CORPO Color.png", null, "CORPO Emission Color.png");
                Material airplaneMat = CreateMaterial("Aviao_DDOS", "DdosPlane",
                    "Plane Painting Color.png");
                Material swordMat = CreateMaterial("Espada_de_troia", "TrojanSword",
                    "ESPADA Color.png", "MetallicSmoothness.png");
                Material shieldMat = CreateMaterial("Escudo_de_Troia", "TrojanShield",
                    "ESCUDO Color.png", "MetallicSmoothness.png");
                Material gunMat = CreateMaterial("PistolaHandyMan", "HandyManPistol",
                    "Pistola.001 Color.png", "MetallicSmoothness.png",
                    "Pistola.001 Emission Color.png");
                Material nukeMat = CreateMaterial("MiniNuke", "MiniNuke",
                    "Material Color.png", null, "Material Emission Color.png");
                Material bulletMat = CreateNeutralMetal("ProjectileMetal");

                CreateSimple("Espada_de_troia.fbx", WeaponPrefabs + "EspadaTroia.prefab", swordMat);
                CreateSimple("Escudo_de_troia.fbx", WeaponPrefabs + "EscudoTroia.prefab", shieldMat);
                CreateSimple("Bala.fbx", ProjectilePrefabs + "Bala.prefab", bulletMat);
                CreateSimple("Capsula.fbx", ProjectilePrefabs + "Capsula.prefab", bulletMat);
                CreateSimple("PistolaHandyMan.fbx", WeaponPrefabs + "PistolaHandyMan.prefab", gunMat);
                CreateMiniNuke(nukeMat);
                CreatePlayer(playerMat);
                CreateEnemy("Soldado_de_troia.fbx", EnemyPrefabs + "SoldadoTroia.prefab",
                    "Soldado de troia", soldierMat, EnemyProceduralAnimator.EnemyKind.TrojanSoldier);
                CreateEnemy("Aviao_DDOS_TCC.fbx", EnemyPrefabs + "AviaoDDOS.prefab",
                    "AVIAO DDOS", airplaneMat, EnemyProceduralAnimator.EnemyKind.DdosPlane);

                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                RemoveMissingOldModels(scene);
                GameObject group = new GameObject(GroupName);
                SceneManager.MoveGameObjectToScene(group, scene);

                PlacePrefab(PlayerPrefabs + "Player.prefab", group.transform, scene, "Player",
                    new Vector3(629.59f, 146.5f, 207.9f), Quaternion.Euler(0f, 180f, 0f));
                PlacePrefab(EnemyPrefabs + "SoldadoTroia.prefab", group.transform, scene,
                    "Soldado de troia", new Vector3(625.00256f, 144.66f, 200.67404f),
                    Quaternion.identity);
                PlacePrefab(EnemyPrefabs + "AviaoDDOS.prefab", group.transform, scene,
                    "AVIAO DDOS", new Vector3(618.6285f, 144.71823f, 206.1077f),
                    Quaternion.identity);
            // MiniNuke agora sai do DropPoint do DDoS: nao criar decoracao estatica.
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException("Failed to save scene " + ScenePath);

                AssetDatabase.SaveAssets();
                Debug.Log("[FinalSpam-Import] COMPLETE: 9 FBX, 8 gameplay prefabs + "
                    + "1 player prefab, 4 scene instances. Missing from pack: VirusTorre, MulherV1.");
            }
            catch (Exception e)
            {
                Debug.LogError("[FinalSpam-Import] FAILED: " + e);
                throw;
            }
        }

        private static void EnsureDirectories()
        {
            MakeFolder("Assets/Materials");
            MakeFolder("Assets/Prefabs/Weapons");
            MakeFolder("Assets/Prefabs/Projectiles");
            MakeFolder("Assets/Prefabs/Props");
            MakeFolder("Assets/Prefabs/Characters");
            MakeFolder("Assets/Prefabs/Enemies");
        }

        private static void MakeFolder(string path)
        {
            string[] parts = path.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(parent, parts[i]);
                parent = next;
            }
        }

        private static void ConfigureLinearMap(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || !importer.sRGBTexture) return;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        private static Material CreateMaterial(string folder, string name, string color,
            string metallicGloss=null, string emission=null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new Exception("URP/Lit shader not available.");
            string path = Materials + folder + "/FS_" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;

            mat.SetColor("_BaseColor", Color.white);
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(Materials + folder + "/" + color);
            if (albedo == null) throw new FileNotFoundException("Color texture: " + folder + "/" + color);
            mat.SetTexture("_BaseMap", albedo);

            Texture2D packed = metallicGloss == null ? null :
                AssetDatabase.LoadAssetAtPath<Texture2D>(Materials + folder + "/" + metallicGloss);
            if (packed != null)
            {
                mat.SetTexture("_MetallicGlossMap", packed);
                mat.SetFloat("_Metallic", 1f);
                mat.SetFloat("_Smoothness", 1f);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                mat.SetTexture("_MetallicGlossMap", null);
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Smoothness", 0.45f);
                mat.DisableKeyword("_METALLICSPECGLOSSMAP");
            }

            Texture2D emissionTex = emission == null ? null :
                AssetDatabase.LoadAssetAtPath<Texture2D>(Materials + folder + "/" + emission);
            if (emissionTex != null)
            {
                mat.SetTexture("_EmissionMap", emissionTex);
                mat.SetColor("_EmissionColor", Color.white * 0.9f);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.SetTexture("_EmissionMap", null);
                mat.SetColor("_EmissionColor", Color.black);
                mat.DisableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateNeutralMetal(string name)
        {
            string path = "Assets/Materials/FS_" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", new Color(0.72f, 0.65f, 0.48f, 1f));
            m.SetFloat("_Metallic", 0.75f);
            m.SetFloat("_Smoothness", 0.75f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static GameObject AddModel(GameObject parent, string fbx, Material mat)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Models + fbx);
            if (source == null) throw new FileNotFoundException("FBX missing: " + Models + fbx);

            GameObject visual = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (visual == null) throw new Exception("Cannot instantiate " + fbx);
            visual.name = "Visual_" + Path.GetFileNameWithoutExtension(fbx);
            visual.transform.SetParent(parent.transform, false);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i=0; i < slots.Length; i++) slots[i] = mat;
                if (slots.Length == 0) slots = new Material[] { mat };
                renderer.sharedMaterials = slots;
            }
            return visual;
        }

        private static Transform FindBone(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                int colon = n.LastIndexOf(':');
                if (colon >= 0) n = n.Substring(colon + 1);
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                    return t;
            }
            return null;
        }

        private static GameObject StartPrefab(string name)
        {
            return new GameObject(name);
        }

        private static void SavePrefab(GameObject root, string path)
        {
            try
            {
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new IOException("Unable to save prefab: " + path);
                Debug.Log("[FinalSpam-Import] Prefab: " + path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CreateSimple(string fbx, string path, Material mat)
        {
            GameObject root = StartPrefab(Path.GetFileNameWithoutExtension(path));
            GameObject visual = AddModel(root, fbx, mat);
            if (fbx == "PistolaHandyMan.fbx")
                visual.transform.localPosition = new Vector3(0f, -0.57f, 0f);
            SavePrefab(root, path);
        }

        private static void CreateMiniNuke(Material mat)
        {
            GameObject root = StartPrefab("MiniNuke");
            AddModel(root, "MiniNuke.fbx", mat);
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.radius = 0.42f;
            collider.center = new Vector3(0, 0.42f, 0);
            SavePrefab(root, PropsPrefabs + "MiniNuke.prefab");
        }

        private static void CreatePlayer(Material mat)
        {
            GameObject root = StartPrefab("Player");
            GameObject visual = AddModel(root, "Player.fbx", mat);

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.32f; cc.center = Vector3.up;
            ThirdPersonController controller = root.AddComponent<ThirdPersonController>();
            ProceduralCharacterAnimator characterAnimator = root.AddComponent<ProceduralCharacterAnimator>();
            characterAnimator.armatureRoot = FindBone(visual.transform, "Armature");

            GameObject gunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabs + "PistolaHandyMan.prefab");
            if (gunPrefab == null) throw new FileNotFoundException("HandyMan pistol prefab missing");
            GameObject gun = PrefabUtility.InstantiatePrefab(gunPrefab) as GameObject;
            Transform hand = FindBone(visual.transform, "HAND.R");
            if (hand == null) hand = FindBone(visual.transform, "FOREARM.R");
            if (hand == null) throw new Exception("Player FBX has no HAND.R or FOREARM.R");
            gun.transform.SetParent(hand, false);
            gun.transform.localPosition = new Vector3(0.014f, 0.0f, 0.03f);
            gun.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
            gun.transform.localScale = Vector3.one;

            RaycastPistol pistol = gun.AddComponent<RaycastPistol>();
            pistol.playerController = controller;
            pistol.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabs + "Bala.prefab");
            pistol.casingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabs + "Capsula.prefab");
            pistol.bulletVisualSpeed = 75f;
            pistol.bulletTrailTime = 0.17f;
            pistol.bulletTrailStartWidth = 0.042f;
            pistol.bulletTrailEndWidth = 0.003f;
            pistol.bulletVisualScale = 1.25f;
            pistol.muzzlePoint = MakePoint(gun.transform, "MuzzlePoint", new Vector3(0f, 0.19f, 0.41f));
            pistol.casingEjectionPoint = MakePoint(gun.transform, "CasingEjectionPoint", new Vector3(0.05f, 0.19f, 0.13f));
            characterAnimator.pistol = pistol;
            SavePrefab(root, PlayerPrefabs + "Player.prefab");
        }

        private static Transform MakePoint(Transform parent, string name, Vector3 localPosition)
        {
            GameObject point = new GameObject(name);
            point.transform.SetParent(parent, false);
            point.transform.localPosition = localPosition;
            return point.transform;
        }

        private static void CreateEnemy(string fbx, string path, string name, Material mat,
            EnemyProceduralAnimator.EnemyKind kind)
        {
            GameObject root = StartPrefab(name);
            GameObject visual = AddModel(root, fbx, mat);
            EnemyProceduralAnimator anim = root.AddComponent<EnemyProceduralAnimator>();
            anim.enemyKind = kind;
            anim.modelRoot = visual.transform;
            anim.turbineSpinAxis = EnemyProceduralAnimator.BoneAxis.Y;
            anim.AutoAssignBones();
            RaycastDamageable health = root.AddComponent<RaycastDamageable>();
            health.maxHealth = kind == EnemyProceduralAnimator.EnemyKind.DdosPlane ? 120f : 100f;

            if (kind == EnemyProceduralAnimator.EnemyKind.DdosPlane)
            {
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.4f, 0f);
                collider.size = new Vector3(2.2f, 1.1f, 2.2f);
            }
            else
            {
                CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
                collider.height = 1.9f; collider.radius = 0.4f;
                collider.center = new Vector3(0f, 0.95f, 0f);

                AttachEquipment(visual.transform, "HAND_L",
                    WeaponPrefabs + "EscudoTroia.prefab", new Vector3(0f, 0.44f, 0.22f),
                    Quaternion.Euler(180f, 0f, 0f));
                AttachEquipment(visual.transform, "HAND_R",
                    WeaponPrefabs + "EspadaTroia.prefab", new Vector3(0f, 0.02f, -0.42f),
                    Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f));

                Transform shieldMount = FindBone(visual.transform, "HAND_L");
                Transform swordMount = FindBone(visual.transform, "HAND_R");
                if (shieldMount != null && swordMount != null &&
                    shieldMount.Find("EscudoTroia") != null &&
                    swordMount.Find("EspadaTroia") != null)
                {
                    TrojanEquipmentSetup.AlignEquipment(root.transform,
                        shieldMount.Find("EscudoTroia"),
                        swordMount.Find("EspadaTroia"));
                }
            }

            SavePrefab(root, path);
        }

        private static void AttachEquipment(Transform visual, string bone, string path,
            Vector3 offset, Quaternion rotation)
        {
            Transform mount = FindBone(visual, bone);
            if (mount == null)
            {
                Debug.LogWarning("[FinalSpam-Import] Bone not found: " + bone);
                return;
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException(path);
            GameObject equipment = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            equipment.transform.SetParent(mount, false);
            equipment.transform.localPosition = offset;
            equipment.transform.localRotation = rotation;
            equipment.transform.localScale = Vector3.one;
        }

        private static void RemoveMissingOldModels(Scene scene)
        {
            string[] oldNames = { "HomemV1", "MulherV1", "Soldado de troia",
                "AVIAO DDOS", "VirusTorre", "Player", GroupName };
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                foreach (string name in oldNames)
                {
                    if (go.name == name)
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                        Debug.Log("[FinalSpam-Import] Replaced old scene object: " + name);
                        break;
                    }
                }
            }
        }

        private static void PlacePrefab(string prefabPath, Transform parent, Scene scene,
            string name, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new FileNotFoundException(prefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null) throw new Exception("Failed to instantiate: " + prefabPath);
            instance.name = name;
            instance.transform.SetParent(parent, true);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            Debug.Log("[FinalSpam-Import] Scene object: " + name + " @ " + position);
        }
    }
}
#endif
