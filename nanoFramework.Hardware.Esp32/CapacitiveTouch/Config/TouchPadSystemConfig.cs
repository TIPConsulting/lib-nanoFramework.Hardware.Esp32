//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32
{
    /// <summary>
    /// Configuration parameters for 
    /// </summary>
    public class TouchPadSystemConfig
    {
        private TouchHighVolt touchHighVolt = TouchHighVolt.H2V7;
        private TouchLowVolt touchLowVolt = TouchLowVolt.L0V5;
        private TouchVoltAtten touchVoltAtten = TouchVoltAtten.A1V;
        private uint touchPadFilterTouchPeriod = 10;


        /// <summary>
        /// Charging voltage threshold of the internal circuit of the touch sensor.
        /// Default = <see cref="TouchHighVolt.H2V7"/>
        /// </summary>
        public TouchHighVolt TouchHighVolt
        {
            get => touchHighVolt;
            set => touchHighVolt = value;
        }

        /// <summary>
        /// Discharging voltage threshold of the internal circuit of the touch sensor.
        /// Default = <see cref="TouchLowVolt.L0V5"/>
        /// </summary>
        public TouchLowVolt TouchLowVolt
        {
            get => touchLowVolt;
            set => touchLowVolt = value;
        }

        /// <summary>
        /// High voltage attenuation value (HATTEN).
        /// Default = <see cref="TouchVoltAtten.A1V"/>
        /// </summary>
        public TouchVoltAtten TouchVoltAtten
        {
            get => touchVoltAtten;
            set => touchVoltAtten = value;
        }

        /// <summary>
        /// Touch pad filter calibration period, in ms.
        /// Default = 10
        /// </summary>
        public uint TouchPadFilterTouchPeriod
        {
            get => touchPadFilterTouchPeriod;
            set => touchPadFilterTouchPeriod = value;
        }

    }
}
