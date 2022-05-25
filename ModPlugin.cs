using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using IPA;
using IPA.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
//using Harmony;
using UnityEngine.Networking;
using BeatSaberMarkupLanguage.Settings;
using System.Xml;
using System.Xml.Serialization;
using System.Threading;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BS_Utils.Utilities;
using UnityEngine.PlayerLoop;

namespace ShockwaveSuit {

    [Plugin(RuntimeOptions.SingleStartInit)]
    public class ModPlugin {
        public static ModPlugin Instance {
            get; private set;
        }
        public static string ModName = "Shockwave Suit";
        public string Name => $"{ModName}";// {Version.ToString()}";
        public string Version => "1.22.1";// System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public static bool writeLogOnExit = true;
        public static string modDataPath = $"./UserData/{ModName}";
        public static string logFilePath = $"{modDataPath}/{ModName}.log";
        public static string LogFileData = "";
        public static string logFileFolder = "./UserData/logs";
        protected static string appFolder = "./";

        public static string cfgFilePath = $"{modDataPath}/{ModName}.cfg";

        public static ModConfiguration cfg = null;
        public static List<string> charts = new List<string>();

        //Limit Logging writes to a specific frequency, but continue to log until a write tick happens
        protected static double LogWriteFrequency = 2.0;
        protected static System.DateTime WriteLogTime = System.DateTime.UtcNow;

        protected static int mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        public static System.Object logFileLock = new System.Object();

        public static FirstPersonFlyingController fpfc = null;

        public bool IsAtMainMenu = true;
        public bool IsApplicationExiting = false;

        public static Logger log = null;
        public static GameObject coRunnerObj;

        ShockwaveManager suit;

        public ModPlugin() : base() {
            Instance = this;
            Task.Run(() => WaitForSuit());
            LoadModConfiguration();

            //Task.Run(() => Update());
        }

        ~ModPlugin() {
            Log("ModPlugin Deconstructing");
            if (File.Exists(ModPlugin.logFilePath)) {
                if (!Directory.Exists(ModPlugin.logFileFolder))
                    Directory.CreateDirectory(ModPlugin.logFileFolder);

                Log("Writing out final log");
                //Valid log file, rename and stow in case we had a crash
                string prevLog = File.ReadAllText(ModPlugin.logFilePath);
                string prevLogFile = $"{ModPlugin.logFileFolder}/{System.DateTime.UtcNow.ToFileTimeUtc()}_{ModPlugin.ModName}.log";
                File.WriteAllText(prevLogFile, prevLog);
                File.Delete(ModPlugin.logFilePath);
            }
        }

        public async Task WaitForSuit() {
            Log($"~~~SHOCKWAVE~~~ Waiting for Suit");
            suit = ShockwaveManager.Instance;
            suit.InitializeSuit();

            suit.enableBodyTracking = false;
            while (!ShockwaveManager.Instance.Ready && ShockwaveManager.Instance.error == 0) { 
                await Task.Delay(1000);
                if (ModPlugin.cfg.verbose)
                    Log($"~~~SHOCKWAVE~~~ Waiting...");
            }

            if(ShockwaveManager.Instance.error > 0) {
                Log($"~~~SHOCKWAVE~~~ Initialization Error {ShockwaveManager.Instance.error}");
            } else if (ShockwaveManager.Instance.Ready) {
                ShockwaveManager.Instance.InitSequence();
                Log($"~~~SHOCKWAVE~~~ Suit Connected { (ShockwaveManager.Instance.isUsingSteamVR ? "SteamVR" : "Native") }");
            } else
                Log($"~~~SHOCKWAVE~~~ Unknown Init Error");
        }

