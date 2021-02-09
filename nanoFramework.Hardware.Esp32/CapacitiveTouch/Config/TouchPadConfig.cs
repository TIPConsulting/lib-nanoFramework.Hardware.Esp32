//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32.TouchPad
{
	/// <summary>
	/// Configuration parameters for TouchPad interrupt based class
	/// </summary>
	public class TouchPadConfig : TouchPadConfigBase
	{
		/// <summary>
		/// The threshold to trigger interrupt when the pad is touched.
		/// Sensor value must be greater than this value to trigger an interrupt
		/// </summary>
		public float InterruptThresholdValue { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public TouchPadConfig() : base()
		{
			// By default, use 2/3 of read value as the threshold to trigger interrupt when the pad is touched.
			InterruptThresholdValue = (float)2 / 3;
		}
	}
}
