using Sirenix.OdinInspector;
using UnityEngine;
namespace MieMieFrameWork.CharacterController
{

    [CreateAssetMenu(menuName = "MieMieFrameTools/CharacterController/CharacterDataFile", fileName = "New CharacterDataFile")]
    public class CharacterDataFIle : SerializedScriptableObject
    {
        // 控制器模式（配置项）
        [SerializeField, LabelText("模式")]
        public CharacterDataModule.PlayerControllerMode controllerMode;

        // 动态参数（配置项，非运行时核心参数）
        [Header("动态参数（配置）")]
        [SerializeField, LabelText("移动速度")] public float MoveSpeed = 5f;
        [SerializeField, LabelText("旋转速度")] public float RotateSpeed = 8f;

        [Header("地面检测（配置）")]
        [SerializeField, LabelText("是否启用地面检测")] public bool ApplyGroundCheck = true;
        [SerializeField, LabelText("球形检测半径")] public float CheckGroundRadius = 0.2f;
        [SerializeField, LabelText("检测高度偏移量")] public float CheckGroundHeightOffset = 0.1f;
        [SerializeField, LabelText("高度差检测距离")] public float CheckGroundMaxDistance = 0.3f;
        [SerializeField, LabelText("地面层级")] public LayerMask GroundLayer;

        [Header("重力（配置）")]
        [SerializeField, LabelText("是否启用重力")] public bool ApplyGravity = true;
        [SerializeField, LabelText("重力系数")] public float GravityFactor = -9.81f;
        [SerializeField, LabelText("最大下落速度")] public float MaxDownSpeed = -20f;

        [Header("斜坡（配置）")]
        [SerializeField, LabelText("斜坡层级")] public LayerMask SlopLayer;
        [SerializeField, LabelText("最大爬坡角度")] public float MaxSlopeAngle = 45f;
        [SerializeField, LabelText("斜坡检测距离")] public float SlopeCheckDistance = 1f;
        [SerializeField, LabelText("平滑过渡到斜坡速度")] public float SlopeSmoothSpeed = 5f;
        [SerializeField, LabelText("斜坡检测高度偏移量")] public float CheckGroundH = 0.1f;
    }
}