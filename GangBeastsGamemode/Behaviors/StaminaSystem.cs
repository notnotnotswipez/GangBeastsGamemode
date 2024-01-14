using System;
using BoneLib;
using MelonLoader;
using UnityEngine;

namespace GangBeastsGamemode
{
    [RegisterTypeInIl2Cpp]
    public class StaminaSystem : MonoBehaviour
    {
        public float stamina = 0f;
        public float maxStamina = 7f;

        public static StaminaSystem Instance;
        
        public StaminaSystem(IntPtr intPtr) : base(intPtr)
        {
        }

        public void Awake()
        {
            Instance = this;
            stamina = maxStamina;
        }
        
        public void Update()
        {
            if (!GangBeastsMode.IsFullActive())
            {
                return;
            }

            if (!Player.physicsRig.physG.isGrounded && isGrabbingStaticObject())
            {
                if (stamina < maxStamina)
                {
                    stamina += Time.deltaTime;
                    float mappedStamina = map(stamina, 0, maxStamina, 0, 2);
                    if (mappedStamina > 1.3f)
                    {
                        Player.leftController.haptor.SENDHAPTIC(0, 0.2f, 0.2f, mappedStamina);
                        Player.rightController.haptor.SENDHAPTIC(0, 0.2f, 0.2f, mappedStamina);
                    }

                    if (stamina > maxStamina)
                    {
                        if (Player.leftHand.joint)
                        {
                            if (!Player.leftHand.joint.connectedBody)
                            {
                                Player.leftHand.DetachJoint();
                                Player.leftHand.DetachObject();
                            }
                        }
                        
                        if (Player.rightHand.joint)
                        {
                            if (!Player.rightHand.joint.connectedBody)
                            {
                                Player.rightHand.DetachJoint();
                                Player.rightHand.DetachObject();
                            }
                        }
                    }
                }
            }
            else
            {
                if (stamina > 0)
                {
                    stamina -= Time.deltaTime;
                }
            }
        }

        private float map(float value, float min, float max, float newMin, float newMax)
        {
            return (value - min) * (newMax - newMin) / (max - min) + newMin;
        }

        public bool isGrabbingStaticObject()
        {
            if (Player.leftHand.joint)
            {
                if (!Player.leftHand.joint.connectedBody)
                {
                    return true;
                }
            }
            
            if (Player.rightHand.joint)
            {
                if (!Player.rightHand.joint.connectedBody)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}