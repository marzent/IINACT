using Dalamud.Plugin.Services;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace FetchDependencies;

internal class Patcher
{
    private Version PluginVersion { get; }
    private string WorkPath { get; }
    private IPluginLog PluginLog { get; }

    public Patcher(Version version, string workPath, IPluginLog pluginLog)
    {
        PluginVersion = version;
        WorkPath = workPath;
        PluginLog = pluginLog;
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

    public void MachinaFFXIV()
    {
        var machinaDalamud = new TargetAssembly(Path.Combine(WorkPath, "Machina.FFXIV.Dalamud.dll"));

        var machina = new TargetAssembly(Path.Combine(WorkPath, "Machina.FFXIV.dll"));
        machina.RemoveStrongNaming();

        // For the Machina library, we want to replace all usages of `DeucalionClient` with our reimplemented
        // `NotDeucalionClient`. That class is used only in `Machina.FFXIV.FFXIVNetworkMonitor` class. So we need
        // to do the following modifications:
        //
        // 1. Replace its private member `_deucalionClient`'s type.
        // 2. In its every method:
        //   - Replace all its `ldfld` and `stfld` instructions of the current class's `_deucalionClient` member.
        //   - Replace all its `ldfld` and `stfld` instructions of `DeucalionClient`'s members with `NotDeucalionClient`'s
        //     corresponding members. Thay have the same names and types, so we can just replace the type references.
        //   - Replace all its `newobj` and `call*` instructions of `DeucalionClient`'s methods with `NotDeucalionClient`'s
        //     corresponding methods.
        //   - To prevent the Deucalion library from being really injected, replace all its `call*` instructions that to
        //     class `DeucalionInjector` with `NotDeucalionInjector`.
        // 3. Safely remove the `DeucalionClient` and `DeucalionInjector` classes from the Machina library.

        var oldClientFullName = "Machina.FFXIV.Deucalion.DeucalionClient";
        var newClientFullName = "Machina.FFXIV.Dalamud.NotDeucalionClient";
        var oldClientDefiniton = machina.Assembly.MainModule.Types.First(type => type.FullName == oldClientFullName);
        var newClientDefiniton = machinaDalamud.Assembly.MainModule.Types.First(type => type.FullName == newClientFullName);

        var oldInjectorFullName = "Machina.FFXIV.Deucalion.DeucalionInjector";
        var newInjectorFullName = "Machina.FFXIV.Dalamud.NotDeucalionInjector";
        var oldInjectorDefiniton = machina.Assembly.MainModule.Types.First(type => type.FullName == oldInjectorFullName);
        var newInjectorDefiniton = machinaDalamud.Assembly.MainModule.Types.First(type => type.FullName == newInjectorFullName);

        // For method replacements, also replace for the nested types of `DeucalionClient`.
        // (i.e. `MessageReceivedHandler` / `MessageSentHandler` delegates)
        var typeReplacements = new Dictionary<string, TypeDefinition>
        {
            {oldClientFullName, newClientDefiniton},
            {oldInjectorFullName, newInjectorDefiniton},
        };
        foreach (var nestedType in oldClientDefiniton.NestedTypes)
        {
            var target = newClientDefiniton.NestedTypes.FirstOrDefault(
                type => type.Name == nestedType.Name);
            if (target != null)
                typeReplacements[nestedType.FullName] = target;
        }

        // Get the `FFXIVNetworkMonitor` class.
        var ffxivNetworkMonitor = machina.Assembly.MainModule.Types.First(type =>
            type.FullName == "Machina.FFXIV.FFXIVNetworkMonitor");

        {
            // Step 1 - Replace its private member `_deucalionClient`'s type.
            var deucalionClientField = ffxivNetworkMonitor.Fields.First(field => field.Name == "_deucalionClient");
            deucalionClientField.FieldType = machina.Assembly.MainModule.ImportReference(newClientDefiniton);
        }
        {
            // Step 2
            foreach (var method in ffxivNetworkMonitor.Methods)
            {
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode == OpCodes.Newobj ||
                        instruction.OpCode == OpCodes.Call ||
                        instruction.OpCode == OpCodes.Calli ||
                        instruction.OpCode == OpCodes.Callvirt)
                    {
                        var methodReference = (MethodReference)instruction.Operand;
                        foreach (var (oldType, newType) in typeReplacements)
                        {
                            if (methodReference.DeclaringType.FullName == oldType)
                            {
                                var newMethodDefinition = newType.Methods.FirstOrDefault(m =>
                                    m.Name == methodReference.Name && m.Parameters.Count == methodReference.Parameters.Count);
                                if (newMethodDefinition != null)
                                    instruction.Operand = machina.Assembly.MainModule.ImportReference(newMethodDefinition);
                                else
                                {
                                    PluginLog.Error($"[Patcher] Could not find method {methodReference.Name} in {newType.FullName}");
                                }
                            }
                        }
                    }
                    else if (instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Stfld)
                    {
                        var fieldReference = (FieldReference)instruction.Operand;
                        if (fieldReference.DeclaringType.FullName == ffxivNetworkMonitor.FullName &&
                            fieldReference.FieldType.FullName == oldClientFullName)
                        {
                            fieldReference.FieldType = machina.Assembly.MainModule.ImportReference(newClientDefiniton);
                        }
                        else if (fieldReference.DeclaringType.FullName == oldClientFullName)
                        {
                            var newFieldDefiniton = newClientDefiniton.Fields.First(f => f.Name == fieldReference.Name);
                            instruction.Operand = machina.Assembly.MainModule.ImportReference(newFieldDefiniton);
                        }
                    }
                }
            }
        }

        // Step 3 - Remove the unused classes.
        machina.Assembly.MainModule.Types.Remove(oldClientDefiniton);
        machina.Assembly.MainModule.Types.Remove(oldInjectorDefiniton);

        machina.WriteOut();
    }
}
