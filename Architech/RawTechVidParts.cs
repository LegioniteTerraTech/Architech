﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;

namespace Architech
{
    internal class RawTechVidParts
    {
        public const string RawtechCore = "{\"t\":\"GSO_Cab_111\",\"p\":{\"x\":0.0,\"y\":0.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Block_111\",\"p\":{\"x\":-1.0,\"y\":0.0,\"z\":0.0},\"r\":11}|{\"t\":\"EXP_Block_Faired_111\",\"p\":{\"x\":-1.0,\"y\":1.0,\"z\":0.0},\"r\":11}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":0.0},\"r\":2}|{\"t\":\"EXP_Circuits_Wire_Red_111\",\"p\":{\"x\":-2.0,\"y\":2.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Display_Pixel_RGB_111\",\"p\":{\"x\":-2.0,\"y\":3.0,\"z\":0.0},\"r\":3}|{\"t\":\"EXP_Circuits_Input_Button_111\",\"p\":{\"x\":-2.0,\"y\":1.0,\"z\":0.0},\"r\":11}|{\"t\":\"EXP_Circuits_Value_Subtract_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":-1.0},\"r\":5}|{\"t\":\"EXP_Circuits_Wire_Red_111\",\"p\":{\"x\":-2.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Signal_Transmitter_121\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":-1.0},\"r\":10}|{\"t\":\"EXP_Circuits_Display_Pixel_RGB_111\",\"p\":{\"x\":-2.0,\"y\":3.0,\"z\":1.0},\"r\":3}|{\"t\":\"EXP_Circuits_Display_Pixel_RGB_111\",\"p\":{\"x\":-2.0,\"y\":3.0,\"z\":2.0},\"r\":3}|{\"t\":\"EXP_Circuits_Display_Pixel_RGB_111\",\"p\":{\"x\":-2.0,\"y\":3.0,\"z\":3.0},\"r\":3}|{\"t\":\"EXP_Circuits_Display_Pixel_RGB_111\",\"p\":{\"x\":-2.0,\"y\":3.0,\"z\":4.0},\"r\":3}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":1.0},\"r\":3}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":2.0},\"r\":3}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":2.0},\"r\":3}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":3.0},\"r\":3}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":3.0},\"r\":3}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":3.0},\"r\":3}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":4.0},\"r\":3}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":4.0},\"r\":3}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":4.0},\"r\":3}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":2.0,\"y\":3.0,\"z\":4.0},\"r\":3}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":2.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":2.0,\"y\":3.0,\"z\":3.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":2.0,\"y\":3.0,\"z\":2.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":3.0,\"y\":3.0,\"z\":4.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":3.0,\"y\":3.0,\"z\":3.0},\"r\":0}";
        //public const string RawtechCore = "{\"t\":\"GSO_Cab_111\",\"p\":{\"x\":0.0,\"y\":0.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Value_Subtract_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":-1.0},\"r\":5}|{\"t\":\"EXP_Circuits_Wire_Red_111\",\"p\":{\"x\":-2.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":0.0},\"r\":2}|{\"t\":\"EXP_Circuits_Wire_Red_111\",\"p\":{\"x\":-2.0,\"y\":2.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Display_Pixel_RGB_111\",\"p\":{\"x\":-2.0,\"y\":3.0,\"z\":0.0},\"r\":3}|{\"t\":\"EXP_Circuits_Signal_Transmitter_121\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":-1.0},\"r\":10}|{\"t\":\"EXP_Circuits_Input_Button_111\",\"p\":{\"x\":-2.0,\"y\":1.0,\"z\":0.0},\"r\":11}|{\"t\":\"EXP_Block_Faired_111\",\"p\":{\"x\":-1.0,\"y\":1.0,\"z\":0.0},\"r\":11}|{\"t\":\"EXP_Block_111\",\"p\":{\"x\":-1.0,\"y\":0.0,\"z\":0.0},\"r\":11}";
        private static List<RawBlockMem> _core = null;
        public static List<RawBlockMem> core
        {
            get
            {
                if (_core == null)
                    _core = RawTechBase.JSONToMemoryExternal(RawtechCore);
                return _core;
            }
        }

        public const string RawtechMemSet = "{\"t\":\"EXP_Circuits_Logic_AND_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":0.0},\"r\":11}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":1.0},\"r\":10}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Value_Add_111\",\"p\":{\"x\":1.0,\"y\":2.0,\"z\":-1.0},\"r\":1}|{\"t\":\"EXP_Circuits_Value_Multiply_111\",\"p\":{\"x\":0.0,\"y\":1.0,\"z\":1.0},\"r\":11}|{\"t\":\"EXP_Circuits_Input_Value_111\",\"p\":{\"x\":0.0,\"y\":0.0,\"z\":1.0},\"r\":11}|{\"t\":\"EXP_Circuits_Input_Value_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":2.0},\"r\":10}|{\"t\":\"EXP_Circuits_Input_Value_111\",\"p\":{\"x\":0.0,\"y\":1.0,\"z\":0.0},\"r\":8}";
        private static List<RawBlockMem> _mems = null;
        public static List<RawBlockMem> mems
        {
            get {
                if (_mems == null)
                    _mems = RawTechBase.JSONToMemoryExternal(RawtechMemSet);
                return _mems;
            }
        }

        public const string RawtechConnect2 = "{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":2.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":3.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":4.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":5.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":6.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":6.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":2.0,\"z\":6.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":4.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":2.0,\"z\":4.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":2.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":2.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":3.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":4.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":5.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":5.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":5.0},\"r\":0}";
        private static List<RawBlockMem> _conex = null;
        public static List<RawBlockMem> conex
        {
            get
            {
                if (_conex == null)
                    _conex = RawTechBase.JSONToMemoryExternal(RawtechConnect2);
                return _conex;
            }
        }

        //public const string RawtechSkyward = "{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":2.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":4.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":5.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":4.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":5.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":6.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":7.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":8.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":1.0,\"y\":8.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":-1.0,\"y\":8.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":6.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":7.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":7.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":7.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":7.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":7.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":7.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":7.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":7.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":6.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":5.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":4.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":4.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":4.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":4.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":4.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":1.0},\"r\":0}";
        public const string RawtechSkyward = "{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":4.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":4.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":4.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":3.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":4.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":4.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":3.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":2.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":2.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":3.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":0.0,\"y\":2.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":5.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":5.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":5.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":1.0,\"y\":5.0,\"z\":1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":1.0,\"y\":6.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":0.0,\"y\":6.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Green_111\",\"p\":{\"x\":-1.0,\"y\":6.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":5.0,\"z\":0.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":5.0,\"z\":-1.0},\"r\":0}|{\"t\":\"EXP_Circuits_Wire_Blue_111\",\"p\":{\"x\":-1.0,\"y\":5.0,\"z\":1.0},\"r\":0}";
        private static List<RawBlockMem> _skyTower = null;
        public static List<RawBlockMem> skyTower
        {
            get
            {
                if (_skyTower == null)
                    _skyTower = RawTechBase.JSONToMemoryExternal(RawtechSkyward);
                return _skyTower;
            }
        }
    }
}
