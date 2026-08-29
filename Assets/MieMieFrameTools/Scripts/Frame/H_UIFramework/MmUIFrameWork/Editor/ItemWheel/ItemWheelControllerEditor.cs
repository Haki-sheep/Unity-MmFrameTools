#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// ItemWheelController 判定区可视化
/// </summary>
[CustomEditor(typeof(ItemWheelController))]
public class ItemWheelControllerEditor : Editor
{
    private static readonly Color ActivationInnerColor = new Color(0.25f, 0.85f, 1f, 0.9f);
    private static readonly Color ActivationOuterColor = new Color(1f, 0.55f, 0.15f, 0.9f);
    private static readonly Color VisualInnerColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
    private static readonly Color VisualOuterColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);

    private void OnSceneGUI()
    {
        var controller = (ItemWheelController)target;
        SerializedObject so = serializedObject;
        so.Update();

        RectTransform wheelRoot = so.FindProperty("wheelRoot").objectReferenceValue as RectTransform;
        if (wheelRoot == null)
            wheelRoot = controller.transform as RectTransform;
        if (wheelRoot == null)
            return;

        RingDraw ringDraw = wheelRoot.GetComponentInChildren<RingDraw>();
        if (ringDraw == null)
            return;

        float expandedInner = so.FindProperty("expandedInnerRadius").floatValue;
        float expandedOuter = so.FindProperty("expandedOuterRadius").floatValue;

        float visualInner = ringDraw.InnerRadius;
        float visualOuter = ringDraw.OuterRadius;
        float activationInner = visualInner - expandedInner;
        float activationOuter = visualOuter + expandedOuter;

        DrawCircle(wheelRoot, visualInner, VisualInnerColor, 1f);
        DrawCircle(wheelRoot, visualOuter, VisualOuterColor, 1f);
        DrawCircle(wheelRoot, activationInner, ActivationInnerColor, 2f);
        DrawCircle(wheelRoot, activationOuter, ActivationOuterColor, 2f);

        Vector3 labelBase = wheelRoot.TransformPoint(Vector3.zero);
        Handles.Label(labelBase + wheelRoot.TransformVector(Vector3.left * activationInner),
            $"判定内径 {activationInner:0}");
        Handles.Label(labelBase + wheelRoot.TransformVector(Vector3.right * activationOuter),
            $"判定外径 {activationOuter:0}");

        if (so.ApplyModifiedProperties())
            SceneView.RepaintAll();
    }

    /// <summary>
    /// 在 UI 本地 XY 平面画圆
    /// </summary>
    private static void DrawCircle(RectTransform rt, float radius, Color color, float thickness)
    {
        if (radius <= 0f) return;

        const int segments = 64;
        Handles.color = color;
        Vector3 prev = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float rad = i * (Mathf.PI * 2f / segments);
            Vector3 local = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
            Vector3 world = rt.TransformPoint(local);

            if (i > 0)
                Handles.DrawLine(prev, world, thickness);

            prev = world;
        }
    }
}
#endif
