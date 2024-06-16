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
            var bundleName = "modassets";
            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), bundleName));
            if (ModAssets == null) {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }

            // We load our assets from our asset bundle. Remember to rename them both here and in our Unity project.
            var ExampleEnemy = ModAssets.LoadAsset<EnemyType>("SCP3199");
            var ExampleEnemyTN = ModAssets.LoadAsset<TerminalNode>("ExampleEnemyTN");
            var ExampleEnemyTK = ModAssets.LoadAsset<TerminalKeyword>("ExampleEnemyTK");
            NetworkPrefabs.RegisterNetworkPrefab(ExampleEnemy.enemyPrefab);

            // For different ways of registering your enemy, see https://github.com/EvaisaDev/LethalLib/blob/main/LethalLib/Modules/Enemies.cs
            Enemies.RegisterEnemy(ExampleEnemy, BoundConfig.SpawnWeight.Value, Levels.LevelTypes.All, ExampleEnemyTN, ExampleEnemyTK);
            // For using our rarity tables, we can use the following:
            // Enemies.RegisterEnemy(SCP3199, ExampleEnemyLevelRarities, ExampleEnemyCustomLevelRarities, ExampleEnemyTN, ExampleEnemyTK);
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void InitializeNetworkBehaviours() {
            // See https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        } 
    }
}