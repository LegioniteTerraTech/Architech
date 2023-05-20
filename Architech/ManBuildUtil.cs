using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;



namespace Architech
{

    /// <summary>
    /// Allows the player far more control over Tech construction.  Work in progress.
    /// Modified version of TAC_AI's AIERepair.DesignMemory's rebuilding functions.
    /// </summary>
    internal class ManBuildUtil : MonoBehaviour
    {
        static FieldInfo highL = typeof(ManPointer).GetField("m_HighlightPrefab", BindingFlags.NonPublic | BindingFlags.Instance);

        static FieldInfo paintBool = typeof(ManPointer).GetField("m_PaintingSkin", BindingFlags.NonPublic | BindingFlags.Instance);




        private static bool DebugBlockRotations = false;


        internal static ManBuildUtil inst;
        private static ObjectHighlight OH;
        internal static Tank currentTank;

        internal static bool IsThisSuppressed => Input.GetKey(KickStart.SuppressControl);
        internal static bool IsPaintSkinsActive => ManPointer.inst.BuildMode == ManPointer.BuildingMode.PaintSkin;
        internal static bool IsPaintingSkin => IsPaintSkinsActive && Input.GetMouseButton(0);
        internal static bool IsGrabbingTechsActive => Input.GetKey(KickStart.GrabTechs);
        internal static bool IsHoveringGrabbableTech => ManPointer.inst.targetVisible?.block?.tank ? ManPointer.inst.targetVisible.block.tank != Singleton.playerTank : false;

        internal static bool IsBatchActive = false;
        internal static bool ToggleBatchMode => Input.GetKey(KickStart.ToggleBatch) && KickStart.IsIngame;
        internal static bool ToggleMirrorMode => Input.GetKey(KickStart.ToggleMirrorMode) && KickStart.IsIngame;
        internal static bool IsHoveringMirrored => lastHovered;
        internal static bool IsHoldingMirrored => MirrorHeld;
        internal bool IsHoldingBatch => PointerBatchCache || MirrorBatchCache;
        internal static bool IsMirrorModeActive = false;
        internal static bool IsBatching => (IsBatchActive || IsGrabbingTechsActive) && !CabDetached && KickStart.IsIngame;
        internal static bool IsMirroring => IsMirrorModeActive && !IsGrabbingTechsActive && !BusyGrabbingTechs;
        internal static bool Busy = false;
        internal static bool BusyBatching = false;
        internal static bool BusyGrabbingTechs = false;
        internal static bool CabDetached = false;
        internal static bool LockToCOM = true;

        private static bool lastFrameButton = false;
        private static bool lastFrameButton2 = false;
        internal static Vector3 lastDragVelo = Vector3.zero;



        private static bool PaintBlocks => ManPointer.inst.BuildMode == ManPointer.BuildingMode.PaintBlock && KickStart.IsIngame && !BusyGrabbingTechs;
        internal static Transform rBlock => currentTank.rootBlockTrans;
        internal Vector3 currentTankCenter => rBlock.GetComponent<TankBlock>() ?
            SnapToMirrorAxi(rBlock.localPosition +
                (rBlock.localRotation * rBlock.GetComponent<TankBlock>().BlockCellBounds.center))
            : currentTank.blockBounds.center;
        internal static Vector3 GetTankCenterNoCheck(Tank currentTank)
        {
            var rBlock = currentTank.rootBlockTrans;
            return rBlock.GetComponent<TankBlock>() ?
            SnapToMirrorAxi(rBlock.localPosition +
                (rBlock.localRotation * rBlock.GetComponent<TankBlock>().BlockCellBounds.center))
            : currentTank.blockBounds.center;
        }

        private static string lastDraggedName;
        private static Vector3 lastDraggedPosition = Vector3.zero;
        internal static TankBlock cachePointerHeld;
        private static TankBlock anonMirrorHeld;
        private static TankBlock MirrorHeld;
        private static bool InventoryBlock = false;
        private static TankBlock lastHovered;
        private static TankBlock lastAttached;
        private static TankBlock lastDetached;
        private static BlockTypes lastType = BlockTypes.GSOAIController_111;
        private static BlockTypes pairType = BlockTypes.GSOAIController_111;
        private static Dictionary<BlockTypes, MirrorAngle> cacheRotations = new Dictionary<BlockTypes, MirrorAngle>();
        private static Dictionary<BlockTypes, MirrorAngle> cacheRotationsModded = new Dictionary<BlockTypes, MirrorAngle>();

        private static bool attachFrameDelay = false;
        internal static bool lastFramePlacementInvalid = false;

        private static MirrorAngle cachedMirrorAngle = MirrorAngle.None;

        private static BlockBatch PointerBatchCache;
        private static bool PointerBatchCacheMirrorCheck = false;
        private static BlockBatch MirrorBatchCache;
        private static bool MirrorBatchCacheIsInventory = false;


        public static void Init()
        {
            if (inst)
                return;
            inst = new GameObject("BuildUtil").AddComponent<ManBuildUtil>();
            ManTechs.inst.PlayerTankChangedEvent.Subscribe(OnPlayerTechChanged);
            ManTechs.inst.TankBlockAttachedEvent.Subscribe(OnBlockPlaced);
            ManTechs.inst.TankBlockDetachedEvent.Subscribe(OnBlockRemoved);

            OH = Instantiate((ObjectHighlight)highL.GetValue(ManPointer.inst));
            OH.SetHighlightType(ManPointer.HighlightVariation.Normal);
            CursorChanger.AddNewCursors();

        }
        public static void DeInit()
        {
            if (!inst)
                return;

            cacheRotationsModded.Clear();


            Destroy(OH.gameObject);
            OH = null;

            ManTechs.inst.TankBlockDetachedEvent.Unsubscribe(OnBlockRemoved);
            ManTechs.inst.TankBlockAttachedEvent.Unsubscribe(OnBlockPlaced);
            ManTechs.inst.PlayerTankChangedEvent.Unsubscribe(OnPlayerTechChanged);
            Destroy(inst.gameObject);
            inst = null;
        }



        public static void OnPlayerTechChanged(Tank tank, bool yes)
        {
            if (currentTank)
            {
                currentTank.blockman.BlockTableRecentreEvent.Unsubscribe(OnBlockTableChange);
            }
        }
        public static void OnBlockPlaced(Tank tank, TankBlock newlyPlaced)
        {
            if (!newlyPlaced || !tank || tank.FirstUpdateAfterSpawn || tank.Team != ManPlayer.inst.PlayerTeam
                || BusyGrabbingTechs)
                return;
            if (!Busy && (PaintBlocks || MirrorHeld || inst.IsHoldingBatch))
            {
                delayedAdd.Add(new KeyValuePair<Tank, TankBlock>(tank, newlyPlaced));
            }
        }

        private static Tank lastRemovedTank;
        public static void OnBlockRemoved(Tank tank, TankBlock newlyRemoved)
        {
            if (!newlyRemoved || !tank || tank.FirstUpdateAfterSpawn || tank.Team != ManPlayer.inst.PlayerTeam
                || BusyGrabbingTechs)
                return;
            if (IsMirroring)
            {
                if (Busy)
                {
                    if (!BusyBatching)
                    {
                        if (ManPointer.inst.DraggingItem == newlyRemoved.visible)
                        {
                            delayedRemove.Add(new KeyValuePair<Tank, TankBlock>(tank, newlyRemoved));
                            lastRemovedTank = tank;
                            anonMirrorHeld = inst.GetMirroredBlock(tank, newlyRemoved);
                            if (anonMirrorHeld)
                            {
                                inst.CorrectMirrorBlockRotation(ref anonMirrorHeld, lastRemovedTank);
                                foreach (var item in delayedUnsortedBatching)
                                {
                                    item.fromMirror = CenterOn(anonMirrorHeld, newlyRemoved);
                                }
                            }
                        }
                        else if (IsBatching && ManPointer.inst.DraggingItem?.block && lastRemovedTank == tank)
                        {
                            var anchor = newlyRemoved.GetComponent<ModuleAnchor>();
                            if (newlyRemoved.GetComponent<ModuleTechController>() || (anchor && anchor.IsAnchored))
                            {
                                DebugArchitech.Log("BuildUtil: Cannot batch grab cabs or anchored anchors");
                                CabDetached = true;
                                delayedUnsortedBatching.Clear();
                                return;
                            }
                            TankBlock TB = ManPointer.inst.DraggingItem?.block;
                            Rigidbody rbody = newlyRemoved.GetComponent<Rigidbody>();
                            if (rbody)
                            {
                                rbody.velocity = Vector3.zero;
                                rbody.angularVelocity = Vector3.zero;
                            }
                            BlockCache BC = CenterOn(TB, newlyRemoved);
                            BlockCache BCM = BC;
                            if (anonMirrorHeld)
                            {
                                BCM = CenterOn(anonMirrorHeld, newlyRemoved);
                            }
                            BC.TidyUp();
                            delayedUnsortedBatching.Add(new MirrorCache
                            {
                                originTank = tank,
                                handledBlock = newlyRemoved,
                                fromPlayer = BC,
                                fromMirror = BCM
                            });
                        }
                    }
                }
                else
                {
                    if (ManPointer.inst.DraggingItem == newlyRemoved.visible)
                    {
                        DebugArchitech.Log("BuildUtil: Grabbed anonMirrorHeld");
                        delayedRemove.Add(new KeyValuePair<Tank, TankBlock>(tank, newlyRemoved));
                        lastRemovedTank = tank;
                        anonMirrorHeld = inst.GetMirroredBlock(tank, newlyRemoved);
                        if (anonMirrorHeld)
                        {
                            inst.CorrectMirrorBlockRotation(ref anonMirrorHeld, lastRemovedTank);
                            foreach (var item in delayedUnsortedBatching)
                            {
                                item.fromMirror = CenterOn(anonMirrorHeld, newlyRemoved);
                            }
                        }
                    }
                    else if (IsBatching && ManPointer.inst.DraggingItem?.block && !BusyBatching && lastRemovedTank == tank)
                    {
                        var anchor = newlyRemoved.GetComponent<ModuleAnchor>();
                        if (newlyRemoved.GetComponent<ModuleTechController>() || (anchor && anchor.IsAnchored))
                        {
                            DebugArchitech.Log("BuildUtil: Cannot batch grab cabs or anchored anchors");
                            CabDetached = true;
                            delayedUnsortedBatching.Clear();
                            return;
                        }
                        TankBlock TB = ManPointer.inst.DraggingItem?.block;
                        Rigidbody rbody = newlyRemoved.GetComponent<Rigidbody>();
                        if (rbody)
                        {
                            rbody.velocity = Vector3.zero;
                            rbody.angularVelocity = Vector3.zero;
                        }
                        BlockCache BC = CenterOn(TB, newlyRemoved);
                        BlockCache BCM = BC;
                        if (anonMirrorHeld)
                        {
                            BCM = CenterOn(anonMirrorHeld, newlyRemoved);
                        }
                        BC.TidyUp();
                        delayedUnsortedBatching.Add(new MirrorCache
                        {
                            originTank = tank,
                            handledBlock = newlyRemoved,
                            fromPlayer = BC,
                            fromMirror = BCM
                        });
                    }
                }
            }
            else if (IsBatching && ManPointer.inst.DraggingItem?.block && !BusyBatching)
            {
                var anchor = newlyRemoved.GetComponent<ModuleAnchor>();
                if (newlyRemoved.GetComponent<ModuleTechController>() || (anchor && anchor.IsAnchored))
                {
                    DebugArchitech.Log("BuildUtil: Cannot batch grab cabs or anchored anchors");
                    CabDetached = true;
                    delayedUnsortedBatching.Clear();
                    return;
                }
                TankBlock TB = ManPointer.inst.DraggingItem?.block;
                Rigidbody rbody = newlyRemoved.GetComponent<Rigidbody>();
                if (rbody)
                {
                    rbody.velocity = Vector3.zero;
                    rbody.angularVelocity = Vector3.zero;
                }
                BlockCache BC = CenterOn(TB, newlyRemoved);
                BlockCache BCM = BC;
                delayedUnsortedBatching.Add(new MirrorCache
                {
                    originTank = tank,
                    handledBlock = newlyRemoved,
                    fromPlayer = BC,
                    fromMirror = BCM
                });
            }
        }
        public static void OnBlockTableChange()
        {
        }

        public static List<KeyValuePair<Tank, TankBlock>> delayedAdd = new List<KeyValuePair<Tank, TankBlock>>();
        public static List<KeyValuePair<Tank, TankBlock>> delayedRemove = new List<KeyValuePair<Tank, TankBlock>>();
        public static List<MirrorCache> delayedUnsortedBatching = new List<MirrorCache>();
        public void Update()
        {
            UpdateBuildingTools();
            if (ManPointer.inst.DraggingItem?.block && cachePointerHeld != ManPointer.inst.DraggingItem.block)
            {
                cachePointerHeld = ManPointer.inst.DraggingItem.block;
                lastDraggedPosition = cachePointerHeld.trans.position;
            }
        }
        public void FixedUpdate()
        {
            if (cachePointerHeld)
            {
                lastDragVelo = Vector3.ClampMagnitude((cachePointerHeld.trans.position - lastDraggedPosition) / Time.fixedDeltaTime, 120f);
                lastDraggedPosition = cachePointerHeld.trans.position;
            }
            else
                lastDragVelo = Vector3.zero;
        }

