using System;
using System.Collections.Generic;
using BoneLib;
using LabFusion.MarrowIntegration;
using MelonLoader;
using SLZ.Rig;
using SLZ.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace GangBeastsGamemode.ProxyScripts
{
    [RegisterTypeInIl2Cpp]
    public class QuicksandZone : FusionMarrowBehaviour
    {
        public QuicksandZone(IntPtr intPtr) : base(intPtr)
        {
        }

        public static bool isAvailable = true;
        public static bool isStuck = false;

        public static Rigidbody currentDropper;
        public static FixedJoint currentJoint;
        public static GenericOnJointBreak currentJointBreak;

        public void Awake()
        {
            isAvailable = true;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (GangBeastsMode.IsFullActive() && isAvailable)
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

                        isAvailable = false;
                        isStuck = true;
                        
                        GameObject dropper = new GameObject();

                        Rigidbody pelvis = Player.rigManager.physicsRig.m_pelvis.gameObject.GetComponent<Rigidbody>();
                        dropper.transform.position = pelvis.position;

                        Rigidbody fallerBody = dropper.AddComponent<Rigidbody>();
                        fallerBody.drag = 70;
                        fallerBody.angularDrag = 1;
                        fallerBody.mass = 200;
                        GenericOnJointBreak genericOnJointBreak = pelvis.gameObject.AddComponent<GenericOnJointBreak>();

                        FixedJoint joint = pelvis.gameObject.AddComponent<FixedJoint>();
                        joint.connectedBody = fallerBody;
                        joint.breakForce = 60000;
                        joint.breakTorque = float.MaxValue;


                        currentDropper = fallerBody;
                        currentJoint = joint;
                        currentJointBreak = genericOnJointBreak;

                        genericOnJointBreak.JointBreakEvent = new UnityEvent();

                        genericOnJointBreak.JointBreakEvent.AddListener(new Action(() =>
                        {
                            Destroy(currentJointBreak);
                            Destroy(dropper);
                            isStuck = false;
                        }));
                    }
                }
            }
        }

        public static void ResetValues()
        {
            isAvailable = true;
            isStuck = false;
            
            if (currentJoint)
            {
                Destroy(currentJoint);
            }
            
            if (currentDropper)
            {
                Destroy(currentDropper.gameObject);
            }
            
            if (currentJointBreak)
            {
                Destroy(currentJointBreak);
            }
        }
    }
}