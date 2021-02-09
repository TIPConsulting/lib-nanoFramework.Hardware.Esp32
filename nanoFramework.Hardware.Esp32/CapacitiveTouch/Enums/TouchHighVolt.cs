//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32.TouchPad
{
	/// <summary>
	/// Touch sensor high reference voltage
	/// </summary>
	/// <remarks>
	/// Corresponds to touch_high_volt_t
	/// https://docs.espressif.com/projects/esp-idf/en/release-v4.2/esp32s2/api-reference/peripherals/touch_pad.html#_CPPv417touch_high_volt_t
	/// </remarks>
	public enum TouchHighVolt
	{
		/// <summary>
		///Touch sensor high reference voltage, no change 
		/// </summary>
		Keep = -1,

		/// <summary>
		/// Touch sensor high reference voltage, 2.4V
		/// </summary>
		H2V4 = 0,

		/// <summary>
		/// Touch sensor high reference voltage, 2.5V
		/// </summary>
		H2V5,

		/// <summary>
		///  Touch sensor high reference voltage, 2.6V
		/// </summary>
		H2V6,

		/// <summary>
		///  Touch sensor high reference voltage, 2.7V
		/// </summary>
		H2V7,

		/// <summary>
		/// No source documentation available
		/// </summary>
		Max
	}
}
