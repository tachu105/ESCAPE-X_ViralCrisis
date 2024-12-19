using UnityEngine;
using UnityEditor;
using SFEscape.Runtime.Audio;

namespace SFEscape.Editor
{
    [CustomEditor(typeof(PlayerAudioController))]
    public class PlayerAudioControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // デフォルトのインスペクター描画
            DrawDefaultInspector();

            // PlayerAudioControllerの参照を取得
            PlayerAudioController audioController = (PlayerAudioController)target;

            // 区切り線を追加
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("BGM Control", EditorStyles.boldLabel);

            // BGM切り替えボタンを配置
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Change to Chase BGM", GUILayout.Height(30)))
            {
                audioController.ChangeChaseBGM();
            }

            if (GUILayout.Button("Change to Normal BGM", GUILayout.Height(30)))
            {
                audioController.ChangeNormalBGM();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}