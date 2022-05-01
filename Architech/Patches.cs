using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace Architech
{
    internal static class Patches
    {
#if !STEAM
        [HarmonyPatch(typeof(Mode))]
        [HarmonyPatch("EnterPreMode")]//On very late update
        private static class Startup
        {
            private static void Prefix()
            {
                KickStart.DelayedInitAll();
            }
        }
#endif

        /// <summary>
        /// For BuildUtil
        /// </summary>
        [HarmonyPatch(typeof(GameCursor))]
        [HarmonyPatch("GetCursorState")]//On very late update
        private static class GetCursorChange
        {
            /*
            // NEW
            OverTech
            HoldTech
            OverMirror
            HoldMirror
            OverBatch
            HoldBatch
            OverMirrorBatch
            HoldMirrorBatch
            MirroredPainting
            OverMirroredPainting
            PointerMirror
            PointerBatch
            PointerMirrorBatch
            */

            /// <summary>
            /// See BuildUtil for more information
            /// </summary>
            /// <param name="__result"></param>
            private static void Postfix(ref GameCursor.CursorState __result)
            {
                if (!BuildUtil.inst)
                    return;
                int enumC = Enum.GetValues(typeof(GameCursor.CursorState)).Length - 1;
                switch (__result)
                {
                    case GameCursor.CursorState.Painting:
                        if (BuildUtil.IsGrabbingTechsActive)
                        {
                            if (BuildUtil.IsHoveringGrabbableTech)
                            {   // Display tech grab
                                __result = (GameCursor.CursorState)(enumC + 1);
                                return;
                            }
                        }
                        /*
                        if (BuildUtil.IsBatchActive)
                        {
                            if (BuildUtil.IsMirroring)
                            {
                                // Display Batch Grab + Mirror
                                if (BuildUtil.IsHoldingMirrored && !BuildUtil.lastFramePlacementInvalid)
                                {
                                    __result = (GameCursor.CursorState)(enumC + 7);
                                }
                                else
                                {   // Display batch grab
                                    __result = (GameCursor.CursorState)(enumC + 5);
                                }
                            }
                            else
                            {   // Display batch grab
                                __result = (GameCursor.CursorState)(enumC + 5);
                            }
                        }
                        else*/
                        if (BuildUtil.IsMirroring)
                        {
                            if (BuildUtil.IsHoldingMirrored && !BuildUtil.lastFramePlacementInvalid)
                            {   // Display Mirror grab
                                __result = (GameCursor.CursorState)(enumC + 3);
                            }
                        }
                        break;

                    case GameCursor.CursorState.OverGrabbable:
                        if (BuildUtil.IsGrabbingTechsActive)
                        {
                            if (BuildUtil.IsHoveringGrabbableTech)
                            {   // Display tech grab
                                __result = (GameCursor.CursorState)(enumC + 1);
                                return;
                            }
                        }
                        if (BuildUtil.IsBatchActive)
                        {
                            if (BuildUtil.IsMirroring)
                            {
                                if (ManPointer.inst.targetVisible?.block && ManPointer.inst.targetVisible.block.tank)
                                {   // Display Batch Grab + Mirror
                                    if (BuildUtil.IsHoveringMirrored)
                                    {
                                        __result = (GameCursor.CursorState)(enumC + 13);
                                    }
                                    else
                                    {   // Display batch grab
                                        __result = (GameCursor.CursorState)(enumC + 12);
                                    }
                                }
                            }
                            else
                            {
                                if (ManPointer.inst.targetVisible?.block && ManPointer.inst.targetVisible.block.tank)
                                {   // Display batch grab
                                    __result = (GameCursor.CursorState)(enumC + 12);
                                }
                            }
                        }
                        else if (BuildUtil.IsMirroring)
                        {
                            if (BuildUtil.IsHoveringMirrored)
                            {   // Display Mirror grab
                                __result = (GameCursor.CursorState)(enumC + 11);
                            }
                        }
                        break;

                    case GameCursor.CursorState.HoldingGrabbable:
                        if (BuildUtil.IsGrabbingTechsActive)
                        {   // Display Tech Grabbed
                            __result = (GameCursor.CursorState)(enumC + 2);
                        }
                        else if (BuildUtil.IsBatchActive && BuildUtil.inst.IsHoldingBatch)
                        {
                            if (BuildUtil.IsMirroring)
                            {
                                if (BuildUtil.IsHoldingMirrored || BuildUtil.IsHoveringMirrored)
                                {   // Display Batch Grabbed + Mirror 
                                    __result = (GameCursor.CursorState)(enumC + 8);
                                }
                                else
                                {  // Display batch grabbed
                                    __result = (GameCursor.CursorState)(enumC + 6);
                                }
                            }
                            else
                            {  // Display batch grabbed
                                __result = (GameCursor.CursorState)(enumC + 6);
                            }
                        }
                        else if (BuildUtil.IsMirroring)
                        {
                            if (BuildUtil.IsHoldingMirrored || BuildUtil.IsHoveringMirrored)
                            {   // Display Mirror grabbed 
                                __result = (GameCursor.CursorState)(enumC + 4);
                            }
                        }
                        break;

                    //case GameCursor.CursorState.SkinPainting:
                    //    break;

                    case GameCursor.CursorState.SkinPaintingOverPaintable:
                        if (BuildUtil.IsMirroring)
                        { // Display mirror painting
                            if (BuildUtil.IsHoveringMirrored)
                            {
                                if (BuildUtil.IsPaintingSkin)
                                    __result = (GameCursor.CursorState)(enumC + 9);
                                else
                                    __result = (GameCursor.CursorState)(enumC + 10);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// For BuildUtil
        /// </summary>
        /// </summary>
        [HarmonyPatch(typeof(GameCursor))]
        [HarmonyPatch("GetCursorState")]//On very late update
        private static class ApplyCursorChange
        {
            private static void Postfix(ref GameCursor.CursorState __result)
            {

            }
        }
    }
}
