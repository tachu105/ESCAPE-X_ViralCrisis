using UnityEngine;

namespace SFEscape.Runtime.Utilities.Debugger
{
#if UNITY_EDITOR
    public static class DebugEx
    {
        private class GizmoDrawer : MonoBehaviour
        {
            public struct SphereCastCommand
            {
                public Ray ray;
                public float radius;
                public float distance;
                public Color color;
                public float duration;
                public float createdTime;
            }

            private static GizmoDrawer instance;
            private static System.Collections.Generic.List<SphereCastCommand> commands
                = new System.Collections.Generic.List<SphereCastCommand>();

            internal static void Initialize()
            {
                if (instance == null)
                {
                    var go = new GameObject("DebugEx_GizmoDrawer");
                    instance = go.AddComponent<GizmoDrawer>();
                    GameObject.DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                }
            }

            private void OnDrawGizmos()
            {
                if (!Application.isPlaying || commands.Count == 0) return;

                float currentTime = Time.time;
                int count = commands.Count;

                // 不要なコマンドを削除
                for (int i = count - 1; i >= 0; i--)
                {
                    if (currentTime > commands[i].createdTime + commands[i].duration)
                    {
                        commands.RemoveAt(i);
                    }
                }

                // コマンドの描画
                foreach (var cmd in commands)
                {
                    DrawSphereCastGizmo(cmd);
                }
            }

            private void DrawSphereCastGizmo(SphereCastCommand cmd)
            {
                Gizmos.color = cmd.color;

                // 始点と終点の球体
                Gizmos.DrawWireSphere(cmd.ray.origin, cmd.radius);
                Gizmos.DrawWireSphere(cmd.ray.origin + cmd.ray.direction * cmd.distance, cmd.radius);

                // 球体の移動範囲を示す補助線
                Vector3 up = Vector3.Cross(cmd.ray.direction, Vector3.right).normalized * cmd.radius;
                if (up == Vector3.zero)
                    up = Vector3.Cross(cmd.ray.direction, Vector3.forward).normalized * cmd.radius;

                Vector3 right = Vector3.Cross(cmd.ray.direction, up).normalized * cmd.radius;

                Gizmos.DrawLine(cmd.ray.origin + up,
                    cmd.ray.origin + cmd.ray.direction * cmd.distance + up);
                Gizmos.DrawLine(cmd.ray.origin - up,
                    cmd.ray.origin + cmd.ray.direction * cmd.distance - up);
                Gizmos.DrawLine(cmd.ray.origin + right,
                    cmd.ray.origin + cmd.ray.direction * cmd.distance + right);
                Gizmos.DrawLine(cmd.ray.origin - right,
                    cmd.ray.origin + cmd.ray.direction * cmd.distance - right);
            }

            public static void AddSphereCastCommand(Ray ray, float radius, float distance,
                Color color, float duration)
            {
                if (instance == null)
                {
                    Initialize();
                }

                commands.Add(new SphereCastCommand
                {
                    ray = ray,
                    radius = radius,
                    distance = distance,
                    color = color,
                    duration = duration,
                    createdTime = Time.time
                });
            }
        }

        /// <summary>
        /// SphereCastの範囲をギズモで可視化します
        /// </summary>
        /// <param name="ray">レイ</param>
        /// <param name="radius">球体の半径</param>
        /// <param name="distance">レイの距離</param>
        /// <param name="color">ギズモの色</param>
        /// <param name="duration">表示時間（秒）</param>
        public static void DrawSphereCast(Ray ray, float radius, float distance,
            Color? color = null, float duration = 0)
        {
            GizmoDrawer.AddSphereCastCommand(ray, radius, distance,
                color ?? Color.red, duration);
        }
    }
#endif
}
