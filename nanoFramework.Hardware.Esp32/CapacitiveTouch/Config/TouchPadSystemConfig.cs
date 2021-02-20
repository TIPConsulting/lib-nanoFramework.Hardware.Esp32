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
        private TouchHighVolt _touchHighVolt = TouchHighVolt.H2V7;
        private TouchLowVolt _touchLowVolt = TouchLowVolt.L0V5;
        private TouchVoltAtten _touchVoltAtten = TouchVoltAtten.A1V;
        private TouchTriggerMode _touchTriggerMode = TouchTriggerMode.TOUCH_TRIGGER_BELOW;
        private uint _touchPadFilterTouchPeriod = 10;
        private TouchPadReadMode _touchPadReadMode = TouchPadReadMode.Unfiltered;

        /// <summary>
        /// Charging voltage threshold of the internal circuit of the touch sensor.
        /// Default = <see cref="TouchHighVolt.H2V7"/>
        /// </summary>
        public TouchHighVolt TouchHighVolt
        {
            get => _touchHighVolt;
            set => _touchHighVolt = value;
        }

        /// <summary>
        /// Discharging voltage threshold of the internal circuit of the touch sensor.
        /// Default = <see cref="TouchLowVolt.L0V5"/>
        /// </summary>
        public TouchLowVolt TouchLowVolt
        {
            get => _touchLowVolt;
            set => _touchLowVolt = value;
        }

        /// <summary>
        /// High voltage attenuation value (HATTEN).
        /// Default = <see cref="TouchVoltAtten.A1V"/>
        /// </summary>
        public TouchVoltAtten TouchVoltAtten
        {
            get => _touchVoltAtten;
            set => _touchVoltAtten = value;
        }

        /// <summary>
        /// Touchpad interrupt trigger mode
        /// Default = <see cref="TouchTriggerMode.TOUCH_TRIGGER_BELOW"/>
        /// </summary>
        public TouchTriggerMode TouchTriggerMode
        {
            get => _touchTriggerMode;
            set => _touchTriggerMode = value;
        }

        /// <summary>
        /// Touch pad filter calibration period, in ms.
        /// Default = 10
        /// </summary>
        public uint TouchPadFilterTouchPeriod
        {
            get => _touchPadFilterTouchPeriod;
            set => _touchPadFilterTouchPeriod = value;
        }

        /// <summary>
        /// Determine if the pin value reader should use a filtered or unfiltered datastream.
        /// Default: unfiltered
        /// </summary>
        public TouchPadReadMode TouchReadMode
        {
            get => _touchPadReadMode;
            set => _touchPadReadMode = value;
        }
    }
}
