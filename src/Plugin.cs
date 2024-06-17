using System;
using System.Reflection;
using UnityEngine;
using BepInEx;
using LethalLib.Modules;
using BepInEx.Logging;
using System.IO;

namespace SCP3199 {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    public class Plugin : BaseUnityPlugin {
        internal static new ManualLogSource Logger = null!;
        public static AssetBundle? ModAssets;

        private void Awake() {
            Logger = base.Logger;
            InitializeNetworkBehaviours();
            var bundleName = "scp3199modassets";
            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), bundleName));
            if (ModAssets == null) {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }

            // We load our assets from our asset bundle. Remember to rename them both here and in our Unity project.
            var scp3199 = ModAssets.LoadAsset<EnemyType>("Scp3199");
            var scp3199TN = ModAssets.LoadAsset<TerminalNode>("SCP3199TN");
            var scp3199TK = ModAssets.LoadAsset<TerminalKeyword>("SCP3199TK");
            NetworkPrefabs.RegisterNetworkPrefab(scp3199.enemyPrefab);

            // For different ways of registering your enemy, see https://github.com/EvaisaDev/LethalLib/blob/main/LethalLib/Modules/Enemies.cs
            Enemies.RegisterEnemy(scp3199, 40, Levels.LevelTypes.All, scp3199TN, scp3199TK);
            // For using our rarity tables, we can use the following:
            // Enemies.RegisterEnemy(SCP3199, ExampleEnemyLevelRarities, ExampleEnemyCustomLevelRarities, ExampleEnemyTN, ExampleEnemyTK);
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void InitializeNetworkBehaviours()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                );
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(
                        typeof(RuntimeInitializeOnLoadMethodAttribute),
                        false
                    );
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }

                // Check if the type is assignable to EnemyAI
                if (typeof(EnemyAI).IsAssignableFrom(type))
                {
                    // Patch the type here as needed
                    // For example, you could check for specific methods or attributes
                    // and invoke them similarly to RuntimeInitializeOnLoadMethodAttribute
                }
            }
        }



    }
}
