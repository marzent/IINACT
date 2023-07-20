using Mono.Cecil;
using Mono.Cecil.Cil;

namespace FetchDependencies;

internal class Patcher
{
    private Version PluginVersion { get; }
    private string WorkPath { get; }

    public Patcher(Version version, string workPath)
    {
        PluginVersion = version;
        WorkPath = workPath;
    }

    public void MainPlugin()
    {
        var plugin = new TargetAssembly(Path.Combine(WorkPath, "FFXIV_ACT_Plugin.dll"));
        var resources = plugin.Assembly.MainModule.Resources.ToArray();

        foreach (var resource in resources)
        {
            if (Costura.CheckForPlugin(resource.Name))
            {
                using var stream = (resource as EmbeddedResource)!.GetResourceStream();
                var dllPath = Path.Combine(WorkPath, Costura.Fix(resource.Name));
                Costura.Decompress(stream, dllPath);
                var resourceAssembly = new TargetAssembly(dllPath);
                resourceAssembly.RemoveStrongNaming();
                resourceAssembly.MakePublic();
                resourceAssembly.WriteOut();
            }

            plugin.Assembly.MainModule.Resources.Remove(resource);
        }

        var method = plugin.GetMethod(
            "System.Void FFXIV_ACT_Plugin.ACTWrapper::RunOnACTUIThread(System.Action)");
        var ilProcessor = method.Body.GetILProcessor();
        ilProcessor.Replace(0, Instruction.Create(OpCodes.Ret));

        plugin.RemoveStrongNaming();
        plugin.MakePublic();
        plugin.WriteOut();
    }

    public void LogFilePlugin()
    {
        var logfile = new TargetAssembly(Path.Combine(WorkPath, "FFXIV_ACT_Plugin.Logfile.dll"));
        {
            var method = logfile.GetMethod(
                "System.Void FFXIV_ACT_Plugin.Logfile.LogOutput::Run(System.Object)");
            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.Replace(0, Instruction.Create(OpCodes.Ret));
        }

        {
            var method = logfile.GetMethod(
                "System.String FFXIV_ACT_Plugin.Logfile.LogFormat::FormatVersion()");
            var ilProcessor = method.Body.GetILProcessor();
            while (ilProcessor.Body.Instructions.First().OpCode != OpCodes.Ldstr)
                ilProcessor.RemoveAt(0);
            ilProcessor.Replace(
                0, Instruction.Create(OpCodes.Ldstr, $"This is IINACT {PluginVersion} (API {ApiVersion.IinactApiVersion}) based on FFXIV_ACT_Plugin {{0}}"));
            ilProcessor.Replace(1, Instruction.Create(OpCodes.Ldc_I4_1));
            var stelemIndex = Array.FindIndex(ilProcessor.Body.Instructions.ToArray(),
                                              code => code.OpCode == OpCodes.Stelem_Ref);
            Enumerable.Range(0, 5).ToList().ForEach(_ => ilProcessor.RemoveAt(stelemIndex + 1));
        }

        logfile.WriteOut();
    }

