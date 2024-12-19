using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using R3;
using SFEscape.Runtime.Utilities.Debugger;

namespace SFEscape.Editor
{
    /// <summary>
    /// シーン内のDebugVariableAttributeが付与されたフィールドやプロパティを一覧表示するエディタウィンドウ
    /// UniRxおよびUniTaskのReactivePropertyに対応
    /// </summary>
    public class DebugVariablesInspector : EditorWindow
    {
        // ウィンドウの状態管理
        private Vector2 scrollPosition;
        private bool showPrivateFields = true;
        private string searchFilter = "";
        private GUIStyle headerStyle;
        private GUIStyle descriptionStyle;

        // 自動更新の設定
        private bool autoRefresh = true;
        private float refreshInterval = 0.5f;
        private double lastRefreshTime;
        private bool stylesInitialized = false;

        // UniRxとUniTaskのReactivePropertyタイプの定義
        private static readonly Type[] ReactivePropertyTypes = {
        typeof(ReactiveProperty<>),              // UniRx: 標準のReactiveProperty
        typeof(ReadOnlyReactiveProperty<>),     // UniRx: 読み取り専用インターフェース
        typeof(IReadOnlyAsyncReactiveProperty<>) // UniTask: 非同期ReactivePropery
    };

        /// <summary>
        /// エディタウィンドウを開くメニューアイテム
        /// </summary>
        [MenuItem("Window/Debug Variables Inspector")]
        public static void ShowWindow()
        {
            GetWindow<DebugVariablesInspector>("Debug Inspector");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// GUIスタイルの初期化。一度だけ実行される。
        /// </summary>
        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle();
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            headerStyle.fontSize = 12;
            headerStyle.margin = new RectOffset(0, 0, 10, 5);

            descriptionStyle = new GUIStyle(EditorStyles.miniLabel);
            descriptionStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();
            DrawToolbar();
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollView.scrollPosition;
                DisplayAllDebugVariables();
            }
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// 自動更新の処理。設定された間隔でウィンドウを再描画する。
        /// </summary>
        private void OnEditorUpdate()
        {
            if (autoRefresh && EditorApplication.timeSinceStartup > lastRefreshTime + refreshInterval)
            {
                lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        /// <summary>
        /// ツールバーの描画。フィルタリングや更新設定のUIを含む。
        /// </summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // privateフィールドの表示設定
                showPrivateFields = EditorGUILayout.ToggleLeft("Show Private", showPrivateFields, GUILayout.Width(100));

                // 自動更新の設定
                autoRefresh = EditorGUILayout.ToggleLeft("Auto Refresh", autoRefresh, GUILayout.Width(100));
                if (autoRefresh)
                {
                    EditorGUI.BeginChangeCheck();
                    refreshInterval = EditorGUILayout.FloatField("Interval", refreshInterval, GUILayout.Width(70));
                    if (EditorGUI.EndChangeCheck())
                    {
                        refreshInterval = Mathf.Max(0.1f, refreshInterval); // 最小間隔を0.1秒に制限
                    }
                }

                GUILayout.FlexibleSpace();

                // 検索フィルター
                EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
                searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(200));

                // 手動更新ボタン
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Repaint();
                }
            }
        }

        /// <summary>
        /// シーン内のすべてのデバッグ変数を階層的に表示する
        /// </summary>
        private void DisplayAllDebugVariables()
        {
            var allDebugVars = GetAllDebugVariables();

            if (!allDebugVars.Any())
            {
                EditorGUILayout.HelpBox("No debug variables found in the scene", MessageType.Info);
                return;
            }

            // GameObjectごとにグループ化して表示
            var groupedByGameObject = allDebugVars
                .GroupBy(x => x.gameObject)
                .OrderBy(g => g.Key.name);

            foreach (var gameObjectGroup in groupedByGameObject)
            {
                var gameObject = gameObjectGroup.Key;

                EditorGUILayout.Space(10);
                using (new EditorGUILayout.HorizontalScope())
                {
                    // GameObjectのアクティブ状態トグル
                    bool isActive = EditorGUILayout.Toggle(gameObject.activeSelf, GUILayout.Width(15));
                    if (isActive != gameObject.activeSelf)
                    {
                        gameObject.SetActive(isActive);
                    }

                    EditorGUILayout.ObjectField(gameObject, typeof(GameObject), true);
                }

                EditorGUI.indentLevel++;

                // コンポーネントごとにグループ化して表示
                var groupedByComponent = gameObjectGroup
                    .GroupBy(x => x.component)
                    .OrderBy(g => g.Key.GetType().Name);

                foreach (var componentGroup in groupedByComponent)
                {
                    var component = componentGroup.Key;
                    EditorGUILayout.LabelField($"[{component.GetType().Name}]");

                    EditorGUI.indentLevel++;

                    foreach (var debugVar in componentGroup)
                    {
                        DisplayFieldValue(debugVar.memberInfo, debugVar.component);
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// デバッグ変数の情報を保持するための内部クラス
        /// </summary>
        private class DebugVariableInfo
        {
            public GameObject gameObject;
            public MonoBehaviour component;
            public MemberInfo memberInfo;
        }

        /// <summary>
        /// シーン内のすべてのデバッグ変数を収集する
        /// </summary>
        /// <returns>デバッグ変数の情報のコレクション</returns>
        private IEnumerable<DebugVariableInfo> GetAllDebugVariables()
        {
            var results = new List<DebugVariableInfo>();
            var currentScene = SceneManager.GetActiveScene();
            var rootObjects = currentScene.GetRootGameObjects();

            foreach (var rootObject in rootObjects)
            {
                // 非アクティブなオブジェクトも含めてすべてのMonoBehaviourを取得
                var allObjects = rootObject.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (var component in allObjects)
                {
                    if (component == null) continue;

                    // フィールドとプロパティの両方を収集
                    var debugFields = GetDebugFields(component);
                    foreach (var field in debugFields)
                    {
                        if (ShouldIncludeField(field, component))
                        {
                            results.Add(new DebugVariableInfo
                            {
                                gameObject = component.gameObject,
                                component = component,
                                memberInfo = field
                            });
                        }
                    }

                    var debugProperties = GetDebugProperties(component);
                    foreach (var property in debugProperties)
                    {
                        if (ShouldIncludeProperty(property, component))
                        {
                            results.Add(new DebugVariableInfo
                            {
                                gameObject = component.gameObject,
                                component = component,
                                memberInfo = property
                            });
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// コンポーネントからDebugVariableAttributeが付与されたフィールドを取得
        /// </summary>
        private IEnumerable<FieldInfo> GetDebugFields(MonoBehaviour component)
        {
            if (component == null) return Enumerable.Empty<FieldInfo>();

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (showPrivateFields)
                bindingFlags |= BindingFlags.NonPublic;

            return component.GetType()
                .GetFields(bindingFlags)
                .Where(f => f.GetCustomAttribute<DebugVariableAttribute>() != null);
        }

        /// <summary>
        /// コンポーネントからDebugVariableAttributeが付与されたプロパティを取得
        /// </summary>
        private IEnumerable<PropertyInfo> GetDebugProperties(MonoBehaviour component)
        {
            if (component == null) return Enumerable.Empty<PropertyInfo>();

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (showPrivateFields)
                bindingFlags |= BindingFlags.NonPublic;

            return component.GetType()
                .GetProperties(bindingFlags)
                .Where(p => p.GetCustomAttribute<DebugVariableAttribute>() != null && p.CanRead);
        }

        /// <summary>
        /// フィールドが検索フィルターに一致するかチェック
        /// </summary>
        private bool ShouldIncludeField(FieldInfo field, MonoBehaviour component)
        {
            if (string.IsNullOrEmpty(searchFilter)) return true;

            var searchTerms = searchFilter.ToLower().Split(' ');
            var searchText = $"{component.gameObject.name} {component.GetType().Name} {field.Name}".ToLower();

            return searchTerms.All(term => searchText.Contains(term));
        }

        /// <summary>
        /// プロパティが検索フィルターに一致するかチェック
        /// </summary>
        private bool ShouldIncludeProperty(PropertyInfo property, MonoBehaviour component)
        {
            if (string.IsNullOrEmpty(searchFilter)) return true;

            var searchTerms = searchFilter.ToLower().Split(' ');
            var searchText = $"{component.gameObject.name} {component.GetType().Name} {property.Name}".ToLower();

            return searchTerms.All(term => searchText.Contains(term));
        }

        /// <summary>
        /// フィールドまたはプロパティの値をGUIに表示
        /// </summary>
        private void DisplayFieldValue(MemberInfo memberInfo, MonoBehaviour component)
        {
            object value = null;
            if (memberInfo is FieldInfo fieldInfo)
            {
                value = fieldInfo.GetValue(component);
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                try
                {
                    value = propertyInfo.GetValue(component);
                }
                catch (Exception)
                {
                    value = "Unable to get value";
                }
            }

            string valueStr = FormatValue(value, GetMemberType(memberInfo));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{memberInfo.Name}: {valueStr}");

                var attribute = memberInfo.GetCustomAttribute<DebugVariableAttribute>();
                if (!string.IsNullOrEmpty(attribute.Description))
                {
                    EditorGUILayout.LabelField(attribute.Description, descriptionStyle);
                }
            }
        }

        /// <summary>
        /// メンバー情報から型情報を取得
        /// </summary>
        private Type GetMemberType(MemberInfo memberInfo)
        {
            return memberInfo switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                _ => typeof(object)
            };
        }

        /// <summary>
        /// 値を文字列形式にフォーマット
        /// UniRx/UniTaskのReactiveProperty、配列、IEnumerable等の特殊な型に対応
        /// </summary>
        private string FormatValue(object value, Type memberType)
        {
            if (value == null) return "null";

            // ReactivePropertyの判定（UniRx と UniTask の両方に対応）
            if (memberType.IsGenericType)
            {
                var genericTypeDef = memberType.GetGenericTypeDefinition();
                if (ReactivePropertyTypes.Contains(genericTypeDef))
                {
                    var propertyValue = memberType.GetProperty("Value")?.GetValue(value);
                    return propertyValue?.ToString() ?? "null";
                }
            }

            // 配列の場合
            if (memberType.IsArray)
            {
                return FormatArray((Array)value);
            }

            // IEnumerable（文字列以外）の場合
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(memberType)
                && memberType != typeof(string))
            {
                return FormatEnumerable((System.Collections.IEnumerable)value);
            }

            return value.ToString();
        }

        /// <summary>
        /// 配列を文字列形式にフォーマット
        /// </summary>
        private string FormatArray(Array array)
        {
            return $"[{string.Join(", ", array.Cast<object>().Select(x => x?.ToString() ?? "null"))}]";
        }

        /// <summary>
        /// IEnumerableを文字列形式にフォーマット
        /// </summary>
        private string FormatEnumerable(System.Collections.IEnumerable enumerable)
        {
            return $"[{string.Join(", ", enumerable.Cast<object>().Select(x => x?.ToString() ?? "null"))}]";
        }
    }
}