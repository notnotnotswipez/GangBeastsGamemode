using System;
using BoneLib;
using LabFusion.Patching;
using LabFusion.Utilities;
using MelonLoader;
using SwipezGamemodeLib.Events;
using SwipezGamemodeLib.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GangBeastsGamemode
{
    [RegisterTypeInIl2Cpp]
    public class KnockOutter : MonoBehaviour
    {
        
        public int previousKnockouts = 0;
        public float knockoutTime = 0;
        
        public float minimumKnockoutTime = 1.5f;
        public float randomizationRate = 2f;
        
        public float maxKnockoutTime = 13f;
        
        public float knockoutChance = 0.15f;

        public bool knockedOut = false;
        
        public static KnockOutter Instance;
        
        public KnockOutter(IntPtr intPtr) : base(intPtr)
        {
        }
        
        public void Awake()
        {
            Instance = this;
        }
        
        public void Punch()
        {
            if (knockedOut)
            {
                return;
            }
            
            if (Random.Range(0f, 1f) < knockoutChance)
            {
                Knockout();
                //knockoutChance += 0.01f;
                knockoutChance = Mathf.Clamp(knockoutChance, 0, 0.5f);
            }
        }
        
        public void Update()
        {
            if (knockoutTime > 0 && knockedOut)
            {
                knockoutTime -= Time.deltaTime;
                if (knockoutTime < 0)
                {
                    UnKnockout();
                }
            }
        }

        private void UnKnockout()
        {
            knockoutTime = 0;
            knockedOut = false;
            Player.rigManager.physicsRig.UnRagdollRig();
        }
        
        public void Knockout()
        {
            if (knockedOut)
            {
                return;
            }
             
            SwipezGamemodeLibEvents.SendPlayerEvent(GangBeastsMode.PlayerKnockOutKey);
            
            FusionAudio.Play3D(Player.rigManager.physicsRig.m_pelvis.position,
                GangBeastsAssets.elimination, 1);
            
            previousKnockouts++;
            for (int i = 0; i < previousKnockouts; i++)
            {
                knockoutTime += minimumKnockoutTime - (Random.RandomRange(0, randomizationRate));
            }
            
            knockoutTime = Mathf.Clamp(knockoutTime, 0, maxKnockoutTime);
            
            knockedOut = true;
            
            Player.rigManager.physicsRig.RagdollRig();
            
            Player.leftHand.DetachJoint();
            Player.rightHand.DetachJoint();
                    
            Player.leftHand.DetachObject();
            Player.rightHand.DetachObject();
        }
    }
}