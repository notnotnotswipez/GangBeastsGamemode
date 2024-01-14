using System;
using BoneLib;
using LabFusion.MarrowIntegration;
using MelonLoader;
using SLZ.Rig;
using UnityEngine;

namespace GangBeastsGamemode.ProxyScripts
{
    [RegisterTypeInIl2Cpp]
    public class LauncherZone : FusionMarrowBehaviour
    {
        public LauncherZone(IntPtr intPtr) : base(intPtr)
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
                        
                        Player.rigManager.physicsRig.m_chest.GetComponent<Rigidbody>().AddForce(transform.forward * 200f, ForceMode.Impulse);
                    }
                }
            }
        }
    }
}