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
        static Harmony harmonyInstance = new Harmony("legionite.architech");
        //private static bool patched = false;
#if STEAM
        public static void OfficialEarlyInit()
        {
            //Where the fun begins

            //Initiate the madness
            try
            {
                harmonyInstance.PatchAll();
                //EdgePatcher(true);
                Debug.Log("Architech: Patched");
                patched = true;
            }
            catch (Exception e)
            {
                Debug.Log("Architech: Error on patch");
                Debug.Log(e);
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
                    harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                    patchStep++;
                    //EdgePatcher(true);
                    Debug.Log("Architech: Patched");
                    patched = true;
                }
                catch (Exception e)
                {
                    Debug.Log("Architech: Error on patch " + patchStep);
                    Debug.Log(e);
                }
            }
            MirrorManager.Init();
            BuildUtil.Init();
        }
        public static void DeInitALL()
        {
            if (patched)
            {
                try
                {
                    harmonyInstance.UnpatchAll("legionite.Architech");
                    //EdgePatcher(false);
                    Debug.Log("Architech: UnPatched");
                    patched = false;
                }
                catch (Exception e)
                {
                    Debug.Log("Architech: Error on UnPatch");
                    Debug.Log(e);
                }
            }
            BuildUtil.DeInit();
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
            BuildUtil.Init();
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
            Debug.Log("Architech: CALLED");
            return true;
        }

        // IDK what I should init here...
        public override void EarlyInit()
        {
            Debug.Log("Architech: CALLED EARLYINIT");
            if (oInst == null)
            {
                KickStart.OfficialEarlyInit();
                oInst = this;
            }
        }
        public override void Init()
        {
            Debug.Log("Architech: CALLED INIT");
            if (isInit)
                return;
            if (oInst == null)
                oInst = this;

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
