using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        /// For CursorChanger
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
            /// See CursorChanger for more information
            /// </summary>
            /// <param name="__result"></param>
            private static void Postfix(ref GameCursor.CursorState __result)
            {
                if (!ManBuildUtil.inst)
                    return;
                //int enumC = Enum.GetValues(typeof(GameCursor.CursorState)).Length - 1;
                switch (__result)
                {
                    case GameCursor.CursorState.Painting:
                        if (ManBuildUtil.IsGrabbingTechsActive)
                        {
                            if (ManBuildUtil.IsHoveringGrabbableTech)
                            {   // Display tech grab
                                __result = (GameCursor.CursorState)CursorChanger.CursorIndexCache[0];
                                return;
                            }
                        }
                        if (ManBuildUtil.IsMirroring)
                        {
                            if (ManBuildUtil.IsHoldingMirrored && !ManBuildUtil.lastFramePlacementInvalid)
                            {   // Display Mirror grab
                                __result = (GameCursor.CursorState)CursorChanger.CursorIndexCache[2];
                            }
                        }
                        break;

                    case GameCursor.CursorState.OverGrabbable:
                        if (ManBuildUtil.IsGrabbingTechsActive)
                        {
                            if (ManBuildUtil.IsHoveringGrabbableTech)
                            {   // Display tech grab
                                __result = (GameCursor.CursorState)(CursorChanger.CursorIndexCache[0]);
                                return;
                            }
                        }
                        if (ManBuildUtil.IsBatchActive)
                        {
                            if (ManBuildUtil.IsMirroring)
                            {
                                if (ManPointer.inst.targetVisible?.block && ManPointer.inst.targetVisible.block.tank)
                                {   // Display Batch Grab + Mirror
                                    if (ManBuildUtil.IsHoveringMirrored)
                                    {
                                        __result = (GameCursor.CursorState)(CursorChanger.CursorIndexCache[12]);
                                    }
                                    else
                                    {   // Display batch grab
                                        __result = (GameCursor.CursorState)(CursorChanger.CursorIndexCache[11]);
                                    }
                                }
                            }
                            else
                            {
                                if (ManPointer.inst.targetVisible?.block && ManPointer.inst.targetVisible.block.tank)
                                {   // Display batch grab
                                    __result = (GameCursor.CursorState)(CursorChanger.CursorIndexCache[11]);
                                }
                            }
                        }
                        else if (ManBuildUtil.IsMirroring)
                        {
                            if (ManBuildUtil.IsHoveringMirrored)
                            {   // Display Mirror grab
                                __result = (GameCursor.CursorState)(CursorChanger.CursorIndexCache[10]);
                            }
                        }
                        break;

                    case GameCursor.CursorState.HoldingGrabbable:
                        if (ManBuildUtil.IsGrabbingTechsActive)
                        {   // Display Tech Grabbed
                            if (ManPointer.inst.DraggingItem?.block?.tank 
                                && ManPointer.inst.DraggingItem.block.tank != Singleton.playerTank)
                                __result = (GameCursor.CursorState)(CursorChanger.CursorIndexCache[1]);
                        }
                        else if (ManBuildUtil.IsBatchActive && ManBuildUtil.inst.IsHoldingBatch)
                        {
                            if (ManBuildUtil.IsMirroring)
                            {
                                if (ManBuildUtil.IsHoldingMirrored || ManBuildUtil.IsHoveringMirrored)
                                {   // Display Batch Grabbed + Mirror 
                                    __result = (GameCursor.CursorState)(CursorChanger.CursorIndexCache[7]);
                                }
                                else
                                {  // Display batch grabbed
                                    __result = (GameCursor.CursorState)CursorChanger.CursorIndexCache[5];
                                }
                            }
                            else
                            {  // Display batch grabbed
                                __result = (GameCursor.CursorState)CursorChanger.CursorIndexCache[5];
                            }
                        }
                        else if (ManBuildUtil.IsMirroring)
                        {
                            if (ManBuildUtil.IsHoldingMirrored || ManBuildUtil.IsHoveringMirrored)
                            {   // Display Mirror grabbed 
                                __result = (GameCursor.CursorState)(CursorChanger.CursorIndexCache[3]);
                            }
                        }
                        break;

                    //case GameCursor.CursorState.SkinPainting:
                    //    break;

                    case GameCursor.CursorState.SkinPaintingOverPaintable:
                        if (ManBuildUtil.IsMirroring)
                        { // Display mirror painting
                            if (ManBuildUtil.IsHoveringMirrored)
                            {
                                if (ManBuildUtil.IsPaintingSkin)
                                    __result = (GameCursor.CursorState)(CursorChanger.CursorIndexCache[8]);
                                else
                                    __result = (GameCursor.CursorState)(CursorChanger.CursorIndexCache[9]);
                            }
                        }
                        break;
                }
            }
        }


        
        /// <summary>
        /// For ManAltBuild
        /// </summary>
        [HarmonyPatch(typeof(TankCamera))]
        [HarmonyPatch("UpdateBuildBeamBlockFocusControl")]
        private static class LockCamToBuildingBlock
        {
            private static MethodBase setThis = typeof(TankCamera).GetMethod("SetFocussedBuildBlock", BindingFlags.NonPublic | BindingFlags.Instance);

            private static void Postfix(TankCamera __instance, ref Tank tankToFollow)
            {
                if (tankToFollow && ManAltBuild.IsActive)
                {
                    setThis.Invoke(__instance, new object[1] { ManAltBuild.HeldBlock });
                }
            }
        }
    }
}
