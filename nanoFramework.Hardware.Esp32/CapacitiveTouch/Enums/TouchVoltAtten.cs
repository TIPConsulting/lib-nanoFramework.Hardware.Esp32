//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32
{
	/// <summary>
	/// Touch sensor high reference voltage attenuation
	/// </summary>
	/// <remarks>
	/// Corresponds to touch_volt_atten_t
	/// https://docs.espressif.com/projects/esp-idf/en/release-v4.2/esp32s2/api-reference/peripherals/touch_pad.html#_CPPv418touch_volt_atten_t
	/// </remarks>
	public enum TouchVoltAtten
	{
		/// <summary>
		/// Touch sensor high reference voltage attenuation, no change
		/// </summary>
		Keep = -1,

		/// <summary>
		/// Touch sensor high reference voltage attenuation, 1.5V attenuation
		/// </summary>
		A1V5 = 0,

		/// <summary>
		/// Touch sensor high reference voltage attenuation, 1V attenuation
		/// </summary>
		A1V,

		/// <summary>
		/// Touch sensor high reference voltage attenuation, 0.5V attenuation
		/// </summary>
		A0V5,

		/// <summary>
		/// Touch sensor high reference voltage attenuation, 0V attenuation
		/// </summary>
		A0V,

		/// <summary>
		/// No source documentation available
		/// </summary>
		Max
	}
}
