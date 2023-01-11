using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs
{
    public static class Utilities
    {
        public static unsafe string Utf8StringToString(dynamic obj, FFXIVMemory memory)
        {
            if (obj.GetType().Name == "ManagedType")
            {
                obj = obj.ToType();
            }

            if (((object)obj).GetType().Name != "Utf8String")
            {
                return null;
            }

            object objCast = obj;
            var ptrVal = objCast.GetType().GetField("StringPtr").GetValue(objCast);
            IntPtr ptr = new IntPtr((long)Pointer.Unbox((Pointer)ptrVal));
            int len = (int)((long)obj.BufUsed);

            var byteArr = memory.GetByteArray(ptr, len);
            return FFXIVMemory.GetStringFromBytes(byteArr, 0, len);
        }
    }
}
