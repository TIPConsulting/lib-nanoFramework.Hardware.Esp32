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
    public abstract class TouchPadBase : IDisposable
    {
        /// <summary>
        /// Initialize the native touchpad featureset
        /// </summary>
        /// <param name="config"></param>
        public static void Init(TouchPadConfigBase config)
        {
            if (!TouchPadInit())
                throw new Exception();

            if (!TouchPadFilterStart(config.TouchPadFilterTouchPeriod))
                throw new Exception();

            if (!TouchPadSetVoltage(config.TouchHighVolt, config.TouchLowVolt, config.TouchVoltAtten))
                throw new Exception();

            if (!TouchPadSetFsmMode(TouchFsmMode.Timer))
                throw new Exception();
        }


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

        private bool _isDisposed;
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


            if (!TouchPadConfig(_touchPadIndex, config.TouchThreshNoUse))
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


        #region IDisposable Support
        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    //noop
                }

                DisposeNative();

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Disposable pattern destructor
        /// </summary>
        ~TouchPadBase()
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
        private static extern bool TouchPadInit();

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
        protected static extern bool TouchPadSetFsmMode(TouchFsmMode touchFsmMode);

        /// <summary>
        /// Set touch sensor high voltage threshold of chanrge. 
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
        protected static extern bool TouchPadSetVoltage(TouchHighVolt touchHighVolt, TouchLowVolt touchLowVolt, TouchVoltAtten touchVoltAtten);

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
        protected static extern bool TouchPadConfig(int touchPadIndex, ushort threshold);

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
        protected static extern bool TouchPadSetFilterPeriod(uint newPeriodMs);

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
        protected static extern ushort TouchPadRead(int touchPadIndex);

        /// <summary>
        /// Get filtered touch sensor counter value by IIR filter.
        /// touch_pad_filter_start has to be called before calling touch_pad_read_filtered. This function can be called from ISR
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_read_filtered(touch_pad_ttouch_num, uint16_t *touch_value)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv423touch_pad_read_filtered11touch_pad_tP8uint16_t
        /// </remarks>
        /// <param name="touchPadIndex">Touch pad index</param>
        /// <returns>Touch sensor value</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        protected static extern ushort TouchPadReadFiltered(int touchPadIndex);

        //[MethodImpl(MethodImplOptions.InternalCall)]
        //TODO
        //protected extern ushort TouchPadSetReadRawData(int touchPadIndex); //esp_err_t touch_pad_read_raw_data(touch_pad_ttouch_num, uint16_t *touch_value)

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
        protected static extern bool TouchPadSetThresh(int touchPadIndex, ushort threshold);

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
        protected static extern bool TouchPadFilterStart(uint FilterPeriod);

        /// <summary>
        /// Register touch-pad ISR. The handler will be attached to the same CPU core that this function is running on.
        /// </summary>
        /// <remarks>
        /// esp_err_t touch_pad_isr_register(intr_handler_tfn, void* arg)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv422touch_pad_isr_register14intr_handler_tPv
        /// </remarks>
        /// <param name=""></param>
        /// <returns>True if successful</returns>
        //TODO
        //[MethodImpl(MethodImplOptions.InternalCall)]
        //protected static extern bool TouchPadIsrRegister(todo);



        /// <summary>
        /// Deregister the handler previously registered using touch_pad_isr_handler_register.
        /// </summary>
        /// <remarks>
        /// touch_pad_isr_deregister(void (*fn)(void *), void *arg)
        /// https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/touch_pad.html#_CPPv424touch_pad_isr_deregisterPFvPvEPv
        /// </remarks>
        /// <param name=""></param>
        /// <returns></returns>
        //TODO
        //[MethodImpl(MethodImplOptions.InternalCall)]
        //protected static extern bool TouchPadIsrDeregister(todo);

        /// <summary>
        /// Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void DisposeNative();
        #endregion

    }
}