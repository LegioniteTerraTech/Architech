﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Architech
{
    public struct BlockCache
    {   // Save the blocks!
        private static TankPreset.BlockSpec BlockDataHolder = new TankPreset.BlockSpec();

        public TankBlock inst;
        public BlockTypes t;
        public Vector3 p;
        public OrthoRotation r;
        public Dictionary<int, Module.SerialData> serial;
        public List<string> serial2;

        public BlockCache(TankBlock Active)
        {
            inst = Active;
            t = Active.BlockType;
            p = Vector3.zero;
            r = OrthoRotation.identity;
            if (ManGameMode.inst.GetCurrentGameType() == ManGameMode.GameType.MainGame)
            {
                BlockDataHolder.textSerialData = new List<string>();
                Active.SerializeToText(true, BlockDataHolder, Active.tank);
                serial2 = BlockDataHolder.textSerialData;
                serial = null;
            }
            else
            {
                BlockDataHolder.saveState = new Dictionary<int, Module.SerialData>();
                Active.Serialize(true, BlockDataHolder);
                serial = BlockDataHolder.saveState;
                serial2 = null;
            }
        }

        public BlockCache(TankBlock Active, BlockTypes Type, Vector3 localPosition, OrthoRotation localRotation)
        {
            inst = Active;
            t = Type;
            p = localPosition;
            r = localRotation;
            if (ManGameMode.inst.GetCurrentGameType() == ManGameMode.GameType.MainGame)
            {
                BlockDataHolder.textSerialData = new List<string>();
                Active.SerializeToText(true, BlockDataHolder, Active.tank);
                serial2 = BlockDataHolder.textSerialData;
                serial = null;
            }
            else
            {
                BlockDataHolder.saveState = new Dictionary<int, Module.SerialData>();
                Active.Serialize(true, BlockDataHolder);
                serial = BlockDataHolder.saveState;
                serial2 = null;
            }
            TidyUp();
        }

        /// <summary>
        /// CLONE
        /// </summary>
        /// <param name="Active"></param>
        public BlockCache(BlockCache toCopy, TankBlock duplicate)
        {
            inst = duplicate;
            t = toCopy.t;
            p = toCopy.p;
            r = toCopy.r;
            serial = toCopy.serial;
            serial2 = toCopy.serial2;
        }

        public static BlockCache CenterOn(TankBlock mainHeld, TankBlock toSet)
        {
            return new BlockCache(toSet, toSet.BlockType,
                mainHeld.trans.InverseTransformPoint(toSet.trans.position),
                ManBuildUtil.SetCorrectRotation(ManBuildUtil.InvTransformRot(toSet.trans, mainHeld.trans)));
        }
        public BlockCache CenterOn(TankBlock mainHeld)
        {
            DebugArchitech.Assert(inst, "BlockCache - Called while inst is NULL");
            p = mainHeld.trans.InverseTransformPoint(inst.trans.position);
            r = ManBuildUtil.SetCorrectRotation(ManBuildUtil.InvTransformRot(inst.trans, mainHeld.trans));
            return this;
        }



        public void HoldInRelation(TankBlock master)
        {
            try
            {
                inst.trans.SetRotationIfChanged(master.trans.rotation * r);
                inst.trans.SetPositionIfChanged(master.trans.position + (master.trans.rotation * p));
            }
            catch
            {
                DebugArchitech.LogError("BlockBatch: HoldInRelation - Block was expected but it was null!  Was it destroyed in tranzit!?");
            }
        }

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

    public class BlockCacheEnum : IEnumerator
    {
        public BlockCache[] blockC;
        int num = -1;

        public BlockCacheEnum(BlockCache[] list)
        {
            blockC = list;
        }

        public bool MoveNext()
        {
            num++;
            return num < blockC.Length;
        }

        public void Reset()
        {
            num = -1;
        }

        object IEnumerator.Current => Current;

        public BlockCache Current
        {
            get
            {
                try
                {
                    return blockC[num];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
