using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using TerraTechETCUtil;
using Newtonsoft.Json;

namespace Architech
{
    /// <summary>
    /// cause bad apple
    /// </summary>
    public class TechVideoEncoder : MonoBehaviour
    {
        public enum TechVideoState
        {
            Off,
            Active,
            Stop,
            GenerateTech
        }

        public static TechVideoEncoder inst = null;
        public static ModuleHUDSliderControl controller = null;
        private static TankPreset.BlockSpec Copier;
        private static Queue<int[]> BatchedInfo = new Queue<int[]>();
        public static IntVector2 resolution = new IntVector2(10, 8);
        public static TechVideoState active = TechVideoState.Off;
        public static bool busy = false;
        public static bool button = false;
        //public static int SkipFrames = 8;
        private static WaitForEndOfFrame WFEOF = new WaitForEndOfFrame();
        private static VideoPlayer VP = null;
        private static IntVector2 Dim = new IntVector2(480, 360);
        private static IntVector2 Dim2 = new IntVector2(480, 360);

        private static string GetImg = "C:/Users/Legionite/Desktop/OverBatch1.png";
        private static string GetVid = "C:/Users/Legionite/Desktop/Touhou - Bad Apple.mov";
        private static string Dest = "C:/Users/Legionite/Desktop/FileVidExport.txt";

        public static void BuildTechFromVideo()
        {
            if (busy)
                return;
            if (inst == null)
            {
                inst = Instantiate(new GameObject("TechVideoEncoder_INST"), null).AddComponent<TechVideoEncoder>();
                Dim = new IntVector2(Display.main.renderingWidth, Display.main.renderingHeight);
            }
            active++;
            switch (active)
            {
                case TechVideoState.Off:
                    break;
                case TechVideoState.Active:
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        RecordImage();
                        active = TechVideoState.GenerateTech;
                    }
                    else if(Input.GetKey(KeyCode.RightShift))
                        RecordVideo();
                    else
                    {
                        //BuildDataNodes(Vector3.up * 4);
                        active = TechVideoState.Stop;
                    }
                    break;
                case TechVideoState.Stop:
                    Stop();
                    break;
                case TechVideoState.GenerateTech:
                    if (Encodeable)
                        SetupValues();
                    BuildDataNodes(Vector3.up * 4);
                    break;
                default:
                    active = TechVideoState.Off;
                    break;
            }
        }

        public static void Stop()
        {
            Cleanup(null);
        }
        public static Texture2D TEMP = null;
        public static void RecordImage()
        {
            BatchedInfo.Clear();
            TEMP = FileUtils.LoadTexture(GetImg);
            CaptureImage(TEMP);
            File.WriteAllText(Dest, JsonConvert.SerializeObject(BatchedInfo));
            DebugArchitech.Log("Finished render");
            ManSFX.inst.PlayMiscSFX(ManSFX.MiscSfxType.CheatCode);
        }
        public static void RecordVideo()
        {
            BatchedInfo.Clear();
            VP = Instantiate(new GameObject("Vidplayers"), null).AddComponent<VideoPlayer>();
            VP.url = GetVid;
            VP.sendFrameReadyEvents = true;
            VP.playbackSpeed = 1;//15;
            VP.frameReady += WaitFrame;
            VP.loopPointReached += Cleanup;
            var cam = Camera.current;
            VP.targetCamera = cam;
            VP.renderMode = VideoRenderMode.CameraNearPlane;
            VP.audioOutputMode = VideoAudioOutputMode.AudioSource;
            VP.Prepare();
            VP.prepareCompleted += PreparedVid;
            //RenTex = new RenderTexture(480, 360, 16);
            DebugArchitech.LogConsole("Starting rendergrab");
        }
        public static void PreparedVid(VideoPlayer source)
        {
            DebugArchitech.LogConsole("Starting video");
            ManSFX.inst.PlayMiscSFX(ManSFX.MiscSfxType.CheatCode);
            VP.Play();
        }
        public static IEnumerator CaptureFrame()
        {
            // yield return WFS;
            if (!GetFrameNow)
                yield return null;
            yield return WFEOF;
            try
            {
                GetFrameNow = false;
                CaptureImage();
                VP.Play();
            }
            catch
            {
            }
            yield break;
        }
        public static void CaptureImage(Texture2D Tex = null)
        {
            try
            {
                int frameSet = 0;
                int horzSet = 0;
                int[] dataMapping16 = new int[5] { 0, 0, 0, 0, 0 };
                if (Tex == null)
                {
                    Tex = new Texture2D(Dim.x, Dim.y, TextureFormat.RGB24, false);
                    Tex.ReadPixels(new Rect(0, 0, Dim.x, Dim.y), 0, 0);
                    Tex.Apply();
                }
                TextureScale.Point(Tex, resolution.x, resolution.y);

                for (int x = 0; x < resolution.x; x++)
                {
                    for (int y = 0; y < resolution.y; y++)
                    {
                        dataMapping16[frameSet] *= 2;
                        dataMapping16[frameSet] += (Tex.GetPixel(x, y).r > 0.5f) ? 1 : 0;
                    }

                    frameSet++;
                    if (frameSet > 4)
                    {
                        frameSet = 0;
                    }
                }
                BatchedInfo.Enqueue(dataMapping16);
                DebugArchitech.Log("Finished frame " + BatchedInfo.Count + " with values: " + dataMapping16[0] + "  |  " +
                    dataMapping16[1] + "  |  " + dataMapping16[2] + "  |  " + dataMapping16[3] + "  |  " + dataMapping16[4]);
            }
            catch { }
        }
        public static bool GetFrameNow = false;
        public static void WaitFrame(VideoPlayer source, long frameIdx)
        {
            VP.Pause();
            GetFrameNow = true;
            inst.StartCoroutine(CaptureFrame());
        }
        public static void Cleanup(VideoPlayer source)
        {
            /*
            var cam = Camera.current;
            cam.targetTexture = null;
            */
            //VP.targetTexture.Release();
            if (VP)
            {
                Destroy(VP.gameObject);
                VP = null;
                /*
                StringBuilder SB = new StringBuilder();
                foreach (var item in BatchedInfo)
                {
                    SB.AppendLine(item[0] + "  |  " + item[1] + "  |  " + item[2] + "  |  " + item[3] + "  |  " + item[4]);
                }*/
                File.WriteAllText(Dest, JsonConvert.SerializeObject(BatchedInfo));
                inst.StopAllCoroutines();
                DebugArchitech.Log("Finished render");
                ManSFX.inst.PlayMiscSFX(ManSFX.MiscSfxType.CheatCode);
            }
        }


