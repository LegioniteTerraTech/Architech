using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Architech
{
    /// <summary>
    /// Saves and loads block serialization SEPERATELY
    /// </summary>
    public class BlockEncoder
    {
        private static TankPreset.BlockSpec Copier = new TankPreset.BlockSpec();
        public static void GetSerialDataText(TankBlock block, ref TankPreset.BlockSpec context)
        {
            block.serializeTextEvent.Send(true, context, block.IsAttached);
        }
        public static void SetSerialDataText(TankBlock block, ref TankPreset.BlockSpec context)
        {
            block.serializeTextEvent.Send(false, context, block.IsAttached);
        }



        private static TankPreset.BlockSpec CachedContext = new TankPreset.BlockSpec();
        private static void CopySerialDataText(TankBlock block)
        {
            GetSerialDataText(block, ref CachedContext);
        }
        private static void PasteSerialDataText(TankBlock block)
        {
            if (CachedContext.GetBlockType() == block.BlockType)
            {
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.AcceptMission);
                SetSerialDataText(block, ref CachedContext);
            }
            else
            {
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.MissionFailed);
            }
        }
    }
}
