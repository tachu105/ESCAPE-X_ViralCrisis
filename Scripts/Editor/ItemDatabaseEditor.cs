using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SFEscape.Runtime.InteractableObjects;

namespace SFEscape.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// ItemDatabaseのカスタムエディタ。
    /// ItemType enumの動的な編集と生成を可能にする。
    /// </summary>
    [CustomEditor(typeof(ItemDatabase))]
    public class ItemDatabaseEditor : UnityEditor.Editor
    {
        private string newEnumValue = "";
        private bool showEnumEditor = false;
        private Vector2 enumScrollPosition;
        private const string ENUM_FILE_PATH = "Assets/Scripts/Generated/ItemType.cs";
        private List<string> currentEnumValues = new List<string>();
        private bool isDirty = false;
        private int selectedIndex = -1;

        private void OnEnable()
        {
            LoadCurrentEnumValues();
        }

        public override void OnInspectorGUI()
        {
            ItemDatabase itemDB = (ItemDatabase)target;

            // 通常のインスペクター表示
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Enum編集セクション
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                showEnumEditor = EditorGUILayout.Foldout(showEnumEditor, "ItemType Enum Editor", true);

                if (showEnumEditor)
                {
                    // 新しいenum値の追加
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        newEnumValue = EditorGUILayout.TextField("New Item Type", newEnumValue);
                        GUI.enabled = !string.IsNullOrEmpty(newEnumValue) && !currentEnumValues.Contains(newEnumValue);
                        if (GUILayout.Button("Add", GUILayout.Width(100)))
                        {
                            if (!string.IsNullOrEmpty(newEnumValue))
                            {
                                currentEnumValues.Add(newEnumValue);
                                newEnumValue = "";
                                isDirty = true;
                            }
                        }
                        GUI.enabled = true;
                    }

                    EditorGUILayout.Space(5);

                    // 既存のenum値の表示と順序変更
                    EditorGUILayout.LabelField("Enum Values (Drag to reorder):", EditorStyles.boldLabel);

                    // ScrollViewScopeを使用してスクロール可能なリストを表示
                    using (var scrollView = new EditorGUILayout.ScrollViewScope(enumScrollPosition, GUILayout.Height(200)))
                    {
                        enumScrollPosition = scrollView.scrollPosition;

                        for (int i = 0; i < currentEnumValues.Count; i++)
                        {
                            GUI.backgroundColor = selectedIndex == i ? Color.cyan : Color.white;

                            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                            {
                                // 移動ボタン（Noneは移動不可）
                                GUI.enabled = i != 0;
                                if (GUILayout.Button("↑", GUILayout.Width(30)) && i > 1)
                                {
                                    SwapEnumValues(i, i - 1);
                                }
                                if (GUILayout.Button("↓", GUILayout.Width(30)) && i < currentEnumValues.Count - 1)
                                {
                                    SwapEnumValues(i, i + 1);
                                }
                                GUI.enabled = true;

                                // 値の表示
                                if (GUILayout.Button(currentEnumValues[i], EditorStyles.label))
                                {
                                    selectedIndex = i;
                                }

                                // 削除ボタン（Noneは削除不可）
                                GUI.enabled = i != 0;
                                if (GUILayout.Button("X", GUILayout.Width(30)))
                                {
                                    currentEnumValues.RemoveAt(i);
                                    isDirty = true;
                                    break;
                                }
                                GUI.enabled = true;
                            }
                        }
                    }

                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.Space(5);

                    // 更新ボタン（変更がある場合のみ有効）
                    GUI.enabled = isDirty;
                    if (GUILayout.Button("Update Enum File", GUILayout.Height(30)))
                    {
                        GenerateEnumFile();
                        isDirty = false;
                    }
                    GUI.enabled = true;

                    // 変更があることを表示
                    if (isDirty)
                    {
                        EditorGUILayout.HelpBox("There are unsaved changes. Click 'Update Enum File' to apply changes.", MessageType.Info);
                    }
                }
            }
        }

        /// <summary>
        /// 指定された2つのインデックスのenum値を入れ替える
        /// </summary>
        private void SwapEnumValues(int index1, int index2)
        {
            string temp = currentEnumValues[index1];
            currentEnumValues[index1] = currentEnumValues[index2];
            currentEnumValues[index2] = temp;
            isDirty = true;
        }

        /// <summary>
        /// 既存のenum定義ファイルから値を読み込む
        /// </summary>
        private void LoadCurrentEnumValues()
        {
            currentEnumValues.Clear();
            currentEnumValues.Add("None"); // デフォルト値として必ずNoneを追加

            if (File.Exists(ENUM_FILE_PATH))
            {
                string content = File.ReadAllText(ENUM_FILE_PATH);

                // 波括弧の中身を抽出
                Match match = Regex.Match(content, @"\{([^}]*)\}");
                if (match.Success)
                {
                    string enumContent = match.Groups[1].Value;
                    string[] lines = enumContent.Split(',');

                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("//"))
                        {
                            string enumValue = trimmedLine.Split('=')[0].Trim();
                            if (!string.IsNullOrEmpty(enumValue) && !currentEnumValues.Contains(enumValue))
                            {
                                currentEnumValues.Add(enumValue);
                            }
                        }
                    }
                }
            }
            isDirty = false;
        }

        /// <summary>
        /// 現在のenum値リストから新しいenum定義ファイルを生成
        /// </summary>
        private void GenerateEnumFile()
        {
            // 既存のファイルを読み込む
            string existingContent = "";
            if (File.Exists(ENUM_FILE_PATH))
            {
                existingContent = File.ReadAllText(ENUM_FILE_PATH);
            }

            // 既存のコードから名前空間とアクセス修飾子を抽出して保持
            string nameSpace = "SFEscape.InteractableObjects";
            string accessModifier = "public";

            Match namespaceMatch = Regex.Match(existingContent, @"namespace\s+(\w+)\s*\{");
            if (namespaceMatch.Success)
            {
                nameSpace = namespaceMatch.Groups[1].Value;
            }

            Match accessMatch = Regex.Match(existingContent, @"(public|private|internal)\s+enum\s+ItemType");
            if (accessMatch.Success)
            {
                accessModifier = accessMatch.Groups[1].Value;
            }

            // ディレクトリが存在しない場合は作成
            string directoryPath = Path.GetDirectoryName(ENUM_FILE_PATH);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // enumコンテンツの作成（インデックスを付与）
            List<string> enumLines = new List<string>();
            for (int i = 0; i < currentEnumValues.Count; i++)
            {
                enumLines.Add($"{currentEnumValues[i]} = {i}");
            }
            string enumContent = string.Join(",\n    ", enumLines);

            // 完全なファイル内容の生成
            string fileContent;
            if (!string.IsNullOrEmpty(nameSpace))
            {
                fileContent = $@"
// このファイルは自動生成されたものです。手動で変更しないでください。
// enumを編集する場合はItemDatabase（ScriptableObject）のインスペクターから変更してください。
namespace {nameSpace}
{{
    {accessModifier} enum ItemType
    {{
        {enumContent}
    }}
}}";
            }
            else
            {
                fileContent = $@"
// このファイルは自動生成されたものです。手動で変更しないでください。
// enumを編集する場合はItemDatabase（ScriptableObject）のインスペクターから変更してください。
{accessModifier} enum ItemType
{{
    {enumContent}
}}";
            }

            File.WriteAllText(ENUM_FILE_PATH, fileContent);
            AssetDatabase.Refresh();
        }
    }
#endif
}