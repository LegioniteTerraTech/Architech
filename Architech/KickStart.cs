using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using TerraTechETCUtil;

#if !STEAM
using ModHelper.Config;
#else
using ModHelper;
#endif
using Nuterra.NativeOptions;

namespace Architech
{
    public class KickStart
    {
        internal const string ModName = "Architech";

        public static KeyCode SuppressControl = KeyCode.LeftControl;
        public static KeyCode ChangeRoot = KeyCode.Backslash;
        public static KeyCode GrabTechs = KeyCode.Backspace;
        public static KeyCode ToggleBatch = KeyCode.RightShift;
        public static KeyCode ToggleMirrorMode = KeyCode.CapsLock;

        public static int savSuppression = (int)SuppressControl;
        public static int savChangeRoot = (int)ChangeRoot;
        public static int savGrabTechs = (int)GrabTechs;
        public static int savToggleBatch = (int)ToggleBatch;
        public static int savToggleMirrorMode = (int)ToggleMirrorMode;

        public static bool IsIngame { get { return !ManPauseGame.inst.IsPaused && !ManPointer.inst.IsInteractionBlocked; } }

        public static void ReleaseControl(int ID)
        {
            if (GUIUtility.hotControl == ID)
            {
                GUI.FocusControl(null);
                GUI.UnfocusWindow();
                GUIUtility.hotControl = 0;
            }
        }

        private static bool patched = false;
        static Harmony harmonyInstance;
        //private static bool patched = false;
#if STEAM
        // OFFICIAL
        public static void OfficialEarlyInit()
        {
            ModStatusChecker.EncapsulateSafeInit(ModName, OfficialEarlyInit_internal);
        }
        private static void OfficialEarlyInit_internal()
        {
            //Where the fun begins

            //Initiate the madness
            try
            {   // init the mod 
                Harmony hi = new Harmony("legionite.architech");
                harmonyInstance = hi;
                harmonyInstance.PatchAll();
                //EdgePatcher(true);
                DebugArchitech.Log("Architech: Patched");
                patched = true;
            }
            catch (Exception e)
            {
                DebugArchitech.Log("Architech: Error on patch");
                DebugArchitech.Log(e);
            }
        }


        public static void MainOfficialInit()
        {
            ModStatusChecker.EncapsulateSafeInit(ModName, MainOfficialInit_Internal);
        }

        private static void MainOfficialInit_Internal()
        {
            //Where the fun begins

            //Initiate the madness
            if (!patched)
            {
                int patchStep = 0;
                try
                {
                    Harmony hi = new Harmony("legionite.architech");
                    harmonyInstance = hi;
                    harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                    patchStep++;
                    //EdgePatcher(true);
                    DebugArchitech.Log("Architech: Patched");
                    patched = true;
                }
                catch (Exception e)
                {
                    DebugArchitech.Log("Architech: Error on patch " + patchStep);
                    DebugArchitech.Log(e);
                }
            }
            MirrorManager.Init();
            ManBuildUtil.Init();
            ManBlockBatches.Subcribble(true);
            try
            {

                KickStartOptions.PushExtModOptionsHandling();
                DebugArchitech.Log("Architech: Hooked up to ConfigHelper and NativeOptions!");
            }
            catch
            {
                DebugArchitech.Log("Architech: Failed to hook up to ConfigHelper and NativeOptions - they are likely not installed.");
            }
        }
        public static void DeInitALL()
        {
            if (patched)
            {
                try
                {
                    harmonyInstance.UnpatchAll("legionite.Architech");
                    //EdgePatcher(false);
                    DebugArchitech.Log("Architech: UnPatched");
                    patched = false;
                }
                catch (Exception e)
                {
                    DebugArchitech.Log("Architech: Error on UnPatch");
                    DebugArchitech.Log(e);
                }
            }
            ManBlockBatches.Subcribble(false);
            ManBuildUtil.DeInit();
            MirrorManager.DeInit();
        }

#else
        // UNOFFICIAL
        public static void Main()
        {
            //Where the fun begins

            //Initiate the madness
            Harmony harmonyInstance = new Harmony("legionite.architech");
            try
            {
                harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Debug.Log("Architech: Error on patch");
                Debug.Log(e);
            }
            KickStartOptions.PushExtModOptionsHandling();
            ManBuildUtil.Init();
        }
        public static void DelayedInitAll()
        {
            ManBlockBatches.Subcribble(true);
            MirrorManager.Init();
        }
#endif
    }

