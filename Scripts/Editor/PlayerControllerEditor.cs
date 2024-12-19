using UnityEngine;
using UnityEditor;
using SFEscape.Runtime.Player;
using static SFEscape.Runtime.Player.HealthConditionHandler;

namespace SFEscape.Editor
{
    [CustomEditor(typeof(HealthConditionHandler))]
    public class PlayerControllerEditor : UnityEditor.Editor
    {
        private PlayerCondition selectedCondition;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            HealthConditionHandler player = (HealthConditionHandler)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("状態変更", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            // 状態選択用のEnum Popup
            selectedCondition = (PlayerCondition)EditorGUILayout.EnumPopup("変更先の状態", selectedCondition);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);

            // 状態変更ボタン
            if (GUILayout.Button("状態を変更", GUILayout.Height(30)))
            {
                // privateメソッドを呼び出すためにリフレクションを使用
                // 第2引数はメソッドの検索範囲（publicなのか、privateなのか、インスタンス・メソッドなのかなど）
                var method = typeof(HealthConditionHandler).GetMethod(
                    "ChangeCondition",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance
                );

                //TypeはすべてのC#クラスで使える
                //// クラスの型情報を取得
                //Type type = typeof(PlayerController);
                //// このクラスが持つメソッドを探す
                //var method = type.GetMethod(
                //    "ChangeCondition",
                //    BindingFlags.NonPublic | BindingFlags.Instance
                //);

                try
                {
                    //第1引数は呼び出すメソッドが属するインスタンス
                    //第2引数はメソッドに渡す引数（複数の型に対応するため、object[]型で渡している）
                    method.Invoke(player, new object[] { selectedCondition });
                }
                catch (System.Reflection.TargetInvocationException e)
                {
                    if (e.InnerException is System.ArgumentException)
                    {
                        EditorUtility.DisplayDialog(
                            "エラー",
                            "無効な状態変更です。現在の状態から遷移できない状態が選択されました。",
                            "OK"
                        );
                    }
                }
            }

            // 現在の状態を表示
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("現在の状態", player.CurrentCondition.ToString());
        }
    }
}