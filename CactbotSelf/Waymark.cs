using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CactbotSelf
{
	public class Waymark
	{
		/// <summary>
		/// X Coordinate of Waymark.
		/// </summary>
		public float X { get; set; }

		/// <summary>
		/// Y Coordinate of Waymark.
		/// </summary>
		public float Y { get; set; }

		/// <summary>
		/// Z Coordinate of Waymark.
		/// </summary>
		public float Z { get; set; }

		/// <summary>
		/// ID of Waymark.
		/// </summary>
		public WaymarkID ID { get; set; }

		/// <summary>
		/// Active state of the Waymark.
		/// </summary>
		public bool Active { get; set; }


		/// <summary>
		/// PropertyChanged event handler for this model.
		/// </summary>
#pragma warning disable 67
#pragma warning restore 67
	}

	/// <summary>
	/// Waymark ID is the byte value of the waymark ID in memory.
	/// </summary>
	public enum WaymarkID : byte { A = 0, B, C, D, One, Two, Three, Four }
}
