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
        public readonly List<BlockCache> batch = new List<BlockCache>();
        private Bounds bounds = new Bounds();
        public int Count => batch.Count;

        public BlockBatch(TankBlock Root)
        {
            root = Root;
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
