#if UNITY_EDITOR
using FinalSpam.Enemies;
using UnityEditor;
using UnityEngine;

namespace FinalSpam.EditorTools
{
    [CustomEditor(typeof(EnemyProceduralAnimator))]
    [CanEditMultipleObjects]
    public sealed class EnemyProceduralAnimatorEditor : UnityEditor.Editor
    {
        private static readonly string[] AxisNames =
        {
            "X positivo (+)", "X negativo (-)",
            "Y positivo (+)", "Y negativo (-)",
            "Z positivo (+)", "Z negativo (-)"
        };
        private static readonly string[] SpaceNames =
        {
            "Personagem (frente / lado / cima)",
            "Osso HAND (local, FBX)",
            "Pai do osso (local)"
        };
        private static readonly string[] HiddenProperties =
        {
            "m_Script",
            "trojanSwordAxis", "trojanSwordAxisSpace",
            "trojanShieldAxis", "trojanShieldAxisSpace",
            "trojanSecondaryAxisWeight",
            "trojanSwordAttackArc", "trojanShieldTransitionTime",
            "trojanShieldReachPhase", "trojanShieldCoverYaw",
            "trojanShieldForwardReachAngle", "trojanShieldCoverRoll",
            "trojanShieldForwardPush", "trojanShieldRightPush",
            "trojanShieldCoverLift", "trojanShieldCoverInward",
            "ddosMuzzleLeft", "ddosMuzzleRight", "ddosMiniNukeDropPoint",
            "previewTrojanAxes", "previewSwordAxisAngle", "previewShieldAxisAngle"
        };