        public static void Log(string message, GameObject gob = null) {
            if (Thread.CurrentThread.ManagedThreadId == ModPlugin.mainThreadId)
                Debug.Log($"{ModName}] {message}", gob);
            else
                Console.WriteLine($"{ModName}] {message}");

            lock (ModPlugin.logFileLock)
                ModPlugin.LogFileData += $"{ModName}] {message}\n";

            if (!File.Exists(ModPlugin.logFilePath)) {
                //Add time first so we're not skewing the write cycle
                WriteLogTime = System.DateTime.UtcNow.AddSeconds(LogWriteFrequency);

                //Create the Log Directory if necessary
                if (!Directory.Exists(modDataPath))
                    Directory.CreateDirectory(modDataPath);
                if (!Directory.Exists(logFileFolder))
                    Directory.CreateDirectory(logFileFolder);

                lock (ModPlugin.logFileLock) {
                    //Write out the log
                    File.WriteAllText(ModPlugin.logFilePath, ModPlugin.LogFileData);
                    //Clear Log File in Memory to keep things tidy
                    ModPlugin.LogFileData = "";
                }
            } else if (System.DateTime.UtcNow >= WriteLogTime) {
                //Add time first so we're not skewing the write cycle
                WriteLogTime = System.DateTime.UtcNow.AddSeconds(LogWriteFrequency);
                lock (ModPlugin.logFileLock) {
                    //Write out the log
                    File.AppendAllText(ModPlugin.logFilePath, ModPlugin.LogFileData);
                    //Clear Log File in Memory to keep things tidy
                    ModPlugin.LogFileData = "";
                }
            }
        }

        #region Harmony
        //public static string strHarmonyInstance = $"com.XypherOrion.BeatSaber.{ModName}";

        public static void ApplyPatches() {
            /*
            bool success = true;
            if (harmony != null) {
                Log("Applying Harmony Patches");
                try {
                    harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
                } catch (Exception ex) {
                    Log(ex.ToString());
                    success = false;
                }

            } else {
                Log("Harmony has not been initialized...");
                success = false;
            }

            if (success)
                Log("Harmony Patches Successful");
            else
                Log("Harmony Patches FAILED");
                */
        }

        public static void RemovePatches() {
            //if(harmony.HasAnyPatches(strHarmonyInstance))
            //    harmony.UnpatchAll(strHarmonyInstance);
        }
        #endregion

        #region Configuration
        public static void SaveModConfiguration() {
            Log("Saving Mod Configuration");
            if (!Directory.Exists($"{modDataPath}"))
                Directory.CreateDirectory($"{modDataPath}");

            if (ModPlugin.cfg == null) {
                ModPlugin.cfg = new ModConfiguration();
                Log("  Generating Default SpinMod Configuration");
            }

            File.WriteAllText(cfgFilePath, ModPlugin.cfg.ToString(), System.Text.Encoding.ASCII);
        }

        protected static void LoadModConfiguration() {
            if (!File.Exists(cfgFilePath)) {
                Log($"Writing default {ModName} Configuration");
                if (!Directory.Exists($"{modDataPath}"))
                    Directory.CreateDirectory($"{modDataPath}");

                if (cfg == null)
                    cfg = new ModConfiguration();

                File.WriteAllText(cfgFilePath, cfg.ToString(), System.Text.Encoding.ASCII);
            } else {
                cfg = ModConfiguration.FromJson(File.ReadAllText(cfgFilePath));
                Log("Current cfg: " + cfg.ToString());
            }
        }

        protected static void LoadAssetBundle() {
            if (coRunnerObj == null)
                coRunnerObj = new GameObject("Coroutine Runner");
            BundleLoader bundleLoader = new BundleLoader();
            bundleLoader.LoadAssets("Shockwave");
        }

        public static void OnAssetBundlesLoaded() {
            Log("Loaded Asset Bundles");
        }

        #endregion

        #region Arrays
        public static List<ShockwaveManager.HapticGroup> LeftArmEffect = new List<ShockwaveManager.HapticGroup>() {
            ShockwaveManager.HapticGroup.LEFT_FOREARM,
            ShockwaveManager.HapticGroup.LEFT_ARM,
            ShockwaveManager.HapticGroup.LEFT_BICEP,
            ShockwaveManager.HapticGroup.LEFT_SHOULDER,
            ShockwaveManager.HapticGroup.LEFT_CHEST,
            ShockwaveManager.HapticGroup.LEFT_TORSO,
            ShockwaveManager.HapticGroup.LEFT_WAIST,
            ShockwaveManager.HapticGroup.LEFT_THIGH,
            ShockwaveManager.HapticGroup.LEFT_LOWER_LEG,
            ShockwaveManager.HapticGroup.LEFT_CALF
        };

