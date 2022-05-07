using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Architech
{
    internal class MirrorCache
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
}
