/*
 * User: Std Created by SharpDevelop.
 * 
 * Date: 11/18/2009
 * Time: 10:35 AM
 * 
 * Modified of http://www.codeproject.com/KB/cs/highperformancetimercshar.aspx
 */

using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace TestSFMLDotNet {
	/// <summary>
	/// Time-handler for a game loop. It uses a precise calculation from
	/// kernel32.dll but will fall-back on less precise means if importing the
	/// dll failed.
	/// </summary>
	public class Timer {
		// Get support from the Windows DLL.
		[DllImport("kernel32.dll")]
		private static extern bool QueryPerformanceCounter(
			out long lpPerformanceCount);
		[DllImport("kernel32.dll")]
		private static extern bool QueryPerformanceFrequency(
			out long lpPerformanceCount);
		
		protected long startTick = 0, stopTick = 0;
		protected long frequency;
		protected bool highFreq = true;
		
		public Timer() {
			if (QueryPerformanceFrequency(out frequency) == false)
				// It's not available. Fallback on less accurate timing.
				highFreq = false;
			Reset();
		}
		
		/// <summary>
		/// Resets the initial tick count to  the current value and returns it.
		/// </summary>
		/// <returns>The current tick count.</returns>
		public long Reset() {
			if (highFreq)
				QueryPerformanceCounter(out startTick);
			else
				startTick = System.Environment.TickCount;
			return startTick;
		}
		
		/// <summary>
		/// Returns the approximate number of milliseconds passed from the last
		/// time Reset() was called to the moment this function is called.
		/// </summary>
		/// <returns>The milliseconds since the last Reset() call.</returns>
		public double GetTicks() {
			if (highFreq) {
				QueryPerformanceCounter(out stopTick);
				return (double) (stopTick - startTick) / (double) frequency;
			}
			return (double) (System.Environment.TickCount - startTick) / 1000.0;
		}
	}
}
