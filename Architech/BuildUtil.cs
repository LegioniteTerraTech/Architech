using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;



namespace Architech
{

    /// <summary>
    /// Allows the player far more control over Tech construction.  Work in progress.
    /// Modified version of TAC_AI's AIERepair.DesignMemory's rebuilding functions.
    /// </summary>
    internal class BuildUtil : MonoBehaviour
    {
        static FieldInfo highL = typeof(ManPointer).GetField("m_HighlightPrefab", BindingFlags.NonPublic | BindingFlags.Instance);

        static FieldInfo paintBool = typeof(ManPointer).GetField("m_PaintingSkin", BindingFlags.NonPublic | BindingFlags.Instance);

        static FieldInfo existingCursors = typeof(MousePointer).GetField("m_CursorDataSets", BindingFlags.NonPublic | BindingFlags.Instance);




        private static bool DebugBlockRotations = false;


        internal static BuildUtil inst;
        private static ObjectHighlight OH;
        internal static Tank currentTank;

        internal static bool IsPaintSkinsActive => ManPointer.inst.BuildMode == ManPointer.BuildingMode.PaintSkin;
        internal static bool IsPaintingSkin => IsPaintSkinsActive && Input.GetMouseButton(0);
        internal static bool IsGrabbingTechsActive => Input.GetKey(KeyCode.Backspace);
        internal static bool IsHoveringGrabbableTech => ManPointer.inst.targetVisible?.block?.tank ? ManPointer.inst.targetVisible.block.tank != Singleton.playerTank : false;

        internal static bool IsBatchActive = false;
        internal static bool ToggleBatchMode => Input.GetKey(KeyCode.RightShift);
        internal static bool ToggleMirrorMode => Input.GetKey(KeyCode.CapsLock);
        internal static bool IsHoveringMirrored => lastHovered;
        internal static bool IsHoldingMirrored => MirrorHeld;
        internal bool IsHoldingBatch => PointerBatchCache.Count > 0 || MirrorBatchCache.Count > 0;
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

        private static bool PaintBlocks => ManPointer.inst.BuildMode == ManPointer.BuildingMode.PaintBlock && KickStart.IsIngame && !BusyGrabbingTechs;
        internal static Transform rBlock => currentTank.rootBlockTrans;
        internal Vector3 tankCenter => rBlock.GetComponent<TankBlock>() ?
            SnapToMirrorAxi(rBlock.localPosition +
                (rBlock.localRotation * rBlock.GetComponent<TankBlock>().BlockCellBounds.center))
            : currentTank.blockBounds.center;

        private static string lastDraggedName;
        private static Vector3 lastDraggedPosition = Vector3.zero;
        private static TankBlock cachePointerHeld;
        private static TankBlock anonMirrorHeld;
        private static TankBlock MirrorHeld;
        private static bool InventoryBlock = false;
        private static TankBlock lastHovered;
        private static TankBlock lastAttached;
        private static TankBlock lastDetached;
        private static BlockTypes lastType = BlockTypes.GSOAIController_111;
        private static BlockTypes pairType = BlockTypes.GSOAIController_111;
        private static bool attachFrameDelay = false;
        internal static bool lastFramePlacementInvalid = false;

        private static MirrorAngle cachedMirrorAngle = MirrorAngle.None;

        private static List<KeyValuePair<TankBlock, BlockCache>> PointerBatchCache = new List<KeyValuePair<TankBlock, BlockCache>>();
        private static List<KeyValuePair<TankBlock, BlockCache>> MirrorBatchCache = new List<KeyValuePair<TankBlock, BlockCache>>();
        private static List<Texture2D> cursorCache = new List<Texture2D>();

        public static void Init()
        {
            if (inst)
                return;
            inst = new GameObject("BuildUtil").AddComponent<BuildUtil>();
            ManTechs.inst.PlayerTankChangedEvent.Subscribe(OnPlayerTechChanged);
            ManTechs.inst.TankBlockAttachedEvent.Subscribe(OnBlockPlaced);
            ManTechs.inst.TankBlockDetachedEvent.Subscribe(OnBlockRemoved);
            OH = Instantiate((ObjectHighlight)highL.GetValue(ManPointer.inst));
            OH.SetHighlightType(ManPointer.HighlightVariation.Normal);
            AddNewCursors();
        }
        public static void DeInit()
        {
            if (!inst)
                return;
            ManTechs.inst.TankBlockDetachedEvent.Unsubscribe(OnBlockRemoved);
            ManTechs.inst.TankBlockAttachedEvent.Unsubscribe(OnBlockPlaced);
            ManTechs.inst.PlayerTankChangedEvent.Unsubscribe(OnPlayerTechChanged);
            Destroy(OH.gameObject);
            OH = null;
            Destroy(inst.gameObject);
            inst = null;
        }



