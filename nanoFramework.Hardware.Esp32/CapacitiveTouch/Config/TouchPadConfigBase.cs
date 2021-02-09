﻿//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32.TouchPad
{
    /// <summary>
    /// Configuration parameters for derived classes of TouchPadBase
    /// </summary>
    public class TouchPadConfigBase
    {
        /// <summary>
        /// Charging voltage threshold of the internal circuit of the touch sensor.
        /// Default = <see cref="TouchHighVolt.H2V7"/>
        /// </summary>
        public TouchHighVolt TouchHighVolt { get; set; } = TouchHighVolt.H2V7;

        /// <summary>
        /// Discharging voltage threshold of the internal circuit of the touch sensor.
        /// Default = <see cref="TouchLowVolt.L0V5"/>
        /// </summary>
        public TouchLowVolt TouchLowVolt { get; set; } = TouchLowVolt.L0V5;

        /// <summary>
        /// High voltage attenuation value (HATTEN).
        /// Default = <see cref="TouchVoltAtten.A1V"/>
        /// </summary>
        public TouchVoltAtten TouchVoltAtten { get; set; } = TouchVoltAtten.A1V;

        /// <summary>
        /// Interrupt threshold.
        /// Default = 0
        /// </summary>
        public ushort TouchThreshNoUse { get; set; } = 0;

        /// <summary>
        /// Touch pad filter calibration period, in ms.
        /// Default = 10
        /// </summary>
        public uint TouchPadFilterTouchPeriod { get; set; } = 10;

        /// <summary>
        /// Select the method in which pins are indexed.
        /// Pins can be indexed by GPIO number or touch index
        /// </summary>
        public TouchPinSelectMode PinSelectMode;
    }
}
