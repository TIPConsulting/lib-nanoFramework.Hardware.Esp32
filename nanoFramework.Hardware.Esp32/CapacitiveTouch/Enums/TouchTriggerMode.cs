//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32
{
    /// <summary>
    /// The mode used to trigger touch interrupts
    /// </summary>
    public enum TouchTriggerMode
    {
        /// <summary>
        /// Touch interrupt will happen if counter value is less than threshold.
        /// </summary>
        TOUCH_TRIGGER_BELOW = 0,
        /// <summary>
        /// Touch interrupt will happen if counter value is larger than threshold.
        /// </summary>
        TOUCH_TRIGGER_ABOVE = 1,
        /// <summary>
        /// No source documentation available
        /// </summary>
        TOUCH_TRIGGER_MAX
    }
}