    public void MemoryPlugin()
    {
        var memory = new TargetAssembly(Path.Combine(WorkPath, "FFXIV_ACT_Plugin.Memory.dll"));

        var dataSubscription = memory.Assembly.MainModule.Types.First(type => type.Name == "DataSubscription");
        var delegates = dataSubscription.Methods.Where(method => method.Name.StartsWith("On"));

        void BeginInvokeFix(MethodDefinition method)
        {
            var originalIl = method.Body.Instructions.ToArray();
            var invokeIndex =
                Array.FindIndex(
                    originalIl,
                    code => code.OpCode == OpCodes.Callvirt && code.Operand.ToString()!.Contains("BeginInvoke"));
            if (invokeIndex == -1)
                throw new DllNotFoundException("Could not find BeginInvoke instruction");
            var invokeInstruction = originalIl[invokeIndex];
            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.RemoveAt(invokeIndex + 1);
            ilProcessor.InsertBefore(invokeInstruction, Instruction.Create(OpCodes.Pop));
            ilProcessor.InsertBefore(invokeInstruction, Instruction.Create(OpCodes.Pop));
            var beginInvokeMethod = invokeInstruction.Operand as MethodReference;
            var declaringType = beginInvokeMethod!.DeclaringType.Resolve();
            var invokeMethod = declaringType.Methods.First(methodDefinition => methodDefinition.Name == "Invoke");
            var invokeMethodReference = memory.Assembly.MainModule.ImportReference(invokeMethod);
            ilProcessor.Replace(invokeInstruction, Instruction.Create(OpCodes.Callvirt, invokeMethodReference));
        }

        foreach (var method in delegates)
            BeginInvokeFix(method);

        {
            var method = memory.GetMethod(
                "System.Void FFXIV_ACT_Plugin.Memory.ScanMemory::Run(System.Object)");
            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.Replace(0, Instruction.Create(OpCodes.Ret));
        }
        
        var marshallType = memory.Assembly.MainModule.ImportReference(typeof(System.Runtime.InteropServices.Marshal)).Resolve();
        var marshallCopyBuffer = marshallType.Methods.First(
            m => m.FullName ==
                 "System.Void System.Runtime.InteropServices.Marshal::Copy(System.IntPtr,System.Byte[],System.Int32,System.Int32)");
        var marshallCopyBufferReference = memory.Assembly.MainModule.ImportReference(marshallCopyBuffer);

        void UseMarshallCopyBuffer(MethodDefinition method)
        {
            var originalIl = method.Body.Instructions.ToArray();
            var bufferIndex =
                Array.FindIndex(
                    originalIl,
                    code => code.OpCode == OpCodes.Callvirt && code.Operand.ToString()!.Contains("ReadBuffer"));
            var dynamicLength = originalIl[bufferIndex - 1].OpCode == OpCodes.Conv_I4;
            var offset = dynamicLength ? -3 : 0;
            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.Replace(bufferIndex, Instruction.Create(OpCodes.Call, marshallCopyBufferReference));
            ilProcessor.InsertAfter(bufferIndex - 2 + offset, Instruction.Create(OpCodes.Ldc_I4_0));
            ilProcessor.RemoveAt(bufferIndex - 6 + offset);
            ilProcessor.RemoveAt(bufferIndex - 6 + offset);
        }

        var readBufferMethods = new[]
        {
            "System.IntPtr FFXIV_ACT_Plugin.Memory.MemoryReader.ReadParty::Read()",
            "System.IntPtr FFXIV_ACT_Plugin.Memory.MemoryReader.ReadZoneMap::Read()",
            "System.IntPtr FFXIV_ACT_Plugin.Memory.MemoryReader.ReadCombatant::Read(System.IntPtr)",
            "System.IntPtr FFXIV_ACT_Plugin.Memory.MemoryReader.ReadPlayer::Read()",
            "System.IntPtr FFXIV_ACT_Plugin.Memory.MemoryReader.ReadMobArray::Read64()"
        };

        foreach (var methodName in readBufferMethods)
            UseMarshallCopyBuffer(memory.GetMethod(methodName));
        
        var intPtrType = memory.Assembly.MainModule.ImportReference(typeof(IntPtr)).Resolve();
        var intPtrOp =
            intPtrType.Methods.First(m => m.FullName == "System.Void* System.IntPtr::op_Explicit(System.IntPtr)");
        var intPtrOpReference = memory.Assembly.MainModule.ImportReference(intPtrOp);
        
        {
            var method = memory.GetMethod(
                "System.UInt32 FFXIV_ACT_Plugin.Memory.MemoryReader.ReadMemory::ReadUInt32(System.IntPtr)");
            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.Clear();
            ilProcessor.Emit(OpCodes.Ldarg_1);
            ilProcessor.Emit(OpCodes.Call, intPtrOpReference);
            ilProcessor.Emit(OpCodes.Ldind_U4);
            ilProcessor.Emit(OpCodes.Ret);
        }
        
        {
            var method = memory.GetMethod(
                "System.UInt64 FFXIV_ACT_Plugin.Memory.MemoryReader.ReadMemory::ReadUInt64(System.IntPtr)");
            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.Clear();
            ilProcessor.Emit(OpCodes.Ldarg_1);
            ilProcessor.Emit(OpCodes.Call, intPtrOpReference);
            ilProcessor.Emit(OpCodes.Ldind_I8);
            ilProcessor.Emit(OpCodes.Ret);
        }
        
        {
            var method = memory.GetMethod(
                "System.IntPtr FFXIV_ACT_Plugin.Memory.MemoryReader.ReadMemory::ReadPointer(System.IntPtr)");
            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.Clear();
            ilProcessor.Emit(OpCodes.Ldarg_1);
            ilProcessor.Emit(OpCodes.Call, intPtrOpReference);
            ilProcessor.Emit(OpCodes.Ldind_I8);
            var intPtrCtor =
                intPtrType.Methods.First(m => m.FullName == "System.Void System.IntPtr::.ctor(System.Int64)");
            var intPtrCtorReference = memory.Assembly.MainModule.ImportReference(intPtrCtor);
            ilProcessor.Emit(OpCodes.Newobj, intPtrCtorReference);
            ilProcessor.Emit(OpCodes.Ret);
        }
        
        // not needed and sometimes throws an exception during early load, see #61
        {
            var method = memory.GetMethod(
                "System.Void FFXIV_ACT_Plugin.Memory.SignatureManager::RefreshVtable()");
            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.Replace(0, Instruction.Create(OpCodes.Ret));
        }
        
        memory.WriteOut();
    }
}
