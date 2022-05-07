using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace Architech
{
    public class KickStart
    {
        internal const string ModName = "Architech";


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
        public static void OfficialEarlyInit()
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
            ManBuildUtil.DeInit();
            MirrorManager.DeInit();
        }

        // UNOFFICIAL
#else
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
            ManBuildUtil.Init();
        }
        public static void DelayedInitAll()
        {
            MirrorManager.Init();
        }
#endif
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
