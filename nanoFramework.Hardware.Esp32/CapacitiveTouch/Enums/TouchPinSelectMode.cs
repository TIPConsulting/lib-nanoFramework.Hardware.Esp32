//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32
{
    /// <summary>
    /// Determine if the TouchPad pin input is selected by the raw GPIO pin number or the touch pad index
    /// </summary>
    public enum TouchPinSelectMode
    {
        /// <summary>
        /// Select TouchPad pin by its raw GPIO value.
        /// The available touch GPIO pins are 0, 2, 4, 12, 13, 14, 15, 27, 32, 33
        /// </summary>
        GpioIndex = 0,
        /// <summary>
        /// Select TouchPad pin by its touch pin index.
        /// The available indexes are 0-9
        /// </summary>
        TouchIndex
    }
}