        public static bool SubCircuits = false;
        public static Tank Encodeable = null;
        public static int TechRendererMaxCharges = 0;
        public static int TechRendererChargesUsed = 0;
        public static void InitCircuitListener()
        {
            if (!SubCircuits)
            {
                SubCircuits = true;
                Circuits.PostSlowUpdate.Subscribe(CircuitListenerTick);
                TechRendererChargesUsed = 0;
            }
        }
        public static void CircuitListenerTick()
        {
            TechRendererChargesUsed++;
            if (TechRendererChargesUsed >= TechRendererMaxCharges -2)
            {
                TechRendererChargesUsed = 0;
                CircuitRebufferIfNeeded();
            }
        }

        public static void PushButton()
        {
            if (button)
            {
                try
                {
                    ModuleCircuit_Input_Button bu = Encodeable.blockman.IterateBlockComponents<ModuleCircuit_Input_Button>().FirstOrDefault();
                    bu.PowerControlSetting = true;
                    InvokeHelper.Invoke((x) => { x.PowerControlSetting = false; }, 0.0001f, bu);
                }
                catch (Exception)
                {
                }
            }
        }
        public static void CircuitRebufferIfNeeded()
        {
            if (BatchedInfo.Any() && active == TechVideoState.GenerateTech)
            {
                BuildDataNodes(Vector3.zero);
            }
            else
            {
                Circuits.PostSlowUpdate.Unsubscribe(CircuitListenerTick);
                SubCircuits = false;
            }
        }

