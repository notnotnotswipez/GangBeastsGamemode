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
    public class KillZone : FusionMarrowBehaviour
    {
        public KillZone(IntPtr intPtr) : base(intPtr)
        {
        }

        public void OnTriggerEnter(Collider other)
        {
            if (GangBeastsMode.IsFullActive())
            {
                if (GangBeastsMode.lockGameState)
                {
                    return;
                }

                if (other.attachedRigidbody && NetworkInfo.IsServer)
                {
                    RigManager parentManager = other.attachedRigidbody.GetComponentInParent<RigManager>();
                    if (parentManager && !GangBeastsMode.ignoredRigInstances.Contains(parentManager.gameObject.GetInstanceID()))
                    {
                        if (parentManager.gameObject.GetInstanceID() == Player.rigManager.gameObject.GetInstanceID())
                        {
                            string role = GangBeastsMode.Instance.GetRole(PlayerIdManager.LocalId);
                            if (role == GangBeastsMode.SPECTATOR_ROLE)
                            {
                                return;
                            }
                            
                            GangBeastsMode.Instance.SetRole(PlayerIdManager.LocalId, GangBeastsMode.SPECTATOR_ROLE);
                            GangBeastsMode.ignoredRigInstances.Add(parentManager.gameObject.GetInstanceID());
                        }
                        else
                        {
                            if (PlayerRepManager.TryGetPlayerRep(parentManager, out var rep))
                            {
                                string role = GangBeastsMode.Instance.GetRole(rep.PlayerId);
                                if (role == GangBeastsMode.SPECTATOR_ROLE)
                                {
                                    return;
                                }
                            
                                GangBeastsMode.Instance.SetRole(rep.PlayerId, GangBeastsMode.SPECTATOR_ROLE);
                                GangBeastsMode.ignoredRigInstances.Add(parentManager.gameObject.GetInstanceID());
                            }
                        }
                    }
                }
            }
        }
    }
}