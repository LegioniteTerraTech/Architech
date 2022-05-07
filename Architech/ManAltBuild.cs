using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Architech
{
    /// <summary>
    /// Lets the player build with a far more complex, but non-combat restricted building system.
    /// I don't know why TerraTech consistantly fails to see that it's building system is very  
    ///   mobile and action-oriented.
    ///   
    ///   Let me ASSERT that TerraTech only uses 2 keys and the right-mouse for block rotations -  it's
    ///   main strength is building while singleplayer-driving which NO OTHER vehicle building 
    ///   videogame TO FREAKING DATE can utilise effectively.  By using this system you might as 
    ///   well go off and play another game...
    /// </summary>
    internal class ManAltBuild : MonoBehaviour
    {
        /// So we have to override the normal point-n-click building system. Great.
        /// 
        public static bool IsActive => BuildTarget && cursorBlock && IsBuilding;
        public static TankBlock HeldBlock => cursorBlock;

        internal static ManAltBuild inst;
        private static Tank BuildTarget;
        private static TankBlock cursorBlock;
        private static bool IsBuilding = false;

        private static IntVector3 PosOnTarget = IntVector3.zero;
        private static OrthoRotation RotOnTarget = OrthoRotation.identity;

        private float lastBoundsRad = 1;

        public int MoveNextSameFace => ManInput.inst.GetButtonRepeating(16) ? 1 : (ManInput.inst.GetNegativeButtonRepeating(16) ? -1 : 0);

        public static void Init()
        {
            if (inst)
                return;
            inst = new GameObject("AltBuildUtil").AddComponent<ManAltBuild>();
            ManTechs.inst.PlayerTankChangedEvent.Subscribe(OnPlayerTechChanged);
            ManTechs.inst.TankBlockAttachedEvent.Subscribe(OnBlockPlaced);
            ManTechs.inst.TankBlockDetachedEvent.Subscribe(OnBlockRemoved);
        }
        public static void DeInit()
        {
            if (!inst)
                return;
            ManTechs.inst.TankBlockDetachedEvent.Unsubscribe(OnBlockRemoved);
            ManTechs.inst.TankBlockAttachedEvent.Unsubscribe(OnBlockPlaced);
            ManTechs.inst.PlayerTankChangedEvent.Unsubscribe(OnPlayerTechChanged);
            Destroy(inst.gameObject);
            inst = null;
        }
        public static void OnPlayerTechChanged(Tank tank, bool yes)
        {
        }
        public static void OnBlockPlaced(Tank tank, TankBlock newlyPlaced)
        {
            if (tank != BuildTarget)
                return;
        }
        public static void OnBlockRemoved(Tank tank, TankBlock newlyRemoved)
        {
            if (tank != BuildTarget)
                return;
            inst.PushOutOfBounds(tank, newlyRemoved);
        }

        public void PushOutOfBounds(Tank tank, TankBlock newlyRemoved)
        {
            Vector3 awayVec = (newlyRemoved.trans.position - tank.boundsCentreWorld).normalized * lastBoundsRad;
            newlyRemoved.visible.Teleport(awayVec, newlyRemoved.trans.rotation);
        }


        public bool SetBuildTarget(Tank newTarget)
        {
            if (IsBuilding && BuildTarget)
                return false;
            BuildTarget = newTarget;
            return true;
        }


        public void Rotate(Vector3 direction)
        {
            if (direction.x == 1)
            {
                RotOnTarget = new OrthoRotation(RotOnTarget * Quaternion.LookRotation(Vector3.down, Vector3.forward));
            }
            else if (direction.x == -1)
            {
                RotOnTarget = new OrthoRotation(RotOnTarget * Quaternion.LookRotation(Vector3.up, Vector3.back));
            }

            if (direction.z == 1)
            {
                RotOnTarget = new OrthoRotation(RotOnTarget * Quaternion.LookRotation(Vector3.right, Vector3.up));
            }
            else if (direction.z == -1)
            {
                RotOnTarget = new OrthoRotation(RotOnTarget * Quaternion.LookRotation(Vector3.left, Vector3.up));
            }

            if (direction.z == 1)
            {
                RotOnTarget = new OrthoRotation(RotOnTarget * Quaternion.LookRotation(Vector3.forward, Vector3.left));
            }
            else if (direction.z == -1)
            {
                RotOnTarget = new OrthoRotation(RotOnTarget * Quaternion.LookRotation(Vector3.forward, Vector3.right));
            }
        }

        /// <summary>
        /// Maintain block positions
        /// </summary>
        private void Update()
        {
            if (IsBuilding && CameraManager.inst.IsCurrent<TankCamera>())
            {
                if (BuildTarget)
                {
                    lastBoundsRad = BuildTarget.blockBounds.extents.magnitude;
                }
            }
            else
            {
            }
        }
    }
}
