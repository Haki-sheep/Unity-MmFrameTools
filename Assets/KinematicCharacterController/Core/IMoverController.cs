using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KinematicCharacterController
{
    public interface IMoverController
    {
        /// <summary>
        /// 这叫做让你告诉“物理移动器”它现在应该在什么位置。
        /// </summary>
        void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime);
    }
}