        public static List<ShockwaveManager.HapticGroup> RightArmEffect = new List<ShockwaveManager.HapticGroup>() {
            ShockwaveManager.HapticGroup.RIGHT_FOREARM,
            ShockwaveManager.HapticGroup.RIGHT_ARM,
            ShockwaveManager.HapticGroup.RIGHT_BICEP,
            ShockwaveManager.HapticGroup.RIGHT_SHOULDER,
            ShockwaveManager.HapticGroup.RIGHT_CHEST,
            ShockwaveManager.HapticGroup.RIGHT_TORSO,
            ShockwaveManager.HapticGroup.RIGHT_WAIST,
            ShockwaveManager.HapticGroup.RIGHT_THIGH,
            ShockwaveManager.HapticGroup.RIGHT_LOWER_LEG,
            ShockwaveManager.HapticGroup.RIGHT_CALF
        };

        public static List<ShockwaveManager.HapticGroup> TwoHandSpecialEffect = new List<ShockwaveManager.HapticGroup>() {
            ShockwaveManager.HapticGroup.SHOULDERS_FRONT,
            ShockwaveManager.HapticGroup.SHOULDERS,
            ShockwaveManager.HapticGroup.SHOULDERS_BACK,
            ShockwaveManager.HapticGroup.CHEST_FRONT,
            ShockwaveManager.HapticGroup.CHEST,
            ShockwaveManager.HapticGroup.CHEST_BACK,
            ShockwaveManager.HapticGroup.TORSO_FRONT,
            ShockwaveManager.HapticGroup.TORSO,
            ShockwaveManager.HapticGroup.TORSO_BACK
        };


        public static List<ShockwaveManager.HapticGroup> OneHandSpecialEffect = new List<ShockwaveManager.HapticGroup>() {
            ShockwaveManager.HapticGroup.SHOULDERS_FRONT,
            ShockwaveManager.HapticGroup.CHEST_FRONT,
            ShockwaveManager.HapticGroup.TORSO_FRONT,
            ShockwaveManager.HapticGroup.TORSO,
            ShockwaveManager.HapticGroup.TORSO_BACK
        };
        #endregion

        [Init]
        public void Init(Logger logger) {
            //Initialize non-"game" code (presumably do not use Unity code here)

            log = logger;
            //Log("Creating Harmony Instance " + strHarmonyInstance);
            //if ((harmony = HarmonyInstance.Create(strHarmonyInstance)) != null)
            //    ApplyPatches();

            //Ensure that archive path exists
            if (!Directory.Exists(ModPlugin.logFileFolder))
                Directory.CreateDirectory(ModPlugin.logFileFolder);

            if (File.Exists(ModPlugin.logFilePath)) {
                //Valid log file, rename and stow in case we had a crash
                string prevLog = File.ReadAllText(ModPlugin.logFilePath);
                string prevLogFile = $"{ModPlugin.logFileFolder}/{System.DateTime.UtcNow.ToFileTimeUtc()}_{ModName}_Recovered.log";
                prevLog += ("Writing out recovered log");
                File.WriteAllText(prevLogFile, prevLog);
                File.Delete(ModPlugin.logFilePath);
            }

            Log($"{Name} Started, loading Mod Configuration");
            Log("Finished Initialization");
        }

       [OnStart]
        public void OnStart() {
            Instance = this;

            Log("ModPlugin OnStart");
            UI.ModSettingsUI.CreateMenu();

            // setup handle for fresh menu scene changes
            BS_Utils.Utilities.BSEvents.OnLoad();
            BS_Utils.Utilities.BSEvents.earlyMenuSceneLoadedFresh += OnEarlyMenuSceneLoadedFresh;
            BS_Utils.Utilities.BSEvents.lateMenuSceneLoadedFresh += OnLateMenuSceneLoadedFresh;
            BS_Utils.Utilities.BSEvents.levelFailed += OnFailed;
            BS_Utils.Utilities.BSEvents.songPaused += SongPaused;
            BS_Utils.Utilities.BSEvents.songUnpaused += SongUnPaused;
            BS_Utils.Utilities.BSEvents.noteWasCut += NoteWasCut;
            BS_Utils.Utilities.BSEvents.noteWasMissed += NoteWasMissed;

                // keep track of active scene
            BS_Utils.Utilities.BSEvents.menuSceneActive += () => {
                Log("Menu Scene Active");
                IsAtMainMenu = true; 
                SetupControllersForMenuScene();
            };

            BS_Utils.Utilities.BSEvents.gameSceneLoaded += () => {

                Log("Game Scene Loaded");
                IsAtMainMenu = false;

                FindPlayerObject();

                SetupControllersForGameScene();
            };

            //BS_Utils.Utilities.BSEvents.menuSceneLoaded += SetupControllersForMenuScene;
            //BS_Utils.Utilities.BSEvents.gameSceneLoaded += SetupControllersForGameScene;

            fpfc = GameObject.FindObjectOfType<FirstPersonFlyingController>();
            if(fpfc != null)
                Log($"Found FPFC!");

            Log($"OnStart Finished");
        }

