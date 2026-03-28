using Sirenix.OdinInspector;
using UnityEngine;
namespace MieMieFrameWork.CharacterController
{
    public class GroundCheckModule : I_CharacterControlBase
    {
        private RaycastHit[] currentHit = new RaycastHit[1];

        //当前命中信息
        public RaycastHit CurrentHitInfo { get => currentHit[0]; }
        private CharacterDataModule characterData;

        public void Init(CharacterControlBase cc)
        {
            this.characterData = cc.CharacterDataBase;
        }

        /// <summary>
        /// 球形地面检测 
        /// </summary>
        public void CheckGroundHandle(Transform characterTransform)
        {
            if (!characterData.ApplyGroundCheck) return;

            Vector3 origin = characterTransform.position + new Vector3(0, characterData.CheckGroundHeightOffset, 0);

            int hitCount = PhysicRayCast.SphereCastNonAlloc(
                origin,
                characterData.CheckGroundRadius,
                Vector3.down,
                currentHit,
                characterData.CheckGroundMaxDistance,
                characterData.GroundLayer,
                QueryTriggerInteraction.Ignore,
                true
            );

            // Debug.Log(hitCount);
            // 根据命中数量判断是否在地面
            if (hitCount > 0)
            {
                // 检查距离是否合理（距离太远不算在地面） 
                float distanceToGround = currentHit[0].distance;
                if (distanceToGround <= 0.2f)
                    characterData.IsGrounded = true;
                else
                    characterData.IsGrounded = false;
            }
            else
                characterData.IsGrounded = false;
        }

        #region 重力
        /// <summary>
        /// 获取垂直速度
        /// 计算速度 : V =V0 + a△t(△t = Time.deltaTime)
        /// </summary>
        /// <returns></returns>
        public float GetVerticalSpeed()
        {
            if (!characterData.ApplyGravity)
                return characterData.VerticalSpeed = 0f;

            if (characterData.IsGrounded)
            {
                characterData.VerticalSpeed = -1f; // 调整地面时的速度
            }
            else
            {
                characterData.VerticalSpeed += characterData.GravityFactor * Time.deltaTime;
                characterData.VerticalSpeed = Mathf.Clamp(characterData.VerticalSpeed, characterData.MaxDownSpeed, 0f);
            }
            return characterData.VerticalSpeed;
        }

        /// <summary>
        /// 计算位移 s =v*△t
        /// </summary>
        /// <returns></returns>
        public Vector3 GetVerticalVector() => new Vector3(0, GetVerticalSpeed() * Time.deltaTime, 0);

        #endregion
    }
}
