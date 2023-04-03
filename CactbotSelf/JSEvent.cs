using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CactbotSelf
{
	public interface JSEvent
	{

		string EventName();
	};
	public class JSEvents
	{
		//public class Camera
		//{
		//	public Camera(long VTable,float X, float Z,float Y,float CurrentZoom,float MinZoom,float MaxZoom,float CurrentFoV,float MinFoV,float MaxFoV,float AddedFoV, float CurrentHRotation, float CurrentVRotation, float MinVRotation, float MaxVRotation, float Tilt, int Mode, float LookAtHeightOffset, byte ResetLookatHeightOffset, float z2)
		//	{
		//		vTable = VTable;
		//		x = X;
		//		z = Z;
		//		y = Y;
		//		currentZoom = CurrentZoom;
		//		minZoom = MinZoom;
		//		maxZoom = MaxZoom;
		//		currentFoV = CurrentFoV;
		//		minFoV = MinFoV;
		//		maxFoV = MaxFoV;
		//		addedFoV = AddedFoV;
		//		currentHRotation = CurrentHRotation;
		//		currentVRotation = CurrentVRotation;
		//		minVRotation = MinVRotation;
		//		maxVRotation = MaxVRotation;
		//		tilt = Tilt;
		//		mode = Mode;
		//		lookAtHeightOffset = LookAtHeightOffset;
		//		resetLookatHeightOffset = ResetLookatHeightOffset;
		//		this.z2 = z2;
		//	}
		//	 public long vTable;
		//	 public float x;
		//	 public float z;
		//	 public float y;
		//	 public float currentZoom; 
		//	 public float minZoom; 
		//	 public float maxZoom; // 20
		//	 public float currentFoV; // 0.78
		//	 public float minFoV; // 0.69
		//	 public float maxFoV; // 0.78
		//	 public float addedFoV; // 011c
		//	 public float currentHRotation; // -pi -> pi, default is pi
		//	 public float currentVRotation; // -0.349066
		//	 public float minVRotation; // -1.483530, should be -+pi/2 for straight down/up but camera breaks so use -+1.569
		//	 public float maxVRotation; // 0.785398 (pi/4)
		//	 public float tilt;
		//	 public int mode; // camera mode??? (0 = 1st person, 1 = 3rd person, 2+ = weird controller mode? cant look up/down)
		//										  // public int ControlType; // 0 first person, 1 legacy, 2 standard, 3/5/6 ???, 4 ???
		//	 public float lookAtHeightOffset; // No idea what to call this
		//	 public byte resetLookatHeightOffset; // No idea what to call this
		//	 public float z2;
		//}
		public class Camera
		{
			public Camera(float CurrentHRotation, float CurrentVRotation)
			{

				currentHRotation = CurrentHRotation;
				currentVRotation = CurrentVRotation;

			}

			public float currentHRotation; // -pi -> pi, default is pi
			public float currentVRotation; // -0.349066

		}
		public class PlayerControlEvent : JSEvent
		{
			public PlayerControlEvent(Camera f, Waymark a, Waymark b, Waymark c, Waymark d, Waymark one, Waymark two, Waymark three, Waymark four)
			{
				camera = f;
				A = a;
				B = b;
				C = c;
				D = d;
				ONE = one;
				TWO = two;
				THREE = three;
				FOUR = four;
			}
			public string EventName()
			{
				return "onPlayerControl";
			}
			public Camera camera;
			public Waymark A;
			public Waymark B;
			public Waymark C;
			public Waymark D;
			public Waymark ONE;
			public Waymark TWO;
			public Waymark THREE;
			public Waymark FOUR;
		}
		public class ShunXu
		{
			public int order;
			public string job;
			public ShunXu(int Order, string Job)
			{
				order = Order;
				job = Job;
			}
		}
		public class GetConfigEvent : JSEvent
		{
			public bool isOpen;
			public List<ShunXu> shunxu;
			public GetConfigEvent()
			{

			}

			public string EventName()
			{
				return "getConfig";
			}
		}
	}
}
