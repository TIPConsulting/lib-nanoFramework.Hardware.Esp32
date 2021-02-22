using System;
using System.Runtime.CompilerServices;


namespace nanoFramework.Hardware.Esp32
{
    /// <summary>
    /// Touchpad subsystem manager.
    /// Synchronizes pin access and event handling.
    /// Use a singleton instance for best results.
    /// </summary>
    public sealed class TouchPadController : IDisposable
    {
        private readonly Action _valueChangedInvoker;
        private readonly object _pinOpenLock = new object();
        private readonly object _eventLock = new object();
        private readonly Action[] _valueChanged;
        private readonly TouchPadSystemConfig _config;
        private int _eventHandlerCount = 0;
        private int _pinOpenState = 0;
        private bool _isDisposed = false;


        /// <summary>
        /// TouchPad system configuration
        /// </summary>
        public TouchPadSystemConfig Config => _config;

        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="Config"></param>
        public TouchPadController(TouchPadSystemConfig Config)
        {
            _valueChanged = new Action[10];
            _valueChangedInvoker = InvokeValueChanged;
            _config = Config;
        }


        /// <summary>
        /// Initialize the native touchpad featureset
        /// </summary>
        public void Init()
        {
            {
                var result = NativeTouchPadInit();
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }

            {
                //we need timer mode for interrupts to work
                //if software mode is needed, then we also need to implement and call touch_pad_sw_start()
                var result = NativeTouchPadSetFsmMode(TouchFsmMode.Timer);
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }

            {
                var result = NativeTouchPadSetVoltage(_config.TouchHighVolt, _config.TouchLowVolt, _config.TouchVoltAtten);
                if (result != EspNativeError.OK)
                    throw new Exception(result.ToString());
            }



            if (_config.TouchReadMode == TouchPadReadMode.Filtered)
            {
                {
                    var result = NativeTouchPadFilterStart(_config.TouchPadFilterTouchPeriod);
                    if (result != EspNativeError.OK)
                        throw new Exception(result.ToString());
                }

                TouchPadSetTriggerMode(_config.TouchTriggerMode);
            }

        }

        /// <summary>
        /// Register a command to be invoked when a touchpad interupt is triggered
        /// </summary>
        /// <param name="touchPadIndex"></param>
        /// <param name="handler"></param>
        public void RegisterTouchpadHandler(int touchPadIndex, Action handler)
        {
            if (touchPadIndex < 0 || touchPadIndex > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(touchPadIndex));
            }

            if (_isDisposed)
            {
                throw new ObjectDisposedException();
            }

            lock (_eventLock)
            {
                if (_eventHandlerCount == 0)
                {
                    {
                        var result = NativeTouchPadEnableInterrupts();
                        if (result != EspNativeError.OK)
                        {
                            throw new Exception(result.ToString());
                        }
                    }
                    {
                        //TODO: figure out how to use event subsystem
                        var result = NativeTouchPadIsrRegister(_valueChangedInvoker);
                        if (result != EspNativeError.OK)
                        {
                            throw new Exception(result.ToString());
                        }
                    }
                }
                _valueChanged[touchPadIndex] += handler;
                _eventHandlerCount++;
            }
        }

        /// <summary>
        /// Deregister a command to be invoked when a touchpad interupt is triggered
        /// </summary>
        /// <param name="touchPadIndex"></param>
        /// <param name="handler"></param>
        public void DeregisterTouchpadHandler(int touchPadIndex, Action handler)
        {
            if (touchPadIndex < 0 || touchPadIndex > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(touchPadIndex));
            }

            if (_isDisposed)
            {
                throw new ObjectDisposedException();
            }

