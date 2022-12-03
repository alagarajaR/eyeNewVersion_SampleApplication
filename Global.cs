using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApplication
{
    public class Global
    {
        public struct GainmMatrix
        {
            public double gain00;
            public double gain01;
            public double gain02;

            public double gain10;
            public double gain11;
            public double gain12;


            public double gain20;
            public double gain21;
            public double gain22;

        }
        public struct DeviceConfig
        {
            public decimal frameRate;
            public decimal exposureTime;
            public decimal analogGain;
            public decimal digitalGain;

            public string exposureAuto;
            public string gainAuto;

            public decimal percentile;
            public int target;
            public int tolerence;

            public GainmMatrix gainMatrix;

        }
    }
}
