//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace nanoFramework.Hardware.Esp32
{
    /// <summary>
    /// Base class for touch pad sensors: interrupt or polling driven
    /// </summary>
    public class TouchPad : IDisposable
    {
        #region Static
        /// <summary>
        /// Map of GPIO to touch pad number.
        /// ESP32 offers up to 10 capacitive IOs that detect changes in capacitance on touch sensors due to finger contact or proximity.
        /// Index is the Touch pin number, value is the raw GPIO pin number
        /// </summary>
        /// <remarks>
        /// This works on ESP32, but not ESP32-S2 (different mapping)
        /// TODO: resolve the pinmap issue
        /// See this link: https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv411touch_pad_t
        /// </remarks>
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
        #endregion Static


        private readonly object _eventLock = new object();
        private readonly int _gpioPinNumber = -1;
        private readonly int _touchPadIndex = -1;
        private readonly TouchPadController _controller;
        private readonly TouchPadConfig _config;
        private TouchPadValueChangedEventHandler _valueChanged;
        private bool _isDisposed;


        /// <summary>
        /// Event triggered when the touchpad value changes
        /// </summary>
        [Obsolete("Not implemented", true)]
        public event TouchPadValueChangedEventHandler ValueChanged
        {
            add
            {
                lock (_eventLock)
                {
                    if (_isDisposed)
                    {
                        throw new ObjectDisposedException();
                    }

                    if (_valueChanged == null)
                    {
                        _controller.RegisterTouchpadHandler(_touchPadIndex, InvokeValueChanged);
                    }
                    _valueChanged += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    if (_isDisposed)
                    {
                        throw new ObjectDisposedException();
                    }

                    _valueChanged -= value;
                    if (_valueChanged == null)
                    {
                        _controller.DeregisterTouchpadHandler(_touchPadIndex, InvokeValueChanged);
                    }
                }
            }
        }



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
        /// <param name="Controller">Parent touch controller</param>
        /// <param name="PinNumber">A valid touch pad pin</param>
        /// <param name="config">Touchpad configuration object.  Changes to the config will have no effect on constructed objects</param>
        /// <exception cref="ArgumentException">Invalid touchpad pin number</exception>
        /// <exception cref="ArgumentNullException">Configuration parameter is null</exception>
        /// <exception cref="Exception">One of native calls returned not OK return value.</exception>
        internal TouchPad(TouchPadController Controller, int PinNumber, TouchPadConfig config)
        {
            //DO NOT invoke native calls here
            //native calls go in INIT()

            if (Controller is null)
            {
                throw new ArgumentNullException(nameof(Controller));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }


            if (config.PinSelectMode == TouchPinSelectMode.GpioIndex)
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

                _gpioPinNumber = PinNumber;
                _touchPadIndex = touchIdx;
            }
            else if (config.PinSelectMode == TouchPinSelectMode.TouchIndex)
            {
                if (PinNumber >= 0 && PinNumber <= 9)
                {
                    _gpioPinNumber = _gpioTouchPadArr[PinNumber];
                    _touchPadIndex = PinNumber;
                }
            }

            if (_gpioPinNumber == -1 || _touchPadIndex == -1)
                throw new ArgumentException(nameof(PinNumber));

            _controller = Controller;
            _config = config;

        }

        private void InvokeValueChanged()
        {
            var val = this.Read();
            _valueChanged?.Invoke(this, val);
        }

        /// <summary>
        /// Initialize native touchpad
        /// </summary>
        public void Init()
        {
            {
                var result = NativeTouchPadConfig(_touchPadIndex, _config.TouchThreshNoUse);
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }

            SetTouchPadTriggerThreshold(_config.InterruptThresholdValue);
        }


        /// <summary>
        /// Sets touch pad interrupt threshold.
        /// </summary>
        /// <exception cref="Exception">Native call returned not OK return value.</exception>
        public void SetTouchPadTriggerThreshold(float interruptThreshold)
        {
            ushort touchPadValue = NativeTouchPadReadFiltered(_touchPadIndex);
            {
                var result = NativeTouchPadSetThresh(_touchPadIndex, (ushort)(touchPadValue * interruptThreshold));
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }
        }

        /// <summary>
        /// Check if the ESP driver detects a touchpad touch
        /// </summary>
        /// <returns></returns>
        [Obsolete("Does not work", true)]
        public bool IsTouched()
        {
            //TODO: why doesnt this work?
            var status = TouchPadController.NativeTouchpadGetStatus();
            return (status & (1 << _touchPadIndex)) != 0;
        }

        /// <summary>
        /// Read current sensor value using the preferred read mode set in the configuration.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// if unfiltered => NativeTouchPadRead,
        /// if filtered => NativeTouchPadReadFiltered
        /// </remarks>
        /// <exception cref="NotImplementedException">Config.ReadMode is set to an unsupported value</exception>
        public ushort Read()
        {
            if (_controller.Config.TouchReadMode == TouchPadReadMode.Unfiltered)
            {
                return NativeTouchPadRead(_touchPadIndex);
            }
            else if (_controller.Config.TouchReadMode == TouchPadReadMode.Filtered)
            {
                //TODO: figure out the issues with NativeTouchPadReadRawData and NativeTouchPadReadFiltered
                return NativeTouchPadReadFiltered(_touchPadIndex);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Read current raw sensor data using the preferred API.
        /// If filtering is disabled, this function is identical to <see cref="Read"/>
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// if unfiltered => NativeTouchPadRead,
        /// if filtered => NativeTouchPadReadRawData
        /// </remarks>
        /// <exception cref="NotImplementedException">Config.ReadMode is set to an unsupported value</exception>
        public ushort ReadRaw()
        {
            if (_controller.Config.TouchReadMode == TouchPadReadMode.Unfiltered)
            {
                return NativeTouchPadRead(_touchPadIndex);
            }
            else if (_controller.Config.TouchReadMode == TouchPadReadMode.Filtered)
            {
                //TODO: figure out the issues with NativeTouchPadReadRawData and NativeTouchPadReadFiltered
                return NativeTouchPadReadRawData(_touchPadIndex);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            lock (_eventLock)
            {
                _valueChanged = null;
                _controller.DeregisterTouchpadHandler(_touchPadIndex, InvokeValueChanged);
            }
            _controller.ClosePin(_touchPadIndex);
        }


        #region external calls to native implementations

        /// <summary>
        /// Configure touch pad interrupt threshold.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_config(touch_pad_ttouch_num, uint16_t threshold)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv416touch_pad_config11touch_pad_t8uint16_t
        /// </remarks>
        /// <param name="touchPadIndex">Touch pad index</param>
        /// <param name="threshold">Interrupt threshold,</param>
        /// <returns>True if successful</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadConfig(int touchPadIndex, ushort threshold);

        /// <summary>
        /// Get touch sensor counter value. 
        /// Each touch sensor has a counter to count the number of charge/discharge cycles. 
        /// When the pad is not ‘touched’, we can get a number of the counter. 
        /// When the pad is ‘touched’, the value in counter will get smaller because of the larger equivalent capacitance.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_read(touch_pad_ttouch_num, uint16_t* touch_value)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv414touch_pad_read11touch_pad_tP8uint16_t
        /// </remarks>
        /// <param name="touchPadIndex">Touch pad index</param>
        /// <returns>Touch sensor value</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern ushort NativeTouchPadRead(int touchPadIndex);

        /// <summary>
        /// Get filtered touch sensor counter value by IIR filter.
        /// touch_pad_filter_start has to be called before calling touch_pad_read_filtered. This function can be called from ISR
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_read_filtered(touch_pad_ttouch_num, uint16_t *touch_value)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv423touch_pad_read_filtered11touch_pad_tP8uint16_t
        /// 
        /// touch_pad_filter_start has to be called before calling touch_pad_read_filtered. This function can be called from ISR
        /// </remarks>
        /// <param name="touchPadIndex">Touch pad index</param>
        /// <returns>Touch sensor value</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern ushort NativeTouchPadReadFiltered(int touchPadIndex);

        /// <summary>
        /// Get raw data (touch sensor counter value) from IIR filter process. Need not request hardware measurements.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_read_raw_data(touch_pad_ttouch_num, uint16_t *touch_value)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv423touch_pad_read_raw_data11touch_pad_tP8uint16_t
        /// 
        /// touch_pad_filter_start has to be called before calling touch_pad_read_raw_data. This function can be called from ISR
        /// </remarks>
        /// <param name="touchPadIndex"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected extern ushort NativeTouchPadReadRawData(int touchPadIndex);

        /// <summary>
        /// Set touch sensor interrupt threshold.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_set_thresh(touch_pad_ttouch_num, uint16_t threshold)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv420touch_pad_set_thresh11touch_pad_t8uint16_t
        /// </remarks>
        /// <param name="touchPadIndex">Touch pad index</param>
        /// <param name="threshold">Threshold of touchpad count, refer to touch_pad_set_trigger_mode to see how to set trigger mode.</param>
        /// <returns>True if successful</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadSetThresh(int touchPadIndex, ushort threshold);
        #endregion

    }
}