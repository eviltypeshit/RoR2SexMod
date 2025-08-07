using BepInEx;
using System;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections;
using Assets;
using EmotesAPI;
using System.Linq;
using System.Collections.Generic;
using KinematicCharacterController;
using RoR2.CharacterAI;

namespace SexMod
{
    [BepInDependency("com.weliveinasociety.CustomEmotesAPI")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class SexMod : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Glorpus";
        public const string PluginName = "SexMod";
        public const string PluginVersion = "0.0.1";
        public const int SEX_TIME = 15;
        public static List<SexAnimation> sexAnimations = new List<SexAnimation>();
        public static Dictionary<String, CharacterType> characterNames;
        public void Awake()
        {
            Log.Init(Logger);
            LoadAssets();
            GlobalEventManager.onServerDamageDealt += CheckForTrigger;
        }

        /// <summary>
        /// Adds an animation to the catalog of animations.
        /// </summary>
        /// <param name="animation">animation to be added</param>
        public static void AddSexAnimation(SexAnimation animation, AnimationClipParams topParams, AnimationClipParams bottomParams)
        {
            CustomEmotesAPI.AddCustomAnimation(topParams);
            CustomEmotesAPI.AddCustomAnimation(bottomParams);
            sexAnimations.Add(animation);
        }
        public static void AddSexAnimation(SexAnimation animation)
        {
            sexAnimations.Add(animation);
        }

        /// <summary>
        /// Makes (<paramref name="player"/> and <paramref name="mate"/>) begin a sex animation.
        /// </summary>
        /// <param name="player">GameObject corresponding to the player, eg. CommandoBody(Copy)</param>
        /// <param name="mate">GameObject player will have sex with, eg. LemurianBody(Copy)</param>
        /// <param name="mateIsTop"> True if mate is topping (penetrating) the player </param>
        public static IEnumerator Sex(GameObject player, GameObject mate, Boolean mateIsTop)
        {
            Log.Message("Sex Called!");

            // I don't know why, but this doesn't affect the player or mate's colliders. 
            GameObject[] validEntities = getValidEntities();
            shiftActive(false, validEntities);

            // Aligning Characters
            // disable colliders
            player.GetComponent<Collider>().enabled = false;
            mate.GetComponent<Collider>().enabled = false;
            // teleport characters
            TeleportHelper.TeleportBody(mate.GetComponent<CharacterBody>(), player.GetComponent<CharacterBody>().footPosition, true);
            // set rotation to 0
            if (mate.GetComponent<CharacterDirection>() != null)
            {
                mate.GetComponent<CharacterDirection>().yaw = 0;
            }

            // Play Animations
            // get skeletons
            BoneMapper mateMapper = mate.GetComponent<ModelLocator>().modelTransform.GetComponentInChildren<BoneMapper>();
            BoneMapper playerMapper = player.GetComponent<ModelLocator>().modelTransform.GetComponentInChildren<BoneMapper>();

            // call animations
            SexAnimation animation = findRandomAnimation();
            if (animation != null)
            {
                CustomEmotesAPI.PlayAnimation(animation.GetBottomName(), playerMapper);
                if (mateMapper != null)
                {
                    CustomEmotesAPI.PlayAnimation(animation.GetTopName(), mateMapper);
                }
            }
            else
            {
                Log.Warning("Did not find any suitable sex animations for " + mate.name + " and " + player.name + ". Using defaults.");
                CustomEmotesAPI.PlayAnimation("Bottoming2", playerMapper);
                if (mateMapper != null)
                {
                    CustomEmotesAPI.PlayAnimation("Topping2", mateMapper);
                }
            }
                
            // disable colliders again (I don't know why the colliders re-enable).
            player.GetComponent<Collider>().enabled = false;
            mate.GetComponent<Collider>().enabled = false;
            // stop player from doing inputs, because stunning them doesn't work for some reason.
            player.GetComponent<CharacterDirection>().yaw = 0;
            player.GetComponent<CharacterMotor>().enabled = false;
            player.GetComponent<CharacterDirection>().enabled = false;
            player.GetComponent<KinematicCharacterMotor>().enabled = false;
            foreach (GenericSkill skill in player.GetComponents<GenericSkill>())
            {
                skill.stock = 0;
                skill.enabled = false;
            }

            yield return new WaitForSeconds(SEX_TIME);

            CustomEmotesAPI.PlayAnimation("none", playerMapper);
            if (mateMapper != null)
            {
                CustomEmotesAPI.PlayAnimation("none", mateMapper);
            }

            // turn everything back on
            shiftActive(true, validEntities);
            mate.GetComponent<Collider>().enabled = true;
            player.GetComponent<CharacterMotor>().enabled = true;
            player.GetComponent<CharacterDirection>().enabled = true;
            player.GetComponent<KinematicCharacterMotor>().enabled = true;
            foreach (GenericSkill skill in player.GetComponents<GenericSkill>())
            {
                skill.enabled = true;
            }

            mate.GetComponent<SetStateOnHurt>().SetStun(3);
            TeleportHelper.TeleportGameObject(player, player.transform.position + Vector3.up * 5);
            TeleportHelper.TeleportGameObject(mate, mate.transform.position + Vector3.up * 5);
        }
        private static void LoadAssets()
        {
            Assets.AddBundle("skeletons");
            Assets.AddBundle("animations");
            CustomEmotesAPI.AddCustomAnimation(Assets.Load<AnimationClip>("topping2.anim"), true);
            CustomEmotesAPI.AddCustomAnimation(Assets.Load<AnimationClip>("bottoming2.anim"), true);
            CustomEmotesAPI.ImportArmature(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lemurian/LemurianBody.prefab").WaitForCompletion(), Assets.Load<GameObject>("lemurian.prefab"));
            CustomEmotesAPI.ImportArmature(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Golem/GolemBody.prefab").WaitForCompletion(), Assets.Load<GameObject>("golem.prefab"));
            CustomEmotesAPI.ImportArmature(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Imp/ImpBody.prefab").WaitForCompletion(), Assets.Load<GameObject>("golem.prefab"));
            CustomEmotesAPI.ImportArmature(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LemurianBruiser/LemurianBruiserBody.prefab").WaitForCompletion(), Assets.Load<GameObject>("elderlemurian.prefab"));

            SexAnimation sexAnimation = new SexAnimation("Topping2", "Bottoming2", CharacterType.Global, CharacterType.Global);
            AddSexAnimation(sexAnimation);
        }
        /// <summary>
        /// Changes if an array of entities are active
        /// </summary>
        /// <param name="active">True if entities are being turned on, false if being turned off</param>
        /// <param name="entities">Entities to shift activity of</param>
        private static void shiftActive(Boolean active, GameObject[] entities)
        {
            foreach (GameObject entity in entities)
            {
                if (!entity.GetComponent<HealthComponent>().isDefaultGodMode)
                {
                    entity.GetComponent<HealthComponent>().godMode = !active;
                }
                if (!active)
                {
                    if (entity.GetComponent<SetStateOnHurt>() != null)
                    {
                        entity.GetComponent<SetStateOnHurt>().SetStun(SEX_TIME);
                    }
                }
                if (entity.GetComponent<SetStateOnHurt>() == null && entity.GetComponent<CharacterMaster>() != null)
                {
                    foreach (BaseAI ai in entity.GetComponent<CharacterMaster>().aiComponents)
                    {
                        ai.enabled = active;
                    }
                }

            }
        }
        /// <summary>
        /// gets all entities
        /// </summary>
        /// <returns></returns>
        private static GameObject[] getValidEntities()
        {
            List<GameObject> result = ((GameObject[])FindObjectsByType(typeof(GameObject), FindObjectsSortMode.None)).ToList<GameObject>();
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].GetComponent<HealthComponent>() == null || result[i].GetComponent<CharacterMotor>() == null)
                {
                    result.RemoveAt(i);
                    i--;
                }
            }
            return result.ToArray();
        }
        /// <summary>
        /// checks if player should be put into sex after a hit occurs.
        /// </summary>
        /// <param name="report"></param>
        private void CheckForTrigger(DamageReport report)
        {
            // if the victim is a player, the attacker and player are alive, and player health is low
            if (report.victim.gameObject.tag == "Player" && report.attacker != null && report.attacker.tag != "Player" && report.victim.alive && report.victim.isHealthLow)
            {
                StartCoroutine(Sex(report.victim.gameObject, report.attacker.gameObject, true));
            }
        }
        private static SexAnimation findRandomAnimation(CharacterType topCharacterType, CharacterType bottomCharacterType) 
        { 
            List<SexAnimation> set = new List<SexAnimation>();
            for (int i = 0; i < sexAnimations.Count; i++)
            {
                if (sexAnimations[i].isMatchedTypes(topCharacterType, bottomCharacterType))
                {
                    set.Add(sexAnimations[i]);
                }
            }
            if (set.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, set.Count);
                return set[index];
            }
            return null;
        }
        /// <summary>
        /// finds a random animation.
        /// </summary>
        /// <returns>a globally usable sex animation</returns>
        private static SexAnimation findRandomAnimation()
        {
            List<SexAnimation> set = new List<SexAnimation>();
            for (int i = 0; i < sexAnimations.Count; i++)
            {
                if (sexAnimations[i].isMatchedTypes(CharacterType.Global, CharacterType.Global))
                {
                    set.Add(sexAnimations[i]);
                }
            }
            if (set.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, set.Count);
                return set[index];
            }
            return null;
        }
    }

    public class SexAnimation
    {
        private String topName;
        private String bottomName;
        private CharacterType bottomCharacterType;
        private CharacterType topCharacterType;
        /// <summary>
        /// Creates a new SexAnimation object, consisting of a topping animation and a bottoming animation that will be played together.
        /// </summary>
        /// <param name="topParameters">AnimationClipParams for the topping animation</param>
        /// <param name="bottomParameters">AnimationClipParams for the bottoming animation></param>
        /// <param name="topName">Name for the topping animation, what you named the clip in unity when you exported it. NOT the file (eg. run.anim), the name of the animation (eg. Run).</param>
        /// <param name="bottomName">Name for the topping animation, what you named the clip in unity when you exported it. NOT the file (eg. run.anim), the name of the animation (eg. Run).</param>
        /// <param name="topCharacterType">The type of character that is included in the topping animation</param>
        /// <param name="bottomCharacterType">The type of character that is included in the bottoming animation</param>
        public SexAnimation(String topName, String bottomName, CharacterType topCharacterType, CharacterType bottomCharacterType)
        {
            this.topName = topName;
            this.bottomName = bottomName;
            this.topCharacterType = topCharacterType;
            this.bottomCharacterType = bottomCharacterType;

        }
        public String GetTopName()
        {
            return topName;
        }
        public String GetBottomName()
        {
            return bottomName;
        }
        public CharacterType GetTopCharacterType()
        {
            return topCharacterType;
        }
        public CharacterType GetBottomCharacterType()
        {
            return bottomCharacterType;
        }
        public Boolean isMatchedTypes(CharacterType topType, CharacterType bottomType)
        {
            if ((bottomType == bottomCharacterType || bottomType == CharacterType.Global) && (topType == topCharacterType || topType == CharacterType.Global))
            {
                return true;
            }
            return false;
        }
    }
    public enum CharacterType
    {
        Global,
        Player,
        Vulture,
        MinorConstruct,
        Beetle,
        BeetleGuard,
        Bison,
        FlyingVermin,
        Vermin,
        Bell,
        Child,
        ClayGrenadier,
        ClayBruiser,
        LemurianBruiser,
        Geep,
        Gip,
        GreaterWisp,
        Gup,
        Halcyonite,
        HermitCrab,
        Imp,
        Jellyfish,
        AcidLarva,
        Lemurian,
        Wisp,
        LunarExploder,
        LunarGolem,
        LunarWisp,
        MiniMushroom,
        Parent,
        Scorchling,
        RoboBallMini,
        Golem,
        VoidBarnacle,
        VoidJailer,
        Nullifier,
        BeetleQueen,
        ClayBoss,
        GrandParent,
        Gravekeeper,
        ImpBoss,
        MagmaWorm,
        ElectricWorm,
        Scav,
        RoboBallBoss,
        Titan,
        VoidMegaCrab,
        Vagrant,
        MegaConstruct,
        Shopkeeper,
        VoidInfestor,
        SuperRoboBallBoss,
        TitanGold,
        FalseSonBoss,
        ScavLunar,
        Brother,
        VoidRaidCrab
    }

}
