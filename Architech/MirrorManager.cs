using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Architech
{
    internal class MirrorManager : MonoBehaviour
    {
        private static MirrorManager inst;
        private List<BlockTypes> cachedNonVanilla;
        private List<BlockTypes> Added;

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.B))
                Reboot();
        }

        private static void Reboot()
        {
            DeInit();
            Init();
        }

        public static void Init()
        {
            if (inst)
                return;
            inst = new GameObject("MirrorMan").AddComponent<MirrorManager>();
            inst.Added = new List<BlockTypes>();
            if (inst.cachedNonVanilla == null)
                inst.cachedNonVanilla = GetNonVanillaBlocks();
            inst.Invoke("DelayedAdd", 0.01f);
        }
        public static void DeInit()
        {
            if (!inst)
                return;
            inst.CancelInvoke();
            BlockPairsList BPL = Globals.inst.m_BlockPairsList;
            foreach (var item in BPL.m_BlockPairs)
            {
                if (inst.Added.Contains(item.m_Block))
                {
                    inst.Added.Remove(item.m_Block);
                }
                else if (inst.Added.Contains(item.m_PairedBlock))
                {
                    inst.Added.Remove(item.m_PairedBlock);
                }
            }
            DebugArchitech.Assert(inst.Added.Count > 0, "Architech - MirrorManager: Failed to remove all added pairs!");
            inst.Added = null;
            inst.cachedNonVanilla = null;
            Destroy(inst.gameObject);
            inst = null;
        }
        private void DelayedAdd()
        {
            foreach (var item in cachedNonVanilla)
            {
                TankBlock TB = ManSpawn.inst.GetBlockPrefab(item);
                if (TB)
                {
                    var mirrorComp = TB.GetComponent<Mirrorable>();
                    if (mirrorComp)
                    {
                        AddBlock(mirrorComp, item);
                    }
                }
            }
            DebugArchitech.Log("Architech - MirrorManager: Loaded " + Added.Count + " blocks");
        }

        private static List<BlockTypes> GetNonVanillaBlocks()
        {
            int maxVanilla = Enum.GetValues(typeof(BlockTypes)).Length;
            List<BlockTypes> names = ManSpawn.inst.GetLoadedTankBlockNames().ToList();
            names.RemoveAll(delegate (BlockTypes BTC) { return (int)BTC < maxVanilla; });
            return names;
        }

        /// <summary>
        /// Can Hash Collision, check if failiure
        /// </summary>
        /// <param name="toSearch"></param>
        /// <param name="BT"></param>
        /// <returns></returns>
        private bool FindBlockTypeFromName(string toSearch, out BlockTypes BT)
        {
            int hash = toSearch.GetHashCode();
            BT = BlockTypes.GSOAIController_111;
            foreach (var item in cachedNonVanilla)
            {
                TankBlock TB = ManSpawn.inst.GetBlockPrefab(item);
                if (TB)
                {
                    string nameUI = StringLookup.GetItemName(ObjectTypes.Block, (int)item);
                    /*
                    if (ManMods.inst.IsModdedBlock(item))
                    {
                        nameUI = ManMods.inst.FindBlockName((int)item);
                    }
                    else
                    {
                        nameUI = 
                    }
                    */
                    //Debug.Log(" looking up " + nameUI);
                    if (!nameUI.NullOrEmpty() && nameUI.GetHashCode() == hash)
                    {
                        BT = item;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool AlreadyExists(BlockTypes BT)
        {
            BlockPairsList BPL = Globals.inst.m_BlockPairsList;
            foreach (var item in BPL.m_BlockPairs)
            {
                if (item.m_Block == BT)
                    return true;
                else if (item.m_PairedBlock == BT)
                    return true;
            }
            return false;
        }

        public void AddBlock(Mirrorable mirror, BlockTypes hostMirrorType)
        {
            if (!mirror.GetComponent<TankBlock>())
                return;
            if (cachedNonVanilla == null)
                cachedNonVanilla = GetNonVanillaBlocks();

            if (AlreadyExists(hostMirrorType))
                return;
            BlockTypes mirrorBT = BlockTypes.GSOAIController_111;
            BlockTypes BTC;
            try
            {
                if (Enum.TryParse(mirror.MirrorBlockName, out BTC))
                {
                    mirrorBT = BTC;
                }
                else
                {
                    // Search non-vanilla
                    if (FindBlockTypeFromName(mirror.MirrorBlockName, out BTC))
                    {
                        mirrorBT = BTC;
                    }
                }
                if (mirrorBT == BlockTypes.GSOAIController_111)
                {
                    DebugArchitech.Log("Architech - MirrorManager: Could not find any loaded block of such name "
                        + (mirror.MirrorBlockName.NullOrEmpty() ? "NO_NAME" : mirror.MirrorBlockName));
                    return;
                }
                if (AlreadyExists(mirrorBT))
                    return;
                BlockPairsList BPL = Globals.inst.m_BlockPairsList;
                Array.Resize(ref BPL.m_BlockPairs, BPL.m_BlockPairs.Length + 1);
                BPL.m_BlockPairs[BPL.m_BlockPairs.Length - 1] = new BlockPairsList.BlockPairs
                {
                    m_Block = hostMirrorType,
                    m_PairedBlock = mirrorBT,
                };
                Added.Add(hostMirrorType);
                DebugArchitech.Log("Architech - MirrorManager: Registered " + mirror.name);
                return;
            }
            catch (Exception e)
            {
                DebugArchitech.LogError("Architech - MirrorManager: Error " + e);
            }
        }
    }

}
