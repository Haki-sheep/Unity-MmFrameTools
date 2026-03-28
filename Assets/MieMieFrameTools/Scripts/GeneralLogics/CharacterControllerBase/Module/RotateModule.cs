
namespace MieMieFrameWork.CharacterController
{
    using MieMieFrameWork.M_InputSystem;

    using UnityEngine;
    using static MieMieFrameWork.CharacterController.CharacterDataModule;

    public class RotateModule : I_CharacterControlBase
    {

        public bool ApplyRoation;
        private Quaternion targetRot;
        public Quaternion TargetRot { get => targetRot; set => targetRot = value; }

        public PlayerControllerMode curMode;
        private CharacterDataModule characterData;

        public void Init(CharacterControlBase cc)
        {

            ApplyRoation = true;
            this.characterData = cc.CharacterDataBase;
            curMode = characterData.ControllerMode;
        }


        /// <summary>
        /// 获取旋转向量
        /// </summary>
        /// <param name="currentRotation">当前旋转角度</param>
        /// <param name="targetDir">目标方向</param>
        /// <returns>旋转角度</returns>
        public Quaternion GetThirdRotateVector(Quaternion currentRotation, Vector3 targetDir)
        {
            if (!ApplyRoation) return currentRotation;
            if (curMode != PlayerControllerMode.ThirdPerson) return currentRotation;

            //如果目标方向为0 则不进行旋转
            if (targetDir.sqrMagnitude < 1e-6f) return currentRotation;

            //计算目标角度
            targetRot = Quaternion.LookRotation(targetDir);
            //计算旋转速度
            float t = Mathf.Clamp01(characterData.RotateSpeed * Time.deltaTime);
            //平滑旋转
            return Quaternion.Slerp(currentRotation, targetRot, t);
        }

        /// <summary>
        /// 第一人称：根据输入系统的水平视角输入旋转角色
        /// </summary>
        public Quaternion GetFirstPersonRotate(Quaternion currentRotation)
        {
            if (!ApplyRoation) return currentRotation;
            if (curMode != PlayerControllerMode.FirstPerson) return currentRotation;

            // 使用输入系统的水平视角输入（x）
            float lookX = ModuleHub.Instance.GetManager<InputManager>().LookInput.x;
            if (Mathf.Abs(lookX) < 1e-6f) return currentRotation;

            // 根据旋转速度和 deltaTime 计算本帧的增量角度
            float deltaYaw = lookX * characterData.RotateSpeed * Time.deltaTime;

            // 在当前旋转的基础上叠加增量
            targetRot = currentRotation * Quaternion.Euler(0f, deltaYaw, 0f);
            return targetRot;
        }
    }
}