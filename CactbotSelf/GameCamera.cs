using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CactbotSelf
{
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct GameCamera
	{
		public static int Size => Marshal.SizeOf(typeof(GameCamera));
		[FieldOffset(0x0)] public IntPtr* VTable;
		[FieldOffset(0x90)] public float X;
		[FieldOffset(0x94)] public float Z;
		[FieldOffset(0x98)] public float Y;
		[FieldOffset(0x114)] public float CurrentZoom; // 6
		[FieldOffset(0x118)] public float MinZoom; // 1.5
		[FieldOffset(0x11C)] public float MaxZoom; // 20
		[FieldOffset(0x120)] public float CurrentFoV; // 0.78
		[FieldOffset(0x124)] public float MinFoV; // 0.69
		[FieldOffset(0x128)] public float MaxFoV; // 0.78
		[FieldOffset(0x12C)] public float AddedFoV; // 011c
		[FieldOffset(0x130)] public float CurrentHRotation; // -pi -> pi, default is pi
		[FieldOffset(0x134)] public float CurrentVRotation; // -0.349066
															//[FieldOffset(0x138)] public float HRotationDelta;
		[FieldOffset(0x148)] public float MinVRotation; // -1.483530, should be -+pi/2 for straight down/up but camera breaks so use -+1.569
		[FieldOffset(0x14C)] public float MaxVRotation; // 0.785398 (pi/4)
		[FieldOffset(0x160)] public float Tilt;
		[FieldOffset(0x170)] public int Mode; // camera mode??? (0 = 1st person, 1 = 3rd person, 2+ = weird controller mode? cant look up/down)
											  //[FieldOffset(0x174)] public int ControlType; // 0 first person, 1 legacy, 2 standard, 3/5/6 ???, 4 ???
		[FieldOffset(0x218)] public float LookAtHeightOffset; // No idea what to call this
		[FieldOffset(0x21C)] public byte ResetLookatHeightOffset; // No idea what to call this
		[FieldOffset(0x2B4)] public float Z2;
	}
}
