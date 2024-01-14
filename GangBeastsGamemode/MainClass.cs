using System;
using System.Collections.Generic;
using System.Reflection;
using BoneLib;
using BoneLib.BoneMenu.Elements;
using BoneLib.Nullables;
using GangBeastsGamemode.ProxyScripts;
using HarmonyLib;
using LabFusion.Data;
using LabFusion.MarrowIntegration;
using LabFusion.Network;
using LabFusion.Representation;
using LabFusion.SDK.Gamemodes;
using LabFusion.Utilities;
using MelonLoader;
using SLZ.Interaction;
using SLZ.Marrow.Data;
using SLZ.Marrow.Pool;
using SLZ.Marrow.SceneStreaming;
using SLZ.Marrow.Warehouse;
using SLZ.Rig;
using SLZ.SFX;
using SwipezGamemodeLib.Events;
using SwipezGamemodeLib.Extensions;
using SwipezGamemodeLib.Spawning;
using SwipezGamemodeLib.Spectator;
using SwipezGamemodeLib.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GangBeastsGamemode
{

    public class MainClass : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Hooking.OnLevelInitialized += OnLevelInitialized;
            SwipezGamemodeLibEvents.OnPlayerSendEvent += OnPlayerSendEvent;
            
            MultiplayerHooking.OnLoadingBegin += OnLoadingBegin;
            GamemodeRegistration.LoadGamemodes(Assembly.GetExecutingAssembly());
            
            AssetBundle assetBundle;

            if (!HelperMethods.IsAndroid())
            {
                assetBundle = EmbeddedAssetBundle.LoadFromAssembly(Assembly.GetExecutingAssembly(), "GangBeastsGamemode.Resources.gangbeasts.gamemode");
            }
            else
            {
                //TODO: CHANGE THIS!!
                assetBundle = EmbeddedAssetBundle.LoadFromAssembly(Assembly.GetExecutingAssembly(), "GangBeastsGamemode.Resources.gangbeasts.android.gamemode");
            }
            
            GangBeastsAssets.LoadAssets(assetBundle);
        }
        
        private void OnLoadingBegin()
        {
            if (GangBeastsMode.IsFullActive())
            {
                if (NetworkInfo.IsServer)
                {
                    GangBeastsMode.Instance.BeginLoadState();
                }
            }
            
            GangBeastsMode.selfLoaded = false;
            GangBeastsMode.lockGameState = false;
        }
        
        private void OnPlayerSendEvent(PlayerId playerId, string eventName)
        {
            if (eventName == GangBeastsMode.PlayerKnockOutKey)
            {
                if (GangBeastsMode.IsFullActive())
                {
                    PlayerRepManager.TryGetPlayerRep(playerId, out var rep);
                    FusionAudio.Play3D(rep.RigReferences.RigManager.physicsRig.m_pelvis.position,
                        GangBeastsAssets.elimination, 1);
                }
            }
        }

        private void OnLevelInitialized(LevelInfo info)
        {
            Player.physicsRig.gameObject.AddComponent<KnockOutter>();
            Player.physicsRig.gameObject.AddComponent<StaminaSystem>();
            GangBeastsMode.selfLoaded = true;
            if (GangBeastsMode.Instance != null)
            {
                if (GangBeastsMode.Instance.IsActive())
                {
                    GangBeastsMode.Instance.OnLoadingFinished();
                }
            }
        }
    }

    public class GangBeastsAssets
    {
        public static AudioClip knockout;
        public static AudioClip elimination;
        public static AudioClip ding;
        public static GameObject scoreCard;
        public static GameObject playerScore;
        public static GameObject singlePoint;

        public static Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();

        public static void LoadAssets(AssetBundle bundle)
        {
            knockout = bundle.LoadPersistentAsset<AudioClip>("assets/bundleassets/knockout.mp3");
            elimination = bundle.LoadPersistentAsset<AudioClip>("assets/bundleassets/final.mp3");
            ding = bundle.LoadPersistentAsset<AudioClip>("assets/bundleassets/ding.ogg");
            
            scoreCard = bundle.LoadPersistentAsset<GameObject>("assets/bundleassets/scorecard.prefab");
            playerScore = bundle.LoadPersistentAsset<GameObject>("assets/bundleassets/playerscore.prefab");
            singlePoint = bundle.LoadPersistentAsset<GameObject>("assets/bundleassets/singlepoint.prefab");

            foreach (var asset in bundle.GetAllAssetNames())
            {
                if (asset.EndsWith("png"))
                {
                    Texture2D texture = bundle.LoadPersistentAsset<Texture2D>(asset);
                    string mapping = asset.Replace("assets/bundleassets/", "").Replace(".png", "");
                    
                    mappedTextures.Add(mapping, texture);
                }
            }
            
            
        }
    }

    [HarmonyPatch(typeof(PhysicsRig), "UnRagdollRig")]
    public class OnUnRagdollKnockedOutPatch {
        public static bool Prefix(PhysicsRig __instance)
        {
            if (__instance.manager.GetInstanceID() != Player.rigManager.GetInstanceID())
            {
                return true;
            }

            if (GangBeastsMode.IsFullActive())
            {
                if (KnockOutter.Instance.knockedOut)
                {
                    __instance.RagdollRig();
                    return false;
                }

                var teleport = Player.physicsRig.feet.transform.position + new Vector3(0, 0.25f, 0);
                Player.rigManager.Teleport(teleport);
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(Hand), "AttachObject")]
    public class OnKnockOutHandGrabPatch {
        public static bool Prefix(Hand __instance)
        {
            if (__instance.manager.GetInstanceID() != Player.rigManager.GetInstanceID())
            {
                return true;
            }

            if (GangBeastsMode.IsFullActive())
            {
                if (KnockOutter.Instance.knockedOut)
                {
                    return false;
                }
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(HandSFX), "PunchAttack", typeof(Collision), typeof(float), typeof(float))]
    public static class PunchDamagePatch
    {
        public static void Prefix(HandSFX __instance, Collision c)
        {
            if (GangBeastsMode.IsFullActive())
            {
                if (__instance._hand.manager.GetInstanceID() == Player.rigManager.GetInstanceID())
                {
                    return;
                }

                RigManager managerPunched = c.gameObject.GetComponentInParent<RigManager>();

                if (managerPunched)
                {
                    if (managerPunched.GetInstanceID() != Player.rigManager.GetInstanceID())
                    {
                        return;
                    }

                    KnockOutter.Instance.Punch();
                }
            }
        }
    }

    public class GangBeastsMode : Gamemode
    {
        public override string GamemodeCategory => "Gang Beasts";
        public override string GamemodeName => "Gang Beasts Mode";
        
        public override bool DisableDevTools => true;
        public override bool DisableSpawnGun => true;
        public override bool PreventNewJoins => true;
        
        public override bool AutoStopOnSceneLoad => false;
        
        public const string DefaultPrefix = "InternalGangBeastsMetadata";
        public const string PlayerScoreKey = DefaultPrefix + ".Score";
        public const string PlayerRoleKey = DefaultPrefix + ".Role";
        public const string PlayerColorKey = DefaultPrefix + ".Color";
        
        public const string PlayerKnockOutKey = DefaultPrefix + ".KnockOut";

        public const string SPECTATOR_ROLE = "Spectator";
        public const string PLAYER_ROLE = "Player";

        public const string CONFETTI_BARCODE = "notnotnotswipez.GangBeasts.Spawnable.Confetti";
        public const string GOLDEN_LOAF_BARCODE = "notnotnotswipez.GangBeasts.Spawnable.GoldenLoaf";

        public bool roundRunning = false;
        public static bool selfLoaded = false;
        
        public GamemodeTimer mapTimer = new GamemodeTimer();
        
        public GameObject scoreCard;

        public int scoreToWin = 5;
        
        public static float scoreCardDistance = 1f;
        public bool lockMap = false;
        
        public static bool lockGameState = false;

        private static List<string> availableColors = new List<string>()
        {
            "red",
            "yellow",
            "blue",
            "green",
            "orange",
            "purple",
            "tan",
            "pink"
        };

        private static List<string> maps = new List<string>()
        {
            "notnotnotswipez.GangBeasts.Level.GangBeastsSubway",
            "notnotnotswipez.GangBeasts.Level.GangBeastsGirders",
            "notnotnotswipez.GangBeasts.Level.GangBeastsGrind",
            "notnotnotswipez.GangBeasts.Level.GangBeastsIncinerator",
            "notnotnotswipez.GangBeasts.Level.GangBeastsRooftop"
        };

        private static List<string> shuffle = new List<string>();

        public static List<int> ignoredRigInstances = new List<int>();

        public static GangBeastsMode Instance { get; private set; }

        public override void OnGamemodeRegistered()
        {
            base.OnGamemodeRegistered();
            
            Instance = this;
            FusionOverrides.OnValidateNametag += OnValidateNametag;
        }
        
        public override void OnGamemodeUnregistered()
        {
            base.OnGamemodeUnregistered();
            if (Instance == this)
                Instance = null;
            
            FusionOverrides.OnValidateNametag -= OnValidateNametag;
        }

        public override void OnBoneMenuCreated(MenuCategory category)
        {
            base.OnBoneMenuCreated(category);

            category.CreateIntElement("Rounds To Win", Color.yellow, scoreToWin, 1, 2, 7, i =>
            {
                scoreToWin = i;
            });    
            category.CreateBoolElement("Lock Map", Color.yellow, lockMap,  i =>
            {
                lockMap = i;
            });     
        }

        protected override void OnStartGamemode()
        {
            base.OnStartGamemode();
            
            if (NetworkInfo.IsServer)
            {
                // Shuffle
                shuffle = new List<string>(maps);
                for (int i = 0; i < shuffle.Count; i++)
                {
                    string temp = shuffle[i];
                    int randomIndex = Random.Range(i, shuffle.Count);
                    shuffle[i] = shuffle[randomIndex];
                    shuffle[randomIndex] = temp;
                }
                
                if (!maps.Contains(FusionSceneManager.Level._barcode))
                {
                    FusionNotifier.Send(new FusionNotification()
                    {
                        title = new NotificationText($"You are NOT on a Gang Beasts Map!", Color.cyan, true),
                        showTitleOnPopup = true,
                        popupLength = 3f,
                        isMenuItem = false,
                        isPopup = true,
                    });
                    lockGameState = true;
                    StopGamemode();
                    return;
                }

                List<string> availableColorsCopy = new List<string>(availableColors);
                foreach (PlayerId playerId in PlayerIdManager.PlayerIds) {
                    SetRole(playerId, PLAYER_ROLE);
                    SetScore(playerId, 0);
                    string randomColor = availableColorsCopy[Random.Range(0, availableColorsCopy.Count)];
                    SetColor(playerId, randomColor);
                    availableColorsCopy.Remove(randomColor);
                }
            }
            
            FusionPlayer.SetMortality(false);
        }
        
        protected override void OnStopGamemode()
        {
            base.OnStopGamemode();
            
            roundRunning = false;
            mapTimer.Reset();
            if (scoreCard)
            {
                 GameObject.Destroy(scoreCard);
            }

            lockGameState = false;
            
            
            foreach (PlayerId playerId in PlayerIdManager.PlayerIds) {
                SetRole(playerId, PLAYER_ROLE);
                SetScore(playerId, 0);
                
                playerId.SetHeadIcon(null);
            }
            
            FusionOverrides.ForceUpdateOverrides();
        }

        

        public void BeginLoadState()
        {
            roundRunning = false;
            TryInvokeTrigger("EndRoundState");
        }
        
        public void StartRoundState()
        {
            roundRunning = true;
            TryInvokeTrigger("StartRoundState");
        }

        private void SpawnConfetti(Vector3 pos)
        {
            SpawnManager.SpawnGameObject(CONFETTI_BARCODE, pos, Quaternion.identity, go => { });
        }

        private void SpawnLoaf(Vector3 pos)
        {
            SpawnManager.SpawnGameObject(GOLDEN_LOAF_BARCODE, pos, Quaternion.identity, go => { });
        }
        
        protected bool OnValidateNametag(PlayerId id) {
            if (!IsActive())
            {
                return true;
            }
            return false;
        }

        private void GoToRandomMap()
        {
            if (NetworkInfo.IsServer)
            {
                if (!lockMap)
                {
                    string selectedBarcode = shuffle[Random.Range(0, shuffle.Count)];
                    // Remove it from the list
                    shuffle.Remove(selectedBarcode);
                    SceneStreamer.Load(selectedBarcode);
                    
                    // Reshuffle if empty
                    if (shuffle.Count == 0)
                    {
                        shuffle = new List<string>(maps);
                        for (int i = 0; i < shuffle.Count; i++)
                        {
                            string temp = shuffle[i];
                            int randomIndex = Random.Range(i, shuffle.Count);
                            shuffle[i] = shuffle[randomIndex];
                            shuffle[randomIndex] = temp;
                        }
                    }
                }
                else
                {
                    SceneStreamer.Load(SceneStreamer._session.Level._barcode);
                }
            }
        }

        protected override void OnEventTriggered(string value)
        {
            if (value == "EndRoundState")
            {
                roundRunning = false;
                FusionSceneManager.HookOnTargetLevelLoad(() => {
                    foreach (PlayerId playerId in PlayerIdManager.PlayerIds)
                    {
                        playerId.Show();
                    }
                });
            }
            
            if (value == "StartRoundState")
            {
                roundRunning = true;
                FusionSceneManager.HookOnTargetLevelLoad(() => {
                    
                    foreach (PlayerId playerId in PlayerIdManager.PlayerIds)
                    {
                        if (!playerId.IsSelf)
                        {
                            playerId.SetHeadIcon(null);
                            playerId.SetHeadIcon(GangBeastsAssets.mappedTextures[GetColor(playerId)]);
                        }
                    }
                    
                    FusionPlayerExtended.SetWorldInteractable(true);
                    FusionPlayer.SetMortality(false);
                    
                    List<Transform> transforms = new List<Transform>();
                    foreach (var point in DeathmatchSpawnpoint.Cache.Components) {
                        transforms.Add(point.transform);
                    }
                    
                    Transform spawn = transforms[Random.Range(0, transforms.Count)];
                    
                    FusionPlayer.Teleport(spawn.position, spawn.forward);

                    lockGameState = false;
                    
                    FusionNotifier.Send(new FusionNotification()
                    {
                        title = new NotificationText($"START!", Color.cyan, true),
                        showTitleOnPopup = true,
                        popupLength = 3f,
                        isMenuItem = false,
                        isPopup = true,
                    });
                    
                    FusionOverrides.ForceUpdateOverrides();
                    
                    ignoredRigInstances.Clear();
                    
                    Player_Health health = Player.rigManager.GetComponentInChildren<Player_Health>();
                    health.Vignetter.gameObject.SetActive(false);
                });
            }

            if (value.StartsWith("RoundWin"))
            {
                PlayerId winner = PlayerIdManager.GetPlayerId(byte.Parse(value.Split(';')[1]));
                winner.TryGetDisplayName(out var name);

                foreach (PlayerId playerId in PlayerIdManager.PlayerIds)
                {
                    if (NetworkInfo.IsServer)
                    {
                        SetRole(playerId, PLAYER_ROLE);
                    }

                    playerId.Show();
                    //playerId.SetHeadIcon(null);
                    //playerId.SetHeadIcon(GangBeastsAssets.mappedTextures[GetColor(playerId)]);
                }

                lockGameState = true;
                
                Vector3 pos;
                
                if (!winner.IsSelf)
                {
                    PlayerRepManager.TryGetPlayerRep(winner, out var rep);
                    pos = rep.RigReferences.RigManager.physicsRig.m_head.position + new Vector3(0, 1, 0);
                }
                else
                {
                    pos = Player.physicsRig.m_head.position + new Vector3(0, 1, 0);
                }

                SpawnConfetti(pos);
                
                CreateScoreCard();
            }
            
            if (value.StartsWith("TotalWin"))
            {
                PlayerId winner = PlayerIdManager.GetPlayerId(byte.Parse(value.Split(';')[1]));
                winner.TryGetDisplayName(out var name);
                
                
                FusionNotifier.Send(new FusionNotification()
                {
                    title = new NotificationText($"{name} wins!", Color.cyan, true),
                    showTitleOnPopup = true,
                    popupLength = 3f,
                    isMenuItem = false,
                    isPopup = true,
                });
                
                Vector3 pos;
                
                if (!winner.IsSelf)
                {
                    PlayerRepManager.TryGetPlayerRep(winner, out var rep);
                    pos = rep.RigReferences.RigManager.physicsRig.m_head.position + new Vector3(0, 2, 0);
                }
                else
                {
                    pos = Player.physicsRig.m_head.position + new Vector3(0, 2, 0);
                }
                
                SpawnLoaf(pos);
            }
        }

        private void CreateScoreCard()
        {
            scoreCard = GameObject.Instantiate(GangBeastsAssets.scoreCard);
            scoreCard.transform.position = Player.physicsRig.m_head.position + Player.controllerRig.hmdTransform.forward * scoreCardDistance;
            scoreCard.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Transform playerListHolder = scoreCard.transform.Find("Canvas").Find("Lister");

            foreach (var playerId in PlayerIdManager.PlayerIds)
            {
                GameObject playerScore = GameObject.Instantiate(GangBeastsAssets.playerScore, playerListHolder);
                Texture2D color = GangBeastsAssets.mappedTextures[GetColor(playerId)];
                RawImage image = playerScore.transform.Find("PlayerIcon").GetComponent<RawImage>();
                image.texture = color;
                for (int i = 0; i < GetScore(playerId); i++)
                {
                    GameObject.Instantiate(GangBeastsAssets.singlePoint, playerScore.transform.Find("ScoreHolding"));
                }
            }
        }

        protected override void OnMetadataChanged(string key, string value)
        {
            if (key.StartsWith(PlayerRoleKey))
            {
                string[] split = key.Split('.');
                byte id = byte.Parse(split[2]);
                PlayerId associatedId = PlayerIdManager.GetPlayerId(id);
                
                OnRoleChanged(associatedId, value);
            }
        }

        private void OnRoleChanged(PlayerId playerId, string role)
        {
            
            if (role == SPECTATOR_ROLE)
            {
                if (playerId.IsSelf)
                {
                    FusionSceneManager.HookOnTargetLevelLoad(() =>
                    {
                        FusionPlayerExtended.SetCanDamageOthers(false);
                        FusionPlayerExtended.SetWorldInteractable(false);
                        
                        List<Transform> transforms = new List<Transform>();
                        foreach (var point in SpectatorSpawnpoint.Cache.Components) {
                            transforms.Add(point.transform);
                        }
                     
                        Transform spawn = transforms[Random.Range(0, transforms.Count)];
                     
                        FusionPlayer.Teleport(spawn.position, spawn.forward);
                        
                        FusionAudio.Play2D(GangBeastsAssets.ding, 1);
                        
                        QuicksandZone.ResetValues();
                    });
                }
                else
                {
                    FusionSceneManager.HookOnTargetLevelLoad(() =>
                    {
                        playerId.Hide();
                        playerId.SetHeadIcon(null);
            
                        FusionAudio.Play2D(GangBeastsAssets.ding, 1);
                    });
                }
            }
            else
            {
                if (playerId.IsSelf)
                {
                    FusionSceneManager.HookOnTargetLevelLoad(() =>
                    {
                        FusionPlayerExtended.SetCanDamageOthers(true);
                        FusionPlayerExtended.SetWorldInteractable(true);
                    });
                }
                else
                {
                    FusionSceneManager.HookOnTargetLevelLoad(() =>
                    {
                        playerId.Show();
                    });
                }
            }
        }
        
        private List<PlayerId> GetAlivePlayers()
        {
            List<PlayerId> alivePlayers = new List<PlayerId>();
            foreach (var playerId in PlayerIdManager.PlayerIds)
            {
                if (GetRole(playerId) != SPECTATOR_ROLE)
                {
                    alivePlayers.Add(playerId);
                }
            }

            return alivePlayers;
        }

        public void SetScore(PlayerId playerId, int score)
        {
            TrySetMetadata(GetScoreKey(playerId), score+"");
        }
        
        public void SetRole(PlayerId playerId, string role)
        {
            TrySetMetadata(GetRoleKey(playerId), role);
        }
        
        public void SetColor(PlayerId playerId, string color)
        {
            TrySetMetadata(GetColorKey(playerId), color);
        }
        
        public int GetScore(PlayerId playerId)
        {
            TryGetMetadata(GetScoreKey(playerId), out var value);
            
            if (value == null)
            {
                return 0;
            }
            
            return int.Parse(value);
        }
         
        public string GetRole(PlayerId playerId)
        {
            TryGetMetadata(GetRoleKey(playerId), out var value);
            
            if (value == null)
            {
                return PLAYER_ROLE; 
            }

            return value;
        }
        
        public string GetColor(PlayerId playerId)
        {
            TryGetMetadata(GetColorKey(playerId), out var value);
            
            if (value == null)
            {
                return "red"; 
            }

            return value;
        }
         
        private string GetScoreKey(PlayerId playerId)
        {
            return PlayerScoreKey + "." + playerId.SmallId;
        }
         
        private string GetRoleKey(PlayerId playerId)
        {
            return PlayerRoleKey + "." + playerId.SmallId;
        }
        
        private string GetColorKey(PlayerId playerId)
        {
            return PlayerColorKey + "." + playerId.SmallId;
        }

        public void OnLoadingFinished()
        {
            if (!roundRunning)
            {
                FusionPlayer.SetMortality(false);
                FusionPlayerExtended.SetWorldInteractable(false);
            }
        }

        protected override void OnUpdate()
        {
            if (NetworkInfo.IsServer)
            {
                if (PlayerIdManager.PlayerIds.Count == 1)
                {
                    StopGamemode();
                    return;
                }

                if (mapTimer.isRunning)
                {
                    if (mapTimer.IsFinishedInSeconds(10))
                    {
                        
                        foreach (PlayerId id in PlayerIdManager.PlayerIds)
                        {
                            SetRole(id, PLAYER_ROLE);
                        }

                        mapTimer.Reset();
                        GoToRandomMap();
                    }
                }

                if (!roundRunning && selfLoaded && !lockGameState)
                {
                    if (EveryoneLoaded())
                    {
                        StartRoundState();
                    }
                }
                
                if (roundRunning && !mapTimer.isRunning && !lockGameState)
                {
                    List<PlayerId> alivePlayers = GetAlivePlayers();
                    if (alivePlayers.Count == 1)
                    {
                        PlayerId winner = alivePlayers[0];
                        SetScore(winner, GetScore(winner) + 1);
                        if (GetScore(winner) >= scoreToWin - 1)
                        {
                            TryInvokeTrigger("TotalWin;" + winner.SmallId);
                            StopGamemode(); 
                            roundRunning = false;
                            lockGameState = false;
                            mapTimer.Reset();
                            return;
                        }
                        
                        TryInvokeTrigger("RoundWin;" + winner.SmallId);
                        
                        mapTimer.Start();
        
                        
                    }
                }
            }

            if (scoreCard)
            {
                Vector3 target = Player.physicsRig.m_head.position + Player.controllerRig.hmdTransform.forward * scoreCardDistance;
                // Lerp
                scoreCard.transform.position = Vector3.Lerp(scoreCard.transform.position, target, Time.deltaTime * 10f);
                // Look at player
                scoreCard.transform.LookAt(Player.physicsRig.m_head.position);
            }

            if (roundRunning)
            {
                Player.rigManager.health.curr_Health = Player.rigManager.health.max_Health;
            }
        }

        private bool EveryoneLoaded()
        {
            bool allLoaded = true;
            foreach (var rep in PlayerRepManager.PlayerReps)
            {
                if (!rep.IsCreated)
                {
                    allLoaded = false;
                }
            }

            return allLoaded;
        }

        public static bool IsFullActive()
        {
            if (Instance != null)
            {
                if (Instance.IsActive())
                {
                    return Instance.roundRunning;
                }
            }

            return false;
        }
    }
}