        public const int TechDimLim = 2;//12;
        public static IntVector3 TechLim = new IntVector3(9, TechDimLim, TechDimLim);
        public static IntVector3 TechSize = new IntVector3(2, 3, 5);
        public static IntVector3 Direction = new IntVector3(1, 1, 1);
        public static IntVector3 offsetBlue = new IntVector3(-1, -1, 0);
        public static List<TankBlock> indexes = new List<TankBlock>();
        public static void UpdateDataNodes(RawTech existingTank)
        {
            int step = 0;
            while (BatchedInfo.Any())
            {
                var item = BatchedInfo.Dequeue();
                for (int stepI = 0; stepI < 5; stepI++)
                {
                    int val = item[stepI];
                    if (indexes.Count < step + 2)
                    {
                        DebugArchitech.LogConsole("Partial set Data node");
                        existingTank.DecodeSerialData(Encodeable);
                        PushButton();
                        return;
                    }
                    if (stepI == 1)
                    {
                        SetData8Display(indexes[step], val / 256);
                        SetData8Display(indexes[step + 1], val & 255);
                    }
                    else
                    {
                        SetData8Display(indexes[step], val & 255);
                        SetData8Display(indexes[step + 1], val / 256);
                    }
                    step += 2;
                }
            }
            DebugArchitech.LogConsole("Finished set Data node");
            existingTank.DecodeSerialData(Encodeable);
            PushButton();
        }
        public static void GenerateDataNodes(RawTech existingTank, Vector3 spawnPosition)
        {
            IntVector3 offset = IntVector3.zero;
            TechRendererMaxCharges = 0;
            Encodeable = null;
            AddCache.Clear();
            indexes.Clear();
            var mems = RawTechVidParts.core;
            for (int step = 0; step < mems.Count; step++)
            {
                var item = mems[step];
                SpawnAttach(existingTank, item.typeSlow, item.p, new OrthoRotation(item.r));
            }
            int dataSetCount = 0;
            int LastVal = -1;
            while (BatchedInfo.Any())
            {
                var item = BatchedInfo.Dequeue();
                foreach (var node in item)
                {
                    if (Direction.x < 0)
                    {
                        TechRendererMaxCharges++;
                        BuildDataNode(existingTank, offset, true, node);
                        offset.x -= TechSize.x;
                    }
                    else
                    {
                        TechRendererMaxCharges++;
                        BuildDataNode(existingTank, offset, false, node);
                        offset.x += TechSize.x;
                    }
                    if (offset.x > TechLim.x || offset.x < 0)
                    {
                        if (Direction.z < 0)
                            offset.z -= TechSize.z;
                        else
                            offset.z += TechSize.z;
                        if (offset.z > TechLim.z || offset.z < 0)
                        {
                            if (offset.z < 0)
                            {
                                offset.z += TechSize.z;
                                Direction.z = 1;
                            }
                            else
                            {
                                offset.z -= TechSize.z;
                                Direction.z = -1;
                            }
                            Upwards(existingTank, offset);
                            offset.y += TechSize.y;
                            if (offset.y > TechLim.y)
                            {
                                active = TechVideoState.Stop;
                                foreach (var cached in AddCache)
                                {
                                    int index = existingTank.savedTech.Count;
                                    if (dataSetCount == 2)
                                    {
                                        SetData8(index, existingTank, 256);
                                        dataSetCount = 0;
                                        LastVal = -1;
                                    }
                                    else
                                    {
                                        if (LastVal == -1)
                                            LastVal = cached.Value;
                                        SetData8Display(index, existingTank, LastVal & 255);
                                        LastVal /= 256;
                                        dataSetCount++;
                                    }
                                    SpawnAttach(existingTank, cached.Key.typeSlow, cached.Key.p, new OrthoRotation(cached.Key.r));
                                }
                                if (indexes.Count % 2 == 1)
                                    throw new Exception("We should NEVER have indexes count that is odd, tf");
                                DebugArchitech.LogConsole("Worked on partial data node");
                                Encodeable = existingTank.SpawnRawTech(spawnPosition, ManPlayer.inst.PlayerTeam, Vector3.forward);
                                if (Encodeable)
                                {
                                    SetupValues();
                                    existingTank.DecodeSerialData(Encodeable);
                                }
                                DebugArchitech.LogConsole("Spawned partial Data node");
                                TechRendererMaxCharges /= 5;
                                PushButton();
                                return;
                            }
                        }
                        else
                        {
                            if (Direction.z < 0)
                                Router(existingTank, offset + new IntVector3(0, 0, TechSize.z), false);
                            else
                                Router(existingTank, offset - new IntVector3(0, 0, TechSize.z), true);
                        }
                        if (offset.x < 0)
                        {
                            offset.x += TechSize.x;
                            Direction.x = 1;
                        }
                        else
                        {
                            offset.x -= TechSize.x;
                            Direction.x = -1;
                        }
                    }
                }
            }
            foreach (var cached in AddCache)
            {
                int index = existingTank.savedTech.Count;
                if (dataSetCount == 2)
                {
                    SetData8(index, existingTank, 256);
                    dataSetCount = 0;
                    LastVal = -1;
                }
                else
                {
                    if (LastVal == -1)
                        LastVal = cached.Value;
                    SetData8Display(index, existingTank, LastVal & 255);
                    LastVal /= 256;
                    dataSetCount++;
                }
                SpawnAttach(existingTank, cached.Key.typeSlow, cached.Key.p, new OrthoRotation(cached.Key.r));
            }
            if (indexes.Count % 2 == 1)
                throw new Exception("We should NEVER have indexes count that is odd, tf");
            DebugArchitech.LogConsole("Finished Data node");
            Encodeable = existingTank.SpawnRawTech(spawnPosition, ManPlayer.inst.PlayerTeam, Vector3.forward);
            if (Encodeable)
            {
                SetupValues();
                existingTank.DecodeSerialData(Encodeable);
            }
            DebugArchitech.LogConsole("Spawned Data node");
            TechRendererMaxCharges /= 5;
            PushButton();
        }
        public static void SetupValues()
        {
            int index = 0;
            int dataSetCount = 0;
            indexes.Clear();
            foreach (var cached in Encodeable.blockman.IterateBlocks())
            {
                if (cached.BlockType == BlockTypes.EXP_Circuits_Input_Value_111)
                {
                    if (dataSetCount == 2)
                    {
                        dataSetCount = 0;
                    }
                    else
                    {
                        indexes.Add(cached);
                        dataSetCount++;
                    }
                }
                index++;
            }
        }
        public static void BuildDataNodes(Vector3 spawnPosition)
        {
            var existingTank = new RawTech();
            existingTank.IgnoreChecks = true;
            //SpawnAttach(existingTank, BlockTypes.GSOCockpit_111, IntVector3.zero, Quaternion.identity);
            //existingTank.AddBlock(BlockTypes.GSOCockpit_111, Vector3.zero, Quaternion.identity);
            if (!BatchedInfo.Any() && File.Exists(Dest))
            {
                BatchedInfo = JsonConvert.DeserializeObject<Queue<int[]>>(File.ReadAllText(Dest));
            }
            InitCircuitListener();
            Direction = IntVector3.one;
            if (Encodeable && Encodeable.visible.isActive)
            {
                UpdateDataNodes(existingTank);
            }
            else
            {
                GenerateDataNodes(existingTank, spawnPosition);
            }
        }
        public static void Upwards(RawTech existingTank, IntVector3 offset)
        {
            var mems = RawTechVidParts.skyTower;
            for (int step = 0; step < mems.Count; step++)
            {
                var item = mems[step];
                SpawnAttach(existingTank, item.typeSlow, item.p + offset, Quaternion.identity);
            }
        }
        public static void Router(RawTech existingTank, IntVector3 offset, bool forward)
        {
            var mems = RawTechVidParts.conex;
            for (int step = 0; step < mems.Count; step++)
            {
                var item = mems[step];
                var type = item.typeSlow;
                Quaternion rotPos = (!forward) ? Quaternion.AngleAxis(180, Vector3.up) : Quaternion.identity;
                SpawnAttach(existingTank, type, (rotPos * item.p) + offset, Quaternion.identity);
            }
        }
        public static List<IntVector3> Selected = new List<IntVector3>();
        public static List<KeyValuePair<RawBlock, int>> AddCache = new List<KeyValuePair<RawBlock, int>>();
        public static void BuildDataNode(RawTech existingTank, IntVector3 offset, bool flipped, int data)
        {
            if (existingTank == null)
                throw new NullReferenceException("BuildDataNode - Tank null");
            var mems = RawTechVidParts.mems;
            for (int step = 0; step < mems.Count; step++)
            {
                var item = mems[step];
                var type = item.typeSlow;
                Quaternion rotPos = flipped ? Quaternion.AngleAxis(180, Vector3.up) : Quaternion.identity;
                OrthoRotation rotBlock = new OrthoRotation(item.r);
                Quaternion rotCombined = ManBuildUtil.SetCorrectRotation(rotBlock, rotPos);
                Vector3 SetPos = (rotPos * item.p) + offset;
                if (type == BlockTypes.EXP_Circuits_Input_Value_111)
                {
                    AddCache.Add(new KeyValuePair<RawBlock, int>(new RawBlock(type, SetPos, rotCombined), data));
                }
                else
                {
                    SpawnAttach(existingTank, type, SetPos, rotCombined);
                }
            }
        }
        private static void SpawnAttach(RawTech tank, BlockTypes blockType, IntVector3 pos, Quaternion rot)
        {
            if (tank.TryAddBlock(blockType, pos, rot, true))
            {
            }
            else
            {
                //DebugArchitech.Log("Failed to attach " + blockType.ToString() + " at " + pos);
                /*
                throw new OperationCanceledException("TechVideoEncoder.SpawnAttach failed because a block \"" + blockType.ToString() +
                        "\" failed to attach properly!");
                */
            }
        }

