using System;
using BoneLib;
using LabFusion.MarrowIntegration;
using LabFusion.Network;
using LabFusion.Representation;
using MelonLoader;
using SLZ.Rig;
using UnityEngine;

namespace GangBeastsGamemode.ProxyScripts
{
    [RegisterTypeInIl2Cpp]
    public class KnockoutterZone : FusionMarrowBehaviour
    {
        public KnockoutterZone(IntPtr intPtr) : base(intPtr)
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
                        
                        KnockOutter.Instance.Knockout();
                    }
                }
            }
        }
    }
}