//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;

namespace nanoFramework.Hardware.Esp32.TouchPad
{
    /// <summary>
    /// TouchPad sensor using polling
    /// </summary>
    public sealed class TouchPadReader : TouchPadBase
    {
        private readonly TouchPadReaderConfig _config;

        /// <summary>
        /// Create new instance with default configuration
        /// </summary>
        /// <param name="pinNumber"></param>
        public TouchPadReader(int pinNumber) : this(pinNumber, new TouchPadReaderConfig())
        { }

        /// <summary>
        /// Create new instance with supplied configuration
        /// </summary>
        /// <param name="pinNumber"></param>
        /// <param name="config"></param>
        public TouchPadReader(int pinNumber, TouchPadReaderConfig config) : base(pinNumber, config)
        {
            this._config = config;
        }

        /// <summary>
        /// Read current sensor value using the preferred read mode set in the configuration
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">Config.ReadMode is set to an unsupported value</exception>
        public ushort Read()
        {
            if (_config.ReadMode == TouchPadReadMode.Filtered)
            {
                return NativeTouchPadReadFiltered(TouchPadIndex);
            }
            else
            {
                //TODO: implement raw read mode
                throw new NotImplementedException();
            }
        }

    }
}