    public class KickStartOptions
    {
        public static OptionKey hold;
        public static OptionKey mirror;
        public static OptionKey batch;
        public static OptionKey root;
        public static OptionKey grabTechs;

        private static bool launched = false;

        internal static void PushExtModOptionsHandling()
        {
            if (launched)
                return;
            launched = true;
            ModConfig thisModConfig = new ModConfig();
            thisModConfig.BindConfig<KickStart>(null, "savSuppression");
            thisModConfig.BindConfig<KickStart>(null, "savChangeRoot");
            thisModConfig.BindConfig<KickStart>(null, "savGrabTechs");
            thisModConfig.BindConfig<KickStart>(null, "savToggleBatch");
            thisModConfig.BindConfig<KickStart>(null, "savToggleMirrorMode");

            KickStart.SuppressControl = (KeyCode)KickStart.savSuppression;
            KickStart.ToggleMirrorMode = (KeyCode)KickStart.savToggleMirrorMode;
            KickStart.ToggleBatch = (KeyCode)KickStart.savToggleBatch;
            KickStart.ChangeRoot = (KeyCode)KickStart.savChangeRoot;
            KickStart.GrabTechs = (KeyCode)KickStart.savGrabTechs;

            var TACAI = KickStart.ModName + " - Hotkey Settings";
            hold = new OptionKey("Suppress Controls [HOLD]", TACAI, KickStart.SuppressControl);
            hold.onValueSaved.AddListener(() =>
            {
                KickStart.SuppressControl = hold.SavedValue;
                KickStart.savSuppression = (int)KickStart.SuppressControl;
            });
            mirror = new OptionKey("Mirror Mode [TOGGLE]", TACAI, KickStart.ToggleMirrorMode);
            mirror.onValueSaved.AddListener(() =>
            {
                KickStart.ToggleMirrorMode = mirror.SavedValue;
                KickStart.savToggleMirrorMode = (int)KickStart.ToggleMirrorMode;
            });
            batch = new OptionKey("Batch Holding [TOGGLE]", TACAI, KickStart.ToggleBatch);
            batch.onValueSaved.AddListener(() =>
            {
                KickStart.ToggleBatch = batch.SavedValue;
                KickStart.savToggleBatch = (int)KickStart.ToggleBatch;
            });
            root = new OptionKey("Root Setter [HOLD]", TACAI, KickStart.ChangeRoot);
            root.onValueSaved.AddListener(() =>
            {
                KickStart.ChangeRoot = root.SavedValue;
                KickStart.savChangeRoot = (int)KickStart.ChangeRoot;
            });
            grabTechs = new OptionKey("Grab Techs [HOLD]", TACAI, KickStart.GrabTechs);
            grabTechs.onValueSaved.AddListener(() =>
            {
                KickStart.GrabTechs = grabTechs.SavedValue;
                KickStart.savGrabTechs = (int)KickStart.GrabTechs;
            });

            NativeOptionsMod.onOptionsSaved.AddListener(() => { thisModConfig.WriteConfigJsonFile(); });
        }

    }

#if STEAM
    public class KickStartArchitech : ModBase
    {
        internal static KickStartArchitech oInst;

        bool isInit = false;
        public override bool HasEarlyInit()
        {
            DebugArchitech.Log("Architech: CALLED");
            return true;
        }

        // IDK what I should init here...
        public override void EarlyInit()
        {
            DebugArchitech.Log("Architech: CALLED EARLYINIT");
            if (oInst == null)
            {
                try
                {
                    KickStart.OfficialEarlyInit();
                    oInst = this;
                }
                catch (Exception e) { 
                    DebugArchitech.Log("Architech: " + e); 
                }
            }
        }
        public override void Init()
        {
            DebugArchitech.Log("Architech: CALLED INIT");
            if (oInst == null)
            {
                try
                {
                    KickStart.OfficialEarlyInit();
                    oInst = this;
                }
                catch (Exception e) { 
                    DebugArchitech.Log("Architech: " + e);
                }
            }
            if (isInit)
                return;

            KickStart.MainOfficialInit();
            isInit = true;
        }
        public override void DeInit()
        {
            if (!isInit)
                return;
            KickStart.DeInitALL();
            isInit = false;
        }
    }
#endif
}