            lock (_eventLock)
            {
                _valueChanged[touchPadIndex] += handler;
                _eventHandlerCount--;
                if (_eventHandlerCount == 0)
                {
                    {
                        var result = NativeTouchPadIsrDeregister(_valueChangedInvoker);
                        if (result != EspNativeError.OK)
                        {
                            throw new Exception(result.ToString());
                        }
                    }
                    {
                        //TODO: figure out how to use event subsystem
                        var result = NativeTouchPadDisableInterrupts();
                        if (result != EspNativeError.OK)
                        {
                            throw new Exception(result.ToString());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set touchpad interrupt trigger mode
        /// </summary>
        /// <param name="mode"></param>
        public void TouchPadSetTriggerMode(TouchTriggerMode mode)
        {
            var result = NativeTouchPadSetTriggerMode(mode);
            if (result != EspNativeError.OK)
            {
                throw new Exception(result.ToString());
            }
        }

        private void InvokeValueChanged()
        {
            var touchStatus = NativeTouchpadGetStatus();
            for (int i = 0; i < 10; ++i)
            {
                if ((touchStatus & (1 << i)) == 0)
                {
                    continue;
                }

                Action handler;
                lock (_eventLock)
                {
                    handler = _valueChanged[i];
                }
                if (handler != null)
                {
                    handler.Invoke();
                }
            }
            _ = NativeTouchpadClearStatus();
        }


        /// <summary>
        /// Initialize pin and return reference.  To close the pin, call <see cref="TouchPad.Dispose()"/>
        /// </summary>
        /// <param name="PinNumber">A valid touch pad pin index. This could be either GPIO or touch index depending on config</param>
        /// <param name="config">Touchpad configuration object.  Changes to the config will have no effect on constructed objects</param>
        /// <returns></returns>
        public TouchPad OpenPin(int PinNumber, TouchPadConfig config)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException();
            }

            var pin = new TouchPad(this, PinNumber, config);

            lock (_pinOpenLock)
            {
                if (IsPinOpen(pin.TouchPadIndex))
                {
                    throw new Exception("Touchpad pin already open");
                }
                _pinOpenState |= (1 << pin.TouchPadIndex);
            }

            pin.Init();
            return pin;
        }


        internal void ClosePin(int touchPadIndex)
        {
            if (touchPadIndex < 0 || touchPadIndex > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(touchPadIndex));
            }

            //should be invoked from TouchPad via Dispose()
            //should not be called manually
            lock (_pinOpenLock)
            {
                _pinOpenState &= ~(1 << touchPadIndex);
            }
        }

        /// <summary>
        /// Determine if a pin is currently marked as open
        /// </summary>
        /// <param name="touchPadIndex"></param>
        /// <returns></returns>
        public bool IsPinOpen(int touchPadIndex)
        {
            lock (_pinOpenLock)
            {
                return (_pinOpenState & (1 << touchPadIndex)) != 0;
            }
        }

        #region IDisposable Support
        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _ = NativeTouchPadIsrDeregister(_valueChangedInvoker);
                    lock (_eventLock)
                    {
                        for (int i = 0; i < 10; ++i)
                            _valueChanged[0] = null;
                    }
                }

                _ = NativeTouchPadDisableInterrupts();
                _ = NativeTouchPadClearInterrupts();
                _ = NativeTouchPadFilterStop();
                _ = NativeTouchPadDeinit();
                NativeDispose();

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Disposable pattern destructor
        /// </summary>
        ~TouchPadController()
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
        #endregion IDisposable Support


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
        static extern EspNativeError NativeTouchPadSetFsmMode(TouchFsmMode touchFsmMode);

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
        static extern EspNativeError NativeTouchPadSetVoltage(TouchHighVolt touchHighVolt, TouchLowVolt touchLowVolt, TouchVoltAtten touchVoltAtten);

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
        static extern EspNativeError NativeTouchPadSetTriggerMode(TouchTriggerMode mode);

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
        static extern EspNativeError NativeTouchPadFilterStart(uint FilterPeriod);

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
        static extern EspNativeError NativeTouchPadSetFilterPeriod(uint newPeriodMs);

        /// <summary>
        /// Stop touch pad filter function Need to call touch_pad_filter_start before all touch filter APIs
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_filter_stop(void)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv421touch_pad_filter_stopv
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern EspNativeError NativeTouchPadFilterStop();

        /// <summary>
        /// To enable touch pad interrupt.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_intr_enable(void)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv421touch_pad_intr_enablev
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern EspNativeError NativeTouchPadEnableInterrupts();

        /// <summary>
        /// To disable touch pad interrupt.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_intr_disable(void)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv422touch_pad_intr_disablev
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern EspNativeError NativeTouchPadDisableInterrupts();

        /// <summary>
        /// To clear touch pad interrupt.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_intr_clear
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv420touch_pad_intr_clearv
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern EspNativeError NativeTouchPadClearInterrupts();

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
        static extern EspNativeError NativeTouchPadIsrRegister(Action Callback);

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
        static extern EspNativeError NativeTouchPadIsrDeregister(Action Callback);

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
        internal static extern uint NativeTouchpadGetStatus();

        /// <summary>
        /// To clear the touch sensor channel active status.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_clear_status(void)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv422touch_pad_clear_statusv
        /// </remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern EspNativeError NativeTouchpadClearStatus();

        /// <summary>
        /// Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NativeDispose();

        #endregion external calls to native implementations
    }
}