        public void UpdateBuildingTools()
        {
            if (ManNetwork.IsNetworked)
            {
                DropAll();
                lastRemovedTank = null;
                cachedMirrorAngle = MirrorAngle.None;
                BusyGrabbingTechs = false;
                return;
            }
            else if (IsThisSuppressed)
            {
                OH.HideHighlight();
                if (lastHovered)
                {
                    ResetLastHovered();
                }
                if (MirrorHeld)
                {
                    PostGrabBlock(MirrorHeld);
                    DropMirror();
                }
                cachePointerHeld = null;
                lastRemovedTank = null;
                cachedMirrorAngle = MirrorAngle.None;
                BusyGrabbingTechs = false;
                return;
            }

            ApplyDetachQueue();
            lastRemovedTank = null;
            ApplyAttachQueue();
            CabDetached = false;

            if (IsGrabbingTechsActive && ManPointer.inst.DraggingItem?.block)
            {
                if (!BusyGrabbingTechs)
                {
                    GrabEntireTechFromBlock();
                }
            }
            else if (BusyGrabbingTechs)
            {
                TryCreateTechFromPointerBlocks();
            }
            else if (!IsBatching || !ManPointer.inst.DraggingItem?.block)
            {
                ReleaseAndTryMaintainBatches();
            }

            if (currentTank != ManPointer.inst.DraggingFocusTech)
            {
                if (currentTank)
                {
                    currentTank.blockman.BlockTableRecentreEvent.Unsubscribe(OnBlockTableChange);
                }
                currentTank = ManPointer.inst.DraggingFocusTech;
                if (currentTank)
                {
                    currentTank.blockman.BlockTableRecentreEvent.Subscribe(OnBlockTableChange);
                }
            }

            if (ToggleBatchMode != lastFrameButton2)
            {
                if (ToggleBatchMode)
                {
                    IsBatchActive = !IsBatchActive;
                    if (IsBatchActive)
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Open);
                    else
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Close);
                }
                lastFrameButton2 = ToggleBatchMode;
            }

