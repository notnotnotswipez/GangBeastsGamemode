using System;
using BoneLib;
using LabFusion.MarrowIntegration;
using MelonLoader;
using SLZ.Rig;
using SLZ.Utilities;
using UnityEngine;

namespace GangBeastsGamemode.ProxyScripts
{
[RegisterTypeInIl2Cpp]
    public class QuicksandSafeZone : FusionMarrowBehaviour
    {
        public QuicksandSafeZone(IntPtr intPtr) : base(intPtr)
        {
        }
        
        public void OnTriggerEnter(Collider other)
        {
            if (GangBeastsMode.IsFullActive())
            {
                if (other.attachedRigidbody)
                {
                    RigManager parentManager = other.attachedRigidbody.GetComponentInParent<RigManager>();
                    if (parentManager)
                    {
                        if (parentManager.GetInstanceID() != Player.rigManager.GetInstanceID())
                        {
                            return;
                        }

                        if (!QuicksandZone.isAvailable)
                        {
                            QuicksandZone.ResetValues();
                        }
                    }
                }
            }
        }
    }
}