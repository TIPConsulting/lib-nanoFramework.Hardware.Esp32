//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace nanoFramework.Hardware.Esp32.TouchPad
{
    /// <summary>
    /// Base class for toch pad sensors: interrupt or polling driven, using single or multiple gpio pins
    /// </summary>
    public abstract class TouchPadBase // : IDisposable
    {
        /// <summary>
        /// Map of GPIO to touch pad number.
        /// ESP32 offers up to 10 capacitive IOs that detect changes in capacitance on touch sensors due to finger contact or proximity.
        /// Index is the Touch pin number, value is the raw GPIO pin number
        /// </summary>
        private static readonly byte[] _gpioTouchPadArr = new byte[]
        {
            4,	//T0
			0,	//T1
			2,	//T2
			15,	//T3
			13,	//T4
			12,	//T5
			14,	//T6
			27,	//T7
			33,	//T8
			32	//T9
		};

        private readonly int _gpioPinNumber = -1;
        private readonly int _touchPadIndex = -1;

        /// <summary>
        /// The selected pin as raw GPIO pin number
        /// </summary>
        public int GpioPinNumber => _gpioPinNumber;
        /// <summary>
        /// The selected pin as touch pin index
        /// </summary>
        public int TouchPadIndex => _touchPadIndex;

        /// <summary>
        /// Constructs and initializes object based on given configuration.
        /// </summary>
        /// <param name="PinNumber">A valid touch pad pin</param>
        /// <param name="config">Touchpad configuration object.  Changes to the config will have no effect on constructed objects</param>
        /// <exception cref="ArgumentException">Invalid touchpad pin number</exception>
        /// <exception cref="ArgumentNullException">Configuration parameter is null</exception>
        /// <exception cref="Exception">One of native calls returned not OK return value.</exception>
        protected TouchPadBase(int PinNumber, TouchPadConfigBase config)
        {
            if (config.PinSelectMode == TouchPinSelectMode.GpioIndex)
            {
                LoadPinsFromGpio(PinNumber, ref _gpioPinNumber, ref _touchPadIndex);
            }
            else
            {
                LoadPinsFromTouch(PinNumber, ref _gpioPinNumber, ref _touchPadIndex);
            }

            if (_gpioPinNumber == -1 || _touchPadIndex == -1)
                throw new ArgumentException(nameof(PinNumber));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!TouchPadInit())
                throw new Exception();

            SetFsmMode();

            if (!TouchPadSetVoltage(config.TouchHighVolt, config.TouchLowVolt, config.TouchVoltAtten))
                throw new Exception();

            if (!TouchPadConfig(_touchPadIndex, config.TouchThreshNoUse))
                throw new Exception();

            if (!TouchPadSetFilterPeriod(config.TouchPadFilterTouchPeriod))
                throw new Exception();
        }

        private void LoadPinsFromGpio(int PinNumber, ref int GpioPinNumber, ref int TouchPadIndex)
        {
            int touchIdx = -1;
            for (int i = 0; i < 10; ++i)
            {
                if (_gpioTouchPadArr[i] == PinNumber)
                {
                    touchIdx = i;
                    break;
                }
            }

            GpioPinNumber = PinNumber;
            TouchPadIndex = touchIdx;
        }

        private void LoadPinsFromTouch(int PinNumber, ref int GpioPinNumber, ref int TouchPadIndex)
        {
            if (PinNumber > 0 && PinNumber < 9)
            {
                GpioPinNumber = _gpioTouchPadArr[PinNumber];
                TouchPadIndex = PinNumber;
            }
        }

        /// <summary>
        /// Set touch sensor FSM mode, the test action can be triggered by the timer, as well as by the software.
        /// The default FSM mode is <see cref="TouchFsmMode.Software"/>. If you want to use interrupt trigger mode, 
        /// then set it to <see cref="TouchFsmMode.Timer"/> after calling init function.
        /// </summary>
        public void SetFsmMode()
        {
            if (!TouchPadSetFsmMode(TouchFsmMode.Timer))
                throw new Exception();
        }


        #region IDisposable Support
        private bool _disposedValue;

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // remove the pin from the event listner
                    //s_eventListener.RemovePin(_pinNumber);
                }

                DisposeNative();

                _disposedValue = true;
            }
        }

#pragma warning disable 1591
        ~TouchPadBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            if (!_disposedValue)
            {
                Dispose(true);

                GC.SuppressFinalize(this);
            }
        }
        #endregion

        #region external calls to native implementations

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool TouchPadInit(); //esp_err_t touch_pad_init() https://docs.espressif.com/projects/esp-idf/en/latest/api-reference/peripherals/touch_pad.html#_CPPv414touch_pad_initv

        [MethodImpl(MethodImplOptions.InternalCall)]
        protected extern bool TouchPadSetFsmMode(TouchFsmMode touchFsmMode); //esp_err_t touch_pad_set_fsm_mode(touch_fsm_mode_tmode) https://docs.espressif.com/projects/esp-idf/en/latest/api-reference/peripherals/touch_pad.html#_CPPv422touch_pad_set_fsm_mode16touch_fsm_mode_t

        [MethodImpl(MethodImplOptions.InternalCall)]
        protected extern bool TouchPadSetVoltage(TouchHighVolt touchHighVolt, TouchLowVolt touchLowVolt, TouchVoltAtten touchVoltAtten); //esp_err_t touch_pad_set_voltage(touch_high_volt_trefh, touch_low_volt_trefl, touch_volt_atten_tatten) 

        [MethodImpl(MethodImplOptions.InternalCall)]
        protected extern bool TouchPadConfig(int touchPadIndex, ushort threshold); //esp_err_t touch_pad_config(touch_pad_ttouch_num, uint16_t threshold)

        [MethodImpl(MethodImplOptions.InternalCall)]
        protected extern bool TouchPadSetFilterPeriod(uint newPeriodMs); //esp_err_t touch_pad_set_filter_period(uint32_t new_period_ms

        [MethodImpl(MethodImplOptions.InternalCall)]
        protected extern ushort TouchPadRead(int touchPadIndex); //esp_err_t touch_pad_read(touch_pad_ttouch_num, uint16_t* touch_value)

        [MethodImpl(MethodImplOptions.InternalCall)]
        protected extern ushort TouchPadReadFiltered(int touchPadIndex); //esp_err_t touch_pad_read_filtered(touch_pad_ttouch_num, uint16_t *touch_value)

        //[MethodImpl(MethodImplOptions.InternalCall)]
        //protected extern ushort TouchPadSetReadRawData(int touchPadIndex); //esp_err_t touch_pad_read_raw_data(touch_pad_ttouch_num, uint16_t *touch_value)

        [MethodImpl(MethodImplOptions.InternalCall)]
        protected extern bool TouchPadSetThresh(int touchPadIndex, ushort threshold); //esp_err_t touch_pad_set_thresh(touch_pad_ttouch_num, uint16_t threshold)


        //todo
        //[MethodImpl(MethodImplOptions.InternalCall)]
        //protected extern bool TouchPadIsrRegister(todo); //esp_err_t touch_pad_isr_register(intr_handler_tfn, void* arg)

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void DisposeNative();
        #endregion

    }
}