            if (ToggleMirrorMode != lastFrameButton)
            {
                if (ToggleMirrorMode)
                {
                    IsMirrorModeActive = !IsMirrorModeActive;
                    if (IsMirrorModeActive)
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Open);
                    else
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Close);
                }
                lastFrameButton = ToggleMirrorMode;
            }

            if (currentTank == null)
            {   // we don't have a valid tech we are building on
                UpdateHasNoTech();
            }
            else
            {   // We have a valid tech we are building on
                UpdateHasValidTech();
            }
        }

        /// <summary>
        /// True if newly registered
        /// </summary>
        private static bool RegisterBlockIfNeeded(BlockTypes type, MirrorAngle angle)
        {
            if (ManMods.inst.IsModdedBlock(type))
            {
                if (!cacheRotationsModded.TryGetValue(type, out _))
                {
                    cacheRotationsModded.Add(type, angle);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                if (!cacheRotations.TryGetValue(type, out _))
                {
                    cacheRotations.Add(type, angle);
                    return true;
                }
                else
                    return false;
            }
        }
        private static bool FetchRegisteredBlock(BlockTypes type, ref MirrorAngle angle)
        {
            if (ManMods.inst.IsModdedBlock(type))
            {
                if (cacheRotationsModded.TryGetValue(type, out angle))
                {
                    return true;
                }
                else
                    return false;
            }
            else
            {
                if (cacheRotations.TryGetValue(type, out angle))
                {
                    return true;
                }
                else
                    return false;
            }
        }

        public void UpdateHasNoTech()
        {
            if (ManPointer.inst.targetTank && Input.GetKey(KickStart.ChangeRoot) && KickStart.IsIngame)
            {   // Root Setter
                if (IsBatchActive)
                    RebuildTechCabForwards(ManPointer.inst.targetTank);
                else
                    SetNextPlacedRootCab(ManPointer.inst.targetTank);
            }
            if (IsMirroring)
            {
                if (ManPointer.inst.targetVisible?.block)
                {
                    // Get the mirror of the hovered over block
                    TankBlock PlayerHeld = ManPointer.inst.targetVisible.block;
                    TankBlock Mirror = MirroredFetch(PlayerHeld);
                    if (Mirror)
                    {
                        if (lastHovered != Mirror)
                        {
                            ResetLastHovered();
                            OH.SetHighlightType(ManPointer.HighlightVariation.Normal);
                            if (IsPaintingSkin)
                                Mirror.visible.Outline.EnableOutline(true, cakeslice.Outline.OutlineEnableReason.CustomSkinHighlight);
                            else
                                OH.Highlight(Mirror.visible);

                            lastType = Mirror.BlockType;
                            pairType = GetPair(Mirror);
                            if (pairType == lastType)
                                GetMirrorNormal(Mirror, ref cachedMirrorAngle);
                            else
                            {
                                DebugArchitech.Log("Block " + Mirror.name + " is x-axis mirror (has a separate mirror block)");
                                cachedMirrorAngle = MirrorAngle.X;
                            }
                            lastHovered = Mirror;
                        }
                        if (lastHovered)
                        {
                            lastType = lastHovered.BlockType;
                            if (cachedMirrorAngle == MirrorAngle.None)
                            {
                                if (pairType == lastType)
                                    GetMirrorNormal(lastHovered, ref cachedMirrorAngle);
                                else
                                {
                                    DebugArchitech.Log("Block " + MirrorHeld.name + " is x-axis mirror (has a separate mirror block)");
                                    cachedMirrorAngle = MirrorAngle.X;
                                }
                            }
                            if (IsPaintSkinsActive)
                            {
                                OH.HideHighlight();
                                lastHovered.visible.Outline.EnableOutline(false, cakeslice.Outline.OutlineEnableReason.Pointer);
                                lastHovered.visible.Outline.EnableOutline(false, cakeslice.Outline.OutlineEnableReason.CustomSkinHighlight);
                                lastHovered.visible.Outline.EnableOutline(true, cakeslice.Outline.OutlineEnableReason.CustomSkinHighlight);
                                if (IsPaintingSkin)
                                {
                                    //Debug.Log("PAINTING");
                                    ManCustomSkins.inst.TryPaintBlock(lastHovered);
                                }
                            }
                        }
                    }
                    else
                    {
                        ResetLastHovered();
                    }
                    attachFrameDelay = true;
                    return;
                }
                else if (ManPointer.inst.DraggingItem?.block)
                {
                    TankBlock PlayerHeld = ManPointer.inst.DraggingItem.block;
                    UpdateMirrorHeldBlock(PlayerHeld, true);
                    attachFrameDelay = true;
                    return;
                }
                else
                {
                    ResetLastHovered();
                }
            }
            else if (IsBatching)
            {
                if (ManPointer.inst.targetVisible?.block)
                {
                    TankBlock PlayerHeld = ManPointer.inst.targetVisible.block;
                    CarryBlockBatchesNoMirror(PlayerHeld);
                    attachFrameDelay = true;
                    return;
                }
                else if (ManPointer.inst.DraggingItem?.block)
                {
                    TankBlock PlayerHeld = ManPointer.inst.DraggingItem.block;
                    CarryBlockBatchesNoMirror(PlayerHeld);
                    attachFrameDelay = true;
                    return;
                }
                else
                {
                    ResetLastHovered();
                }
            }
            else
            {
                ResetLastHovered();
            }
            if (!attachFrameDelay)
            {
                UpdateMirrorHeldBlock(null, false);
            }
            else
            {
                attachFrameDelay = false;
            }
        }
        public void UpdateHasValidTech()
        {
            if (IsPaintSkinsActive)
            {
                DropAll(true);
                if (lastHovered)
                {
                    lastHovered.visible.Outline.EnableOutline(false, cakeslice.Outline.OutlineEnableReason.Pointer);
                    lastHovered.visible.Outline.EnableOutline(false, cakeslice.Outline.OutlineEnableReason.CustomSkinHighlight);
                    lastHovered.visible.Outline.EnableOutline(true, cakeslice.Outline.OutlineEnableReason.CustomSkinHighlight);
                    if (IsPaintingSkin)
                    {
                        //Debug.Log("PAINTING");
                        ManCustomSkins.inst.TryPaintBlock(lastHovered);
                    }
                }
                else
                    OH.HideHighlight();
                return;
            }

            if (Input.GetKey(KickStart.ChangeRoot) && KickStart.IsIngame)
            {
                if (IsBatchActive)
                    RebuildTechCabForwards(currentTank);
                else
                    SetNextPlacedRootCab(currentTank);
            }
            else if (ManPointer.inst.DraggingItem?.block)
            {
                TankBlock playerHeld = ManPointer.inst.DraggingItem.block;
                if (IsMirroring)
                {
                    if (lastType != playerHeld.BlockType)
                    {
                        lastType = playerHeld.BlockType;
                        pairType = GetPair(playerHeld);

                        UpdateMirrorHeldBlock(playerHeld, true);
                        if (!TechUtils.IsBlockAvailInInventory(currentTank, pairType))
                        {   // no more blocks!
                            ManSFX.inst.PlayUISFX(ManSFX.UISfxType.AnchorFailed);
                        }
                    }
                    else
                    {
                        UpdateMirrorHeldBlock(playerHeld, true);
                    }
                }
                else if (IsBatching)
                {
                    if (MirrorBatchCache && MirrorBatchCache.Count > 0)
                    {
                        DropMirrorBlockBatch(true, false);
                    }
                    if (MirrorHeld)
                    {
                        PostGrabBlock(MirrorHeld);
                        DropMirror();
                    }
                    CarryBlockBatchesNoMirror(playerHeld);
                }
                else
                    UpdateMirrorHeldBlock(playerHeld, false);

            }
            else
                UpdateMirrorHeldBlock(null, false);
        }


        public static void TryPushToCursorBatch(BlockBatch BB)
        {
            if (PointerBatchCache)
            {
                DebugArchitech.Assert(true, "Architech: ManBuildUtil - Cannot nest BlockBatches!");
                return;
            }
            PointerBatchCacheMirrorCheck = true;
            PointerBatchCache = BB;
        }
        public static void TryPushToMirrorBatch(BlockBatch BB, bool isInventory)
        {
            if (MirrorBatchCache)
            {
                DebugArchitech.Assert(true, "Architech: ManBuildUtil - Cannot nest BlockBatches!");
                return;
            }
            if (MirrorHeld)
            {
                DebugArchitech.Assert(true, "Architech: ManBuildUtil - Cannot nest MirrorHeld!");
                return;
            }
            DebugArchitech.Assert(!BB.Root, "Architech: ManBuildUtil - MirrorBatchCache's root IS NULL!");
            MirrorHeld = BB.Root;
            MirrorBatchCache = BB;
            MirrorBatchCacheIsInventory = isInventory;
            PointerBatchCacheMirrorCheck = false;
        }

        internal static bool CanReleaseMirrorBlockBatch()
        {
            if (!MirrorBatchCache)
                return false;
            if (MirrorBatchCacheIsInventory)
            {
                return HasNeededInInventory(currentTank, MirrorBatchCache.GetAllBlockTypes());
            }
            return true;
        }
        public void DropMirror()
        {
            if (InventoryBlock)
                ManLooseBlocks.inst.RequestDespawnBlock(MirrorHeld, DespawnReason.Host);
            MirrorHeld = null;
        }
        public void DropBatches()
        {
            if (PointerBatchCache)
                PointerBatchCache.DropAllButRoot();
            PointerBatchCache = null;

            DropMirrorBlockBatch(true, false);
        }
        internal static void DropMirrorBlockBatch(bool DropAllButRoot, bool KeepInWorld)
        {
            if (MirrorBatchCache)
            {
                if (DropAllButRoot)
                    MirrorBatchCache.DropAllButRoot();
                if (KeepInWorld)
                {
                    if (MirrorBatchCacheIsInventory)
                        TakeNeededFromInventory(currentTank, MirrorBatchCache.GetAllBlockTypes());
                }
                else
                {
                    if (MirrorBatchCacheIsInventory)
                    {
                        foreach (var item in MirrorBatchCache.GetAllBlocks())
                        {
                            ManLooseBlocks.inst.RequestDespawnBlock(item, DespawnReason.Host);
                        }
                    }
                }
            }
            MirrorBatchCache = null;
            MirrorBatchCacheIsInventory = false;
        }


        public void ReleaseAndTryMaintainBatches()
        {
            if (CanReleaseMirrorBlockBatch())
            {
                ManBlockBatches.TryMaintainDroppedMirror(MirrorBatchCache, ManBlockBatches.TryMaintainDropped(PointerBatchCache));
                DropMirrorBlockBatch(false, true);
            }
            else
            {
                ManBlockBatches.TryMaintainDropped(PointerBatchCache);
                DropMirrorBlockBatch(false, false);
            }
            PointerBatchCache = null;
        }
        public void DropAll(bool ExcludeHoverAndMaintainBatches = false)
        {
            OH.HideHighlight();
            if (lastHovered && !ExcludeHoverAndMaintainBatches)
            {
                ResetLastHovered();
            }
            if (MirrorHeld)
            {
                PostGrabBlock(MirrorHeld);
                DropMirror();
            }
            if (ExcludeHoverAndMaintainBatches)
                ReleaseAndTryMaintainBatches();
            else
                DropBatches();
            cachePointerHeld = null;
        }
        public void ApplyAttachQueue()
        {
            if (delayedAdd.Count > 0)
            {
                Busy = true;
                bool error = false;
                bool attached = false;
                foreach (var item in delayedAdd)
                {
                    Tank targetTank = item.Key;
                    if (targetTank && item.Value && item.Key.visible.isActive && item.Value.visible.isActive)
                    {
                        if (IsMirroring)
                        {
                            if (MirroredPlacement(targetTank, item.Value))
                                attached = true;
                            else
                                error = true;
                        }
                        else
                        {
                            if (BatchPlacementNonMirror(targetTank, item.Value))
                                attached = true;
                        }
                    }
                }
                if (error)
                    Invoke("DelayedFail", 0.1f);
                if (attached)
                    Invoke("DelayedAffirm", 0.1f);
                delayedAdd.Clear();
                Busy = false;
            }
        }

        public void ApplyDetachQueue()
        {
            if (delayedRemove.Count > 0)
            {
                Busy = true;
                bool error = false;
                foreach (var item in delayedRemove)
                {
                    if (item.Key && item.Value && item.Key.visible.isActive && item.Value.visible.isActive)
                    {
                        if (!MirroredRemove(item.Key, item.Value))
                            error = true;
                    }
                }
                if (error)
                    Invoke("DelayedFail", 0.1f);
                else
                    Invoke("DelayedAffirmDetach", 0.1f);
                delayedRemove.Clear();
            }
            if (delayedUnsortedBatching.Count > 0)
            {
                BusyBatching = true;
                bool batched = false;
                foreach (var item in delayedUnsortedBatching)
                {
                    if (item.Verify())
                    {
                        if (BatchCollect(item.originTank, item.handledBlock, item.fromPlayer, item.fromMirror))
                        {
                            batched = true;
                        }
                    }
                }
                if (batched)
                    Invoke("DelayedBatching", 0.2f);
                delayedUnsortedBatching.Clear();
                BusyBatching = false;
            }
            Busy = false;
        }


        public void ResetLastHovered()
        {
            if (lastHovered)
            {
                OH.HideHighlight();
                lastHovered.visible.Outline.EnableOutline(false, cakeslice.Outline.OutlineEnableReason.CustomSkinHighlight);
                lastHovered = null;
            }
        }
        public void UpdateMirrorHeldBlock(TankBlock toMirror, bool show)
        {
            try
            {
                ResetLastHovered();
                if (show)
                {
                    if (MirrorHeld)
                    {
                        if (MirrorHeld.BlockType != pairType)
                        {
                            if (PaintBlocks)
                            {
                                PostGrabBlock(MirrorHeld);
                                OH.HideHighlight();
                                DropMirror();
                                ReleaseAndTryMaintainBatches();

                                if (currentTank && TechUtils.IsBlockAvailInInventory(currentTank, pairType))
                                {
                                    TankBlock newFake = ManLooseBlocks.inst.HostSpawnBlock(pairType, currentTank.boundsCentreWorld + (Vector3.up * 128), Quaternion.identity);

                                    MirrorHeld = newFake;
                                    PreGrabBlock(MirrorHeld);
                                    InventoryBlock = true;
                                }
                            }
                            cachedMirrorAngle = MirrorAngle.None;
                        }
                        if (MirrorHeld)
                            OH.Highlight(MirrorHeld.visible);
                    }
                    else
                    {
                        if (PaintBlocks)
                        {
                            if (currentTank && TechUtils.IsBlockAvailInInventory(currentTank, pairType))
                            {
                                TankBlock newFake = ManLooseBlocks.inst.HostSpawnBlock(pairType, currentTank.boundsCentreWorld + (Vector3.up * 128), Quaternion.identity);
                                MirrorHeld = newFake;
                                PreGrabBlock(MirrorHeld);
                                cachedMirrorAngle = MirrorAngle.None;
                                OH.Highlight(MirrorHeld.visible);
                                InventoryBlock = true;
                            }
                        }
                    }

                    if (MirrorHeld)
                    {
                        if (InventoryBlock)
                        {
                            byte skinInd = toMirror.GetSkinIndex();
                            if (MirrorHeld.GetSkinIndex() != skinInd)
                            {
                                if (ManCustomSkins.inst.CanUseSkin(ManSpawn.inst.GetCorporation(toMirror.BlockType), skinInd))
                                    MirrorHeld.SetSkinIndex(skinInd);
                            }
                        }

                        if (cachedMirrorAngle == MirrorAngle.None)
                        {
                            if (pairType == lastType)
                                GetMirrorNormal(MirrorHeld, ref cachedMirrorAngle);
                            else
                            {
                                DebugArchitech.Log("Block " + MirrorHeld.name + " is x-axis mirror (has a separate mirror block)");
                                cachedMirrorAngle = MirrorAngle.X;
                            }
                        }
                        if (currentTank)
                        {
                            lastFramePlacementInvalid = DoesBlockConflictWithMain(toMirror) || DoesBlockConflictWithTech();
                            if (ManTechBuilder.inst.IsBlockHeldInPosition(toMirror))
                            {
                                if (lastFramePlacementInvalid)
                                    OH.SetHighlightType(ManPointer.HighlightVariation.Invalid);
                                else
                                    OH.SetHighlightType(ManPointer.HighlightVariation.Attaching);
                            }
                            else
                                OH.SetHighlightType(ManPointer.HighlightVariation.Normal);
                        }
                        else
                            OH.SetHighlightType(ManPointer.HighlightVariation.Normal);

                        MirroredSpace(toMirror, ref MirrorHeld);
                    }
                    else
                    {
                        cachedMirrorAngle = MirrorAngle.None;
                        CarryBlockBatchesNoMirror(toMirror);
                    }
                }
                else
                {
                    if (MirrorHeld)
                    {
                        lastType = BlockTypes.GSOAIController_111;
                        pairType = BlockTypes.GSOAIController_111;
                        PostGrabBlock(MirrorHeld);
                        OH.HideHighlight();
                        DropMirror();
                        cachedMirrorAngle = MirrorAngle.None;
                    }
                    ReleaseAndTryMaintainBatches();
                }
            }
            catch 
            {
                DebugArchitech.Assert(MirrorHeld, "Architech: Game has stolen the locked MirrorHeld block somehow. Discarding...");
                MirrorHeld = null; 
            } // it was hyjacked somehow
        }


        public bool DoesBlockConflictWithMain(TankBlock toMirror)
        {
            foreach (var item in MirrorHeld.filledCells)
            {
                Vector3 V3 = MirrorHeld.trans.position + (MirrorHeld.trans.rotation * item);

                foreach (var item2 in toMirror.filledCells)
                {
                    Vector3 V3M = toMirror.trans.position + (toMirror.trans.rotation * item2);
                    if (V3.Approximately(V3M))
                        return true;
                }
            }
            return false;
        }
        public bool DoesBlockConflictWithTech()
        {
            foreach (var item in MirrorHeld.filledCells)
            {
                IntVector3 IV3 = currentTank.trans.InverseTransformPoint(MirrorHeld.trans.position + (MirrorHeld.trans.rotation * item));
                if (currentTank.blockman.GetBlockAtPosition(IV3))
                    return true;
            }
            return false;
        }


        public void DelayedAffirm()
        {
            if (lastAttached)
            {
                FieldInfo attachSFX = typeof(ManTechBuilder).GetField("m_BlockAttachSFXEvents", BindingFlags.NonPublic | BindingFlags.Instance);
                FMODEvent[] soundSteal = (FMODEvent[])attachSFX.GetValue(Singleton.Manager<ManTechBuilder>.inst);
                ManSFX.inst.AttachInstanceToPosition(soundSteal[(int)lastAttached.BlockConnectionAudioType].PlayEvent(), lastAttached.centreOfMassWorld);
            }
        }
        public void DelayedAffirmDetach()
        {
            if (lastDetached)
            {
                FieldInfo attachSFX = typeof(ManTechBuilder).GetField("m_BlockDetachSFXEvents", BindingFlags.NonPublic | BindingFlags.Instance);
                FMODEvent[] soundSteal = (FMODEvent[])attachSFX.GetValue(Singleton.Manager<ManTechBuilder>.inst);
                ManSFX.inst.AttachInstanceToPosition(soundSteal[(int)lastDetached.BlockConnectionAudioType].PlayEvent(), lastDetached.centreOfMassWorld);
            }
        }
        public void DelayedFail()
        {
            ManSFX.inst.PlayUISFX(ManSFX.UISfxType.AnchorFailed);
        }
        public void DelayedBatching()
        {
        }


        public static Vector3 SnapToMirrorAxi(Vector3 toSnap)
        {
            toSnap.x = SnapToMirrorDim(toSnap.x);
            toSnap.y = SnapToMirrorDim(toSnap.y);
            toSnap.z = SnapToMirrorDim(toSnap.z);
            return toSnap;
        }
        private static float SnapToMirrorDim(float toSnap)
        {
            float extra = Mathf.Abs(toSnap) % 1f;
            if (extra < 0.25f)
                extra = 0;
            else if (extra <= 0.75f)
                extra = 0.5f;
            else
                extra = 1;
            return Mathf.Floor(toSnap) + extra;
        }


        private static bool TryAttachLoose(Tank tankCase, TankBlock tryAttachThis, bool ignoreTerrain = true)
        {
            Vector3 blockPosLocal = tankCase.trans.InverseTransformPoint(tryAttachThis.trans.position);

            BlockCache BC = new BlockCache();
            BC.t = tryAttachThis.BlockType;
            BC.p = blockPosLocal;
            BC.r = SetCorrectRotation(InvTransformRot(tryAttachThis.trans, tankCase));
            BC.TidyUp();
            PostGrabBlock(tryAttachThis);

            if (!ignoreTerrain)
            {
                Vector3 anonPos = tankCase.trans.TransformPoint(BC.p);
                if (anonPos.y < ManWorld.inst.ProjectToGround(anonPos).y)
                    return false;
            }

            return TechUtils.AttemptBlockAttachExt(tankCase, BC, tryAttachThis);
        }



        public void CarryBlockBatchesNoMirror(TankBlock otherBlock)
        {
            if (PointerBatchCache)
                PointerBatchCache.UpdateHold();
        }

        private bool BatchPlacementNonMirror(Tank tankCase, TankBlock otherBlock)
        {
            bool placed = AttachAll(tankCase, otherBlock, PointerBatchCache);
            PointerBatchCache = null;
            return placed;
        }

        private bool BatchCollect(Tank tankCase, TankBlock toCollect, BlockCache playerSideBC, BlockCache mirrorSideBC)
        {
            if (!tankCase.rootBlockTrans.GetComponent<TankBlock>() || toCollect.trans == tankCase.rootBlockTrans)
                return false;
            if (toCollect == MirrorHeld ||
                (ManPointer.inst.DraggingItem?.block && ManPointer.inst.DraggingItem.block == toCollect))
                return false;
            TankBlock TB = ManPointer.inst.DraggingItem.block;
            if (IsMirroring && MirrorHeld)
            {   // handle Batching of Pointer and Mirror blocks
                Vector3 blockLocalPos = tankCase.rootBlockTrans.InverseTransformPoint(toCollect.trans.position);
                Vector3 MirrorHeldLocalPos = tankCase.rootBlockTrans.InverseTransformPoint(MirrorHeld.trans.position);

                if (MirrorHeldLocalPos.x > 0)
                {
                    if (blockLocalPos.x > 0)
                    {   // Mirror Side
                        if (playerSideBC.p == mirrorSideBC.p)
                            DebugArchitech.LogError("BuildUtil: Could not fetch mirrored!");
                        mirrorSideBC.inst = toCollect;
                        if (!MirrorBatchCache)
                        {
                            MirrorBatchCache = new BlockBatch(MirrorHeld);
                            MirrorBatchCacheIsInventory = false;
                        }
                        MirrorBatchCache.Add(mirrorSideBC);
                    }
                    else
                    {   // Player Side
                        playerSideBC.inst = toCollect;
                        if (!PointerBatchCache)
                        {
                            PointerBatchCache = new BlockBatch(TB);
                            PointerBatchCacheMirrorCheck = true;
                        }
                        PointerBatchCache.Add(playerSideBC);
                    }
                }
                else
                {
                    if (blockLocalPos.x > 0)
                    {   // Player Side
                        playerSideBC.inst = toCollect;
                        if (!PointerBatchCache)
                        {
                            PointerBatchCache = new BlockBatch(TB);
                            PointerBatchCacheMirrorCheck = true;
                        }
                        PointerBatchCache.Add(playerSideBC);
                    }
                    else
                    {   // Mirror Side
                        if (playerSideBC.p == mirrorSideBC.p)
                            DebugArchitech.LogError("BuildUtil: Could not fetch mirrored!");
                        mirrorSideBC.inst = toCollect;
                        if (!MirrorBatchCache)
                        {
                            MirrorBatchCache = new BlockBatch(MirrorHeld);
                            MirrorBatchCacheIsInventory = false;
                        }
                        MirrorBatchCache.Add(mirrorSideBC);
                    }
                }
                return true;
            }
            else
            {   // handle Batching of only Pointer blocks
                // Player Side
                playerSideBC.inst = toCollect;
                if (!PointerBatchCache)
                    PointerBatchCache = new BlockBatch(TB);
                PointerBatchCache.Add(playerSideBC);
            }
            return false;
        }

        public bool CorrectMirrorBlockRotation(ref TankBlock toCorrect, Tank fallback)
        {
            if (currentTank == null && fallback)
            {
                currentTank = fallback;
                if (toCorrect && ManPointer.inst.DraggingItem?.block)
                {
                    Quaternion rotStart = toCorrect.trans.rotation;
                    Vector3 prevPos = toCorrect.trans.position + (rotStart * toCorrect.BlockCellBounds.center);
                    MirroredSpace(ManPointer.inst.DraggingItem?.block, ref toCorrect);
                    Quaternion rotEnd = toCorrect.trans.rotation;
                    toCorrect.trans.position = prevPos - (rotEnd * toCorrect.BlockCellBounds.center);
                    currentTank = null;
                    return true;
                }
                currentTank = null;
            }
            else
            {
                if (toCorrect && ManPointer.inst.DraggingItem?.block)
                {
                    Quaternion rotStart = toCorrect.trans.rotation;
                    Vector3 prevPos = toCorrect.trans.position + (rotStart * toCorrect.BlockCellBounds.center);
                    MirroredSpace(ManPointer.inst.DraggingItem.block, ref toCorrect);
                    Quaternion rotEnd = toCorrect.trans.rotation;
                    toCorrect.trans.position = prevPos - (rotEnd * toCorrect.BlockCellBounds.center);
                    return true;
                }
            }
            return false;
        }

        public void MirroredSpace(TankBlock otherBlock, ref TankBlock mirror)
        {
            if (currentTank == null)
            {
                mirror.trans.position = otherBlock.trans.position + (Vector3.up * (otherBlock.BlockCellBounds.size.y + 1));
                mirror.trans.rotation = otherBlock.trans.rotation;

                if (PointerBatchCache)
                    PointerBatchCache.UpdateHold();
                if (MirrorBatchCache)
                    MirrorBatchCache.UpdateHold();
                return;
            }
            Vector3 otherBlockPos = currentTank.trans.InverseTransformPoint(otherBlock.trans.position);

            Vector3 blockCenter = otherBlock.BlockCellBounds.center;
            Quaternion rotOther = InvTransformRot(otherBlock.trans, currentTank);

            Vector3 tankCenter = currentTankCenter;
            Vector3 centerDelta = otherBlockPos + (rotOther * blockCenter) - tankCenter;

            Quaternion rotMirror = MirroredRot(otherBlock, rotOther, mirror.BlockType == otherBlock.BlockType);
            centerDelta.x *= -1;

            blockCenter = mirror.BlockCellBounds.center;

            Vector3 centerMirror = (-(rotMirror * blockCenter)) + tankCenter + centerDelta;

            if (DebugBlockRotations)
            {
                DrawDirIndicator(otherBlock.gameObject, 0, otherBlock.trans.TransformVector(Vector3.right * 3), new Color(1, 0, 0));
                DrawDirIndicator(otherBlock.gameObject, 1, otherBlock.trans.TransformVector(Vector3.up * 3), new Color(0, 1, 0));
                DrawDirIndicator(otherBlock.gameObject, 2, otherBlock.trans.TransformVector(Vector3.forward * 3), new Color(0, 0, 1));

                DrawDirIndicator(mirror.gameObject, 0, mirror.trans.TransformVector(Vector3.right * 3), new Color(1, 0, 0));
                DrawDirIndicator(mirror.gameObject, 1, mirror.trans.TransformVector(Vector3.up * 3), new Color(0, 1, 0));
                DrawDirIndicator(mirror.gameObject, 2, mirror.trans.TransformVector(Vector3.forward * 3), new Color(0, 0, 1));
            }

            mirror.trans.position = currentTank.trans.TransformPoint(centerMirror);
            mirror.trans.rotation = TransformRot(rotMirror, currentTank);
            UpdateHeldBlockBatches();
        }
        
        internal static void DoMirroredRotationInRelationToTankNotSpawned(Tank currentTank, bool hasMirrorPairBlock, ref TankBlock toMirror)
        {
            if (currentTank == null)
                return;
            TankBlock mirrorPre = toMirror;

            Vector3 MirrorBlockPos = currentTank.trans.InverseTransformPoint(toMirror.trans.position);

            Vector3 blockCenter = mirrorPre.BlockCellBounds.center;
            Quaternion rotOther = InvTransformRot(toMirror.trans, currentTank);

            Vector3 tankCenter = GetTankCenterNoCheck(currentTank);
            Vector3 centerDelta = MirrorBlockPos + (rotOther * blockCenter) - tankCenter;

            Quaternion rotMirror = MirroredRot(mirrorPre, rotOther, !hasMirrorPairBlock);
            centerDelta.x *= -1;

            blockCenter = mirrorPre.BlockCellBounds.center;

            Vector3 centerMirror = (-(rotMirror * blockCenter)) + tankCenter + centerDelta;

            if (DebugBlockRotations)
            {
                DrawDirIndicator(mirrorPre.gameObject, 0, mirrorPre.trans.TransformVector(Vector3.right * 3), new Color(1, 0, 0));
                DrawDirIndicator(mirrorPre.gameObject, 1, mirrorPre.trans.TransformVector(Vector3.up * 3), new Color(0, 1, 0));
                DrawDirIndicator(mirrorPre.gameObject, 2, mirrorPre.trans.TransformVector(Vector3.forward * 3), new Color(0, 0, 1));
            }
            // Apply
            mirrorPre.trans.position = currentTank.trans.TransformPoint(centerMirror);
            mirrorPre.trans.rotation = TransformRot(rotMirror, currentTank);
        }
        internal static void UpdateHeldBlockBatches()
        {
            if (PointerBatchCache)
            {
                PointerBatchCache.UpdateHold();

                if (MirrorBatchCache)
                    MirrorBatchCache.UpdateHold();
                else if (PointerBatchCacheMirrorCheck && HasNeededInInventory(currentTank,
                    PointerBatchCache.GetAllBlockTypes().ConvertAll(x => GetPair(x))))
                {
                    TryPushToMirrorBatch(new BlockBatch(currentTank, PointerBatchCache), true);
                    if (MirrorBatchCache)
                        MirrorBatchCache.UpdateHold();
                }
                PointerBatchCacheMirrorCheck = false;
            }
            else if (MirrorBatchCache)
                MirrorBatchCache.UpdateHold();
        }

        internal static bool CanAttachMirrorBlockBatch()
        {
            if (!MirrorBatchCache)
                return false;
            return HasNeededInInventory(currentTank, MirrorBatchCache.GetAllBlockTypes());
        }

        public static Vector3 TryGetCenter(Tank tankCase)
        {
            return tankCase.rootBlockTrans.GetComponent<TankBlock>() ?
            SnapToMirrorAxi(tankCase.rootBlockTrans.localPosition +
                (tankCase.rootBlockTrans.localRotation * tankCase.rootBlockTrans.GetComponent<TankBlock>().BlockCellBounds.center))
            : tankCase.blockBounds.center;
        }

        private bool MirroredPlacement(Tank tankCase, TankBlock otherBlock)
        {
            Vector3 otherBlockPos = otherBlock.trans.localPosition;

            Vector3 blockCenter = otherBlock.BlockCellBounds.center;
            Quaternion rotOther = otherBlock.trans.localRotation;

            Vector3 tankCenter = TryGetCenter(tankCase);

            Vector3 centerDelta = (rotOther * blockCenter) + otherBlockPos - tankCenter;

            BlockCache BC = new BlockCache();
            BC.t = GetPair(otherBlock);

            bool fromInv = false;
            TankBlock newBlock;
            if (!PaintBlocks && MirrorHeld && MirrorHeld.BlockType == BC.t)
            {
                newBlock = MirrorHeld;
                //Debug.Log("MirroredPlacement - Attached held real block");

                PostGrabBlock(MirrorHeld);
                MirrorHeld = null;
            }
            else if (TechUtils.IsBlockAvailInInventory(tankCase, BC.t))
            {
                fromInv = true;
                newBlock = ManLooseBlocks.inst.HostSpawnBlock(BC.t, tankCase.boundsCentreWorld + (Vector3.up * 128), Quaternion.identity);
            }
            else
                return false;

            bool isOther = BC.t != otherBlock.BlockType;
            if (isOther)
            {
                blockCenter = newBlock.BlockCellBounds.center;
            }

            Quaternion rotMirror = MirroredRot(otherBlock, rotOther, !isOther);
            centerDelta.x *= -1;
            Vector3 centerMirror = (-(rotMirror * blockCenter)) + centerDelta + tankCenter;

            BC.p = centerMirror;
            BC.r = SetCorrectRotation(rotMirror);
            BC.TidyUp();

            if (fromInv)
            {
                byte skin = otherBlock.GetSkinIndex();
                if (ManCustomSkins.inst.CanUseSkin(ManSpawn.inst.GetCorporation(newBlock.BlockType), skin))
                    newBlock.SetSkinIndex(skin);
            }

            lastAttached = newBlock;
            if (TechUtils.AttemptBlockAttachExt(tankCase, BC, newBlock))
            {
                if (fromInv)
                    TechUtils.IsBlockAvailInInventory(tankCase, BC.t, 1, true);

                AttachAll(tankCase, otherBlock, PointerBatchCache);
                PointerBatchCache = null;
                AttachAll(tankCase, newBlock, MirrorBatchCache);
                DropMirrorBlockBatch(false, true);
                return true;
            }
            else
            {
                if (fromInv)
                    ManLooseBlocks.inst.HostDestroyBlock(newBlock);

                AttachAll(tankCase, otherBlock, PointerBatchCache);
                PointerBatchCache = null;
                DropMirrorBlockBatch(true, false);
                /*
                newBlock.trans.position = tankCase.trans.TransformPoint(centerMirror + (rotMirror * blockCenter));
                newBlock.trans.rotation = TransformRot(rotMirror);
                */
                return false;
            }
        }

        public static TankBlock MirroredFetch(TankBlock toMirror)
        {
            if (!toMirror || !toMirror.IsAttached || !toMirror.tank)
                return null;
            Tank tankCase = toMirror.tank;
            if (!tankCase.rootBlockTrans.GetComponent<TankBlock>() || toMirror.trans == tankCase.rootBlockTrans)
                return null;

            Vector3 otherBlockPos = toMirror.trans.localPosition;

            Vector3 blockCenter = toMirror.BlockCellBounds.center;
            Quaternion rotOther = toMirror.trans.localRotation;

            Vector3 tankCenter = TryGetCenter(tankCase);

            Vector3 centerDelta = (rotOther * blockCenter) + otherBlockPos - tankCenter;

            BlockTypes otherSideType = GetPair(toMirror);

            bool isOther = otherSideType != toMirror.BlockType;
            if (isOther)
            {
                TankBlock tempBlock = ManSpawn.inst.GetBlockPrefab(otherSideType);
                blockCenter = tempBlock.BlockCellBounds.center - tempBlock.filledCells[0];
            }
            else
                blockCenter = toMirror.BlockCellBounds.center - toMirror.filledCells[0];

            Quaternion rotMirror = MirroredRot(toMirror, rotOther, !isOther);
            centerDelta.x *= -1;
            Vector3 centerMirror = (-(rotMirror * blockCenter)) + centerDelta + tankCenter;

            TankBlock mirrorGet = tankCase.blockman.GetBlockAtPosition(new IntVector3(Vector3Int.RoundToInt(centerMirror)));
            if (mirrorGet && mirrorGet.BlockType == otherSideType)
            {
                if (DebugBlockRotations)
                {
                    DrawDirIndicator(toMirror.gameObject, 0, toMirror.trans.TransformVector(Vector3.right * 3), new Color(1, 0, 0));
                    DrawDirIndicator(toMirror.gameObject, 1, toMirror.trans.TransformVector(Vector3.up * 3), new Color(0, 1, 0));
                    DrawDirIndicator(toMirror.gameObject, 2, toMirror.trans.TransformVector(Vector3.forward * 3), new Color(0, 0, 1));

                    DrawDirIndicator(mirrorGet.gameObject, 0, mirrorGet.trans.TransformVector(Vector3.right * 3), new Color(1, 0, 0));
                    DrawDirIndicator(mirrorGet.gameObject, 1, mirrorGet.trans.TransformVector(Vector3.up * 3), new Color(0, 1, 0));
                    DrawDirIndicator(mirrorGet.gameObject, 2, mirrorGet.trans.TransformVector(Vector3.forward * 3), new Color(0, 0, 1));
                }
                return mirrorGet;
            }
            return null;
        }

        private TankBlock GetMirroredBlock(Tank tankCase, TankBlock otherBlock)
        {
            if (!tankCase.rootBlockTrans.GetComponent<TankBlock>() || otherBlock.trans == tankCase.rootBlockTrans)
                return null;
            BlockTypes otherSideType = GetPair(otherBlock);


            if (ManPointer.inst.DraggingItem?.block && ManPointer.inst.DraggingItem?.block == otherBlock && lastHovered)
            {
                Vector3 centerMirror = lastHovered.CalcFirstFilledCellLocalPos();

                TankBlock blockOnOtherSide = tankCase.blockman.GetBlockAtPosition(new IntVector3(Vector3Int.RoundToInt(centerMirror)));
                if (blockOnOtherSide && blockOnOtherSide.BlockType == otherSideType)
                {
                    return blockOnOtherSide;
                }
                else
                    DebugArchitech.Log("BuildUtil: GetMirroredBlock - blockOnOtherSide was not a valid mirror block. Perhaps lastHovered was incorrect?");
            }
            else
                DebugArchitech.Assert(true, "BuildUtil: GetMirroredBlock - blockOnOtherSide was null?");
            return null;
        }

        private bool MirroredRemove(Tank tankCase, TankBlock otherBlock)
        {
            TankBlock blockOnOtherSide = GetMirroredBlock(tankCase, otherBlock);
            if (blockOnOtherSide)
            {
                ResetLastHovered();
                lastDetached = blockOnOtherSide;
                TechUtils.AttemptBlockDetachExt(tankCase, blockOnOtherSide);
                if (!blockOnOtherSide.IsAttached)
                {
                    if (MirrorHeld == null)
                    {
                        MirrorHeld = blockOnOtherSide;

                        PreGrabBlock(MirrorHeld);
                        InventoryBlock = false;
                        cachedMirrorAngle = MirrorAngle.None;
                    }
                    //blockOnOtherSide.trans.position = otherBlock.trans.position + (otherBlock.BlockCellBounds.size.magnitude * (otherBlock.trans.position - blockOnOtherSide.trans.position).normalized);
                }
                else
                    DebugArchitech.Assert(true, "BuildUtil: Our block has not detached from the Tech it was detached from.  There are now going to be many errors.");
                return true;
            }
            return false;
        }


        public static Quaternion MirroredRot(TankBlock block, Quaternion otherRot, bool sameBlock)
        {
            Vector3 forward;
            Vector3 up;

            MirrorAngle angle = cachedMirrorAngle;
            // If we already have a mirrored block that is not the same, chances are that block is already
            //   properly mirrored.  We can skip calculating the starting rotation of the mirror block 
            //   and just mirror it directly.
            //    THIS CURRENTLY DOES NOT WORK WITH MIRROR BLOCKS THAT ARE OFFSET ROTATED TO BEGIN WITH - Will fix later
            if (!sameBlock)
            {
                return GetCorrectSeperateBlockPairsRotation(block, otherRot);
            }

            // If we have the rotation cached for the type (when holding said block), we can skip recalculating
            //   the mirror block's starting rotation.
            if (block.BlockType != pairType)
            {
                GetMirrorNormal(block, ref angle);
            }

            // We mirror the other block's rotations from it's starting rotation in relation to the X-Axis:
            Quaternion offsetMirrorRot = Quaternion.identity;
            offsetMirrorRot.w = otherRot.w;
            offsetMirrorRot.x = otherRot.x;
            offsetMirrorRot.y = -otherRot.y;
            offsetMirrorRot.z = -otherRot.z;

            Quaternion offsetLook;
            // Let's get the starting rotation for the mirrored block and store it in offsetLook:
            switch (angle)
            {
                case MirrorAngle.X:
                    offsetLook = Quaternion.identity;
                    break;
                case MirrorAngle.Z:
                    offsetLook = Quaternion.LookRotation(Vector3.back, Vector3.up);
                    break;
                case MirrorAngle.Y:
                    offsetLook = Quaternion.LookRotation(Vector3.forward, Vector3.down);
                    break;

                case MirrorAngle.YCorner:
                    offsetLook = Quaternion.LookRotation(Vector3.left, Vector3.up);
                    break;
                case MirrorAngle.YCornerInv:
                    offsetLook = Quaternion.LookRotation(Vector3.right, Vector3.up);
                    break;

                case MirrorAngle.ZCorner:
                    offsetLook = Quaternion.LookRotation(Vector3.forward, Vector3.left);
                    break;
                case MirrorAngle.ZCornerInv:
                    offsetLook = Quaternion.LookRotation(Vector3.forward, Vector3.right);
                    break;

                case MirrorAngle.XCorner:
                    offsetLook = Quaternion.LookRotation(Vector3.down, Vector3.back);
                    break;
                case MirrorAngle.XCornerInv:
                    offsetLook = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                    break;


                case MirrorAngle.SeekerMissile:
                    offsetLook = Quaternion.LookRotation(Vector3.back, Vector3.up);
                    break;

                default:
                    offsetLook = Quaternion.identity;
                    break;
            }

            // Apply the offset rotation to get the mirror at the starting block rotation
            forward = offsetLook * Vector3.forward;
            up = offsetLook * Vector3.up;

            // Then we apply the mirrored rotation the player has set their held block to
            forward = offsetMirrorRot * forward;
            up = offsetMirrorRot * up;

            return Quaternion.LookRotation(forward, up);
        }

        /// <summary>
        /// NOTE: THIS IS INCOMPLETE - Plans are to use this to fix rotation in-accuracies present in other mods
        /// </summary>
        /// <param name="block"></param>
        /// <param name="otherRot"></param>
        /// <returns></returns>
        public static Quaternion GetCorrectSeperateBlockPairsRotation(TankBlock block, Quaternion otherRot)
        {
            Vector3 forward;
            Vector3 up;

            forward = otherRot * Vector3.forward;
            forward.x *= -1;
            up = otherRot * Vector3.up;
            up.x *= -1;
            return Quaternion.LookRotation(forward, up);
        }

        private static List<MeshFilter> meshes = new List<MeshFilter>();
        public static void GetMirrorNormal(TankBlock block, ref MirrorAngle angle)
        {
            if (!FetchRegisteredBlock(block.BlockType, ref angle))
            {
                if (HandleEdgeCases(block, out MirrorAngle angleFix))
                {
                    angle = angleFix;
                }
                else
                { // Get the mirror plane
                    Vector3 blockCenter = block.BlockCellBounds.center;
                    Vector3[] posCentered = new Vector3[block.attachPoints.Length];
                    for (int step = 0; step < block.attachPoints.Length; step++)
                    {
                        posCentered[step] = block.attachPoints[step] - blockCenter;
                    }
                    bool smolBlock = block.filledCells.Length < 3;

                    if (!smolBlock)
                        GetMirrorSuggestion(posCentered, out angle);

                    if (angle == MirrorAngle.NeedsPrecise || smolBlock)
                    {
                        if (block.GetComponent<MeshFilter>()?.sharedMesh)
                        {
                            meshes.Add(block.GetComponent<MeshFilter>());
                        }

                        for (int step = 0; step < block.trans.childCount; step++)
                        {
                            Transform transCase = block.trans.GetChild(step);
                            if (transCase.GetComponent<MeshFilter>()?.sharedMesh)
                            {
                                meshes.Add(transCase.GetComponent<MeshFilter>());
                            }
                        }

                        if (meshes.Any())
                        {
                            //Debug.Log("Block is too simple, trying meshes...");
                            meshes = meshes.OrderByDescending(x => x.sharedMesh.bounds.size.sqrMagnitude).ToList();
                            Transform transMesh = meshes.First().transform;
                            Mesh mesh = meshes.First().sharedMesh;

                            Vector3[] posCenteredMesh = mesh.vertices;
                            for (int step = 0; step < mesh.vertices.Length; step++)
                            {
                                posCenteredMesh[step] = block.trans.InverseTransformPoint(
                                    transMesh.TransformPoint(posCenteredMesh[step])) - blockCenter;
                            }
                            GetMirrorSuggestion(posCenteredMesh, out angle, true);
                            //GetMirrorSuggestion(posCentered, out angle);
                            meshes.Clear();
                        }
                    }
                    if (angle == MirrorAngle.Z && block.GetComponent<ModuleWing>())
                    {
                        angle = MirrorAngle.Y;
                    }
                }
                RegisterBlockIfNeeded(block.BlockType, angle);
            }
            //Debug.Log("Block " + block.name + " is mirror " + angle);
        }

        private static Vector3 angle45 = new Vector3(1, 0, 1).normalized;
        private static Vector3 angleInv45 = new Vector3(-1, 0, 1).normalized;
        private static Vector3 angle45Z = new Vector3(1, 1, 0).normalized; // upper right
        private static Vector3 angle45InvZ = new Vector3(-1, 1, 0).normalized; // upper left
        private static Vector3 angle45X = new Vector3(0, 1, 1).normalized; // upper right
        private static Vector3 angle45InvX = new Vector3(0, 1, -1).normalized; // upper left
        /// <summary>
        /// returns true if the answer is not vague
        /// Needs optimization that accounts for Sphere and Box Colliders
        /// </summary>
        /// <param name="posCentered"></param>
        /// <param name="angle"></param>
        /// <param name="doComplex"></param>
        /// <returns></returns>
        public static bool GetMirrorSuggestion(Vector3[] posCentered, out MirrorAngle angle, bool doComplex = false)
        {
            int countX = CountOffCenter(posCentered, Vector3.forward);

            int countZ = CountOffCenter(posCentered, Vector3.right);

            int countY = CountOffCenterY(posCentered);

            int count90 = CountOffCenter(posCentered, angle45);

            int count90Inv = CountOffCenter(posCentered, angleInv45);


            int count90Z = CountOffCenterZ(posCentered, angle45Z);

            int count90ZInv = CountOffCenterZ(posCentered, angle45InvZ);

            int count90X = CountOffCenterX(posCentered, angle45X);

            int count90XInv = CountOffCenterX(posCentered, angle45InvX);

            List<KeyValuePair<int, MirrorAngle>> comp = new List<KeyValuePair<int, MirrorAngle>> {
                new KeyValuePair<int, MirrorAngle>(countX, MirrorAngle.X),

                new KeyValuePair<int, MirrorAngle>(countZ, MirrorAngle.Z),

                new KeyValuePair<int, MirrorAngle>(countY, MirrorAngle.Y),

                new KeyValuePair<int, MirrorAngle>(count90, MirrorAngle.YCorner),

                new KeyValuePair<int, MirrorAngle>(count90Inv, MirrorAngle.YCornerInv),

                new KeyValuePair<int, MirrorAngle>(count90Z, MirrorAngle.ZCorner),

                new KeyValuePair<int, MirrorAngle>(count90ZInv, MirrorAngle.ZCornerInv),

                new KeyValuePair<int, MirrorAngle>(count90X, MirrorAngle.XCorner),

                new KeyValuePair<int, MirrorAngle>(count90XInv, MirrorAngle.XCornerInv),
            };
            var first = comp.OrderBy(x => Mathf.Abs(x.Key)).First();
            angle = first.Value;
            if (Mathf.Abs(first.Key) > 2)
            {
                if (!doComplex)
                {
                    DebugArchitech.Info("Block is complex mirror!");
                    angle = MirrorAngle.NeedsPrecise;
                    return true;
                }
                DebugArchitech.Info("Block is vague");
                return false;
            }
            return true;
        }
        public static int CountOffCenter(Vector3[] posCentered, Vector3 planeEdgeForward)
        {
            float lHand = 0;
            float rHand = 0;

            Quaternion otherRot = Quaternion.LookRotation(planeEdgeForward);
            Quaternion offsetMirrorRot = Quaternion.identity;
            offsetMirrorRot.w = otherRot.w;
            offsetMirrorRot.x = otherRot.x;
            offsetMirrorRot.y = -otherRot.y;
            offsetMirrorRot.z = -otherRot.z;

            foreach (var item in posCentered)
            {
                float posPlaneX = (offsetMirrorRot * item).x;
                if (posPlaneX.Approximately(0))
                { // center - do not count
                }
                else if (posPlaneX > 0)
                    rHand += Mathf.Abs(posPlaneX);
                else
                    lHand += Mathf.Abs(posPlaneX);
            }
            return Mathf.RoundToInt((rHand - lHand) * 16f);
        }

        public static int CountOffCenterX(Vector3[] posCentered, Vector3 planeEdgeUp)
        {
            float lHand = 0;
            float rHand = 0;

            Quaternion otherRot = Quaternion.LookRotation(Vector3.Cross(Vector3.right, planeEdgeUp), planeEdgeUp);
            Quaternion offsetMirrorRot = Quaternion.identity;
            offsetMirrorRot.w = otherRot.w;
            offsetMirrorRot.x = otherRot.x;
            offsetMirrorRot.y = -otherRot.y;
            offsetMirrorRot.z = -otherRot.z;

            foreach (var item in posCentered)
            {
                float posPlaneX = (offsetMirrorRot * item).z;
                if (posPlaneX.Approximately(0))
                { // center - do not count
                }
                else if (posPlaneX > 0)
                    rHand += Mathf.Abs(posPlaneX);
                else
                    lHand += Mathf.Abs(posPlaneX);
            }
            return Mathf.RoundToInt((rHand - lHand) * 16f);
        }

        public static int CountOffCenterY(Vector3[] posCentered)
        {
            float lHand = 0;
            float rHand = 0;

            Quaternion otherRot = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            Quaternion offsetMirrorRot = Quaternion.identity;
            offsetMirrorRot.w = otherRot.w;
            offsetMirrorRot.x = otherRot.x;
            offsetMirrorRot.y = -otherRot.y;
            offsetMirrorRot.z = -otherRot.z;

            foreach (var item in posCentered)
            {
                float posPlaneX = (offsetMirrorRot * item).y;
                if (posPlaneX.Approximately(0))
                { // center - do not count
                }
                else if (posPlaneX > 0)
                    rHand += Mathf.Abs(posPlaneX);
                else
                    lHand += Mathf.Abs(posPlaneX);
            }
            return Mathf.RoundToInt((rHand - lHand) * 16f);
        }

        public static int CountOffCenterZ(Vector3[] posCentered, Vector3 planeEdgeUp)
        {
            float lHand = 0;
            float rHand = 0;

            Quaternion otherRot = Quaternion.LookRotation(Vector3.forward, planeEdgeUp);
            Quaternion offsetMirrorRot = Quaternion.identity;
            offsetMirrorRot.w = otherRot.w;
            offsetMirrorRot.x = otherRot.x;
            offsetMirrorRot.y = -otherRot.y;
            offsetMirrorRot.z = -otherRot.z;

            foreach (var item in posCentered)
            {
                float posPlaneX = (offsetMirrorRot * item).x;
                if (posPlaneX.Approximately(0))
                { // center - do not count
                }
                else if (posPlaneX > 0)
                    rHand += Mathf.Abs(posPlaneX);
                else
                    lHand += Mathf.Abs(posPlaneX);
            }
            return Mathf.RoundToInt((rHand - lHand) * 16f);
        }

        public static BlockTypes GetPair(TankBlock TB)
        {
            return GetPair(TB.BlockType);
        }
        public static BlockTypes GetPair(BlockTypes type)
        {
            BlockTypes BT = type;
            if (ModdedMirrored(BT, out BlockTypes mirror))
            {
                return mirror;
            }
            else
            {
                switch (BT)
                {
                    // Edge-Cases
                    case BlockTypes.BF_Hover_Flipper_Small_Left_212:
                        return BlockTypes.BF_Hover_Flipper_Small_Right_212;
                    case BlockTypes.BF_Hover_Flipper_Small_Right_212:
                        return BlockTypes.BF_Hover_Flipper_Small_Left_212;
                    case BlockTypes.VENBlockZ1_312:
                        return BlockTypes.VENBlockZ2_312;
                    case BlockTypes.VENBlockZ2_312:
                        return BlockTypes.VENBlockZ1_312;

                    // Normal
                    default:
                        foreach (var item in Globals.inst.m_BlockPairsList.m_BlockPairs)
                        {
                            if (item.m_Block == BT)
                            {
                                return item.m_PairedBlock;
                            }
                            if (item.m_PairedBlock == BT)
                            {
                                return item.m_Block;
                            }
                        }
                        return BT;
                }
            }
        }

        internal static void DrawDirIndicator(GameObject obj, int num, Vector3 endPosGlobalSpaceOffset, Color color)
        {
            GameObject gO;
            var line = obj.transform.Find("DebugLine " + num);
            if (!(bool)line)
            {
                gO = Instantiate(new GameObject("DebugLine " + num), obj.transform, false);
            }
            else
                gO = line.gameObject;

            var lr = gO.GetComponent<LineRenderer>();
            if (!(bool)lr)
            {
                lr = gO.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.positionCount = 2;
                lr.startWidth = 0.5f;
            }
            lr.startColor = color;
            lr.endColor = color;
            Vector3 pos = obj.transform.position;
            Vector3[] vecs = new Vector3[2] { pos, endPosGlobalSpaceOffset + pos };
            lr.SetPositions(vecs);
            Destroy(gO, Time.deltaTime);
        }

        public static void PreGrabBlock(TankBlock TB)
        {
            if (TB.GetComponent<ColliderSwapper>())
                TB.GetComponent<ColliderSwapper>().EnableCollision(false);
            if (TB.rbody)
                TB.rbody.useGravity = false;
        }
        public static void StopMovement(TankBlock TB)
        {
            if (TB.rbody)
            {
                TB.rbody.useGravity = true;
                TB.rbody.velocity = Vector3.zero;
                TB.rbody.angularVelocity = Vector3.zero;
            }
        }
        public static void PostGrabBlock(TankBlock TB)
        {
            try
            {
                if (TB.GetComponent<ColliderSwapper>())
                    TB.GetComponent<ColliderSwapper>().EnableCollision(true); 
                StopMovement(TB);
            }
            catch
            {
                DebugArchitech.LogError("BuildUtil:  Block was expected but it was null!  Was it destroyed in tranzit!?");
            }
        }

        public static Quaternion InvTransformRot(Quaternion rot, Transform trans)
        {
            return Quaternion.Inverse(trans.rotation) * rot;
        }


        public static Quaternion InvTransformRot(Transform transRotateGet, Transform trans)
        {
            return Quaternion.Inverse(trans.rotation) * transRotateGet.rotation;
        }
        public static Quaternion InvTransformRot(Transform transRotateGet, Tank tank)
        {
            return Quaternion.Inverse(tank.transform.rotation) * transRotateGet.rotation;
        }
        public static Quaternion TransformRot(Quaternion otherRot, Tank tank)
        {
            return tank.trans.rotation * otherRot;
        }


        public static void GrabEntireTechFromBlock()
        {
            if (!ManPointer.inst.DraggingItem?.block)
                return;
            TankBlock pointerBlock = ManPointer.inst.DraggingItem.block;
            if (!pointerBlock || !pointerBlock.tank)
                return;
            Tank tank = pointerBlock.tank;
            if (tank == Singleton.playerTank || (!ManGameMode.inst.IsCurrent<ModeMisc>() 
                && tank.Vision.GetFirstVisibleTechIsEnemy(ManPlayer.inst.PlayerTeam)))
                return; // DO NOT ALLOW THE PLAYER TO GRAB THEIR OWN TECH and enemies in non-creative block it.
            BusyGrabbingTechs = true;
            lastDraggedName = tank.name;
            if (!PointerBatchCache)
                PointerBatchCache = new BlockBatch(pointerBlock);
            List<TankBlock> cache = DitchAllBlocks(tank, false);
            PointerBatchCache.BatchCenterOn(pointerBlock, cache);
            cachePointerHeld = pointerBlock;
            PreGrabBlock(cachePointerHeld);
        }

        public static void TryCreateTechFromPointerBlocks()
        {
            TankBlock pointerBlock = cachePointerHeld;
            ManPointer.inst.ReleaseDraggingItem(false);
            if (!PointerBatchCache)
                return;
            if (!IsGrabbingTechsActive)
            {
                DebugArchitech.Log("Architech: TryCreateTechFromPointerBlocks - Tech holding button delberately released before left mouse");
                PointerBatchCache.DropAllButRoot();
                PointerBatchCache = null;
                cachePointerHeld = null;
                BusyGrabbingTechs = false;
                return;
            }
            if (!pointerBlock)
            {
                DebugArchitech.Log("Architech: TryCreateTechFromPointerBlocks - pointerBlock IS NULL somehow");
                PointerBatchCache.DropAllButRoot();
                PointerBatchCache = null;
                cachePointerHeld = null;
                BusyGrabbingTechs = false;
                return;
            }
            if (pointerBlock.tank)
            {
                TryReattachAll(pointerBlock.tank, pointerBlock, PointerBatchCache);
                PointerBatchCache = null;
            }
            else if (currentTank)
            {
                PointerBatchCache.InsureAboveGround();
                if (ManTechBuilder.inst.IsBlockHeldInPosition(pointerBlock))
                {
                    TryReattachAll(currentTank, pointerBlock, PointerBatchCache);
                    PointerBatchCache = null;
                }
                else if (pointerBlock && SetToAnchor(ref pointerBlock, ref PointerBatchCache))
                {
                    DebugArchitech.Log("New root " + pointerBlock.name);
                    inst.CarryBlockBatchesNoMirror(pointerBlock);
                    PointerBatchCache.DropAllButRoot();
                    Tank tank = pointerBlock.GetComponentInParent<Tank>();
                    if (!tank)
                        tank = ManSpawn.inst.WrapSingleBlock(null, pointerBlock, ManPlayer.inst.PlayerTeam,
                            lastDraggedName.NullOrEmpty() ? "Grabbed Tech" : lastDraggedName);
                    TryReattachAll(tank, pointerBlock, PointerBatchCache);
                    tank.FixupAnchors(true);
                    PointerBatchCache = null;
                    DebugArchitech.Log("Made Tech from grabbed");
                }
                else if (pointerBlock && SetToCab(ref pointerBlock, ref PointerBatchCache))
                {
                    DebugArchitech.Log("New root " + pointerBlock.name);
                    inst.CarryBlockBatchesNoMirror(pointerBlock);
                    PointerBatchCache.DropAllButRoot();
                    Tank tank = pointerBlock.GetComponentInParent<Tank>();
                    if (!tank)
                        tank = ManSpawn.inst.WrapSingleBlock(null, pointerBlock, ManPlayer.inst.PlayerTeam,
                            lastDraggedName.NullOrEmpty() ? "Grabbed Tech" : lastDraggedName);
                    TryReattachAll(tank, pointerBlock, PointerBatchCache);
                    PointerBatchCache = null;
                    if (tank.rbody)
                    {
                        tank.rbody.velocity = lastDragVelo;
                    }
                    DebugArchitech.Log("Made Tech from grabbed");
                }
                else
                {
                    DebugArchitech.LogError("Failed to make Tech from grabbed (currentTank)");
                    PointerBatchCache.DropAllButRoot();
                    PointerBatchCache = null;
                }
            }
            else
            {
                PointerBatchCache.InsureAboveGround();
                if (pointerBlock && SetToAnchor(ref pointerBlock, ref PointerBatchCache))
                {
                    DebugArchitech.Log("New root " + pointerBlock.name);
                    inst.CarryBlockBatchesNoMirror(pointerBlock);
                    PointerBatchCache.DropAllButRoot();
                    Tank tank = pointerBlock.GetComponentInParent<Tank>();
                    if (!tank)
                        tank = ManSpawn.inst.WrapSingleBlock(null, pointerBlock, ManPlayer.inst.PlayerTeam,
                            lastDraggedName.NullOrEmpty() ? "Grabbed Tech" : lastDraggedName);
                    TryReattachAll(tank, pointerBlock, PointerBatchCache);
                    tank.FixupAnchors(true);
                    PointerBatchCache = null;
                    DebugArchitech.Log("Made Tech from grabbed");
                }
                else if (pointerBlock && SetToCab(ref pointerBlock, ref PointerBatchCache))
                {
                    DebugArchitech.Log("New root " + pointerBlock.name);
                    inst.CarryBlockBatchesNoMirror(pointerBlock);
                    PointerBatchCache.DropAllButRoot();
                    Tank tank = pointerBlock.GetComponentInParent<Tank>();
                    if (!tank)
                        tank = ManSpawn.inst.WrapSingleBlock(null, pointerBlock, ManPlayer.inst.PlayerTeam,
                            lastDraggedName.NullOrEmpty() ? "Grabbed Tech" : lastDraggedName);
                    TryReattachAll(tank, pointerBlock, PointerBatchCache);
                    PointerBatchCache = null;
                    if (tank.rbody)
                    {
                        tank.rbody.velocity = lastDragVelo;
                    }
                    DebugArchitech.Log("Made Tech from grabbed");
                }
                else
                {
                    DebugArchitech.LogError("Failed to make Tech from grabbed");
                    PointerBatchCache.DropAllButRoot();
                    PointerBatchCache = null;
                }
            }
            cachePointerHeld = null;
            BusyGrabbingTechs = false;
        }


        public static void DetachCabSectionsFromGrabbed()
        {
            if (!ManPointer.inst.DraggingItem?.block)
                return;
            TankBlock pointerBlock = ManPointer.inst.DraggingItem.block;
            if (!pointerBlock || !pointerBlock.tank)
                return;
            Tank tank = pointerBlock.tank;
            List<TankBlock> cache = DetachAllCabsButRoot(tank, false);

            TankBlock mirrored = null;
            if (IsMirroring)
            {
                mirrored = MirroredFetch(pointerBlock);
            }
            tank.blockman.Detach(pointerBlock, false, false, true);
            if (mirrored)
            {
                tank.blockman.Detach(mirrored, false, false, true);
                if (mirrored && !mirrored.IsAttached)
                    anonMirrorHeld = mirrored;
            }

            cache = TryReattachAll(tank, cache);
            if (!PointerBatchCache)
                PointerBatchCache = new BlockBatch(tank.blockman.GetRootBlock());
            foreach (var item in cache)
            {
                Rigidbody rbody = item.GetComponent<Rigidbody>();
                if (rbody)
                {
                    rbody.velocity = Vector3.zero;
                    rbody.inertiaTensorRotation = Quaternion.identity;
                }
                BlockCache BC;
                if (IsOnMirrorSide(tank, item))
                {
                    if (anonMirrorHeld)
                    {
                        BC = CenterOn(anonMirrorHeld, item);
                        MirrorBatchCache.batch.Insert(0, BC);
                    }
                }
                else
                {
                    BC = CenterOn(pointerBlock, item);
                    PointerBatchCache.batch.Insert(0, BC);
                }
            }
        }
        public static List<TankBlock> DetachAllCabsButRoot(Tank tank, bool detachLoose = true)
        {
            if (ManNetwork.IsNetworked || tank == null || tank.blockman.blockCount == 0)
                return new List<TankBlock>();
            List<TankBlock> toDetach = new List<TankBlock>();
            foreach (var item in tank.blockman.IterateBlocks())
            {
                if (tank.blockman.IsRootBlock(item))
                    continue;
                var anchor = item.GetComponent<ModuleAnchor>();
                if (item.GetComponent<ModuleTechController>() || (anchor && anchor.IsAnchored))
                {
                    toDetach.Add(item);
                }
            }
            foreach (var item in toDetach)
            {
                if (item.IsAttached)
                    tank.blockman.Detach(item, false, false, detachLoose);
            }
            return toDetach;
        }
        public static List<BlockCache> TryReattachAll(Tank tank, TankBlock toCenterOn, List<BlockCache> toAttach)
        {
            if (tank == null)
            {
                DebugArchitech.LogError("Tank was NULL");
                return toAttach;
            }
            if (tank.blockman.blockCount == 0)
            {
                DebugArchitech.LogError("Tank had no blocks");
                return toAttach;
            }
            if (ManNetwork.IsNetworked)
                return toAttach;
            TankBlock root = toCenterOn;
            bool Attaching;
            do
            {
                Attaching = false;
                int attachNeeded = toAttach.Count;
                for (int attachPos = 0; attachPos < attachNeeded;)
                {
                    TankBlock item = toAttach[attachPos].inst;
                    toAttach[attachPos].HoldInRelation(root);
                    if (item.rbody)
                    {
                        item.rbody.velocity = Vector3.zero;
                        item.rbody.angularVelocity = Vector3.zero;
                    }
                    if (item.tank)
                        DebugArchitech.LogError("Block " + item.name + " was already attached");
                    if (TryAttachLoose(tank, item, false))
                    {
                        //Debug.Log("Attached " + item.name + " to " + tank.name);
                        toAttach.RemoveAt(attachPos);
                        Attaching = true;
                        break;
                    }
                    else
                        attachPos++;
                }
            } while (Attaching);
            return toAttach;
        }
        public static bool AttachAll(Tank tank, TankBlock toCenterOn, BlockBatch BB)
        {
            if (tank == null)
            {
                DebugArchitech.LogError("Tank was NULL");
                return false;
            }
            if (tank.blockman.blockCount == 0)
            {
                DebugArchitech.LogError("Tank had no blocks");
                return false;
            }
            if (ManNetwork.IsNetworked || !BB)
                return false;
            List<BlockCache> toAttach = BB;
            TankBlock root = toCenterOn;
            bool Attaching;
            bool hasAttached = false;
            do
            {
                Attaching = false;
                int attachNeeded = toAttach.Count;
                for (int attachPos = 0; attachPos < attachNeeded;)
                {
                    TankBlock item = toAttach[attachPos].inst;
                    toAttach[attachPos].HoldInRelation(root);
                    //if (item.tank)
                    //    Debug.LogError("Block " + item.name + " was already attached");
                    if (TryAttachLoose(tank, item))
                    {
                        DebugArchitech.Log("Attached " + item.name + " to " + tank.name);
                        toAttach.RemoveAt(attachPos);
                        Attaching = true;
                        hasAttached = true;
                        break;
                    }
                    else
                        attachPos++;
                }
            } while (Attaching);
            return hasAttached;
        }
        public static List<TankBlock> TryReattachAll(Tank tank, List<TankBlock> toAttach)
        {
            if (tank == null)
            {
                DebugArchitech.LogError("Tank was NULL");
                return toAttach;
            }
            if (tank.blockman.blockCount == 0)
            {
                DebugArchitech.LogError("Tank had no blocks");
                return toAttach;
            }
            if (ManNetwork.IsNetworked)
                return toAttach; 
            TankBlock root = tank.blockman.IterateBlocks().FirstOrDefault();
            tank.blockman.SetRootBlock(root);
            Vector3 initialPos = tank.rootBlockTrans.position;
            Quaternion initialRot = tank.trans.rotation;
            bool Attaching;
            do
            {
                Attaching = false;
                int attachNeeded = toAttach.Count;
                for (int attachPos = 0; attachPos < attachNeeded;)
                {
                    TankBlock item = toAttach[attachPos];
                    if (item.tank)
                        DebugArchitech.LogError("Block " + item.name + " was already attached");
                    if (TryAttachLoose(tank, item))
                    {
                        DebugArchitech.Log("Attached " + item.name + " to " + tank.name);
                        toAttach.RemoveAt(attachPos);
                        Attaching = true;
                        break;
                    }
                    else
                        attachPos++;
                }
                tank.trans.rotation = initialRot;
                tank.trans.position += initialPos - tank.rootBlockTrans.position;
            } while (Attaching);
            return toAttach;
        }
        public static bool IsOnMirrorSide(Tank tank, TankBlock toAttach)
        {
            if (IsMirroring)
            {   // handle Batch blocks
                Vector3 blockLocalPos = tank.rootBlockTrans.InverseTransformPoint(toAttach.trans.position);
                Vector3 MirrorHeldLocalPos = tank.rootBlockTrans.InverseTransformPoint(MirrorHeld.trans.position);

                if (MirrorHeldLocalPos.x > 0)
                {
                    return blockLocalPos.x > 0;
                }
                else
                {
                    return blockLocalPos.x <= 0;
                }
            }
            else
            {   // Player Side
                return false;
            }
        }

        public static bool SetToCab(ref TankBlock mainHeld, ref BlockBatch toSearch)
        {
            if (!toSearch)
                return false;
            if (mainHeld.GetComponent<ModuleTechController>())
                return true;
            foreach (var item in toSearch)
            {
                if (!item.inst)
                    continue;
                if (item.inst.GetComponent<ModuleTechController>())
                {
                    mainHeld = toSearch.BatchReCenterOn(item.inst);
                    return true;
                }
            }
            return false;
        }

        public static bool SetToAnchor(ref TankBlock mainHeld, ref BlockBatch toSearch)
        {
            if (!toSearch)
            {
                return false;
            }
            float height = float.MaxValue;
            TankBlock best = null;
            var anchor = mainHeld.GetComponent<ModuleAnchor>();
            if (anchor && CanAnchor(anchor))
            {
                DebugArchitech.Info("Compare " + anchor.Anchor.GroundPointInitial.y + " vs " + height);
                if (anchor.Anchor.GroundPointInitial.y < height)
                {
                    height = anchor.Anchor.GroundPointInitial.y;
                    best = mainHeld;
                }
            }
            foreach (var item in toSearch)
            {
                if (!item.inst)
                    continue;
                anchor = item.inst.GetComponent<ModuleAnchor>();
                if (anchor && CanAnchor(anchor))
                {
                    DebugArchitech.Info("Compare " + anchor.Anchor.GroundPointInitial.y + " vs " + height);
                    if (anchor.Anchor.GroundPointInitial.y < height)
                    {
                        height = anchor.Anchor.GroundPointInitial.y;
                        best = item.inst;
                    }
                }
            }
            if (best)
            {
                mainHeld = toSearch.BatchReCenterOn(best);
                DebugArchitech.Info("Best " + mainHeld.GetComponent<ModuleAnchor>().Anchor.GroundPointInitial.y);
            }
            return best;
        }
        public static bool CanAnchor(ModuleAnchor anchor)
        {
            float height = ManWorld.inst.ProjectToGround(anchor.Anchor.GroundPointInitial).y - anchor.Anchor.GroundPointInitial.y;
            DebugArchitech.Info("Compare " + (height - anchor.Anchor.m_SnapToleranceDown) + " | " 
                + (height + anchor.Anchor.m_SnapToleranceUp) + " vs " + ManWorld.inst.ProjectToGround(anchor.Anchor.GroundPointInitial).y);
            return height <= anchor.Anchor.m_SnapToleranceUp
                && height >= -anchor.Anchor.m_SnapToleranceDown;
        }

        public static BlockCache CenterOn(TankBlock mainHeld, TankBlock toSet)
        {
            return new BlockCache(toSet, toSet.BlockType,
                mainHeld.trans.InverseTransformPoint(toSet.trans.position),
                SetCorrectRotation(InvTransformRot(toSet.trans, mainHeld.trans)));
        }



        public void SetNextPlacedRootCab(Tank tank)
        {
            if (!tank || tank.blockman.blockCount == 0 || !CanHighlight)
                return;
            if (tank.blockman.IterateBlockComponents<ModuleTechController>().Count() < 2)
                return;
            if (!tank.rootBlockTrans.GetComponent<ModuleTechController>())
            {
                foreach (var item in tank.blockman.IterateBlockComponents<ModuleTechController>())
                {
                    if (item.HandlesPlayerInput)
                    {
                        tank.blockman.SetRootBlock(item.block);
                        HighlightBlock(item.block);
                        return;
                    }
                }
            }
            else
            {
                TankBlock rootCurrent = tank.blockman.GetRootBlock();
                bool isNext = false;
                foreach (var item in tank.blockman.IterateBlockComponents<ModuleTechController>())
                {
                    if (item.block == rootCurrent)
                    {
                        isNext = true;
                    }
                    else if (isNext && item.HandlesPlayerInput)
                    {
                        tank.blockman.SetRootBlock(item.block);
                        HighlightBlock(item.block);
                        return;
                    }
                }
                foreach (var item in tank.blockman.IterateBlockComponents<ModuleTechController>())
                {
                    if (item.HandlesPlayerInput)
                    {
                        tank.blockman.SetRootBlock(item.block);
                        HighlightBlock(item.block);
                        return;
                    }
                }
            }
        }
        private bool CanHighlight => !delayedHideBlock;
        public void HighlightBlock(TankBlock TB)
        {
            if (delayedHideBlock)
                return;
            TB.visible.EnableOutlineGlow(true, cakeslice.Outline.OutlineEnableReason.ScriptHighlight);

            // Some delay
            delayedHideBlock = TB;
            Invoke("UnHighlightBlock", 2f);
        }
        private TankBlock delayedHideBlock;
        private void UnHighlightBlock()
        {
            if (!delayedHideBlock)
                return;
            delayedHideBlock.visible.EnableOutlineGlow(false, cakeslice.Outline.OutlineEnableReason.ScriptHighlight);
            delayedHideBlock = null;
        }

        public void RebuildTechCabForwards(Tank tank)
        {
            if (ManNetwork.IsNetworked || tank == null || tank.blockman.blockCount == 0
                || tank.Team != ManPlayer.inst.PlayerTeam || !CanHighlight)
                return;
            TankBlock root = tank.blockman.GetRootBlock();
            Vector3 pos = tank.visible.centrePosition;
            Quaternion rot = tank.trans.rotation;
            List<BlockCache> mem = TechToForwardsCache(tank, out _);
            List<TankBlock> blocks = DitchAllBlocks(tank, true);
            TechUtils.TurboconstructExt(tank, mem, blocks, false);
            tank.blockman.SetRootBlock(root);
            root = tank.blockman.GetRootBlock();
            tank.blockman.SetRootBlock(root);
            tank.visible.Teleport(pos, rot, false, false);
            HighlightBlock(root);
        }


        public static TankBlock FindProperRootBlockExternal(List<TankBlock> ToSearch)
        {
            bool IsAnchoredAnchorPresent = false;
            float close = 128 * 128;
            TankBlock newRoot = ToSearch.First();
            foreach (TankBlock bloc in ToSearch)
            {
                Vector3 blockPos = bloc.CalcFirstFilledCellLocalPos();
                float sqrMag = blockPos.sqrMagnitude;
                if (bloc.GetComponent<ModuleAnchor>() && bloc.GetComponent<ModuleAnchor>().IsAnchored)
                {   // If there's an anchored anchor, then we base the root off of that
                    //  It's probably a base
                    IsAnchoredAnchorPresent = true;
                    break;
                }
                if (sqrMag < close && (bloc.GetComponent<ModuleTechController>() || bloc.GetComponent<ModuleAIBot>()))
                {
                    close = sqrMag;
                    newRoot = bloc;
                }
            }
            if (IsAnchoredAnchorPresent)
            {
                close = 128 * 128;
                foreach (TankBlock bloc in ToSearch)
                {
                    Vector3 blockPos = bloc.CalcFirstFilledCellLocalPos();
                    float sqrMag = blockPos.sqrMagnitude;
                    if (sqrMag < close && bloc.GetComponent<ModuleAnchor>() && bloc.GetComponent<ModuleAnchor>().IsAnchored)
                    {
                        close = sqrMag;
                        newRoot = bloc;
                    }
                }
            }
            return newRoot;
        }

        /// <summary>
        /// Mandatory for techs that have plans to be built over time by the building AI.
        /// Since the first block placed ultimately determines the base rotation of the Tech
        ///  (Arrow shown on Radar/minimap) we must be ABSOLUTELY SURE to build teh Tech in relation
        ///   to that first block.
        ///   Any alteration on the first block's rotation will have severe consequences in the long run.
        ///   
        /// Split techs on the other hand are mostly free from this issue.
        /// </summary>
        /// <param name="ToSearch">The list of blocks to find the new foot in</param>
        /// <returns></returns>
        public static List<BlockCache> TechToForwardsCache(Tank tank, out TankBlock rootBlock)
        {
            // This resaves the whole tech cab-forwards regardless of original rotation
            //   It's because any solutions that involve the cab in a funny direction will demand unholy workarounds.
            //   I seriously don't know why the devs didn't try it this way, perhaps due to lag reasons.
            //   or the blocks that don't allow upright placement (just detach those lmao)
            List<BlockCache> output = new List<BlockCache>();
            List<TankBlock> ToSave = tank.blockman.IterateBlocks().ToList();
            Vector3 coreOffset;
            Quaternion coreRot;
            rootBlock = tank.blockman.GetRootBlock();
            if (!rootBlock || !rootBlock.GetComponent<ModuleTechController>() || !rootBlock.GetComponent<ModuleTechController>().HandlesPlayerInput)
            {
                rootBlock = FindProperRootBlockExternal(ToSave);
                if (rootBlock != null)
                {
                    if (rootBlock != ToSave.First())
                    {
                        ToSave.Remove(rootBlock);
                        ToSave.Insert(0, rootBlock);
                    }
                    coreOffset = rootBlock.trans.localPosition;
                    coreRot = rootBlock.trans.localRotation;
                    tank.blockman.SetRootBlock(rootBlock);
                }
                else
                {
                    coreOffset = Vector3.zero;
                    coreRot = new OrthoRotation(OrthoRotation.r.u000);
                }
            }
            else
            {
                if (rootBlock != ToSave.First())
                {
                    ToSave.Remove(rootBlock);
                    ToSave.Insert(0, rootBlock);
                }
                coreOffset = rootBlock.trans.localPosition;
                coreRot = rootBlock.trans.localRotation;
            }
            foreach (TankBlock bloc in ToSave)
            {
                if (!Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(bloc.BlockType))
                    continue;
                Quaternion deltaRot = Quaternion.Inverse(coreRot);
                BlockCache mem = new BlockCache
                {
                    t = bloc.BlockType,
                    p = deltaRot * (bloc.trans.localPosition - coreOffset)
                };
                // get rid of floating point errors
                mem.TidyUp();
                //Get the rotation
                mem.r = SetCorrectRotation(bloc.trans.localRotation, deltaRot);
                if (bloc.BlockType == BlockTypes.EXP_Cannon_Repulsor_444 && mem.r * Vector3.up != Vector3.up)
                {   // block cannot be saved - illegal rotation.
                    DebugArchitech.Log("TACtical_AI:  DesignMemory - " + tank.name + ": could not save " + bloc.name + " in blueprint due to illegal rotation.");
                    continue;
                }
                output.Add(mem);
            }
            DebugArchitech.Info("TACtical_AI:  DesignMemory - Saved " + tank.name + " to memory format");

            return output;
        }


        public static List<TankBlock> DitchAllBlocks(Tank tank, bool addToThisFrameLater)
        {
            try
            {
                List<TankBlock> blockCache = tank.blockman.IterateBlocks().ToList();
                tank.blockman.Disintegrate(false, addToThisFrameLater);
                return blockCache;
            }
            catch { }
            return new List<TankBlock>();
        }

        public static OrthoRotation SetCorrectRotation(Quaternion blockRot, Quaternion changeRot)
        {
            Quaternion qRot2 = Quaternion.identity;
            Vector3 endRotF = blockRot * Vector3.forward;
            Vector3 endRotU = blockRot * Vector3.up;
            Vector3 foA = (changeRot * endRotF).normalized;
            Vector3 upA = (changeRot * endRotU).normalized;
            qRot2.SetLookRotation(foA, upA);
            OrthoRotation rot = new OrthoRotation(qRot2);
            if (rot != qRot2)
            {
                bool worked = false;
                for (int step = 0; step < OrthoRotation.NumDistinctRotations; step++)
                {
                    OrthoRotation rotT = new OrthoRotation(OrthoRotation.AllRotations[step]);
                    bool isForeMatch = foA.Approximately(rotT * Vector3.forward, 0.25f);
                    bool isUpMatch = upA.Approximately(rotT * Vector3.up, 0.25f);
                    if (isForeMatch && isUpMatch)
                    {
                        rot = rotT;
                        worked = true;
                        break;
                    }
                }
                if (!worked)
                {
                    DebugArchitech.Log("Architech: SetCorrectRotation - Matching failed - OrthoRotation is missing edge case " + foA + " | " + upA);
                }
            }
            return rot;
        }

        public static OrthoRotation SetCorrectRotation(Quaternion changeRot)
        {
            Vector3 foA = (changeRot * Vector3.forward).normalized;
            Vector3 upA = (changeRot * Vector3.up).normalized;
            //Debug.Log("Architech: SetCorrectRotation - Matching test " + foA + " | " + upA);
            Quaternion qRot2 = Quaternion.LookRotation(foA, upA);
            OrthoRotation rot = new OrthoRotation(qRot2);
            if (rot != qRot2)
            {
                bool worked = false;
                for (int step = 0; step < OrthoRotation.NumDistinctRotations; step++)
                {
                    OrthoRotation rotT = new OrthoRotation(OrthoRotation.AllRotations[step]);
                    bool isForeMatch = (rotT * Vector3.forward).Approximately(foA, 0.35f);
                    bool isUpMatch = (rotT * Vector3.up).Approximately(upA, 0.35f);
                    if (isForeMatch && isUpMatch)
                    {
                        rot = rotT;
                        worked = true;
                        break;
                    }
                }
                if (!worked)
                {
                    DebugArchitech.Log("Architech: SetCorrectRotation - Matching failed - OrthoRotation is missing edge case " + foA + " | " + upA);
                }
            }
            return rot;
        }

        /// <summary>
        /// Gets a loadout of prefabbed 
        /// </summary>
        public static bool HandleEdgeCases(TankBlock block, out MirrorAngle angle)
        {
            switch (block.BlockType)
            {
                case BlockTypes.HE_Missile_Pod_28_111:
                    angle = MirrorAngle.SeekerMissile;
                    return true;
                case BlockTypes.VENLaserTurret_111:
                    angle = MirrorAngle.X;
                    return true;
                default:
                    angle = MirrorAngle.None;
                    return false;
            }
        }

        public static bool ModdedMirrored(BlockTypes type, out BlockTypes mirror)
        {
            if (ManMods.inst.IsModdedBlock(type))
            {
                int id = (int)type;
                string name = ManMods.inst.FindBlockName(id);
                string nameFiltered;
                string name2;
                if (name != null)
                {
                    //StringLookup.GetItemName(new ItemTypeInfo(ObjectTypes.Block, id));
                    if (name.Contains("Left"))
                    {
                        nameFiltered = name.Replace("Left", "");
                        name2 = ManMods.inst.FindBlockName(id - 1);
                        if (name2.Contains("Right") && name2.Replace("Right", "").Contains(nameFiltered))
                        {
                            mirror = (BlockTypes)(id - 1);
                            return true;
                        }
                        name2 = ManMods.inst.FindBlockName(id + 1);
                        if (name2.Contains("Right") && name2.Replace("Right", "").Contains(nameFiltered))
                        {
                            mirror = (BlockTypes)(id + 1);
                            return true;
                        }
                    }
                    else if (name.Contains("Right"))
                    {
                        nameFiltered = name.Replace("Right", "");
                        name2 = ManMods.inst.FindBlockName(id - 1);
                        if (name2.Contains("Left") && name2.Replace("Left", "").Contains(nameFiltered))
                        {
                            mirror = (BlockTypes)(id - 1);
                            return true;
                        }
                        name2 = ManMods.inst.FindBlockName(id + 1);
                        if (name2.Contains("Left") && name2.Replace("Left", "").Contains(nameFiltered))
                        {
                            mirror = (BlockTypes)(id + 1);
                            return true;
                        }
                    }
                    else if (name.EndsWith("L"))
                    {
                        nameFiltered = name.Substring(0, name.Length -1);
                        name2 = ManMods.inst.FindBlockName(id - 1);
                        if (name2.EndsWith("R") && name2.Contains(nameFiltered))
                        {
                            mirror = (BlockTypes)(id - 1);
                            return true;
                        }
                        name2 = ManMods.inst.FindBlockName(id + 1);
                        if (name2.EndsWith("R") && name2.Contains(nameFiltered))
                        {
                            mirror = (BlockTypes)(id + 1);
                            return true;
                        }
                    }
                    else if (name.EndsWith("R"))
                    {
                        nameFiltered = name.Substring(0, name.Length - 1);
                        name2 = ManMods.inst.FindBlockName(id - 1);
                        if (name2.EndsWith("L") && name2.Contains(nameFiltered))
                        {
                            mirror = (BlockTypes)(id - 1);
                            return true;
                        }
                        name2 = ManMods.inst.FindBlockName(id + 1);
                        if (name2.EndsWith("L") && name2.Contains(nameFiltered))
                        {
                            mirror = (BlockTypes)(id + 1);
                            return true;
                        }
                    }
                }
            }
            mirror = BlockTypes.GSOCockpit_111;
            return false;
        }

        public static bool HasNeededInInventory(Tank currentTank, List<BlockCache> BB)
        {
            return HasNeededInInventory(currentTank, BB.ConvertAll(x => x.t));
        }
        public static bool HasNeededInInventory(Tank currentTank, List<BlockTypes> BT)
        {
            Dictionary<BlockTypes, int> counts = new Dictionary<BlockTypes, int>();
            foreach (var item in BT)
            {
                if (counts.TryGetValue(item, out int val))
                    counts[item] = val + 1;
                else
                    counts[item] = 1;
            }
            foreach (var item in counts)
            {
                if (!TechUtils.IsBlockAvailInInventory(currentTank, item.Key, item.Value))
                    return false;
            }
            return true;
        }

        public static void TakeNeededFromInventory(Tank currentTank, List<BlockTypes> BT)
        {
            Dictionary<BlockTypes, int> counts = new Dictionary<BlockTypes, int>();
            foreach (var item in BT)
            {
                if (counts.TryGetValue(item, out int val))
                    counts[item] = val + 1;
                else
                    counts[item] = 1;
            }
            foreach (var item in counts)
            {
                TechUtils.IsBlockAvailInInventory(currentTank, item.Key, item.Value, true);
            }
        }


        internal enum MirrorAngle
        {
            None,
            X,
            Y,
            Z,
            XCorner,
            XCornerInv,
            YCorner,
            YCornerInv,
            ZCorner,
            ZCornerInv,
            SeekerMissile,
            NeedsPrecise,
        }

    }


}
