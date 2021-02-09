//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32.TouchPad
{
    /// <summary>
    /// Configuration parameters for TouchPadReader poll based class
    /// </summary>
    public class TouchPadReaderConfig : TouchPadConfigBase
    {
        /// <summary>
        /// Set preferred sensor read mode - raw or filtered
        /// </summary>
        public TouchPadReadMode ReadMode { get; set; } = TouchPadReadMode.Filtered;

        /// <summary>
        /// Default constructor
        /// </summary>
        public TouchPadReaderConfig() : base()
        {
        }
    }
}
