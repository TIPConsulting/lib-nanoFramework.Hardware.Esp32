//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32
{
	/// <summary>
	/// The touch pad sensing process is under the control of a hardware-implemented finite-state machine 
	/// (FSM) which is initiated by software or a dedicated hardware timer.
	/// </summary>
	/// <remarks>
	/// Corresponds to touch_fsm_mode_t
	/// https://docs.espressif.com/projects/esp-idf/en/release-v4.2/esp32s2/api-reference/peripherals/touch_pad.html#_CPPv416touch_fsm_mode_t
	/// </remarks>
	public enum TouchFsmMode
	{
		/// <summary>
		/// To start touch FSM by timer
		/// </summary>
		Timer = 0,

		/// <summary>
		/// To start touch FSM by software trigger
		/// </summary>
		Software,

		/// <summary>
		/// No source documentation available
		/// </summary>
		Max
	}

}