        public override void OnInspectorGUI()
        {
            EnemyProceduralAnimator active = target as EnemyProceduralAnimator;
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("FinalSpam | Animacoes dos Inimigos", EditorStyles.boldLabel);

            if (active != null && active.enemyKind ==
                EnemyProceduralAnimator.EnemyKind.TrojanSoldier)
            {
                DrawTrojanAxisPanel();
            }
            else if (active != null && active.enemyKind ==
                EnemyProceduralAnimator.EnemyKind.DdosPlane)
            {
                DrawDdosHardpointsPanel();
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Testes de animacao", EditorStyles.boldLabel);
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox(
                        "Defina os eixos acima e entre no Play para testar. " +
                        "Em Play, habilite PREVIA MANUAL para descobrir o eixo correto.",
                        MessageType.Info);
                }
                else
                {
                    bool repeated = active != null && active.previewRepeatedAttacks;
                    if (active == null || active.enemyKind !=
                        EnemyProceduralAnimator.EnemyKind.DdosPlane)
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("ATACAR AGORA", GUILayout.Height(30)))
                        {
                            ApplyToSelected(a =>
                            {
                                a.previewTrojanAxes = false;
                                a.previewRepeatedAttacks = false;
                                a.TriggerAttack();
                            });
                        }
                        if (GUILayout.Button(
                            repeated ? "PARAR REPETICAO" : "REPETIR ATAQUES",
                            GUILayout.Height(30)))
                        {
                            ApplyToSelected(a =>
                            {
                                a.previewTrojanAxes = false;
                                a.previewRepeatedAttacks = !repeated;
                                if (a.previewRepeatedAttacks) a.TriggerAttack();
                            });
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("RECEBER DANO"))
                            ApplyToSelected(a => a.TriggerHit());
                        if (GUILayout.Button("TESTAR MORTE"))
                            ApplyToSelected(a => a.TriggerDeath());
                        if (GUILayout.Button("RESTAURAR"))
                            ApplyToSelected(a =>
                            {
                                a.previewTrojanAxes = false;
                                a.ResetAnimationState();
                            });
                    }
                    if (active != null && active.enemyKind ==
                        EnemyProceduralAnimator.EnemyKind.TrojanSoldier)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("DEFENDER AGORA", GUILayout.Height(28)))
                                ApplyToSelected(a =>
                                {
                                    a.previewTrojanAxes = false;
                                    a.TriggerDefense();
                                });
                            if (GUILayout.Button(
                                active.previewHoldDefense ? "BAIXAR ESCUDO" : "MANTER ESCUDO",
                                GUILayout.Height(28)))
                            {
                                bool current = active.previewHoldDefense;
                                ApplyToSelected(a =>
                                {
                                    a.previewTrojanAxes = false;
                                    a.previewHoldDefense = !current;
                                });
                            }
                        }
                    }
                    if (GUILayout.Button(
                        active != null && active.previewWalking ?
                        "PARAR CAMINHADA DE TESTE" : "SIMULAR CAMINHADA"))
                    {
                        bool walking = active != null && active.previewWalking;
                        ApplyToSelected(a => a.previewWalking = !walking);
                    }
                    EditorGUILayout.HelpBox(
                        active != null && active.enemyKind ==
                            EnemyProceduralAnimator.EnemyKind.DdosPlane
                            ? "Aviao DDoS: sem animacao corporal de ataque. Os dois " +
                                "muzzles e o drop point sao marcadores para a futura IA " +
                                "de tiro / Mini Nuke. Nenhum projetil e disparado ainda."
                            : "Ataque/defesa sao animacoes visuais. Ainda nao causam nem bloqueiam dano.",
                        MessageType.None);
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Configuracoes avancadas", EditorStyles.boldLabel);
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, HiddenProperties);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDdosHardpointsPanel()
        {
            serializedObject.Update();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("AVIAO DDOS | TORRETAS E MINI NUKE",
                    EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Os pontos sao filhos do corpo do aviao e acompanham voo, " +
                    "inclinacao e altura. Selecione o prefab e ligue Gizmos " +
                    "na Scene para ver as setas azuis (Z = direcao inicial). " +
                    "Arraste os Transforms na hierarquia para corrigir posicao.",
                    MessageType.Info);
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("ddosMuzzleLeft"),
                    new GUIContent("Muzzle | Torretinha esquerda"));
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("ddosMuzzleRight"),
                    new GUIContent("Muzzle | Torretinha direita"));
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("ddosMiniNukeDropPoint"),
                    new GUIContent("Drop Point | Mini Nuke"));
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTrojanAxisPanel()
        {
            serializedObject.Update();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("SOLDADO DE TROIA | EIXOS DOS BRACOS",
                    EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Escolha o eixo e o referencial de CADA mao. " +
                    "X/Y/Z positivo ou negativo mudam de verdade o movimento. " +
                    "Nao altera o encaixe ou a posicao da espada e do escudo.",
                    MessageType.None);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("MAO DIREITA | ESPADA (HAND_R)",
                    EditorStyles.boldLabel);
                DrawAxisLine("Eixo da espada", "trojanSwordAxis");
                DrawSpaceLine("Referencia da espada", "trojanSwordAxisSpace");
                EditorGUILayout.Slider(
                    serializedObject.FindProperty("trojanSwordAttackArc"),
                    0.20f, 1f,
                    new GUIContent("Amplitude do golpe",
                        "0,50 = metade da rotacao anterior. Ajusta so o ataque, nao a previa manual."));

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("MAO ESQUERDA | ESCUDO (HAND_L)",
                    EditorStyles.boldLabel);
                DrawAxisLine("Eixo do escudo", "trojanShieldAxis");
                DrawSpaceLine("Referencia do escudo", "trojanShieldAxisSpace");
                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("Defesa em duas etapas", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(
                    "Escudo independente da espada. A protecao usa SOMENTE o eixo " +
                    "configurado para HAND_L, com avancar e centralizar suaves.",
                    MessageType.None);
                EditorGUILayout.Slider(
                    serializedObject.FindProperty("trojanShieldTransitionTime"),
                    0.18f, 1.50f,
                    new GUIContent("Tempo de protecao",
                        "Duracao da elevacao e fechamento do escudo."));
                EditorGUILayout.Slider(
                    serializedObject.FindProperty("trojanShieldReachPhase"),
                    0.20f, 0.80f,
                    new GUIContent("Etapa 1: avancar",
                        "Fracao inicial destinada a avancar o escudo. Depois ele fecha sobre o peito."));
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("AJUSTE FINO | DISTANCIA DO BRACO",
                    EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Valores em centimetros (cm). Clique no numero do slider " +
                    "para digitar um valor exato. Cada distancia e independente " +
                    "e pode ser alterada durante Play.",
                    MessageType.Info);
                DrawCentimeterSlider(
                    "Quanto vai para FRENTE (cm)",
                    "trojanShieldForwardPush",
                    "Primeira etapa: alcanca a frente do corpo. 12 cm padrao.");
                DrawCentimeterSlider(
                    "Quanto vai para DIREITA (cm)",
                    "trojanShieldRightPush",
                    "Segunda etapa: desliza para a direita, sem subir. 7,5 cm padrao.");

                EditorGUILayout.Space(4f);
                SerializedProperty secondary =
                    serializedObject.FindProperty("trojanSecondaryAxisWeight");
                EditorGUILayout.Slider(secondary, 0f, 1f,
                    new GUIContent("Movimento diagonal extra",
                        "Use 0 enquanto procura o eixo certo. 1 ativa os giros adicionais Y/Z."));
                if (Application.isPlaying)
                {
                    EditorGUILayout.Space(5f);
                    SerializedProperty manual =
                        serializedObject.FindProperty("previewTrojanAxes");
                    EditorGUILayout.PropertyField(manual,
                        new GUIContent("ATIVAR PREVIA MANUAL",
                            "Controlar as duas maos com os sliders sem atacar nem defender."));
                    if (manual.boolValue)
                    {
                        EditorGUILayout.Slider(
                            serializedObject.FindProperty("previewSwordAxisAngle"),
                            -180f, 180f, new GUIContent("Angulo da espada"));
                        DrawTestAngles("previewSwordAxisAngle", "Espada");

                        EditorGUILayout.Slider(
                            serializedObject.FindProperty("previewShieldAxisAngle"),
                            -180f, 180f, new GUIContent("Angulo do escudo"));
                        DrawTestAngles("previewShieldAxisAngle", "Escudo");
                        EditorGUILayout.HelpBox(
                            "Mova o slider ou experimente +60 / -60. Alterne X, Y e Z " +
                            "e a referencia ate achar a direcao desejada. " +
                            "Desative a previa para testar os golpes normais.",
                            MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Pressione Play para testar os eixos com sliders de -180 a +180 graus.",
                        MessageType.Info);
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("GRAVAR EIXOS E ANIMACOES NO PREFAB DO SOLDADO",
                GUILayout.Height(25)))
            {
                SaveAxesToPrefab((EnemyProceduralAnimator)target);
            }
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Alteracoes feitas durante Play normalmente somem ao sair. " +
                    "Use GRAVAR EIXOS E ANIMACOES NO PREFAB antes de parar o jogo.",
                    MessageType.Warning);
            }
        }

        // Centimetros no Inspector; metros no componente para o Unity.
        // O EditorGUILayout.Slider tambem permite digitar o valor exato.
        private void DrawCentimeterSlider(string label, string propertyName,
            string tooltip)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            float amountCm = EditorGUILayout.Slider(
                new GUIContent(label, tooltip),
                prop.floatValue * 100f, 0f, 50f);
            prop.floatValue = amountCm / 100f;
        }

        private void DrawAxisLine(string label, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.enumValueIndex = EditorGUILayout.Popup(
                new GUIContent(label), property.enumValueIndex, AxisNames);
        }

        private void DrawSpaceLine(string label, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.enumValueIndex = EditorGUILayout.Popup(
                new GUIContent(label), property.enumValueIndex, SpaceNames);
        }

        private void DrawTestAngles(string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(label + " -60"))
                    property.floatValue = -60f;
                if (GUILayout.Button(label + " 0"))
                    property.floatValue = 0f;
                if (GUILayout.Button(label + " +60"))
                    property.floatValue = 60f;
            }
        }

        private static void SaveAxesToPrefab(EnemyProceduralAnimator active)
        {
            if (active == null) return;
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(
                active.gameObject);
            string path = source != null ? AssetDatabase.GetAssetPath(source) : "";
            if (string.IsNullOrEmpty(path))
                path = AssetDatabase.GetAssetPath(active.gameObject);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
            {
                EditorUtility.DisplayDialog("FinalSpam",
                    "Selecione uma instancia do SoldadoTroia vinculada a um prefab.", "OK");
                return;
            }

            GameObject asset = null;
            try
            {
                asset = PrefabUtility.LoadPrefabContents(path);
                EnemyProceduralAnimator saved = asset.GetComponent<EnemyProceduralAnimator>();
                if (saved == null) throw new System.Exception("Prefab sem animador.");

                saved.trojanSwordAxis = active.trojanSwordAxis;
                saved.trojanSwordAxisSpace = active.trojanSwordAxisSpace;
                saved.trojanShieldAxis = active.trojanShieldAxis;
                saved.trojanShieldAxisSpace = active.trojanShieldAxisSpace;
                saved.trojanSecondaryAxisWeight = active.trojanSecondaryAxisWeight;
                saved.trojanSwordAttackArc = active.trojanSwordAttackArc;
                saved.trojanShieldTransitionTime = active.trojanShieldTransitionTime;
                saved.trojanShieldReachPhase = active.trojanShieldReachPhase;
                saved.trojanShieldForwardPush = active.trojanShieldForwardPush;
                saved.trojanShieldRightPush = active.trojanShieldRightPush;

                PrefabUtility.SaveAsPrefabAsset(asset, path);
                AssetDatabase.SaveAssets();
                Debug.Log("[FinalSpam] Eixos salvos no prefab: " + path);
                EditorUtility.DisplayDialog("FinalSpam",
                    "Eixos de espada e escudo gravados no prefab! " +
                    "Os ajustes continuam depois de sair do Play.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("FinalSpam",
                    "Nao foi possivel gravar os eixos: " + e.Message, "OK");
            }
            finally
            {
                if (asset != null) PrefabUtility.UnloadPrefabContents(asset);
            }
        }

        private void ApplyToSelected(System.Action<EnemyProceduralAnimator> action)
        {
            foreach (Object item in targets)
            {
                EnemyProceduralAnimator animator = item as EnemyProceduralAnimator;
                if (animator == null || !animator.gameObject.scene.IsValid()) continue;
                action(animator);
            }
            Repaint();
        }
    }
}
#endif
