//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32
{
    /// <summary>
    /// Event raised when TouchPad value changes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="value"></param>
    //TODO: add a new enumerable to EventCategory in lib-nanoFramework.Runtime.Events.
    public delegate void TouchPadValueChangedEventHandler(object sender, ushort value);
}