        public void Update() {

        }

        // Shockwave LEDS:
        //  5  0
        //4      1
        //  3  2

        public ColorManager colorMan;
        int[] ledIdx = new int[] { 0 };
        float[] ledColor = new float[] { 0.0f, 0.0f, 0.0f};

        protected void ColorToFloatArray(Color color) {
            ledColor[0] = color.r * color.a;
            ledColor[1] = color.g * color.a;
            ledColor[2] = color.b * color.a;
        }

        private void NoteWasMissed(NoteController obj) {
            NoteData noteData = obj.noteData;

            if (cfg.hapticsMode == ModConfiguration.HapticsResponseMode.OnMiss) {
                Log($"~~~SHOCKWAVE DEBUG~~~ NOTE CUT | {noteData}");

                int duration = Mathf.RoundToInt(noteData.timeToNextColorNote * 1000);
                switch (noteData.noteLineLayer) {
                    case NoteLineLayer.Base:
                        switch (noteData.lineIndex) {
                            case 0:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_LOWER_LEG, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 3;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 1:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_THIGH, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 3;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                } 
                                break;
                            case 2:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_THIGH, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 2;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 3:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_LOWER_LEG, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 2;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            default:
                                break;
                        }
                        return;
                    case NoteLineLayer.Upper:
                        switch (noteData.lineIndex) {
                            case 0:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_WAIST, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 4;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 1:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_TORSO, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 4;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 2:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_WAIST, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 1;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 3:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_TORSO, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 1;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            default:
                                break;
                        }
                        return;
                    case NoteLineLayer.Top:
                        switch (noteData.lineIndex) {
                            case 0:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_SHOULDER, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 5;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 1:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_CHEST, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 5;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 2:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_CHEST, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 0;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 3:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_SHOULDER, 1.0f, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 0;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            default:
                                break;
                        }
                        return;
                    default:
                        return;
                }
            }
        }


