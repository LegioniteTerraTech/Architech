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
                    "OverTech",
                    "HoldTech",
                    "OverMirror",
                    "OverBatch",
                    "HoldBatch",
                    "OverMirrorBatch",
                    "HoldMirrorBatch",
                    "MirroredPainting",
                    "OverMirroredPainting",
                    "PointerMirror",
                    "PointerBatch",
                    "PointerMirrorBatch"
                    );
            }
            else
            {
                DebugArchitech.Assert(true, "CursorChanger: AddNewCursors - Could not find ModContainer for Architech!");
                /*
                MousePointer MP = FindObjectOfType<MousePointer>();
                DebugArchitech.Assert(!MP, "BuildUtil: AddNewCursors - THE CURSOR DOES NOT EXIST!");
                DebugArchitech.LogDevOnly("BuildUtil: AddNewCursors - Path: " + DLLDirectory);
                try
                {
                    int LODLevel = 0;
                    CursorDataTable.CursorDataSet[] cursorLODs = ManUI.inst.CursorDataTable.
                        PlatformSets[CursorDataTable.PlatformSetTypes.PC].m_DataSets;
                    foreach (var item in cursorLODs)
                    {
                        List<CursorDataTable.CursorData> cursorTypes = item.m_CursorData.ToList();

                        TryAddNewCursor(cursorTypes, DLLDirectory, "OverTech", LODLevel, new Vector2(0.5f, 0.5f), 0);// 1
                        TryAddNewCursor(cursorTypes, DLLDirectory, "HoldTech", LODLevel, new Vector2(0.5f, 0.5f), 1);// 2
                        TryAddNewCursor(cursorTypes, DLLDirectory, "OverMirror", LODLevel, new Vector2(0.5f, 0.5f), 2);// 3
                        TryAddNewCursor(cursorTypes, DLLDirectory, "HoldMirror", LODLevel, new Vector2(0.5f, 0.5f), 3);// 4
                        TryAddNewCursor(cursorTypes, DLLDirectory, "OverBatch", LODLevel, new Vector2(0.5f, 0.5f), 4);// 5
                        TryAddNewCursor(cursorTypes, DLLDirectory, "HoldBatch", LODLevel, new Vector2(0.5f, 0.5f), 5);// 6
                        TryAddNewCursor(cursorTypes, DLLDirectory, "OverMirrorBatch", LODLevel, new Vector2(0.5f, 0.5f), 6);// 7
                        TryAddNewCursor(cursorTypes, DLLDirectory, "HoldMirrorBatch", LODLevel, new Vector2(0.5f, 0.5f), 7);// 8
                        TryAddNewCursor(cursorTypes, DLLDirectory, "MirroredPainting", LODLevel, new Vector2(0.3f, 0.3f), 8);// 9
                        TryAddNewCursor(cursorTypes, DLLDirectory, "OverMirroredPainting", LODLevel, new Vector2(0.3f, 0.3f), 9);// 10
                        TryAddNewCursor(cursorTypes, DLLDirectory, "PointerMirror", LODLevel, Vector2.zero, 10);// 11
                        TryAddNewCursor(cursorTypes, DLLDirectory, "PointerBatch", LODLevel, Vector2.zero, 11);// 12
                        TryAddNewCursor(cursorTypes, DLLDirectory, "PointerMirrorBatch", LODLevel, Vector2.zero, 12);// 13

                        item.m_CursorData = cursorTypes.ToArray();
                    }
                }
                catch (Exception e) { DebugArchitech.Log("BuildUtil: AddNewCursors - failed to fetch rest of cursor textures " + e); }
                */
            }
            AddedNewCursors = true;
        }

        /*
        private static void TryAddNewCursor(List<CursorDataTable.CursorData> lodInst, string DLLDirectory, string name, int lodLevel, Vector2 center, int cacheIndex)
        {
            DebugArchitech.LogDevOnly("BuildUtil: AddNewCursors - " + DLLDirectory + " for " + name + " " + lodLevel + " " + center);
            try
            {
                List<FileInfo> FI = new DirectoryInfo(DLLDirectory).GetFiles().ToList();
                Texture2D tex;
                try
                {
                    tex = FileUtils.LoadTexture(FI.Find(delegate (FileInfo cand)
                    { return cand.Name == name + lodLevel + ".png"; }).ToString());
                    CursorTextureCache.Add(tex);
                }
                catch
                {
                    DebugArchitech.Log("BuildUtil: AddNewCursors - failed to fetch cursor texture LOD " + lodLevel + " for " + name);
                    tex = FileUtils.LoadTexture(FI.Find(delegate (FileInfo cand)
                    { return cand.Name == name + "1.png"; }).ToString());
                    CursorTextureCache.Add(tex);
                }
                CursorDataTable.CursorData CD = new CursorDataTable.CursorData
                {
                    m_Hotspot = center * tex.width,
                    m_Texture = tex,
                };
                lodInst.Add(CD);
                CursorIndexCache[cacheIndex] = lodInst.IndexOf(CD);
                DebugArchitech.Info(name + " center: " + CD.m_Hotspot.x + "|" + CD.m_Hotspot.y);
            }
            catch { DebugArchitech.Assert(true, "BuildUtil: AddNewCursors - failed to fetch cursor texture " + name); }
        }
        */
    }
}
