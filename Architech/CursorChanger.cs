using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Architech
{
    public class CursorChanger : MonoBehaviour
    {
        static FieldInfo existingCursors = typeof(MousePointer).GetField("m_CursorDataSets", BindingFlags.NonPublic | BindingFlags.Instance);

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
        private static bool AddedNewCursors = false;
        private static List<Texture2D> CursorTextureCache = new List<Texture2D>();
        public static int[] CursorIndexCache = new int[13];

        public static void AddNewCursors()
        {
            if (AddedNewCursors)
                return;
            MousePointer MP = FindObjectOfType<MousePointer>();
            DebugArchitech.Assert(!MP, "BuildUtil: AddNewCursors - THE CURSOR DOES NOT EXIST!");
            string DLLDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent.ToString();
            DebugArchitech.LogDevOnly("BuildUtil: AddNewCursors - Path: " + DLLDirectory);
            try
            {
                int LODLevel = 0;
                MousePointer.CursorDataSet[] cursorLODs = (MousePointer.CursorDataSet[])existingCursors.GetValue(MP);
                foreach (var item in cursorLODs)
                {
                    List<MousePointer.CursorData> cursorTypes = item.m_CursorData.ToList();

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
            AddedNewCursors = true;
        }

        private static void TryAddNewCursor(List<MousePointer.CursorData> lodInst, string DLLDirectory, string name, int lodLevel, Vector2 center, int cacheIndex)
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
                MousePointer.CursorData CD = new MousePointer.CursorData
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

    }
}