        /*
            Default,
            OverGrabbable,
            HoldingGrabbable,
            Painting,
            SkinPainting,
            SkinPaintingOverPaintable,
            SkinTechPainting,
            SkinTechPaintingOverPaintable,
            Disabled
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
        private static bool AddedNewCursors = false;
        public static void AddNewCursors()
        {
            if (AddedNewCursors)
                return;
            MousePointer MP = FindObjectOfType<MousePointer>();
            Debug.Assert(!MP, "BuildUtil: AddNewCursors - THE CURSOR DOES NOT EXIST!");
            string DLLDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent.ToString();
            Debug.LogDevOnly("BuildUtil: AddNewCursors - Path: " + DLLDirectory);
            try
            {
                int LODLevel = 0;
                MousePointer.CursorDataSet[] cursorLODs = (MousePointer.CursorDataSet[])existingCursors.GetValue(MP);
                foreach (var item in cursorLODs)
                {
                    List<MousePointer.CursorData> cursorTypes = item.m_CursorData.ToList();

                    Debug.Log(item.m_Name + " center: " + item.m_UseSoftwareCursor + " | " + cursorTypes.Count);
                    foreach (var exists in cursorTypes)
                    {
                        try
                        {
                            Debug.Log(exists.m_Texture.name.NullOrEmpty() ? "NULL_NAME" : exists.m_Texture.name + " center: " + exists.m_Hotspot.x + "|" + exists.m_Hotspot.y);
                        }
                        catch
                        {
                            Debug.Log("BuildUtil: AddNewCursors - failed to fetch case");
                        }
                    }

                    TryAddNewCursor(cursorTypes, DLLDirectory, "OverTech", LODLevel, new Vector2(0.5f, 0.5f));// 1
                    TryAddNewCursor(cursorTypes, DLLDirectory, "HoldTech", LODLevel, new Vector2(0.5f, 0.5f));// 2
                    TryAddNewCursor(cursorTypes, DLLDirectory, "OverMirror", LODLevel, new Vector2(0.5f, 0.5f));// 3
                    TryAddNewCursor(cursorTypes, DLLDirectory, "HoldMirror", LODLevel, new Vector2(0.5f, 0.5f));// 4
                    TryAddNewCursor(cursorTypes, DLLDirectory, "OverBatch", LODLevel, new Vector2(0.5f, 0.5f));// 5
                    TryAddNewCursor(cursorTypes, DLLDirectory, "HoldBatch", LODLevel, new Vector2(0.5f, 0.5f));// 6
                    TryAddNewCursor(cursorTypes, DLLDirectory, "OverMirrorBatch", LODLevel, new Vector2(0.5f, 0.5f));// 7
                    TryAddNewCursor(cursorTypes, DLLDirectory, "HoldMirrorBatch", LODLevel, new Vector2(0.5f, 0.5f));// 8
                    TryAddNewCursor(cursorTypes, DLLDirectory, "MirroredPainting", LODLevel, new Vector2(0.3f, 0.3f));// 9
                    TryAddNewCursor(cursorTypes, DLLDirectory, "OverMirroredPainting", LODLevel, new Vector2(0.3f, 0.3f));// 10
                    TryAddNewCursor(cursorTypes, DLLDirectory, "PointerMirror", LODLevel, Vector2.zero);// 11
                    TryAddNewCursor(cursorTypes, DLLDirectory, "PointerBatch", LODLevel, Vector2.zero);// 12
                    TryAddNewCursor(cursorTypes, DLLDirectory, "PointerMirrorBatch", LODLevel, Vector2.zero);// 13

                    item.m_CursorData = cursorTypes.ToArray();
                }
            }
            catch (Exception e) { Debug.Log("BuildUtil: AddNewCursors - failed to fetch rest of cursor textures " + e); }
            AddedNewCursors = true;
        }
        private static void TryAddNewCursor(List<MousePointer.CursorData> lodInst, string DLLDirectory, string name, int lodLevel, Vector2 center)
        {
            Debug.Log("BuildUtil: AddNewCursors - " + DLLDirectory + " for " + name + " " + lodLevel + " " + center);
            try
            {
                List<FileInfo> FI = new DirectoryInfo(DLLDirectory).GetFiles().ToList();
                Texture2D tex;
                try
                {
                    tex = FileUtils.LoadTexture(FI.Find(delegate (FileInfo cand)
                    { return cand.Name == name + lodLevel + ".png"; }).ToString());
                    cursorCache.Add(tex);
                }
                catch
                {
                    Debug.Log("BuildUtil: AddNewCursors - failed to fetch cursor texture LOD " + lodLevel + " for " + name);
                    tex = FileUtils.LoadTexture(FI.Find(delegate (FileInfo cand)
                    { return cand.Name == name + "2.png"; }).ToString());
                    cursorCache.Add(tex);
                }
                MousePointer.CursorData CD = new MousePointer.CursorData
                {
                    m_Hotspot = center * tex.width,
                    m_Texture = tex,
                };
                lodInst.Add(CD);
                Debug.Log(name + " center: " + CD.m_Hotspot.x + "|" + CD.m_Hotspot.y);
            }
            catch { Debug.Assert(true, "BuildUtil: AddNewCursors - failed to fetch cursor texture " + name); }
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
                                Debug.Log("BuildUtil: Cannot batch grab cabs or anchored anchors");
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
                            delayedUnsortedBatching.Add(new BatchCache
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
                        Debug.Log("BuildUtil: Grabbed anonMirrorHeld");
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
                            Debug.Log("BuildUtil: Cannot batch grab cabs or anchored anchors");
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
                        delayedUnsortedBatching.Add(new BatchCache
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
                    Debug.Log("BuildUtil: Cannot batch grab cabs or anchored anchors");
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
                delayedUnsortedBatching.Add(new BatchCache
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
        public static List<BatchCache> delayedUnsortedBatching = new List<BatchCache>();
        public void Update()
        {
            if (ManNetwork.IsNetworked)
            {
                DropAll();
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
                lastDraggedPosition = ManPointer.inst.DraggingItem.block.trans.position;
            }
            else if (BusyGrabbingTechs)
            {
                TryCreateTechFromPointerBlocks();
            }
            else if (!IsBatching || !ManPointer.inst.DraggingItem?.block)
            {
                if (PointerBatchCache.Count > 0)
                {
                    foreach (var item in PointerBatchCache)
                    {
                        PostGrab(item.Key);
                    }
                    PointerBatchCache.Clear();
                }
                if (MirrorBatchCache.Count > 0)
                {
                    foreach (var item in MirrorBatchCache)
                    {
                        PostGrab(item.Key);
                    }
                    MirrorBatchCache.Clear();
                }
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
                if (ManPointer.inst.targetTank && Input.GetKey(KeyCode.Backslash))
                {
                    if (IsBatchActive)
                        RebuildTechCabForwards(ManPointer.inst.targetTank);
                    else
                        SetNextPlacedRootCab(ManPointer.inst.targetTank);
                }
                if (IsMirroring)
                {
                    if (ManPointer.inst.targetVisible?.block)
                    {
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

                                lastHovered = Mirror;
                            }
                            if (lastHovered)
                            {
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
                                else
                                    OH.Highlight(Mirror.visible);
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
                        CarryBatchesNonMirror(PlayerHeld);
                        attachFrameDelay = true;
                        return;
                    }
                    else if (ManPointer.inst.DraggingItem?.block)
                    {
                        TankBlock PlayerHeld = ManPointer.inst.DraggingItem.block;
                        CarryBatchesNonMirror(PlayerHeld);
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
            else
            {   // We have a valid tech we are building on
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

                if (Input.GetKey(KeyCode.Backslash))
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
                            if (!IsBlockAvailInInventory(currentTank, pairType))
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
                        if (MirrorBatchCache.Count > 0)
                        {
                            foreach (var item in MirrorBatchCache)
                            {
                                PostGrab(item.Key);
                            }
                            MirrorBatchCache.Clear();
                        }
                        if (MirrorHeld)
                        {
                            PostGrab(MirrorHeld);
                            DropMirror();
                        }
                        CarryBatchesNonMirror(playerHeld);
                    }
                    else
                        UpdateMirrorHeldBlock(playerHeld, false);

                }
                else
                    UpdateMirrorHeldBlock(null, false);
            }
        }

        public void DropMirror()
        {
            if (InventoryBlock)
                ManLooseBlocks.inst.RequestDespawnBlock(MirrorHeld, DespawnReason.Host);
            MirrorHeld = null;
        }
        public void DropAll(bool dontExcludeHovered = false)
        {
            OH.HideHighlight();
            if (lastHovered && !dontExcludeHovered)
            {
                ResetLastHovered();
            }
            if (MirrorHeld)
            {
                PostGrab(MirrorHeld);
                DropMirror();
            }
            foreach (var item in PointerBatchCache)
            {
                PostGrab(item.Key);
            }
            PointerBatchCache.Clear();

            foreach (var item in MirrorBatchCache)
            {
                PostGrab(item.Key);
            }
            MirrorBatchCache.Clear();
        }
        public void ApplyAttachQueue()
        {
            if (delayedAdd.Count > 0)
            {
                Busy = true;
                bool error = false;
                foreach (var item in delayedAdd)
                {
                    Tank targetTank = item.Key;
                    /*
                    if (currentTank)
                        targetTank = currentTank;
                    */
                    if (targetTank && item.Value && item.Key.visible.isActive && item.Value.visible.isActive)
                    {
                        if (IsMirroring)
                        {
                            if (!MirroredPlacement(targetTank, item.Value))
                                error = true;
                        }
                        else
                        {
                            if (!BatchPlacementNonMirror(targetTank, item.Value))
                                error = true;
                        }
                    }
                }
                if (error)
                    Invoke("DelayedFail", 0.1f);
                else
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
                        if (MirroredRemove(item.Key, item.Value))
                        {
                        }
                        else
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
            ResetLastHovered();
            if (show)
            {
                if (MirrorHeld)
                {
                    if (MirrorHeld.BlockType != pairType)
                    {
                        if (PaintBlocks)
                        {
                            PostGrab(MirrorHeld);
                            OH.HideHighlight();
                            DropMirror();
                            foreach (var item in PointerBatchCache)
                            {
                                PostGrab(item.Key);
                            }
                            PointerBatchCache.Clear();
                            foreach (var item in MirrorBatchCache)
                            {
                                PostGrab(item.Key);
                            }
                            MirrorBatchCache.Clear();

                            if (currentTank && IsBlockAvailInInventory(currentTank, pairType))
                            {
                                TankBlock newFake = ManLooseBlocks.inst.HostSpawnBlock(pairType, currentTank.boundsCentreWorld + (Vector3.up * 128), Quaternion.identity);

                                MirrorHeld = newFake;
                                PreGrab(MirrorHeld);
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
                        if (currentTank && IsBlockAvailInInventory(currentTank, pairType))
                        {
                            TankBlock newFake = ManLooseBlocks.inst.HostSpawnBlock(pairType, currentTank.boundsCentreWorld + (Vector3.up * 128), Quaternion.identity);
                            MirrorHeld = newFake;
                            PreGrab(MirrorHeld);
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
                            Debug.Log("Block " + MirrorHeld.name + " is x-axis mirror (has a separate mirror block)");
                            cachedMirrorAngle = MirrorAngle.X;
                        }
                    }
                    if (currentTank)
                    {
                        lastFramePlacementInvalid = DoesBlockConflictWithMain(toMirror) || DoesBlockConflictWithTech();
                        if (lastFramePlacementInvalid)
                            OH.SetHighlightType(ManPointer.HighlightVariation.Invalid);
                        else
                            OH.SetHighlightType(ManPointer.HighlightVariation.Normal);
                    }
                    else
                        OH.SetHighlightType(ManPointer.HighlightVariation.Normal);

                    MirroredSpace(toMirror, ref MirrorHeld);
                }
                else
                    cachedMirrorAngle = MirrorAngle.None;
            }
            else
            {
                if (MirrorHeld)
                {
                    PostGrab(MirrorHeld);
                    OH.HideHighlight();
                    DropMirror();
                    foreach (var item in PointerBatchCache)
                    {
                        PostGrab(item.Key);
                    }
                    PointerBatchCache.Clear();
                    foreach (var item in MirrorBatchCache)
                    {
                        PostGrab(item.Key);
                    }
                    MirrorBatchCache.Clear();
                }
                lastType = BlockTypes.GSOAIController_111;
                pairType = BlockTypes.GSOAIController_111;
                cachedMirrorAngle = MirrorAngle.None;
            }
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
            float extra = toSnap % 1f;
            if (extra < 0.25f)
                extra = 0;
            else if (extra < 0.75f)
                extra = 0.5f;
            else
                extra = 1;
            return Mathf.Floor(toSnap) + extra;
        }


        private static bool TryAttachLoose(Tank tankCase, TankBlock tryAttachThis)
        {
            Vector3 blockPosLocal = tankCase.trans.InverseTransformPoint(tryAttachThis.trans.position);

            BlockCache BC = new BlockCache();
            BC.t = tryAttachThis.BlockType;
            BC.p = blockPosLocal;
            BC.r = SetCorrectRotation(InvTransformRot(tryAttachThis.trans, tankCase));
            BC.TidyUp();
            PostGrab(tryAttachThis);

            return AttemptBlockAttachExt(tankCase, BC, tryAttachThis);
        }



        public void CarryBatchesNonMirror(TankBlock otherBlock)
        {
            foreach (var item in PointerBatchCache)
            {
                HoldInRelation(otherBlock, item.Key, item.Value);
            }
        }

        private bool BatchPlacementNonMirror(Tank tankCase, TankBlock otherBlock)
        {
            bool placed = false;
            AttachAll(tankCase, otherBlock, PointerBatchCache);
            PointerBatchCache.Clear();
            return placed;
        }

        private bool BatchCollect(Tank tankCase, TankBlock toCollect, BlockCache playerSideBC, BlockCache mirrorSideBC)
        {
            if (!tankCase.rootBlockTrans.GetComponent<TankBlock>() || toCollect.trans == tankCase.rootBlockTrans)
                return false;
            if (toCollect == MirrorHeld ||
                (ManPointer.inst.DraggingItem?.block && ManPointer.inst.DraggingItem.block == toCollect))
                return false;
            if (IsMirroring && MirrorHeld)
            {   // handle Batching of Pointer and Mirror blocks
                Vector3 blockLocalPos = tankCase.rootBlockTrans.InverseTransformPoint(toCollect.trans.position);
                Vector3 MirrorHeldLocalPos = tankCase.rootBlockTrans.InverseTransformPoint(MirrorHeld.trans.position);

                if (MirrorHeldLocalPos.x > 0)
                {
                    if (blockLocalPos.x > 0)
                    {   // Mirror Side
                        if (playerSideBC.p == mirrorSideBC.p)
                            Debug.LogError("BuildUtil: Could not fetch mirrored!");
                        PreGrab(toCollect);
                        MirrorBatchCache.Add(new KeyValuePair<TankBlock, BlockCache>(toCollect, mirrorSideBC));
                    }
                    else
                    {   // Player Side
                        PreGrab(toCollect);
                        PointerBatchCache.Add(new KeyValuePair<TankBlock, BlockCache>(toCollect, playerSideBC));
                    }
                }
                else
                {
                    if (blockLocalPos.x > 0)
                    {   // Player Side
                        PreGrab(toCollect);
                        PointerBatchCache.Add(new KeyValuePair<TankBlock, BlockCache>(toCollect, playerSideBC));
                    }
                    else
                    {   // Mirror Side
                        if (playerSideBC.p == mirrorSideBC.p)
                            Debug.LogError("BuildUtil: Could not fetch mirrored!");
                        PreGrab(toCollect);
                        MirrorBatchCache.Add(new KeyValuePair<TankBlock, BlockCache>(toCollect, mirrorSideBC));
                    }
                }
                return true;
            }
            else
            {   // handle Batching of only Pointer blocks
                // Player Side
                PreGrab(toCollect);
                PointerBatchCache.Add(new KeyValuePair<TankBlock, BlockCache>(toCollect, playerSideBC));
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

                foreach (var item in PointerBatchCache)
                {
                    HoldInRelation(otherBlock, item.Key, item.Value);
                }
                foreach (var item in MirrorBatchCache)
                {
                    HoldInRelation(mirror, item.Key, item.Value);
                }
                return;
            }
            Vector3 otherBlockPos = currentTank.trans.InverseTransformPoint(otherBlock.trans.position);

            Vector3 blockCenter = otherBlock.BlockCellBounds.center;
            Quaternion rotOther = InvTransformRot(otherBlock.trans, currentTank);

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

            foreach (var item in PointerBatchCache)
            {
                HoldInRelation(otherBlock, item.Key, item.Value);
            }
            foreach (var item in MirrorBatchCache)
            {
                HoldInRelation(mirror, item.Key, item.Value);
            }
        }

        private bool MirroredPlacement(Tank tankCase, TankBlock otherBlock)
        {
            Vector3 otherBlockPos = otherBlock.trans.localPosition;

            Vector3 blockCenter = otherBlock.BlockCellBounds.center;
            Quaternion rotOther = otherBlock.trans.localRotation;

            Vector3 tankCenter = tankCase.rootBlockTrans.GetComponent<TankBlock>() ?
            SnapToMirrorAxi(tankCase.rootBlockTrans.localPosition +
                (tankCase.rootBlockTrans.localRotation * tankCase.rootBlockTrans.GetComponent<TankBlock>().BlockCellBounds.center))
            : tankCase.blockBounds.center;

            Vector3 centerDelta = (rotOther * blockCenter) + otherBlockPos - tankCenter;

            BlockCache BC = new BlockCache();
            BC.t = GetPair(otherBlock);

            bool fromInv = false;
            TankBlock newBlock;
            if (!PaintBlocks && MirrorHeld && MirrorHeld.BlockType == BC.t)
            {
                newBlock = MirrorHeld;
                //Debug.Log("MirroredPlacement - Attached held real block");

                PostGrab(MirrorHeld);
                MirrorHeld = null;
            }
            else if (IsBlockAvailInInventory(tankCase, BC.t))
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
            if (AttemptBlockAttachExt(tankCase, BC, newBlock))
            {
                if (fromInv)
                    IsBlockAvailInInventory(tankCase, BC.t, true);

                AttachAll(tankCase, otherBlock, PointerBatchCache);
                PointerBatchCache.Clear();
                AttachAll(tankCase, newBlock, MirrorBatchCache);
                MirrorBatchCache.Clear();
                return true;
            }
            else
            {
                if (fromInv)
                    ManLooseBlocks.inst.HostDestroyBlock(newBlock);

                AttachAll(tankCase, otherBlock, MirrorBatchCache);
                PointerBatchCache.Clear();
                foreach (var item in MirrorBatchCache)
                {
                    PostGrab(item.Key);
                }
                MirrorBatchCache.Clear();
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

            Vector3 tankCenter = tankCase.rootBlockTrans.GetComponent<TankBlock>() ?
            SnapToMirrorAxi(tankCase.rootBlockTrans.localPosition +
                (tankCase.rootBlockTrans.localRotation * tankCase.rootBlockTrans.GetComponent<TankBlock>().BlockCellBounds.center))
            : tankCase.blockBounds.center;

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
                    Debug.Assert(true, "BuildUtil: GetMirroredBlock - blockOnOtherSide was not a valid mirror block. Perhaps lastHovered was incorrect?");
            }
            else
                Debug.Assert(true, "BuildUtil: GetMirroredBlock - blockOnOtherSide was null?");
            return null;
        }

        private bool MirroredRemove(Tank tankCase, TankBlock otherBlock)
        {
            TankBlock blockOnOtherSide = GetMirroredBlock(tankCase, otherBlock);
            if (blockOnOtherSide)
            {
                ResetLastHovered();
                lastDetached = blockOnOtherSide;
                AttemptBlockDetachExt(tankCase, blockOnOtherSide);
                if (!blockOnOtherSide.IsAttached)
                {
                    if (MirrorHeld == null)
                    {
                        MirrorHeld = blockOnOtherSide;

                        PreGrab(MirrorHeld);
                        InventoryBlock = false;
                        cachedMirrorAngle = MirrorAngle.None;
                    }
                    //blockOnOtherSide.trans.position = otherBlock.trans.position + (otherBlock.BlockCellBounds.size.magnitude * (otherBlock.trans.position - blockOnOtherSide.trans.position).normalized);
                }
                else
                    Debug.Assert(true, "BuildUtil: Our block has not detached from the Tech it was detached from.  There are now going to be many errors.");
                return true;
            }
            return false;
        }


        public static Quaternion MirroredRot(TankBlock block, Quaternion otherRot, bool mirrorRot)
        {
            Vector3 forward;
            Vector3 up;

            MirrorAngle angle = cachedMirrorAngle;
            if (!mirrorRot)
            {
                forward = otherRot * Vector3.forward;
                forward.x *= -1;
                up = otherRot * Vector3.up;
                up.x *= -1;
                return Quaternion.LookRotation(forward, up);
            }

            if (block.BlockType != pairType)
            {
                GetMirrorNormal(block, ref angle);
            }

            Quaternion offsetMirrorRot = Quaternion.identity;
            offsetMirrorRot.w = otherRot.w;
            offsetMirrorRot.x = otherRot.x;
            offsetMirrorRot.y = -otherRot.y;
            offsetMirrorRot.z = -otherRot.z;

            Quaternion offsetLook;

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

            forward = offsetLook * Vector3.forward;
            forward = offsetMirrorRot * forward;

            up = offsetLook * Vector3.up;
            up = offsetMirrorRot * up;


            return Quaternion.LookRotation(forward, up);
        }

        public static void GetMirrorNormal(TankBlock block, ref MirrorAngle angle)
        {

            if (block.BlockType == BlockTypes.HE_Missile_Pod_28_111)
            {
                angle = MirrorAngle.SeekerMissile;
                return;
            }

            Vector3 blockCenter = block.BlockCellBounds.center;
            Vector3[] posCentered = new Vector3[block.attachPoints.Length];
            for (int step = 0; step < block.attachPoints.Length; step++)
            {
                posCentered[step] = block.attachPoints[step] - blockCenter;
            }
            bool smolBlock = block.filledCells.Length == 1;

            if (!smolBlock)
                GetMirrorSuggestion(posCentered, out angle);

            if (angle == MirrorAngle.NeedsPrecise || smolBlock)
            {
                List<MeshFilter> meshes = new List<MeshFilter>();
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

                if (meshes.Count > 0)
                {
                    //Debug.Log("Block is too simple, trying meshes...");
                    meshes = meshes.OrderByDescending(x => x.sharedMesh.bounds.size.sqrMagnitude).ToList();
                    Transform transMesh = meshes.First().transform;
                    Mesh mesh = meshes.First().sharedMesh;

                    Vector3[] posCenteredMesh = new Vector3[mesh.vertices.Length];
                    for (int step = 0; step < mesh.vertices.Length; step++)
                    {
                        posCenteredMesh[step] = block.trans.InverseTransformPoint(
                            transMesh.TransformPoint(mesh.vertices[step])) - blockCenter;
                    }
                    GetMirrorSuggestion(posCenteredMesh, out angle, true);
                    //GetMirrorSuggestion(posCentered, out angle);
                }
            }
            if (angle == MirrorAngle.Z && block.GetComponent<ModuleWing>())
            {
                angle = MirrorAngle.Y;
            }
            //Debug.Log("Block " + block.name + " is mirror " + angle);
        }

        /// <summary>
        /// returns true if the answer is not vague
        /// </summary>
        /// <param name="posCentered"></param>
        /// <param name="angle"></param>
        /// <param name="doComplex"></param>
        /// <returns></returns>
        public static bool GetMirrorSuggestion(Vector3[] posCentered, out MirrorAngle angle, bool doComplex = false)
        {

            int countX = CountOffCenter(posCentered, Vector3.forward);
            if (!doComplex && countX == 0)
            {
                angle = MirrorAngle.X;
                return true;
            }

            int countZ = CountOffCenter(posCentered, Vector3.right);
            if (!doComplex && countZ == 0)
            {
                angle = MirrorAngle.Z;
                return true;
            }

            int countY = CountOffCenterY(posCentered);
            if (!doComplex && countY == 0)
            {
                angle = MirrorAngle.Y;
                return true;
            }

            Vector3 angle45 = new Vector3(1, 0, 1).normalized;
            int count90 = CountOffCenter(posCentered, angle45);
            if (!doComplex && count90 == 0)
            {
                angle = MirrorAngle.YCorner;
                return true;
            }

            Vector3 angleInv45 = new Vector3(-1, 0, 1).normalized;
            int count90Inv = CountOffCenter(posCentered, angleInv45);
            if (!doComplex && count90Inv == 0)
            {
                angle = MirrorAngle.YCornerInv;
                return true;
            }


            Vector3 angle45Z = new Vector3(1, 1, 0).normalized; // upper right
            int count90Z = CountOffCenterZ(posCentered, angle45Z);
            if (!doComplex && count90Z == 0)
            {
                angle = MirrorAngle.ZCorner;
                return true;
            }

            Vector3 angle45InvZ = new Vector3(-1, 1, 0).normalized; // upper left
            int count90ZInv = CountOffCenterZ(posCentered, angle45InvZ);
            if (!doComplex && count90ZInv == 0)
            {
                angle = MirrorAngle.ZCornerInv;
                return true;
            }

            Vector3 angle45X = new Vector3(0, 1, 1).normalized; // upper right
            int count90X = CountOffCenterX(posCentered, angle45X);
            if (!doComplex && count90X == 0)
            {
                angle = MirrorAngle.XCorner;
                return true;
            }

            Vector3 angle45InvX = new Vector3(0, 1, -1).normalized; // upper left
            int count90XInv = CountOffCenterX(posCentered, angle45InvX);
            if (!doComplex && count90XInv == 0)
            {
                angle = MirrorAngle.XCornerInv;
                return true;
            }

            if (!doComplex)
            {
                Debug.Log("Block is complex mirror!");
                angle = MirrorAngle.NeedsPrecise;
                return true;
            }

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
            comp = comp.OrderBy(x => Mathf.Abs(x.Key)).ToList();
            angle = comp.First().Value;
            if (Mathf.Abs(comp.First().Key) > 2)
            {
                Debug.Log("Block is vague");
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
            BlockTypes BT = TB.BlockType;
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

        public static void PreGrab(TankBlock TB)
        {
            if (TB.GetComponent<ColliderSwapper>())
                TB.GetComponent<ColliderSwapper>().EnableCollision(false);
            if (TB.rbody)
                TB.rbody.useGravity = false;
        }
        public static void PostGrab(TankBlock TB)
        {
            try
            {
                if (TB.GetComponent<ColliderSwapper>())
                    TB.GetComponent<ColliderSwapper>().EnableCollision(true);
                if (TB.rbody)
                {
                    TB.rbody.useGravity = true;
                    TB.rbody.velocity = Vector3.zero;
                    TB.rbody.angularVelocity = Vector3.zero;
                }
            }
            catch
            {
                Debug.LogError("BuildUtil:  Block was expected but it was null!  Was it destroyed in tranzit!?");
            }
        }

        public static void HoldInRelation(TankBlock master, TankBlock minion, BlockCache BC)
        {
            try
            {
                minion.trans.rotation = master.trans.rotation * BC.r;
                minion.trans.position = master.trans.position + (master.trans.rotation * BC.p);
            }
            catch
            {
                Debug.LogError("BuildUtil: HoldInRelation - Block was expected but it was null!  Was it destroyed in tranzit!?");
            }
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
            if (tank == Singleton.playerTank)
                return;
            BusyGrabbingTechs = true;
            lastDraggedName = tank.name;
            List<TankBlock> cache = DitchAllBlocks(tank, false);
            foreach (var item in cache)
            {
                if (pointerBlock != item)
                    PreGrab(item);
            }
            BatchCenterOn(pointerBlock, cache, out List<KeyValuePair<TankBlock, BlockCache>> batch);
            PointerBatchCache.AddRange(batch);
            cachePointerHeld = pointerBlock;
        }

        public static void TryCreateTechFromPointerBlocks()
        {
            TankBlock pointerBlock = cachePointerHeld;
            ManPointer.inst.ReleaseDraggingItem(false);
            if (!IsGrabbingTechsActive)
            {
                List<TankBlock> allHeld = PointerBatchCache.ConvertAll(x => x.Key);
                foreach (var item in allHeld)
                {
                    PostGrab(item);
                }
                PointerBatchCache.Clear();
            }
            if (pointerBlock.tank)
            {
                TryReattachAll(pointerBlock.tank, pointerBlock, PointerBatchCache);
                PointerBatchCache.Clear();
            }
            else if (currentTank)
            {
                if (ManTechBuilder.inst.IsBlockHeldInPosition(pointerBlock))
                {
                    TryReattachAll(currentTank, pointerBlock, PointerBatchCache);
                    PointerBatchCache.Clear();
                }
                else if (pointerBlock && SetToCab(ref pointerBlock, ref PointerBatchCache))
                {
                    Debug.Log("New root " + pointerBlock.name);
                    inst.CarryBatchesNonMirror(pointerBlock);
                    List<TankBlock> allHeld = PointerBatchCache.ConvertAll(x => x.Key);
                    foreach (var item in allHeld)
                    {
                        PostGrab(item);
                    }
                    Tank tank = pointerBlock.GetComponentInParent<Tank>();
                    if (!tank)
                        tank = ManSpawn.inst.WrapSingleBlock(null, pointerBlock, ManPlayer.inst.PlayerTeam,
                            lastDraggedName.NullOrEmpty() ? "Grabbed Tech" : lastDraggedName);
                    TryReattachAll(tank, pointerBlock, PointerBatchCache);
                    PointerBatchCache.Clear();
                    if (tank.rbody)
                    {
                        tank.rbody.AddForce((cachePointerHeld.trans.position - lastDraggedPosition) / Time.deltaTime, ForceMode.VelocityChange);
                    }
                    Debug.Log("Made Tech from grabbed");
                }
                else
                {
                    Debug.LogError("Failed to make Tech from grabbed");
                    List<TankBlock> allHeld = PointerBatchCache.ConvertAll(x => x.Key);
                    foreach (var item in allHeld)
                    {
                        PostGrab(item);
                    }
                    PointerBatchCache.Clear();
                }
            }
            else
            {
                if (pointerBlock && SetToCab(ref pointerBlock, ref PointerBatchCache))
                {
                    Debug.Log("New root " + pointerBlock.name);
                    inst.CarryBatchesNonMirror(pointerBlock);
                    List<TankBlock> allHeld = PointerBatchCache.ConvertAll(x => x.Key);
                    foreach (var item in allHeld)
                    {
                        PostGrab(item);
                    }
                    Tank tank = pointerBlock.GetComponentInParent<Tank>();
                    if (!tank)
                        tank = ManSpawn.inst.WrapSingleBlock(null, pointerBlock, ManPlayer.inst.PlayerTeam,
                            lastDraggedName.NullOrEmpty() ? "Grabbed Tech" : lastDraggedName);
                    TryReattachAll(tank, pointerBlock, PointerBatchCache);
                    PointerBatchCache.Clear();
                    if (tank.rbody)
                    {
                        tank.rbody.AddForce((cachePointerHeld.trans.position - lastDraggedPosition) / Time.deltaTime, ForceMode.VelocityChange);
                    }
                    Debug.Log("Made Tech from grabbed");
                }
                else
                {
                    Debug.LogError("Failed to make Tech from grabbed");
                    List<TankBlock> allHeld = PointerBatchCache.ConvertAll(x => x.Key);
                    foreach (var item in allHeld)
                    {
                        PostGrab(item);
                    }
                    PointerBatchCache.Clear();
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
                        MirrorBatchCache.Insert(0, new KeyValuePair<TankBlock, BlockCache>(item, BC));
                    }
                }
                else
                {
                    BC = CenterOn(pointerBlock, item);
                    PointerBatchCache.Insert(0, new KeyValuePair<TankBlock, BlockCache>(item, BC));
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
        public static List<KeyValuePair<TankBlock, BlockCache>> TryReattachAll(Tank tank, TankBlock toCenterOn, List<KeyValuePair<TankBlock, BlockCache>> toAttach)
        {
            if (tank == null)
            {
                Debug.LogError("Tank was NULL");
                return toAttach;
            }
            if (tank.blockman.blockCount == 0)
            {
                Debug.LogError("Tank had no blocks");
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
                    TankBlock item = toAttach[attachPos].Key;
                    HoldInRelation(root, item, toAttach[attachPos].Value);
                    if (item.rbody)
                    {
                        item.rbody.velocity = Vector3.zero;
                        item.rbody.angularVelocity = Vector3.zero;
                    }
                    if (item.tank)
                        Debug.LogError("Block " + item.name + " was already attached");
                    if (TryAttachLoose(tank, item))
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
        public static bool AttachAll(Tank tank, TankBlock toCenterOn, List<KeyValuePair<TankBlock, BlockCache>> toAttach)
        {
            if (tank == null)
            {
                Debug.LogError("Tank was NULL");
                return false;
            }
            if (tank.blockman.blockCount == 0)
            {
                Debug.LogError("Tank had no blocks");
                return false;
            }
            if (ManNetwork.IsNetworked)
                return false;
            TankBlock root = toCenterOn;
            bool Attaching;
            bool hasAttached = false;
            do
            {
                Attaching = false;
                int attachNeeded = toAttach.Count;
                for (int attachPos = 0; attachPos < attachNeeded;)
                {
                    TankBlock item = toAttach[attachPos].Key;
                    HoldInRelation(root, item, toAttach[attachPos].Value);
                    if (item.rbody)
                    {
                        item.rbody.velocity = Vector3.zero;
                        item.rbody.angularVelocity = Vector3.zero;
                    }
                    //if (item.tank)
                    //    Debug.LogError("Block " + item.name + " was already attached");
                    if (TryAttachLoose(tank, item))
                    {
                        Debug.Log("Attached " + item.name + " to " + tank.name);
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
                Debug.LogError("Tank was NULL");
                return toAttach;
            }
            if (tank.blockman.blockCount == 0)
            {
                Debug.LogError("Tank had no blocks");
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
                        Debug.LogError("Block " + item.name + " was already attached");
                    if (TryAttachLoose(tank, item))
                    {
                        Debug.Log("Attached " + item.name + " to " + tank.name);
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

        public static bool SetToCab(ref TankBlock mainHeld, ref List<KeyValuePair<TankBlock, BlockCache>> toSearch)
        {
            if (mainHeld.GetComponent<ModuleTechController>())
                return true;
            foreach (var item in toSearch)
            {
                if (!item.Key)
                    continue;
                if (item.Key.GetComponent<ModuleTechController>())
                {
                    List<TankBlock> allHeld = toSearch.ConvertAll(x => x.Key);
                    allHeld.Add(mainHeld);
                    BatchCenterOn(item.Key, allHeld, out toSearch);
                    mainHeld = item.Key;
                    return true;
                }
            }
            return false;
        }
        public static void BatchCenterOn(TankBlock main, List<TankBlock> allHeld, out List<KeyValuePair<TankBlock, BlockCache>> children)
        {
            children = new List<KeyValuePair<TankBlock, BlockCache>>();
            foreach (var item in allHeld)
            {
                if (item == main)
                {
                    PostGrab(item);
                    continue;
                }
                PreGrab(item);
                children.Add(new KeyValuePair<TankBlock, BlockCache>(item, CenterOn(main, item)));
            }
        }
        public static BlockCache CenterOn(TankBlock mainHeld, TankBlock toSet)
        {
            BlockCache BC = new BlockCache
            {
                t = toSet.BlockType,
                p = mainHeld.trans.InverseTransformPoint(toSet.trans.position),
                r = SetCorrectRotation(InvTransformRot(toSet.trans, mainHeld.trans)),
            };
            BC.TidyUp();
            return BC;
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
            List<BlockCache> mem = TechToForwardsCache(tank, out TankBlock rBlock);
            List<TankBlock> blocks = DitchAllBlocks(tank, true);
            TurboconstructExt(tank, mem, blocks, false);
            tank.blockman.SetRootBlock(rBlock);
            HighlightBlock(tank.blockman.GetRootBlock());
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
                    Debug.Log("TACtical_AI:  DesignMemory - " + tank.name + ": could not save " + bloc.name + " in blueprint due to illegal rotation.");
                    continue;
                }
                output.Add(mem);
            }
            Debug.Info("TACtical_AI:  DesignMemory - Saved " + tank.name + " to memory format");

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
                    Debug.Log("Architech: SetCorrectRotation - Matching failed - OrthoRotation is missing edge case " + foA + " | " + upA);
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
                    Debug.Log("Architech: SetCorrectRotation - Matching failed - OrthoRotation is missing edge case " + foA + " | " + upA);
                }
            }
            return rot;
        }


        public static List<BlockTypes> GetMissingBlockTypesExt(List<BlockCache> Mem, List<TankBlock> cBlocks)
        {
            List<BlockTypes> typesToRepair = new List<BlockTypes>();
            int toFilter = Mem.Count();
            for (int step = 0; step < toFilter; step++)
            {
                typesToRepair.Add(Mem.ElementAt(step).t);
            }
            typesToRepair = typesToRepair.Distinct().ToList();

            List<BlockTypes> typesMissing = new List<BlockTypes>();
            int toFilter2 = typesToRepair.Count();
            for (int step = 0; step < toFilter2; step++)
            {
                int present = cBlocks.FindAll(delegate (TankBlock cand) { return typesToRepair[step] == cand.BlockType; }).Count;

                int mem = Mem.FindAll(delegate (BlockCache cand) { return typesToRepair[step] == cand.t; }).Count;
                if (mem > present)// are some blocks not accounted for?
                    typesMissing.Add(typesToRepair[step]);
            }
            return typesMissing;
        }
        /// <summary>
        /// Builds a Tech instantly, no requirements
        /// </summary>
        /// <param name="tank"></param>
        /// <param name="TechMemor"></param>
        public static void TurboconstructExt(Tank tank, List<BlockCache> Mem, List<TankBlock> provided, bool fullyCharge = true)
        {
            Debug.Log("TACtical_AI:  DesignMemory: Turboconstructing " + tank.name);
            int cBCount = tank.blockman.IterateBlocks().ToList().Count();
            int RepairAttempts = Mem.Count() - cBCount + 3;
            try
            {
                List<TankBlock> cBlocks = tank.blockman.IterateBlocks().ToList();
                List<BlockTypes> typesMissing = GetMissingBlockTypesExt(Mem, cBlocks);
                while (RepairAttempts > 0)
                {
                    TurboRepairExt(tank, Mem, ref typesMissing, ref provided);
                    RepairAttempts--;
                }
            }
            catch { return; }
            if (fullyCharge)
                tank.EnergyRegulator.SetAllStoresAmount(1);
        }
        public static void TurboRepairExt(Tank tank, List<BlockCache> Mem, ref List<BlockTypes> typesMissing, ref List<TankBlock> provided)
        {
            List<TankBlock> cBlocks = tank.blockman.IterateBlocks().ToList();
            int savedBCount = Mem.Count;
            int cBCount = cBlocks.Count;
            if (savedBCount != cBCount)
            {

                //Debug.Log("TACtical AI: TurboRepair - Attempting to repair from infinity - " + typesToRepair.Count());
                if (!TryAttachExistingBlockFromListExt(tank, Mem, ref typesMissing, ref provided))
                    Debug.Log("TACtical AI: TurboRepair - attach attempt failed");
            }
            return;
        }
        public static bool TryAttachExistingBlockFromListExt(Tank tank, List<BlockCache> mem, ref List<BlockTypes> typesMissing, ref List<TankBlock> foundBlocks, bool denySD = false)
        {
            int attachAttempts = foundBlocks.Count();
            //Debug.Log("TACtical AI: RepairLerp - Found " + attachAttempts + " loose blocks to use");
            for (int step = 0; step < attachAttempts; step++)
            {
                TankBlock foundBlock = foundBlocks[step];
                BlockTypes BT = foundBlock.BlockType;
                if (!typesMissing.Contains(BT))
                    continue;
                bool attemptW;
                // if we are smrt, run heavier operation
                List<BlockCache> posBlocks = mem.FindAll(delegate (BlockCache cand) { return cand.t == BT; });
                int count = posBlocks.Count;
                //Debug.Log("TACtical AI: RepairLerp - potental spots " + posBlocks.Count + " for block " + foundBlock);
                for (int step2 = 0; step2 < count; step2++)
                {
                    BlockCache template = posBlocks.ElementAt(step2);
                    attemptW = AttemptBlockAttachExt(tank, template, foundBlock);
                    if (attemptW)
                    {
                        if (denySD)
                        {
                            foundBlock.damage.AbortSelfDestruct();
                        }
                        foundBlocks.RemoveAt(step);
                        return true;
                    }
                }
            }
            return false;
        }
        private static bool AttemptBlockAttachExt(Tank tank, BlockCache template, TankBlock canidate)
        {
            //Debug.Log("TACtical_AI: (AttemptBlockAttachExt) AI " + tank.name + ":  Trying to attach " + canidate.name + " at " + template.CachePos);
            return Singleton.Manager<ManLooseBlocks>.inst.RequestAttachBlock(tank, canidate, template.p, template.r);
        }

        private static void AttemptBlockDetachExt(Tank tank, TankBlock toRemove)
        {
            ManLooseBlocks.inst.HostDetachBlock(toRemove, false, true);
        }



        public static bool IsBlockAvailInInventory(Tank tank, BlockTypes blockType, bool taking = false)
        {
            if (!ManSpawn.IsPlayerTeam(tank.Team))
                return true;// Non-player Teams don't actually come with limited inventories.  strange right?
            if (!taking)
            {
                if (Singleton.Manager<ManPlayer>.inst.InventoryIsUnrestricted)
                {
                    //no need to return to infinite stockpile
                    return true;
                }
                else
                {
                    try
                    {
                        bool isMP = Singleton.Manager<ManGameMode>.inst.IsCurrentModeMultiplayer();
                        if (isMP)
                        {
                            if (Singleton.Manager<NetInventory>.inst.IsAvailableToLocalPlayer(blockType))
                            {
                                return Singleton.Manager<NetInventory>.inst.GetQuantity(blockType) > 0;
                            }
                        }
                        else
                        {
                            if (Singleton.Manager<SingleplayerInventory>.inst.IsAvailableToLocalPlayer(blockType))
                            {
                                return Singleton.Manager<SingleplayerInventory>.inst.GetQuantity(blockType) > 0;
                            }
                        }
                    }
                    catch
                    {
                        Debug.Log("BuildUtil: " + tank.name + ":  Tried to repair but block " + blockType.ToString() + " was not found!");
                    }
                }
                return false;
            }
            bool isAvail = false;
            if (Singleton.Manager<ManPlayer>.inst.InventoryIsUnrestricted)
            {
                isAvail = true;
            }
            else
            {
                try
                {
                    int availQuant;
                    bool isMP = Singleton.Manager<ManGameMode>.inst.IsCurrentModeMultiplayer();
                    if (isMP)
                    {
                        if (Singleton.Manager<NetInventory>.inst.IsAvailableToLocalPlayer(blockType))
                        {
                            availQuant = Singleton.Manager<NetInventory>.inst.GetQuantity(blockType);
                            if (availQuant > 0)
                            {
                                availQuant--;
                                isAvail = true;
                                Singleton.Manager<NetInventory>.inst.SetBlockCount(blockType, availQuant);
                            }
                        }
                    }
                    else
                    {
                        if (Singleton.Manager<SingleplayerInventory>.inst.IsAvailableToLocalPlayer(blockType))
                        {
                            availQuant = Singleton.Manager<SingleplayerInventory>.inst.GetQuantity(blockType);
                            if (availQuant > 0)
                            {
                                availQuant--;
                                isAvail = true;
                                Singleton.Manager<SingleplayerInventory>.inst.SetBlockCount(blockType, availQuant);
                            }
                        }
                    }
                }
                catch
                {
                    Debug.Log("BuildUtil: " + tank.name + ":  Tried to repair but block " + blockType.ToString() + " was not found!");
                }
            }
            return isAvail;
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

        internal class BatchCache
        {   // Save the blocks!
            public Tank originTank;
            public TankBlock handledBlock;

            public BlockCache fromPlayer;
            public BlockCache fromMirror;

            public bool Verify()
            {
                return originTank && originTank.visible.isActive && originTank.blockman.blockCount > 0
                    && handledBlock && !handledBlock.IsAttached && handledBlock.CanAttach;
            }
        }

        internal struct BlockCache
        {   // Save the blocks!
            public BlockTypes t;
            public Vector3 p;
            public OrthoRotation r;

            /// <summary>
            /// get rid of floating point errors
            /// </summary>
            public void TidyUp()
            {
                p.x = Mathf.RoundToInt(p.x);
                p.y = Mathf.RoundToInt(p.y);
                p.z = Mathf.RoundToInt(p.z);
            }
        }
    }


}
