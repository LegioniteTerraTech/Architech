using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using TerraTechETCUtil;

namespace Architech
{
    public class CursorChanger : MonoBehaviour
    {
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
        public static CursorChangeHelper.CursorChangeCache Cache;
        private static bool AddedNewCursors = false;
        public static CursorChangeHelper.CursorChangeCache CursorIndexCache => Cache.CursorIndexCache;

        public static void AddNewCursors()
        {
            if (AddedNewCursors)
                return;
            string DLLDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent.ToString();
            if (ResourcesHelper.TryGetModContainer("Architech - Mirror Mod", out ModContainer MC))
            {
                Cache = CursorChangeHelper.GetCursorChangeCache(DLLDirectory, "Cursor_Icons", MC,
                    new KeyValuePair<string, bool>("OverTech", true),
                    new KeyValuePair<string, bool>("HoldTech", true),
                    new KeyValuePair<string, bool>("OverMirror", true),
                    new KeyValuePair<string, bool>("HoldMirror", true),
                    new KeyValuePair<string, bool>("OverBatch", true),
                    new KeyValuePair<string, bool>("HoldBatch", true),
                    new KeyValuePair<string, bool>("OverMirrorBatch", true),
                    new KeyValuePair<string, bool>("HoldMirrorBatch", true),
                    new KeyValuePair<string, bool>("MirroredPainting", true),
                    new KeyValuePair<string, bool>("OverMirroredPainting", true),
                    new KeyValuePair<string, bool>("PointerMirror", false),
                    new KeyValuePair<string, bool>("PointerBatch", false),
                    new KeyValuePair<string, bool>("PointerMirrorBatch", false)
                    );
            }
            else
            {
                DebugArchitech.Assert(true, "CursorChanger: AddNewCursors - Could not find ModContainer for Architech!");
            }
            AddedNewCursors = true;
        }
    }
}