        private void NoteWasCut(NoteController note, NoteCutInfo noteCutInfo) {
            if(cfg.verbose)
                Log($"~~~SHOCKWAVE DEBUG~~~ NOTE CUT | A:{noteCutInfo.cutAngle} Spd:{noteCutInfo.saberSpeed}");

            NoteData noteData = note.noteData;
            if (ModPlugin.cfg.hapticsMode == ModConfiguration.HapticsResponseMode.OnSlash) {
                int duration = 200;
                switch (noteData.noteLineLayer) {
                    case NoteLineLayer.Base:
                        switch (noteData.lineIndex) {
                            case 0:
                                if (ShockwaveManager.Instance != null) {
                                    ShockwaveManager.Instance.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_LOWER_LEG, noteCutInfo.saberSpeed, duration);
                                    if (cfg.ledResponse) {
                                        ledIdx[0] = 3;
                                        ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                        ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                    }
                                }
                                break;
                            case 1:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_THIGH, noteCutInfo.saberSpeed, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 3;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 2:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_THIGH, noteCutInfo.saberSpeed, duration);

                                if (cfg.ledResponse) {
                                    ledIdx[0] = 2;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 3:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_LOWER_LEG, noteCutInfo.saberSpeed, duration);

                                if (cfg.ledResponse) {
                                    ledIdx[0] = 2;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            default:
                                break;
                        }
                        return;
                    case NoteLineLayer.Upper:
                        switch (noteData.lineIndex) {
                            case 0:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_WAIST, noteCutInfo.saberSpeed, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 4;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 1:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_TORSO, noteCutInfo.saberSpeed, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 4;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 2:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_WAIST, noteCutInfo.saberSpeed, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 1;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 3:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_TORSO, noteCutInfo.saberSpeed, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 1;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            default:
                                break;
                        }
                        return;
                    case NoteLineLayer.Top:
                        switch (noteData.lineIndex) {
                            case 0:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_SHOULDER, noteCutInfo.saberSpeed, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 5;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 1:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.LEFT_CHEST, noteCutInfo.saberSpeed, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 5;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 2:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_CHEST, noteCutInfo.saberSpeed, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 0;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 3:
                                ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.RIGHT_SHOULDER, noteCutInfo.saberSpeed, duration);
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 0;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            default:
                                break;
                        }
                        return;
                    default:
                        return;
                }
            } else if (ModPlugin.cfg.hapticsMode == ModConfiguration.HapticsResponseMode.SaberPulse) {
                Task.Run(() => PlayPulse(noteCutInfo.saberType == SaberType.SaberA ? LeftArmEffect : RightArmEffect));

                switch (noteData.noteLineLayer) {
                    case NoteLineLayer.Base:
                        switch (noteData.lineIndex) {
                            case 0:
                                if (ShockwaveManager.Instance != null) {
                                    if (cfg.ledResponse) {
                                        ledIdx[0] = 3;
                                        ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                        ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                    }
                                }
                                break;
                            case 1:
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 3;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 2:

                                if (cfg.ledResponse) {
                                    ledIdx[0] = 2;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 3:

                                if (cfg.ledResponse) {
                                    ledIdx[0] = 2;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            default:
                                break;
                        }
                        return;
                    case NoteLineLayer.Upper:
                        switch (noteData.lineIndex) {
                            case 0:
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 4;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 1:
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 4;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 2:
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 1;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 3:
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 1;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            default:
                                break;
                        }
                        return;
                    case NoteLineLayer.Top:
                        switch (noteData.lineIndex) {
                            case 0:
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 5;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 1:
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 5;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 2:
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 0;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            case 3:
                                if (cfg.ledResponse) {
                                    ledIdx[0] = 0;
                                    ColorToFloatArray(colorMan?.ColorForType(noteData.colorType) ?? Color.clear);
                                    ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                }
                                break;
                            default:
                                break;
                        }
                        return;
                    default:
                        return;
                }
            }
        }

        GameObject playerObject;
        void FindPlayerObject() {
            playerObject = Camera.main.transform.root.gameObject;
            ModPlugin.Log("Player Object Hierarchy:");
            Transform searchTransform = Camera.main.transform;
            ModPlugin.Log($" {searchTransform.name}");
            while(searchTransform != playerObject.transform) {
                searchTransform = searchTransform.parent;
                ModPlugin.Log($" {searchTransform.name}");
            }
        }

        [OnExit]
        public void OnExit() {
            Log("OnExit");
            if (writeLogOnExit)
                File.WriteAllText(logFilePath, LogFileData, System.Text.Encoding.ASCII);
            Log("OnExit Finished");
        }


        public async static Task PlayPulse(List<ShockwaveManager.HapticGroup> pulseList) {
            List<ShockwaveManager.HapticGroup> pulses = new List<ShockwaveManager.HapticGroup>();
            pulses.AddRange(pulseList);
            int pulseDelay = ModPlugin.cfg.saberPulseDelay;

            while (pulses.Count > 0) {
                ShockwaveManager.Instance?.SendHapticGroup(pulses[0], 1.0f, 200);
                pulses.RemoveAt(0);
                await Task.Delay(pulseDelay);
            }
        }

        CancellationTokenSource pauseTokenSource = new CancellationTokenSource();
        Task pauseTask;

        int pauseCycleMs = 200;
        float pauseCycleDir = 1.0f;
        int[] pauseLeds = new int[] { 0, 1, 2, 3, 4, 5 };
        float[] pauseColors = new float[] {
                1.0f, 0.0f, 0.0f,
                1.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 1.0f
            };

        protected async Task OnPause() {
            System.DateTime FlipDirectionTime = System.DateTime.UtcNow.AddMilliseconds(pauseCycleMs);
            int wrapLed = 0, i = 0;
            while(true) {
                if(cfg.ledResponse) {
                    ShockwaveManager.Instance?.sendLEDUpdate(pauseLeds, pauseColors, 6);
                    if (pauseCycleDir > 0.0f) {
                        wrapLed = pauseLeds[0];
                        for (i = 0; i < 5; i++)
                            pauseLeds[i] = pauseLeds[i + 1];
                        pauseLeds[5] = wrapLed;
                    } else {
                        wrapLed = pauseLeds[5];
                        for (i = 1; i < 6; i++)
                            pauseLeds[i] = pauseLeds[i - 1];
                        pauseLeds[0] = wrapLed;
                    }

                    if(System.DateTime.UtcNow > FlipDirectionTime) {
                        pauseCycleDir = -pauseCycleDir;
                        FlipDirectionTime = System.DateTime.UtcNow.AddMilliseconds(pauseCycleMs);
                    }
                }
                await Task.Delay(pauseCycleMs);
            }
        }

        public void SongPaused() {
            if (pauseTask == null)
                pauseTask = Task.Run(() => OnPause(), pauseTokenSource.Token);
        }

        public void SongUnPaused() {
            if(pauseTask != null) {
                pauseTokenSource.Cancel();

                // Just continue on this thread, or await with try-catch:
                try {
                    pauseTask.GetAwaiter().GetResult();
                } catch (OperationCanceledException e) {
                    Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
                } finally {
                    pauseTokenSource.Dispose();
                }
            }
        }

        protected void FreeFPFC() {
            fpfc = GameObject.FindObjectOfType<FirstPersonFlyingController>();
            if (fpfc != null) {
                Log("First Person Flying Controller found, Detaching from Play Space");
                fpfc.transform.SetParent(null);
            }
        }

        protected void FindRootsAndInitialize(string rootTransformName, bool useRootAsBase = false) {
            Log("Finding Roots and Initializing ModPlugin");


            GameObject rootObj = GameObject.Find(rootTransformName);
            if(rootObj) {
                Log($"Found Root {rootTransformName}");
            }
        }

        public void SetupControllersForMenuScene() {
            Log("Setting up controllers for the Menu");
            IsAtMainMenu = true;
            FreeFPFC();
            FindRootsAndInitialize("MenuCore");///Origin");
        }

        public void SetupControllersForGameScene() {
            Log("Setting up controllers for the Game scene");
            IsAtMainMenu = false;

            FreeFPFC();

            FindRootsAndInitialize("LocalPlayerGameCore");// "LocalPlayerGameCore /Origin");
        }

        public void OnFailed(StandardLevelScenesTransitionSetupDataSO so, LevelCompletionResults results) {
        }

        private void OnEarlyMenuSceneLoadedFresh(ScenesTransitionSetupDataSO setup) {
            Log("Early Menu Scene Loaded Fresh");
        }
        private void OnLateMenuSceneLoadedFresh(ScenesTransitionSetupDataSO setup) {
            Log("Late Menu Scene Loaded Fresh");
        }


        void OnApplicationQuit() {
            File.WriteAllText(logFilePath, LogFileData, System.Text.Encoding.ASCII);
            ModPlugin.Log("Shutting down SpinMod");
        }


        public static string ReadTransforms(Transform root, int level, bool writeComponents = false) {
            //Indent
            string i = "";
            if (level > 0)
                for (int s = 0; s < level; s++)
                    i += " ";

            string r = (root?(level > 0) ? $"{i}{root.name}\n" : $"[{root.name}]\n" : "");

            if (root) {
                if (writeComponents) {
                    Component[] components = root.GetComponents<Component>();
                    if (components.Length > 0)
                        for (int cmp = 0; cmp < components.Length; cmp++)
                            r += $"{i}-{components[cmp].GetType()}\n";
                }

                if (root.childCount > 0)
                    for (int c = 0; c < root.childCount; c++)
                        r += ReadTransforms(root.GetChild(c), level + 1, writeComponents);
            }
            return r;
        }

        public static void WriteAllGameObjectsToFile(string fileName, bool writeComponentNames = false) {
            string GameObjectNames = "";
            /*
            List<GameObject> pss = GameObject.FindObjectsOfType<GameObject>().ToList();
            Log("Dumping Game Object Names:");
            foreach (GameObject ps in pss) {
                Log(ps.name);
                GameObjectNames += ps.name + '\n';
            }
            */
            Scene scene;
            for (int s = 0; s < SceneManager.sceneCount; s++) {
                scene = SceneManager.GetSceneAt(s);
                GameObjectNames += $"[SCENE [{scene.name}]]\n";

                List<GameObject> pss = scene.GetRootGameObjects().ToList();
                int psCount = pss.Count;
                if (psCount > 0) {
                    for (int p = 0; p < psCount; p++)
                        GameObjectNames += ReadTransforms(pss[p].transform, 0, writeComponentNames);
                }
            }

            File.WriteAllText($"./{fileName}", GameObjectNames);
        }
    }
}
