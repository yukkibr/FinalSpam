using UnityEngine;

namespace FinalSpam.Enemies
{
    /// <summary>
    /// Visao de inimigos relativa ao transform do proprio inimigo.
    /// Angulo total: 0-360 graus, 360 = todas as direcoes.
    /// </summary>
    public static class EnemySight
    {
        public static Vector3 Forward(Transform observer, Vector3 localForward)
        {
            if (observer == null) return Vector3.forward;
            if (localForward.sqrMagnitude < 0.0001f) localForward = Vector3.forward;
            return observer.TransformDirection(localForward.normalized).normalized;
        }

        public static bool InsideCone(Vector3 eye, Vector3 point,
            Vector3 facing, float viewAngle, float maxRange)
        {
            Vector3 offset = point - eye;
            if (offset.sqrMagnitude > maxRange * maxRange) return false;
            if (offset.sqrMagnitude < 0.0001f) return true;
            if (viewAngle >= 359.99f) return true;
            return Vector3.Angle(facing.normalized, offset.normalized) <=
                Mathf.Clamp(viewAngle, 0f, 360f) * 0.5f;
        }

        public static void DrawVisionCone(Transform observer, Vector3 eye,
            Vector3 localForward, float angle, float distance, Color color)
        {
            if (observer == null) return;
            Vector3 facing = Forward(observer, localForward);
            float half = Mathf.Clamp(angle, 0f, 360f) * 0.5f;
            Gizmos.color = color;
            Gizmos.DrawRay(eye, facing * Mathf.Max(0.01f, distance));

            Vector3 side = Vector3.Cross(Vector3.up, facing);
            if (side.sqrMagnitude < 0.01f)
                side = Vector3.Cross(Vector3.forward, facing);
            side.Normalize();

            Gizmos.DrawRay(eye, Quaternion.AngleAxis(-half, Vector3.up) *
                facing * distance);
            Gizmos.DrawRay(eye, Quaternion.AngleAxis(half, Vector3.up) *
                facing * distance);
            Gizmos.DrawRay(eye, Quaternion.AngleAxis(-half, side) *
                facing * distance);
            Gizmos.DrawRay(eye, Quaternion.AngleAxis(half, side) *
                facing * distance);
#if UNITY_EDITOR
            if (angle < 359.99f)
            {
                UnityEditor.Handles.color = color;
                UnityEditor.Handles.DrawWireArc(eye, Vector3.up,
                    Quaternion.AngleAxis(-half, Vector3.up) * facing,
                    angle, distance);
            }
#endif
        }
    }
}
