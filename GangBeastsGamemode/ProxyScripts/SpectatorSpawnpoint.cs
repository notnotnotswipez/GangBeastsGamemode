using System;
using LabFusion.MarrowIntegration;
using LabFusion.Utilities;
using MelonLoader;
using UnityEngine;

namespace GangBeastsGamemode.ProxyScripts
{
    [RegisterTypeInIl2Cpp]
    public class SpectatorSpawnpoint : FusionMarrowBehaviour    
    {
        public SpectatorSpawnpoint(IntPtr intPtr) : base(intPtr)
        {
        }
        
        public static readonly FusionComponentCache<GameObject, SpectatorSpawnpoint> Cache = new FusionComponentCache<GameObject, SpectatorSpawnpoint>();

        private void OnEnable() {
            Cache.Add(gameObject, this);
        }

        private void OnDisable() {
            Cache.Remove(gameObject);
        }
    }
}