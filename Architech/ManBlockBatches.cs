using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Architech
{
    /// <summary>
    /// Handles loose floating batches of blocks
    /// </summary>
    internal class ManBlockBatches : MonoBehaviour
    {
        internal static ManBlockBatches inst;
        internal static float maxDistSustain = 150;
        internal static float maxDistSustainSqr = maxDistSustain * maxDistSustain;

        private static readonly Dictionary<BlockBatch, VagueBounds> InStasis = new Dictionary<BlockBatch, VagueBounds>();

        internal void BeforeWorldReset(Mode unused)
        {
            if (InStasis.Count > 0)
            {
                foreach (var item in InStasis)
                {
                    item.Key.DropAllButRoot();
                    Destroy(item.Value);
                }
                InStasis.Clear();
            }
        }
        internal static void Subcribble(bool yes)
        {
            if (yes)
            {
                if (inst)
                    return;
                inst = new GameObject("BatchUtil").AddComponent<ManBlockBatches>();
                ManPointer.inst.MouseEvent.Subscribe(inst.TryGetStasisBatch);
                ManGameMode.inst.ModeCleanUpEvent.Subscribe(inst.BeforeWorldReset);
            }
            else
            {
                if (!inst)
                    return;
                inst.BeforeWorldReset(null);
                ManGameMode.inst.ModeCleanUpEvent.Unsubscribe(inst.BeforeWorldReset);
                ManPointer.inst.MouseEvent.Unsubscribe(inst.TryGetStasisBatch);
                Destroy(inst);
                inst = null;
            }
        }
        internal static VagueBounds MakeNewVagueBounds(BlockBatch BB, bool applyForce, VagueBounds BBMirror)
        {
            GameObject GO = new GameObject("VagueBounds_" + BB.Root.name);
            return GO.AddComponent<VagueBounds>().Init(BB, applyForce, BBMirror);
        }
        internal static VagueBounds TryMaintainDropped(BlockBatch BB)
        {
            if (!BB)
                return null;
            DebugArchitech.Assert(InStasis.TryGetValue(BB, out _), "Architech: ManBlockBatches - InStasis already has the batch and we are adding it again!");
            DebugArchitech.Assert(!BB.Root, "Architech: ManBlockBatches - BlockBatch root is NULL");
            BB.InsureAboveGround();
            if (BB.Root && !BB.Root.IsAttached)
            {
                VagueBounds VB = MakeNewVagueBounds(BB, true, null);
                InStasis.Add(BB, VB);
                return VB;
            }
            else
                BB.DropAllButRoot();
            return null;
        }
        internal static void TryMaintainDroppedMirror(BlockBatch BB, VagueBounds BBMirror)
        {
            if (!BB)
                return;
            DebugArchitech.Assert(InStasis.TryGetValue(BB, out _), "Architech: ManBlockBatches(Mirror) - InStasis already has the batch and we are adding it again!");
            DebugArchitech.Assert(!BB.Root, "Architech: ManBlockBatches(Mirror) - BlockBatch root is NULL");
            BB.InsureAboveGround();
            if (BB.Root && !BB.Root.IsAttached)
            {
                InStasis.Add(BB, MakeNewVagueBounds(BB, false, BBMirror));
            }
            else
                BB.DropAllButRoot();
        }

        private readonly List<KeyValuePair<BlockBatch, VagueBounds>> dropped = new List<KeyValuePair<BlockBatch, VagueBounds>>();
        internal void Update()
        {
            if (ManBuildUtil.IsBatchActive && Singleton.playerTank)
            {
                Vector3 playerCenter = Singleton.playerTank.boundsCentreWorld;
                foreach (var item in InStasis)
                {
                    DebugArchitech.Assert(!item.Key, "Architech: ManBlockBatches - Unexpected null BlockBatch in InStasis. Was it improperly added?");
                    if (!item.Key.IsRootValid() || (playerCenter - item.Value.worldCenter).sqrMagnitude > maxDistSustainSqr)
                        dropped.Add(item);
                }
                foreach (var item in dropped)
                {
                    item.Value.DeInit(true);
                }
                dropped.Clear();
            }
            else if (InStasis.Count > 0)
            {
                foreach (var item in InStasis)
                {
                    dropped.Add(item);
                }
                foreach (var item in dropped)
                {
                    item.Value.DeInit(true);
                }
                dropped.Clear();
            }
        }

        int mask = Globals.inst.layerPickup.mask;
        private void TryGetStasisBatch(ManPointer.Event button, bool down, bool yes)
        {
            if (down && Singleton.playerTank)
            {
                Ray toCast;
                RaycastHit hit;
                switch (button)
                {
                    case ManPointer.Event.LMB:
                        if (ManPointer.inst.DraggingItem)
                            return;
                        toCast = ManUI.inst.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(toCast, out hit,
                            ManPointer.inst.PickupRange, mask, QueryTriggerInteraction.Ignore))
                        {
                            var bounds = hit.collider.GetComponent<VagueBounds>();
                            if (bounds)
                            {
                                bounds.Grab(toCast);
                            }
                        }
                        break;
                    case ManPointer.Event.RMB:
                        break;
                    case ManPointer.Event.MMB:
                        if (Singleton.playerTank.Vision.GetFirstVisibleTechIsEnemy(ManPlayer.inst.PlayerTeam))
                            return;
                        toCast = ManUI.inst.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(toCast, out hit,
                            ManPointer.inst.PickupRange, mask, QueryTriggerInteraction.Ignore))
                        {
                            var bounds = hit.collider.GetComponent<VagueBounds>();
                            if (bounds)
                            {
                                bounds.GrabCopy(toCast);
                            }
                        }
                        break;
                    case ManPointer.Event.MWheel:
                        break;
                    default:
                        break;
                }
            }
        }
        internal class VagueBounds : MonoBehaviour
        {
            internal BlockBatch batchAssigned;
            internal Vector3 worldCenter => transform.TransformPoint(batchAssigned.GetBounds().center);
            internal VagueBounds mirrorPair;

            private void Update()
            {
                if (batchAssigned.Root && !ManUndo.inst.UndoInProgress)
                {
                    batchAssigned.Root.trans.SetPositionIfChanged(transform.position);
                    batchAssigned.Root.trans.SetRotationIfChanged(transform.rotation);
                    batchAssigned.UpdateHoldNoCheck();
                    ManBuildUtil.PreGrabBlock(batchAssigned.Root);
                }
            }

            private void OnCollisionEnter(Collision col)
            {
                if (col.gameObject)
                {
                    if (col.gameObject.layer == Globals.inst.layerBullet)
                    {
                        DeInit(true);
                    }
                    else 
                    {
                        var block = col.transform.root.GetComponent<TankBlock>();
                        if (block)
                        {
                            if (batchAssigned.batch.Exists(delegate (BlockCache cand) { return cand.inst == block; }))
                                ManBuildUtil.PreGrabBlock(block);
                        }
                    }
                }
            }

            internal VagueBounds Init(BlockBatch BB, bool applyForce, VagueBounds mirror)
            {
                BB.GrabAllIncludingRoot();
                BB.UpdateHoldNoCheck();

                gameObject.layer = Globals.inst.layerPickup;
                gameObject.transform.position = BB.Root.trans.position;
                gameObject.transform.rotation = BB.Root.trans.rotation;

                var bC = gameObject.GetComponent<BoxCollider>();
                if (!bC)
                    bC = gameObject.AddComponent<BoxCollider>();
                bC.center = BB.GetBounds().center;
                bC.size = BB.GetBounds().size;
                var rbody = GetComponent<Rigidbody>();
                if (!rbody)
                    rbody = gameObject.AddComponent<Rigidbody>();
                rbody.centerOfMass = bC.center;
                rbody.inertiaTensor = bC.size;
                rbody.mass = 12;
                rbody.drag = 2;
                rbody.angularDrag = 2;
                rbody.useGravity = false;

                ManBuildUtil.cachePointerHeld = BB.Root;
                if (applyForce)
                    rbody.AddForce(ManBuildUtil.lastDragVelo, ForceMode.VelocityChange);

                if (mirror)
                {
                    mirror.mirrorPair = this;
                    mirrorPair = mirror;
                }

                batchAssigned = BB;
                DebugArchitech.Info(gameObject.name + " - Added new VagueBounds of size " + bC.size + ", center " + bC.center);
                return this;
            }

            internal void Grab(Ray toCast)
            {
                if (ManPointer.inst.DraggingItem)
                {
                    DebugArchitech.Info("Could not Grab " + gameObject.name + " - We are already holding something");
                    return;
                }

                Vector3 newCenterPos = Vector3.zero;
                if (RaycastAll(toCast, out TankBlock best))
                {
                    newCenterPos = batchAssigned.GetLocalPositionOfBlock(best);
                    batchAssigned.BatchCenterOnNoGrab(best);
                }
                TankBlock root = batchAssigned.Root;

                DebugArchitech.Info(gameObject.name + " - Grabbed");

                ManPointer.inst.ReplaceHeldItem(root.visible);
                ManBuildUtil.TryPushToCursorBatch(batchAssigned);
                if (mirrorPair)
                {
                    DebugArchitech.Info(mirrorPair.gameObject.name + " - Grabbed(Mirror)");
                    //TankBlock newRoot = batchAssigned.TryGetBlockFromLocalPosition(newCenterPos); // will need better maths for this
                    //mirrorPair.batchAssigned.BatchCenterOnNoGrab(newRoot);
                    ManBuildUtil.TryPushToMirrorBatch(mirrorPair.batchAssigned);
                    mirrorPair.DeInit(false);
                }
                DeInit(false);
            }

            internal void GrabCopy(Ray toCast)
            {
                if (ManPointer.inst.DraggingItem)
                {
                    DebugArchitech.Info("Could not GrabCopy " + gameObject.name + " - We are already holding something");
                    return;
                }

                if (RaycastAll(toCast, out TankBlock best))
                {
                    batchAssigned.BatchCenterOnNoGrab(best);
                }


                if (TryCopy(out BlockBatch batch))
                {
                    DebugArchitech.Info(gameObject.name + " - Grabbed Duplicate");
                    ManPointer.inst.ReplaceHeldItem(batch.Root.visible);
                    ManBuildUtil.TryPushToCursorBatch(batch);
                    if (ManBuildUtil.IsMirroring)
                    {
                        if (mirrorPair && mirrorPair.TryCopy(out BlockBatch batchM))
                        {
                            DebugArchitech.Info(gameObject.name + " - Grabbed Duplicate(Mirror)");
                            ManBuildUtil.TryPushToMirrorBatch(batchM);
                        }
                        // Need a big function for this
                        //else if (TryCopyMirrored(out BlockBatch batch2))
                        //    ManBuildUtil.TryPushToMirrorBatch(batch);
                    }
                }
            }

            internal bool TryCopy(out BlockBatch batch)
            {
                batch = null;
                if (!TechUtils.IsBlockAvailInInventory(Singleton.playerTank, batchAssigned.Root.BlockType, true))
                    return false;

                Transform trans = batchAssigned.Root.trans;
                TankBlock newRoot = ManLooseBlocks.inst.HostSpawnBlock(batchAssigned.Root.BlockType, 
                    trans.position, trans.rotation);
                ManBuildUtil.PreGrabBlock(newRoot);
                batch = new BlockBatch(newRoot);
                List<TankBlock> blocks = batchAssigned.batch.ConvertAll(x => x.inst).ToList();
                foreach (var item in blocks)
                {
                    if (TechUtils.IsBlockAvailInInventory(Singleton.playerTank, item.BlockType, true))
                    {
                        Transform trans2 = item.trans;
                        TankBlock newTemp = ManLooseBlocks.inst.HostSpawnBlock(item.BlockType,
                            trans2.position, trans2.rotation);
                        batch.Add(BlockCache.CenterOn(newRoot, newTemp));
                    }
                    else
                        return false;
                }
                return true;
            }

            internal void DeInit(bool dropAllToo)
            {
                if (dropAllToo)
                {
                    if (batchAssigned.Root)
                        ManBuildUtil.PostGrabBlock(batchAssigned.Root);
                    batchAssigned.DropAllButRoot();
                }
                DebugArchitech.Assert(!InStasis.Remove(batchAssigned), "Architech: VagueBounds - " +
                    "The batch that was assigned did not exists in InStasis");
                DestroyImmediate(gameObject);
            }


            internal bool RaycastAll(Ray ray, out TankBlock closest)
            {
                List<TankBlock> blocks = batchAssigned.batch.ConvertAll(x => x.inst).ToList();
                blocks.Insert(0, batchAssigned.Root);
                float dist = maxDistSustain;
                closest = null;
                foreach (var item in blocks)
                {
                    if (RaycastBlock(item, ray, out RaycastHit hit))
                    {
                        if (hit.distance < dist)
                        {
                            dist = hit.distance;
                            closest = item;
                        }
                    }
                }
                return closest;
            }

            internal bool RaycastBlock(TankBlock TB, Ray ray, out RaycastHit closest)
            {
                closest = new RaycastHit { distance = maxDistSustain };
                var CS = TB.GetComponent<ColliderSwapper>();
                if (CS.CollisionEnabled)
                {
                    RaycastRescurse(TB.trans, ray, ref closest);
                }
                else
                {
                    CS.EnableCollision(true);
                    RaycastRescurse(TB.trans, ray, ref closest);
                    CS.EnableCollision(false);
                }
                return closest.distance != maxDistSustain;
            }

            internal void RaycastRescurse(Transform trans, Ray ray, ref RaycastHit closest)
            {
                var col = trans.GetComponent<Collider>();
                RaycastHit hitInfo;
                if (col && col.Raycast(ray, out hitInfo, maxDistSustain))
                {
                    if (hitInfo.distance < closest.distance)
                        closest = hitInfo;
                }

                int childs = trans.childCount;
                for (int step = 0; step < childs; step++)
                {
                    RaycastRescurse(trans.GetChild(step), ray, ref closest);
                }
            }
        }
    }
}
