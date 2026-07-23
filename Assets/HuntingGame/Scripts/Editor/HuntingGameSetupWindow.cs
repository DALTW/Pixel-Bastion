using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game3.Hunting.Editor
{
    public sealed class HuntingGameSetupWindow : EditorWindow
    {
        private static readonly string[] Tabs = { "Art 에셋", "Balance" };

        private HuntingGameConfig config;
        private SerializedObject serializedConfig;
        private Vector2 scroll;
        private int selectedTab;

        public static void Open()
        {
            var window = GetWindow<HuntingGameSetupWindow>("Hunting Game Setup");
            window.minSize = new Vector2(620f, 620f);
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            RefreshConfig();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("GAME-3 · Sunnyside 포획 게임 설정", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play 전에 캐릭터·동물·월드 에셋과 밸런스를 수정할 수 있습니다. " +
                "Rebuild Scene은 현재 설정을 사용하며 값을 덮어쓰지 않습니다.",
                MessageType.Info);

            var selected = (HuntingGameConfig)EditorGUILayout.ObjectField(
                "Game Config", config, typeof(HuntingGameConfig), false);
            if (selected != config)
            {
                config = selected;
                serializedConfig = config != null ? new SerializedObject(config) : null;
            }

            if (serializedConfig == null)
            {
                if (GUILayout.Button("Config 생성"))
                {
                    RefreshConfig();
                }

                return;
            }

            serializedConfig.Update();
            selectedTab = GUILayout.Toolbar(selectedTab, Tabs);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (selectedTab == 0)
            {
                DrawArtTab();
            }
            else
            {
                DrawBalanceTab();
            }

            EditorGUILayout.EndScrollView();
            serializedConfig.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            DrawValidation();
            DrawActions();
        }

        private void DrawArtTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("플레이어 애니메이션", EditorStyles.boldLabel);
            Property("playerIdleSprites", true);
            Property("playerWalkSprites", true);
            Property("playerAttackSprites", true);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("동물과 동료", EditorStyles.boldLabel);
            Property("animals", true);
            Property("dogs", true);
            Property("populations", true);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Sunnyside 월드", EditorStyles.boldLabel);
            Property("worldTileset");
            Property("groundTileRects", true);
            Property("groundSprites", true);
            Property("environmentSprites", true);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("전리품 UI 아이콘", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawSpritePreview("meatIcon", "고기");
            DrawSpritePreview("hideIcon", "가죽");
            DrawSpritePreview("woolIcon", "털");
            DrawSpritePreview("featherIcon", "깃털");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBalanceTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("경제", EditorStyles.boldLabel);
            Property("startingMoney");
            Property("inventoryCapacity");
            Property("lootPrices", true);
            Property("upgrades", true);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("사냥꾼", EditorStyles.boldLabel);
            Property("baseMoveSpeed");
            Property("baseMaxHealth");
            Property("baseSubduePower");
            Property("attackRange");
            Property("attackArc");
            Property("attackCooldown");
            Property("attackDuration");
            Property("attackHitDelay");
            Property("harvestDuration");

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("월드", EditorStyles.boldLabel);
            Property("worldSize");
            Property("campPosition");
            Property("campSafeRadius");
            Property("populations", true);
        }

        private void DrawValidation()
        {
            var errors = HuntingGameBuilder.ValidateConfiguration(config);
            if (errors.Count == 0)
            {
                EditorGUILayout.HelpBox("설정 검증 통과 · Play 및 빌드 준비 완료", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "설정 오류 " + errors.Count + "개\n• " + string.Join("\n• ", errors),
                MessageType.Error);
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Sunnyside Defaults", GUILayout.Height(34f)))
            {
                Execute(() =>
                {
                    HuntingGameBuilder.LoadSunnysideDefaultsAndBuild();
                    RefreshConfig();
                });
            }

            if (GUILayout.Button("Apply Import Settings", GUILayout.Height(34f)))
            {
                Execute(HuntingGameBuilder.ApplyImportSettings);
            }

            if (GUILayout.Button("Validate", GUILayout.Height(34f)))
            {
                var errors = HuntingGameBuilder.ValidateConfiguration(config);
                EditorUtility.DisplayDialog(
                    "Hunting Game Validation",
                    errors.Count == 0 ? "설정 검증을 통과했습니다." : string.Join("\n", errors),
                    "확인");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = HuntingGameBuilder.ValidateConfiguration(config).Count == 0;
            if (GUILayout.Button("Rebuild Scene", GUILayout.Height(34f)))
            {
                Execute(HuntingGameBuilder.RebuildFromCurrentConfig);
            }

            if (GUILayout.Button("Open Scene", GUILayout.Height(34f)))
            {
                Execute(() => EditorSceneManager.OpenScene(HuntingGameBuilder.ScenePath));
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSpritePreview(string propertyName, string label)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(132f));
            var property = serializedConfig.FindProperty(propertyName);
            EditorGUILayout.PropertyField(property, new GUIContent(label));
            var sprite = property.objectReferenceValue as Sprite;
            var preview = sprite != null ? AssetPreview.GetAssetPreview(sprite) : null;
            var rect = GUILayoutUtility.GetRect(96f, 96f, GUILayout.Width(120f));
            if (preview != null)
            {
                GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
            }
            else
            {
                EditorGUI.HelpBox(rect, "미리보기 없음", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void Property(string name, bool includeChildren = false)
        {
            var property = serializedConfig.FindProperty(name);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, includeChildren);
            }
        }

        private void RefreshConfig()
        {
            config = HuntingGameBuilder.GetOrCreateConfig();
            serializedConfig = config != null ? new SerializedObject(config) : null;
            Repaint();
        }

        private static void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("GAME-3 설정 오류", exception.Message, "확인");
            }
        }
    }

    [InitializeOnLoad]
    internal static class HuntingGamePlayPreflight
    {
        static HuntingGamePlayPreflight()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode || Application.isBatchMode)
            {
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<HuntingGameConfig>(HuntingGameBuilder.ConfigPath);
            var errors = HuntingGameBuilder.ValidateConfiguration(config);
            if (errors.Count == 0)
            {
                return;
            }

            EditorApplication.isPlaying = false;
            HuntingGameSetupWindow.Open();
            EditorUtility.DisplayDialog(
                "Hunting Game 설정 필요",
                "Play 전에 다음 설정을 수정하세요:\n\n" + string.Join("\n", errors),
                "설정 창 열기");
        }
    }
}
