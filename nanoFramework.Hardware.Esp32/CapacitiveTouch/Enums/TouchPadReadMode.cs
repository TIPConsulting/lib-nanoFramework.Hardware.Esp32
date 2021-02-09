//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32.TouchPad
{
    /// <summary>
    /// Determine touch pad value read mode for polling applications
    /// </summary>
    public enum TouchPadReadMode
    {
        /// <summary>
        /// Read filtered sensor values.  Used to select touch_pad_read_filtered
        /// </summary>
        Filtered = 0,

        /// <summary>
        /// Read raw sensor values.  Used to select touch_pad_read
        /// </summary>
        //TODO: implement TouchPadSetReadRawData in TouchPadBase
        //Raw
    }
}
