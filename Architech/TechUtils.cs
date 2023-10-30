using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Architech
{
    internal static class TechUtils
    {
        private static List<BlockTypes> typesMissing = new List<BlockTypes>();
        private static List<BlockTypes> typesToRepair = new List<BlockTypes>();
        private static List<BlockTypes> GetMissingBlockTypesExt(List<BlockCache> Mem, List<TankBlock> cBlocks)
        {
            typesMissing.Clear();
            int toFilter = Mem.Count();
            for (int step = 0; step < toFilter; step++)
            {
                typesToRepair.Add(Mem.ElementAt(step).t);
            }
            typesToRepair = typesToRepair.Distinct().ToList();

            int toFilter2 = typesToRepair.Count();
            for (int step = 0; step < toFilter2; step++)
            {
                int present = cBlocks.FindAll(delegate (TankBlock cand) { return typesToRepair[step] == cand.BlockType; }).Count;

                int mem = Mem.FindAll(delegate (BlockCache cand) { return typesToRepair[step] == cand.t; }).Count;
                if (mem > present)// are some blocks not accounted for?
                    typesMissing.Add(typesToRepair[step]);
            }
            typesToRepair.Clear();
            return typesMissing;
        }
        /// <summary>
        /// Builds a Tech instantly, no requirements
        /// </summary>
        /// <param name="tank"></param>
        /// <param name="TechMemor"></param>
        public static void TurboconstructExt(Tank tank, List<BlockCache> Mem, List<TankBlock> provided, bool fullyCharge = true)
        {
            DebugArchitech.Log("TACtical_AI:  DesignMemory: Turboconstructing " + tank.name);
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
        private static void TurboRepairExt(Tank tank, List<BlockCache> Mem, ref List<BlockTypes> typesMissing, ref List<TankBlock> provided)
        {
            List<TankBlock> cBlocks = tank.blockman.IterateBlocks().ToList();
            int savedBCount = Mem.Count;
            int cBCount = cBlocks.Count;
            if (savedBCount != cBCount)
            {

                //Debug.Log("TACtical AI: TurboRepair - Attempting to repair from infinity - " + typesToRepair.Count());
                if (!TryAttachExistingBlockFromListExt(tank, Mem, ref typesMissing, ref provided))
                    DebugArchitech.Log("TACtical AI: TurboRepair - attach attempt failed");
            }
            return;
        }
        private static bool TryAttachExistingBlockFromListExt(Tank tank, List<BlockCache> mem, ref List<BlockTypes> typesMissing, ref List<TankBlock> foundBlocks, bool denySD = false)
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
        internal static bool AttemptBlockAttachExt(Tank tank, BlockCache template, TankBlock canidate)
        {
            //Debug.Log("TACtical_AI: (AttemptBlockAttachExt) AI " + tank.name + ":  Trying to attach " + canidate.name + " at " + template.CachePos);
            return Singleton.Manager<ManLooseBlocks>.inst.RequestAttachBlock(tank, canidate, template.p, template.r);
        }

        internal static void AttemptBlockDetachExt(Tank tank, TankBlock toRemove)
        {
            ManLooseBlocks.inst.HostDetachBlock(toRemove, false, true);
        }



        public static bool IsBlockAvailInInventory(Tank tank, BlockTypes blockType, int count = 1, bool taking = false)
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
                                return Singleton.Manager<NetInventory>.inst.GetQuantity(blockType) >= count;
                            }
                        }
                        else
                        {
                            if (Singleton.Manager<SingleplayerInventory>.inst.IsAvailableToLocalPlayer(blockType))
                            {
                                return Singleton.Manager<SingleplayerInventory>.inst.GetQuantity(blockType) >= count;
                            }
                        }
                    }
                    catch
                    {
                        DebugArchitech.Log("BuildUtil: " + tank.name + ":  Tried to repair but block " + blockType.ToString() + " was not found!");
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
                            if (availQuant >= count)
                            {
                                availQuant -= count;
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
                            if (availQuant >= count)
                            {
                                availQuant -= count;
                                isAvail = true;
                                Singleton.Manager<SingleplayerInventory>.inst.SetBlockCount(blockType, availQuant);
                            }
                        }
                    }
                }
                catch
                {
                    DebugArchitech.Log("BuildUtil: " + tank.name + ":  Tried to repair but block " + blockType.ToString() + " was not found!");
                }
            }
            return isAvail;
        }
        
    }
}
