using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace KinematicCharacterController
{
    [CustomEditor(typeof(KinematicCharacterMotor))]
    public class KinematicCharacterMotorEditor : UnityEditor.Editor
    {
        protected virtual void OnSceneGUI()
        {            
            KinematicCharacterMotor motor = (target as KinematicCharacterMotor);
            if (motor)
            {
                Vector3 characterBottom = motor.transform.position + (motor.Capsule.center + (-Vector3.up * (motor.Capsule.height * 0.5f)));

                Handles.color = Color.yellow;
                if (Event.current.type == EventType.Repaint)
                {
                    Handles.CircleHandleCap(
                        0,
                        characterBottom + (motor.transform.up * motor.MaxStepHeight),
                        Quaternion.LookRotation(motor.transform.up, motor.transform.forward),
                        motor.Capsule.radius + 0.1f,
                        Event.current.type);
                }
            }
        }
    }
}