//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;

namespace nanoFramework.Hardware.Esp32.TouchPad
{
    /// <summary>
    /// TouchPad sensor using interrupts
    /// </summary>
    public sealed class TouchPad : TouchPadBase
    {

        /// <summary>
        /// Event triggered when the touchpad value changes
        /// </summary>
        [Obsolete("ValueChanged event is not yet implemented", true)]
        public event TouchPadValueChangedEventHandler ValueChanged
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }


        //TODO: Wire up interrupt event handler
        //Use touch_pad_isr_register / touch_pad_isr_deregister
        //Interrupt should trigger a local function that then invokes ValueChanged
        //This will allow us to match the native func signature and still pass sender/arg to managed event
        //https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv422touch_pad_isr_register14intr_handler_tPv
        //https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv424touch_pad_isr_deregisterPFvPvEPv

        /// <summary>
        /// Simplified constructor which use default settings
        /// </summary>
        /// <param name="pinNumber">Valid touch pads pin number</param>
        public TouchPad(int pinNumber) : this(pinNumber, new TouchPadConfig())
        {
        }

        /// <summary>
        /// Constructor that allow fine tuning of pit settings
        /// </summary>
        /// <param name="pinNumber">Valid touch pads pin number</param>
        /// <param name="config">Configuration settings</param>
        public TouchPad(int pinNumber, TouchPadConfig config) : base(pinNumber, config)
        {
            SetTouchPadTriggerThreshold(config.InterruptThresholdValue);
        }

        /// <summary>
        /// Sets touch pad interrupt threshold.
        /// </summary>
        /// <exception cref="Exception">Native call returned not OK return value.</exception>
        public void SetTouchPadTriggerThreshold(float interruptThreshold)
        {
            ushort touchPadValue = TouchPadReadFiltered(TouchPadIndex);
            if (!TouchPadSetThresh(TouchPadIndex, (ushort)(touchPadValue * interruptThreshold)))
                throw new Exception();
        }

    }
}
