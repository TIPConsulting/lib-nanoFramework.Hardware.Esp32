//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32
{
    /// <summary>
    /// Touch sensor low reference voltage
    /// </summary>
    /// <remarks>
    /// Corresponds to touch_low_volt_t
    /// https://docs.espressif.com/projects/esp-idf/en/release-v4.2/esp32s2/api-reference/peripherals/touch_pad.html#_CPPv416touch_low_volt_t
    /// </remarks>
    public enum TouchLowVolt
    {
        /// <summary>
        /// Touch sensor low reference voltage, no change
        /// </summary>
        Keep = -1,

        /// <summary>
        /// Touch sensor low reference voltage, 0.5V
        /// </summary>
        L0V5 = 0,

        /// <summary>
        /// Touch sensor low reference voltage, 0.6V
        /// </summary>
        L0V6,

        /// <summary>
        /// Touch sensor low reference voltage, 0.7V
        /// </summary>
        L0V7,

        /// <summary>
        /// Touch sensor low reference voltage, 0.8V
        /// </summary>
        L0V8,

        /// <summary>
        /// No source documentation available
        /// </summary>
        Max
    }
}