        private static TankBlock block;
        /// <summary>
        /// At best
        ///   Only 0-999 supported!
        /// </summary>
        public static TankBlock SetData8(int index, RawTech existingTank, float data)
        {
            if (block == null)
            {
                block = ManSpawn.inst.SpawnBlock(BlockTypes.EXP_Circuits_Input_Value_111, Vector3.down * 100, Quaternion.identity);
                controller = block.GetComponent<ModuleHUDSliderControl>();
                /*
                Copier = new TankPreset.BlockSpec
                {
                    block = block.name,
                    m_BlockType = BlockTypes.EXP_Circuits_Input_Value_111,
                    m_SkinID = 0,
                    m_VisibleID = 300,
                    orthoRotation = 0,
                    position = IntVector3.zero,
                    saveState = null,
                    textSerialData = new List<string>(),
                };
                */
            }
            controller.SetValueMultiplayerSafe(Mathf.RoundToInt(data));
            Copier = TankPreset.BlockSpec.GetBlockConfigState(block);
            //Copier.saveState = new Dictionary<int, Module.SerialData>();
            //Copier.Store(controller, "moduleHUDSliderControl_Value", (float)outData);
            existingTank.EncodeSerialDataToThis(index, Copier);
            return block;
        }
        /// <summary>
        /// At best
        ///   Only 0-999 supported!
        ///   1 -> 3
        ///   2 -> 1
        ///   3 -> 2
        /// </summary>
        public static TankBlock SetData8Display(int index, RawTech existingTank, int data)
        {
            if (block == null)
            {
                block = ManSpawn.inst.SpawnBlock(BlockTypes.EXP_Circuits_Input_Value_111, Vector3.down * 100, Quaternion.identity);
                controller = block.GetComponent<ModuleHUDSliderControl>();
                /*
                Copier = new TankPreset.BlockSpec
                {
                    block = block.name,
                    m_BlockType = BlockTypes.EXP_Circuits_Input_Value_111,
                    m_SkinID = 0,
                    m_VisibleID = 300,
                    orthoRotation = 0,
                    position = IntVector3.zero,
                    saveState = null,
                    textSerialData = new List<string>(),
                };
                */
            }
            int inData = data;
            int nextData = 0;
            for (int step = 0; step < 8; step++)
            {
                nextData *= 4;
                nextData += inData % 4;
                inData /= 4;
            }
            int outData = 0;
            bool flipY = true;
            for (int step = 0; step < 8; step++)
            {
                outData *= 4;
                if (flipY)
                {
                    switch (nextData % 4)
                    {
                        case 1:
                            outData += 1;
                            break;
                        case 2:
                            outData += 3;
                            break;
                        case 3:
                            outData += 2;
                            break;
                    }
                }
                else
                {
                    switch (nextData % 4)
                    {
                        case 1:
                            outData += 3;
                            break;
                        case 2:
                            outData += 1;
                            break;
                        case 3:
                            outData += 2;
                            break;
                    }
                }
                nextData /= 4;
                flipY = !flipY;
            }
            //DebugArchitech.Log("outData is " + outData);
            controller.SetValueMultiplayerSafe(outData);
            Copier = TankPreset.BlockSpec.GetBlockConfigState(block);
            //Copier.saveState = new Dictionary<int, Module.SerialData>();
            //Copier.Store(controller, "moduleHUDSliderControl_Value", (float)outData);
            existingTank.EncodeSerialDataToThis(index, Copier);
            return block;
        }
        public static TankBlock SetData8Display(TankBlock block, int data)
        {
            int inData = data;
            int nextData = 0;
            for (int step = 0; step < 8; step++)
            {
                nextData *= 4;
                nextData += inData % 4;
                inData /= 4;
            }
            int outData = 0;
            bool flipY = true;
            for (int step = 0; step < 8; step++)
            {
                outData *= 4;
                if (flipY)
                {
                    switch (nextData % 4)
                    {
                        case 1:
                            outData += 1;
                            break;
                        case 2:
                            outData += 3;
                            break;
                        case 3:
                            outData += 2;
                            break;
                    }
                }
                else
                {
                    switch (nextData % 4)
                    {
                        case 1:
                            outData += 3;
                            break;
                        case 2:
                            outData += 1;
                            break;
                        case 3:
                            outData += 2;
                            break;
                    }
                }
                nextData /= 4;
                flipY = !flipY;
            }
            //DebugArchitech.Log("outData is " + outData);
            //Copier.saveState = new Dictionary<int, Module.SerialData>();
            block.GetComponent<ModuleHUDSliderControl>().SetValueMultiplayerSafe(outData);
            return block;
        }
    }
}
