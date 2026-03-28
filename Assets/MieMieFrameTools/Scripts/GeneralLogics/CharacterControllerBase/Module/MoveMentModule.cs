using System;
using UnityEngine;
namespace MieMieFrameWork.CharacterController
{
    public class MoveMentModule : I_CharacterControlBase
    {
        public bool ApplyMovement;
        private CharacterDataModule characterDate;
        private GroundCheckModule groundCheckModule;
        private Vector3[] rayOffsets = new Vector3[1];
        private RaycastHit[] slopeHits = new RaycastHit[1];
        public bool IsOnSlope 
        {
            get
            {
                if (!characterDate.IsGrounded) return false;
                Vector3 groundNormal = GetGroundNormal(); //获取地面法线
                float angle = Vector3.Angle(groundNormal, Vector3.up);//计算该法线与向上的角度
                // 10度以上认为是斜坡
                return angle > 10f && angle <= characterDate.MaxSlopeAngle;
            }
        }   

        public void Init(CharacterDataModule characterData)
        {
            throw new NotImplementedException();
        }

        public void Init(CharacterControlBase cc)
        {
            this.ApplyMovement = true;
            this.characterDate = cc.CharacterDataBase;
            this.groundCheckModule = cc.CCContainer.GetComp<GroundCheckModule>();
            rayOffsets.AsSpan()[0] = Vector3.down;
        }


        /// <summary>
        /// 同步相机旋转 应用第一人称移动
        /// </summary>
        /// <returns></returns>
        public Vector3 GetMoveVectorF()
        {
            //角色对齐相机旋转
            var cam = characterDate.mainCamera.transform;
            var yawOnly = Quaternion.Euler(0f, cam.eulerAngles.y, 0f);
            characterDate.transform.rotation = yawOnly;

              characterDate.MoveDirectionFixed =
                (cam.forward * characterDate.MoveDirection.y +
                    cam.right * characterDate.MoveDirection.x).normalized;

            characterDate.MoveDirectionFixed.y = 0;

            return characterDate.MoveDirectionFixed;
        }
        /// <summary> 
        /// 将输入方向按摄像机朝向进行旋转 --向量投影法
        /// </summary>
        public Vector3 GetMoveVectorP()
        {
            if (!ApplyMovement) return Vector3.zero;
            if (characterDate.mainCamera == null) return characterDate.MoveDirection;

            Vector3 forward = characterDate.mainCamera.transform.forward;
            Vector3 right = characterDate.mainCamera.transform.right;

            characterDate.MoveDirectionFixed =
                (forward * characterDate.MoveDirection.y +
                    right * characterDate.MoveDirection.x).normalized;

            characterDate.MoveDirectionFixed.y = 0;

            return characterDate.MoveDirectionFixed;
        }

        /// <summary>
        /// 将输入方向按摄像机朝向进行旋转 -- 四元数旋转法
        /// 此方法不能直接用于第一人称 因为第一人称需要按相机朝向旋转 当前只是移动的时候才同步相机旋转
        /// </summary>
        public Vector3 GetMoveVectorQ()
        {
            if (!ApplyMovement) return Vector3.zero;
            if (characterDate.mainCamera == null) return characterDate.MoveDirection;
            //获得偏航角度
            float yaw = characterDate.mainCamera.transform.rotation.eulerAngles.y;

            //映射到世界坐标系
            Vector3 inputOnPlane = new Vector3(
                characterDate.MoveDirection.x,   // 左右
                0f,
                characterDate.MoveDirection.y    // 前后
            );
            //旋转输入向量
            Vector3 moveDir = (Quaternion.Euler(0, yaw, 0) * inputOnPlane).normalized;
            characterDate.MoveDirectionFixed = moveDir;
            return characterDate.MoveDirectionFixed;
        }


        /// <summary>
        /// 斜坡处理 - 检测并处理斜坡移动
        /// </summary>
        /// <param name="moveDirection">移动方向</param>
        /// <returns>调整后的移动方向</returns>
        public Vector3 SlopeHandle(bool isGrounded, Transform characterTransform)
        {
            if (!isGrounded || characterDate.MoveDirection.magnitude < 0.1f)
                return characterDate.MoveDirection;

            // 检测前方斜坡
            Vector3 forwardCheck = characterTransform.position + characterDate.MoveDirection.normalized * characterDate.SlopeCheckDistance;
            forwardCheck.y += characterDate.CheckGroundH;

            var origins = new Vector3[1] { forwardCheck};

            if (PhysicRayCast.MultiRaycastNoAlloc(
                origins,
                rayOffsets,
                slopeHits,
                characterDate.SlopeCheckDistance + 1f,
                characterDate.SlopLayer,
                QueryTriggerInteraction.Ignore,
                true).hitFound)
            {
                // 计算斜坡角度
                float slopeAngle = Vector3.Angle(slopeHits[0].normal, Vector3.up);

                // 如果斜坡角度在可接受范围内
                if (slopeAngle <= characterDate.MaxSlopeAngle)
                {
                    // 将移动方向投影到斜坡表面
                    Vector3 slopeProjection = Vector3.ProjectOnPlane(characterDate.MoveDirection, slopeHits[0].normal);

                    // 平滑过渡到斜坡方向
                    return Vector3.Lerp(characterDate.MoveDirection, slopeProjection,
                        characterDate.SlopeSmoothSpeed * Time.deltaTime);
                }
                else
                {
                    // 斜坡太陡，阻止移动
                    return Vector3.zero;
                }
            }

            return characterDate.MoveDirection;
        }

        public Vector3 GetGroundNormal()
        {
            if (characterDate.IsGrounded)
                //获取当前接触到地面的法线
                return groundCheckModule.CurrentHitInfo.normal;
            return Vector3.up;
        }

      
    }
}

