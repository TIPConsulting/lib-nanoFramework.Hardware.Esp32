//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

//#define FILTERED

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
        /// Initialize the native touchpad featureset
        /// </summary>
        /// <param name="config"></param>
        public static void Init(TouchPadSystemConfig config)
        {
            {
                var result = NativeTouchPadInit();
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }
            Debug.WriteLine("Init: NativeTouchPadInit Complete");

            {
                var result = NativeTouchPadSetFsmMode(TouchFsmMode.Timer);
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }
            Debug.WriteLine("Init: NativeTouchPadSetFsmMode Complete");

            {
                var result = NativeTouchPadSetVoltage(config.TouchHighVolt, config.TouchLowVolt, config.TouchVoltAtten);
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }
            Debug.WriteLine("Init: NativeTouchPadSetVoltage Complete");

            //TODO: set tpad trigger mode
            //TODO: start tpad interrupts via native call
            //TODO: set touchpad threshhold NativeTouchPadSetThresh

#if FILTERED
            {
                var result = NativeTouchPadFilterStart(config.TouchPadFilterTouchPeriod);
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }
            Debug.WriteLine("Init: NativeTouchPadFilterStart Complete");
#endif
        }

        /// <summary>
        /// Deinitialize the native touchpad featureset
        /// </summary>
        public static void Deinit()
        {
            {
                var result = NativeTouchPadDeinit();
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }
        }

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
        private TouchPadValueChangedEventHandler _valueChanged;
        private readonly Action _valueChangedInvoker;
        private readonly int _gpioPinNumber = -1;
        private readonly int _touchPadIndex = -1;
        private readonly TouchPadConfig _config;
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
                    if (_valueChanged == null)
                    {
                        var result = NativeTouchPadIsrRegister(_valueChangedInvoker);
                        if (result != EspNativeError.OK)
                        {
                            throw new Exception(result.ToString());
                        }
                    }
                    _valueChanged += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _valueChanged -= value;
                    if (_valueChanged == null)
                    {
                        var result = NativeTouchPadIsrDeregister(_valueChangedInvoker);
                        if (result != EspNativeError.OK)
                        {
                            throw new Exception(result.ToString());
                        }
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
        /// <param name="PinNumber">A valid touch pad pin</param>
        /// <param name="config">Touchpad configuration object.  Changes to the config will have no effect on constructed objects</param>
        /// <exception cref="ArgumentException">Invalid touchpad pin number</exception>
        /// <exception cref="ArgumentNullException">Configuration parameter is null</exception>
        /// <exception cref="Exception">One of native calls returned not OK return value.</exception>
        public TouchPad(int PinNumber, TouchPadConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));


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
            else
            {
                if (PinNumber > 0 && PinNumber < 10)
                {
                    _gpioPinNumber = _gpioTouchPadArr[PinNumber];
                    _touchPadIndex = PinNumber;
                }
            }

            if (_gpioPinNumber == -1 || _touchPadIndex == -1)
                throw new ArgumentException(nameof(PinNumber));

            _valueChangedInvoker = InvokeValueChanged;
            _config = config;

            {
                var result = NativeTouchPadConfig(_touchPadIndex, config.TouchThreshNoUse);
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }
        }

        private void InvokeValueChanged()
        {
            _valueChanged?.Invoke(this, NativeTouchPadRead(this.TouchPadIndex));
        }

        /// <summary>
        /// Sets touch pad interrupt threshold.
        /// </summary>
        /// <exception cref="Exception">Native call returned not OK return value.</exception>
        public void SetTouchPadTriggerThreshold(float interruptThreshold)
        {
            ushort touchPadValue = NativeTouchPadReadFiltered(TouchPadIndex);
            {
                var result = NativeTouchPadSetThresh(TouchPadIndex, (ushort)(touchPadValue * interruptThreshold));
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }
        }

        /// <summary>
        /// Read current sensor value using the preferred read mode set in the configuration
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">Config.ReadMode is set to an unsupported value</exception>
        public ushort Read()
        {
            //TODO: add plumbing to allow filtered/unfiltered reads
            //Must be synchronised with system Init()
            //-- If filtered read is requested here, then filtering must be enabled in Init

            //if (_config.ReadMode == TouchPadReadMode.Filtered)
            //{
            //    return NativeTouchPadReadRawData(TouchPadIndex);
            //}
            //else if (_config.ReadMode == TouchPadReadMode.Unfiltered)
            //{
            //    return NativeTouchPadRead(TouchPadIndex);
            //}
            //else
            //{
            //    throw new NotImplementedException();
            //}

#if FILTERED
            return NativeTouchPadReadRawData(TouchPadIndex);
#else
            return NativeTouchPadRead(TouchPadIndex);
#endif
        }


        #region IDisposable Support
        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    //noop
                }

                NativeDispose();

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Disposable pattern destructor
        /// </summary>
        ~TouchPad()
        {
            Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Dispose(true);

                GC.SuppressFinalize(this);
            }
        }
        #endregion

        #region external calls to native implementations

        /// <summary>
        /// No source documentation available
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_init() 
        /// https://docs.espressif.com/projects/esp-idf/en/latest/api-reference/peripherals/touch_pad.html#_CPPv414touch_pad_initv
        /// </remarks>
        /// <returns>True if successful</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern EspNativeError NativeTouchPadInit();

        /// <summary>
        /// No source documentation available
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_deinit() 
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv416touch_pad_deinitv
        /// </remarks>
        /// <returns>True if successful</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern EspNativeError NativeTouchPadDeinit();



        /// <summary>
        /// Set touch sensor FSM mode, the test action can be triggered by the timer, as well as by the software.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_set_fsm_mode(touch_fsm_mode_tmode) 
        /// https://docs.espressif.com/projects/esp-idf/en/latest/api-reference/peripherals/touch_pad.html#_CPPv422touch_pad_set_fsm_mode16touch_fsm_mode_t
        /// </remarks>
        /// <param name="touchFsmMode">FSM mode</param>
        /// <returns>True if successful</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadSetFsmMode(TouchFsmMode touchFsmMode);

        /// <summary>
        /// Set touch sensor high voltage threshold of change. 
        /// The touch sensor measures the channel capacitance value by charging and discharging the channel.
        /// So the high threshold should be less than the supply voltage.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_set_voltage(touch_high_volt_trefh, touch_low_volt_trefl, touch_volt_atten_tatten)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv421touch_pad_set_voltage17touch_high_volt_t16touch_low_volt_t18touch_volt_atten_t
        /// </remarks>
        /// <param name="touchHighVolt">The value of DREFH</param>
        /// <param name="touchLowVolt">The value of DREFL</param>
        /// <param name="touchVoltAtten">The attenuation on DREFH</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadSetVoltage(TouchHighVolt touchHighVolt, TouchLowVolt touchLowVolt, TouchVoltAtten touchVoltAtten);



        /// <summary>
        /// Set touch sensor interrupt trigger mode. Interrupt can be triggered either when counter result is less than threshold or when counter result is more than threshold.
        /// </summary>
        /// <param name="mode">pointer to accept touch sensor interrupt trigger mode</param>
        /// <remarks>
        /// esp_err_t touch_pad_set_trigger_mode(touch_trigger_mode_tmode)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv426touch_pad_set_trigger_mode20touch_trigger_mode_t
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadSetTriggerMode(TouchTriggerMode mode);

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
        /// Set touch pad filter calibration period, in ms. Need to call touch_pad_filter_start before all touch filter APIs
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_set_filter_period(uint32_t new_period_ms)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv427touch_pad_set_filter_period8uint32_t
        /// </remarks>
        /// <param name="newPeriodMs">Filter period, in ms</param>
        /// <returns>True if successful</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadSetFilterPeriod(uint newPeriodMs);

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

        /// <summary>
        /// Start touch pad filter function This API will start a filter to process the noise in order to prevent false triggering when detecting slight change of capacitance. 
        /// Need to call touch_pad_filter_start before all touch filter APIs
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_filter_start(uint32_t filter_period_ms)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv422touch_pad_filter_start8uint32_t
        /// </remarks>
        /// <param name="FilterPeriod">filter calibration period, in ms</param>
        /// <returns>True if successful</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadFilterStart(uint FilterPeriod);


        /// <summary>
        /// Stop touch pad filter function Need to call touch_pad_filter_start before all touch filter APIs
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_filter_stop(void)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv421touch_pad_filter_stopv
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadFilterStop();

        /// <summary>
        /// To enable touch pad interrupt.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_intr_enable(void)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv421touch_pad_intr_enablev
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadEnableInterrupts();

        /// <summary>
        /// To disable touch pad interrupt.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_intr_disable(void)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv422touch_pad_intr_disablev
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadDisableInterrupts();

        /// <summary>
        /// To clear touch pad interrupt.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_intr_clear
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv420touch_pad_intr_clearv
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadClearInterrupts();

        /// <summary>
        /// Register touch-pad ISR. The handler will be attached to the same CPU core that this function is running on.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_isr_register(intr_handler_tfn, void* arg)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv422touch_pad_isr_register14intr_handler_tPv
        /// </remarks>
        /// <param name="Callback">The callback function to register</param>
        /// <returns>True if successful</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadIsrRegister(Action Callback);

        /// <summary>
        /// Deregister the handler previously registered using touch_pad_isr_handler_register.
        /// </summary>
        /// <remarks>
        /// touch_pad_isr_deregister(void (*fn)(void *), void *arg)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv424touch_pad_isr_deregisterPFvPvEPv
        /// </remarks>
        /// <param name="Callback">The callback function to unregister</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchPadIsrDeregister(Action Callback);

        /// <summary>
        /// Get the touch sensor channel active status mask.
        /// The bit position represents the channel number. 
        /// The 0/1 status of the bit represents the trigger status.
        /// </summary>
        /// <remarks>
        /// uint32_t touch_pad_get_status(void)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv420touch_pad_get_statusv
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern uint NativeTouchpadGetStatus();

        /// <summary>
        /// To clear the touch sensor channel active status.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_clear_status(void)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv422touch_pad_clear_statusv
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern EspNativeError NativeTouchpadClearStatus();


        /// <summary>
        /// Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NativeDispose();
        #endregion

    }
}