//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32
{
    /// <summary>
    /// Configuration parameters for individual touchpad objects
    /// </summary>
    public class TouchPadConfig
    {
        
        private ushort touchThreshNoUse = 0;
        private TouchPinSelectMode pinSelectMode = TouchPinSelectMode.GpioIndex;
        private float interruptThresholdValue = 2f / 3f;


        /// <summary>
        /// Interrupt threshold.
        /// Default = 0
        /// </summary>
        public ushort TouchThreshNoUse
        {
            get => touchThreshNoUse;
            set => touchThreshNoUse = value;
        }

        /// <summary>
        /// Select the method in which pins are indexed.
        /// Pins can be indexed by GPIO number or touch index
        /// </summary>
        public TouchPinSelectMode PinSelectMode
        {
            get => pinSelectMode;
            set => pinSelectMode = value;
        }

        //TODO: re-add ReadMode (filter/not filtered)

        /// <summary>
        /// The threshold to trigger interrupt when the pad is touched.
        /// Sensor value must be greater than this value to trigger an interrupt
        /// </summary>
        /// <remarks>
        /// By default, use 2/3 of read value as the threshold to trigger interrupt when the pad is touched.
        /// </remarks>
        public float InterruptThresholdValue
        {
            get => interruptThresholdValue;
            set => interruptThresholdValue = value;
        }
    }
}
