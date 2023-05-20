using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Architech
{
    /// <summary>
    /// Batches of blocks left floating with no control are handled here
    /// </summary>
    public class BlockBatch : IEnumerable
    {
        private TankBlock root;
        public TankBlock Root => root;
        /// <summary>
        /// Does NOT include the root block which other blocks are positioned around
        /// </summary>
        public readonly List<BlockCache> batch = new List<BlockCache>();
        private Bounds bounds = new Bounds();
        public int Count => batch.Count;

        public BlockBatch(TankBlock Root)
        {
            root = Root;
        }
        /// <summary>
        /// PULLS BLOCKS FROM INVENTORY
        /// </summary>
        /// <param name="toCopy"></param>
        public BlockBatch(BlockBatch toCopy)
        {
            root = ManLooseBlocks.inst.HostSpawnBlock(toCopy.root.BlockType, toCopy.root.trans.position, toCopy.root.trans.rotation);
            foreach (var item in toCopy)
            {
                TankBlock block = ManLooseBlocks.inst.HostSpawnBlock(item.t, item.inst.trans.position, item.inst.trans.rotation);
                Possess(block);
                BlockCache BC = new BlockCache(item, block);
                BC.TidyUp();
                batch.Add(BC);
            }
            UpdateHold();
        }
        /// <summary>
        /// COPY MIRRORED
        /// </summary>
        /// <param name="hostTank"></param>
        /// <param name="toCopy"></param>
        public BlockBatch(Tank hostTank, BlockBatch toCopy)
        {
            BlockTypes rootMirrorType = ManBuildUtil.GetPair(toCopy.Root.BlockType);
            root = ManLooseBlocks.inst.HostSpawnBlock(rootMirrorType, toCopy.root.trans.position, toCopy.root.trans.rotation);
            ManBuildUtil.DoMirroredRotationInRelationToTankNotSpawned(hostTank, rootMirrorType != toCopy.Root.BlockType, ref root);
            foreach (var item in toCopy)
            {
                BlockTypes childMirrorType = ManBuildUtil.GetPair(item.t);
                TankBlock block = ManLooseBlocks.inst.HostSpawnBlock(childMirrorType, item.inst.trans.position, item.inst.trans.rotation);
                ManBuildUtil.DoMirroredRotationInRelationToTankNotSpawned(hostTank, childMirrorType != item.t, ref block);
                BlockCache BC = new BlockCache(item, block);
                BC.TidyUp();
                batch.Add(BC);
            }
            BatchReCenterOn(root);
            UpdateHold();
        }

        public static implicit operator bool(BlockBatch inst)
        {
            return inst != null;
        }

        public static implicit operator List<BlockCache>(BlockBatch inst)
        {
            if (!inst)
                return new List<BlockCache>();
            return inst.batch;
        }
        

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public BlockCacheEnum GetEnumerator()
        {
            return new BlockCacheEnum(batch.ToArray());
        }

        public void Add(BlockCache BC)
        {
            DebugArchitech.Assert(!BC.inst, "BlockBatch - Adding BlockCache with null inst");
            Possess(BC.inst);
            batch.Add(BC);
        }

        public bool IsRootValid()
        {
            return root && root.visible.isActive && !root.tank;
        }

        /// <summary>
        /// Drops batch blocks automatically if root is null
        /// </summary>
        /// <returns></returns>
        public bool UpdateHold()
        {
            if (!IsRootValid())
            {
                DropAllButRoot();
                return false;
            }
            UpdateHoldNoCheck();
            return true;
        }
        public void UpdateHoldNoCheck()
        {
            foreach (var item in batch)
            {
                item.HoldInRelation(root);
            }
        }

        public void InsureAboveGround()
        {
            Bounds bounded = GetBounds();
            Vector3 localBoundsLowest = bounded.ClosestPoint(root.trans.InverseTransformDirection(Vector3.down) * 90000);
            Vector3 height = root.trans.TransformPoint(localBoundsLowest);
            if (ManWorld.inst.GetTerrainHeight(height, out float newHeight))
            {
                if (newHeight > height.y)
                {
                    float newHeightOffset = newHeight - (root.trans.rotation * localBoundsLowest).y + 1;
                    root.trans.position = root.trans.position.SetY(newHeightOffset);
                    ManBuildUtil.StopMovement(root);
                    DebugArchitech.Log("Moved above the ground " + newHeightOffset);
                }
            }
            foreach (var item in batch)
            {
                item.HoldInRelation(root);
            }
        }

        /// <summary>
        /// Recenter the batch around a specific block
        /// </summary>
        /// <param name="main"></param>
        /// <returns>The block that the batch was centered on</returns>
        public TankBlock BatchCenterOn(TankBlock main, List<TankBlock> allHeld)
        {
            root = main;
            batch.Clear();
            foreach (var item in allHeld)
            {
                if (item == main)
                {
                    root = item;
                    Release(item);
                    continue;
                }
                Possess(item);
                batch.Add(BlockCache.CenterOn(main, item));
            }
            bounds = new Bounds();
            return main;
        }

        /// <summary>
        /// Recenter the batch around a specific block
        /// </summary>
        /// <param name="main"></param>
        /// <returns>The block that the batch was centered on</returns>
        public TankBlock BatchReCenterOn(TankBlock main)
        {
            List<TankBlock> allHeld = batch.ConvertAll(x => x.inst);
            allHeld.Insert(0, root);

            DebugArchitech.Assert(!allHeld.Contains(main), "Architech: BlockBatch - " +
                "Setting a new root that is not registered in the batch");

            root = main;
            batch.Clear();
            foreach (var item in allHeld)
            {
                if (item == main)
                {
                    root = item;
                    Release(item);
                    continue;
                }
                Possess(item);
                batch.Add(BlockCache.CenterOn(main, item));
            }
            bounds = new Bounds();
            return main;
        }

        /// <summary>
        /// Recenter the batch around a specific block
        /// </summary>
        /// <param name="main"></param>
        /// <returns>The block that the batch was centered on</returns>
        public TankBlock BatchCenterOnNoGrab(TankBlock main)
        {
            List<TankBlock> allHeld = batch.ConvertAll(x => x.inst);
            allHeld.Insert(0, root);

            DebugArchitech.Assert(!allHeld.Contains(main), "Architech: BlockBatch - " +
                "Setting a new root that is not registered in the batch");

            root = main;
            batch.Clear();
            foreach (var item in allHeld)
            {
                if (item == main)
                    continue;
                batch.Add(BlockCache.CenterOn(main, item));
            }
            bounds = new Bounds();
            return main;
        }

        public Vector3 GetLocalPositionOfBlock(TankBlock block)
        {
            if (block == root)
                return Vector3.zero;
            foreach (var item in batch)
            {
                if (item.inst == block)
                    return item.p;
            }
            return Vector3.zero;
        }
        public TankBlock TryGetBlockFromLocalPosition(Vector3 localPos)
        {
            if (localPos == Vector3.zero)
                return root;
            foreach (var item in batch)
            {
                if (item.p.Approximately(localPos, 0.45f))
                    return item.inst;
            }
            return null;
        }
        public Bounds GetBounds()
        {
            if (!root)
                return new Bounds(Vector3.zero, Vector3.one);
            if (bounds.size != Vector3.zero)
                return bounds;
            bounds = new Bounds(Vector3.zero, Vector3.one);
            foreach (var cell in root.filledCells)
            {
                bounds.Encapsulate(cell);
            }
            foreach (var item in batch)
            {
                foreach (var cell in item.inst.filledCells)
                {
                    bounds.Encapsulate(item.p + (item.r * cell));
                }
            }
            bounds.Expand(0.5f);
            DebugArchitech.Info("BlockBatch - Added new Bounds of size " + bounds.size + ", center " + bounds.center);
            return bounds;
        }


        public void DropAllButRoot()
        {
            foreach (var item in batch)
            {
                Release(item.inst);
            }
        }
        public void GrabAllIncludingRoot()
        {
            if (root)
            {
                ManBuildUtil.StopMovement(root);
                Possess(root);
            }
            foreach (var item in batch)
            {
                ManBuildUtil.StopMovement(item.inst);
                Possess(item.inst);
            }
        }

        private static List<BlockTypes> blockT = new List<BlockTypes>();
        public List<BlockTypes> GetAllBlockTypes()
        {
            blockT.Clear();
            blockT.Add(root.BlockType);
            blockT.AddRange(batch.ConvertAll(x => x.t));
            return blockT;
        }
        private static List<TankBlock> tankB = new List<TankBlock>();
        public List<TankBlock> GetAllBlocks()
        {
            tankB.Clear();
            tankB.Add(root);
            tankB.AddRange(batch.ConvertAll(x => x.inst));
            return tankB;
        }
        public bool HasAllBlocksNeededInInventory(Tank currentTank)
        {
            return ManBuildUtil.HasNeededInInventory(currentTank, GetAllBlockTypes());
        }

        private static void Possess(TankBlock TB)
        {
            if (TB.GetComponent<ColliderSwapper>())
                TB.GetComponent<ColliderSwapper>().EnableCollision(false);
            if (TB.rbody)
            {
                TB.rbody.useGravity = false;
            }
        }
        private static void Release(TankBlock TB)
        {
            try
            {
                if (TB.GetComponent<ColliderSwapper>())
                    TB.GetComponent<ColliderSwapper>().EnableCollision(true);
                if (TB.rbody)
                {
                    TB.rbody.useGravity = true;
                    ManBuildUtil.StopMovement(TB);
                }
            }
            catch
            {
                DebugArchitech.LogError("BuildUtil:  Block was expected but it was null!  Was it destroyed in tranzit!?");
            }
        }
    }
}
