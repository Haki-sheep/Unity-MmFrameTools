using MieMieFrameWork.M_InputSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MieMieFrameWork.CharacterController
{

    public class CharacterDataModule : MonoBehaviour
    {

        [SerializeField, LabelText("保存容器")] private CharacterDataFIle characterDataFile;

        [LabelText("模式")]
        public enum PlayerControllerMode { FirstPerson, ThirdPerson };
        [SerializeField, EnumToggleButtons] private PlayerControllerMode controllerMode;
        public PlayerControllerMode ControllerMode { get => controllerMode; set => controllerMode = value; }

        [Header("动态参数")]
        [SerializeField, LabelText("移动速度")] public float MoveSpeed = 5f;
        [SerializeField, LabelText("旋转速度")] public float RotateSpeed = 8f;
        [SerializeField, LabelText("主摄像机")] public Camera mainCamera;


        [Header("核心参数")]
        [LabelText("当前输入方向")]
        public Vector3 MoveDirection
        {
            get => ModuleHub.Instance.GetManager<InputManager>().MoveInput;
        }

        [SerializeField, LabelText("修正后移动方向")] public Vector3 MoveDirectionFixed;
        [field: SerializeField, LabelText("当前垂直速度")] public float VerticalSpeed { get; set; }

        [Header("地面检测")]
        [SerializeField, LabelText("是否启用地面检测")] public bool ApplyGroundCheck = true;
        [SerializeField, LabelText("是否在地面")] public bool IsGrounded;
        [SerializeField, LabelText("球形检测半径")] public float CheckGroundRadius = 0.2f;
        [SerializeField, LabelText("检测高度偏移量")] public float CheckGroundHeightOffset = 0.1f;
        [SerializeField, LabelText("高度差检测距离")] public float CheckGroundMaxDistance = 0.3f;
        [SerializeField, LabelText("地面层级")] public LayerMask GroundLayer;

        [Header("重力")]
        [SerializeField, LabelText("是否启用重力")] public bool ApplyGravity = true;
        [SerializeField, LabelText("重力系数")] public float GravityFactor = -9.81f;
        [SerializeField, LabelText("最大下落速度")] public float MaxDownSpeed = -20f;

        [Header("斜坡")]
        [SerializeField, LabelText("斜坡层级")] public LayerMask SlopLayer;
        [SerializeField, LabelText("最大爬坡角度")] public float MaxSlopeAngle = 45f;
        [SerializeField, LabelText("斜坡检测距离")] public float SlopeCheckDistance = 1f;
        [SerializeField, LabelText("平滑过渡到斜坡速度")] public float SlopeSmoothSpeed = 5f;
        [SerializeField, LabelText("斜坡检测高度偏移量")] public float CheckGroundH = 0.1f;


        [Button("Save Parms to So")]
        public void SaveParmsToSo()
        {
            if (characterDataFile is null)
            {
                Debug.LogError($"【CharacterDataModule】保存失败：{gameObject.name} 的保存容器（characterDataFile）未赋值！", this);
                return;
            }

            characterDataFile.controllerMode = this.controllerMode;
            characterDataFile.MoveSpeed = this.MoveSpeed;
            characterDataFile.RotateSpeed = this.RotateSpeed;

            // 地面检测配置（排除 IsGrounded 运行时参数）
            characterDataFile.ApplyGroundCheck = this.ApplyGroundCheck;
            characterDataFile.CheckGroundRadius = this.CheckGroundRadius;
            characterDataFile.CheckGroundHeightOffset = this.CheckGroundHeightOffset;
            characterDataFile.CheckGroundMaxDistance = this.CheckGroundMaxDistance;
            characterDataFile.GroundLayer = this.GroundLayer;

            // 重力配置
            characterDataFile.ApplyGravity = this.ApplyGravity;
            characterDataFile.GravityFactor = this.GravityFactor;
            characterDataFile.MaxDownSpeed = this.MaxDownSpeed;

            // 斜坡配置
            characterDataFile.SlopLayer = this.SlopLayer;
            characterDataFile.MaxSlopeAngle = this.MaxSlopeAngle;
            characterDataFile.SlopeCheckDistance = this.SlopeCheckDistance;
            characterDataFile.SlopeSmoothSpeed = this.SlopeSmoothSpeed;
            characterDataFile.CheckGroundH = this.CheckGroundH;

            // 3. 编辑器下持久化保存SO修改
            Debug.Log($"【CharacterDataModule】可持久化参数已成功保存到SO：{characterDataFile.name}", this);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(characterDataFile);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}