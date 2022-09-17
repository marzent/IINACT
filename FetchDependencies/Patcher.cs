using Mono.Cecil;
using Mono.Cecil.Cil;

namespace FetchDependencies {
    internal class Patcher {
        private readonly string _workPath;

        public Patcher(string workPath) {
            _workPath = workPath;
        }

        public void MainPlugin() {
            var plugin = new TargetAssembly(Path.Combine(_workPath, "FFXIV_ACT_Plugin.dll"));
            var resources = plugin.Assembly.MainModule.Resources.ToArray();

            foreach (var resource in resources) {
                if (Costura.CheckForPlugin(resource.Name)) {
                    using var stream = (resource as EmbeddedResource)!.GetResourceStream();
                    var dllPath = Path.Combine(_workPath, Costura.Fix(resource.Name));
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

        public void LogFilePlugin() {
            var logfile = new TargetAssembly(Path.Combine(_workPath, "FFXIV_ACT_Plugin.Logfile.dll"));

            var method = logfile.GetMethod(
                "System.Void FFXIV_ACT_Plugin.Logfile.LogOutput::Run(System.Object)");
            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.Replace(0, Instruction.Create(OpCodes.Ret));

            logfile.WriteOut();
        }

        public void MemoryPlugin() {
            var memory = new TargetAssembly(Path.Combine(_workPath, "FFXIV_ACT_Plugin.Memory.dll"));

            var dataSubscription = memory.Assembly.MainModule.Types.First(type => type.Name == "DataSubscription");
            var delegates = dataSubscription.Methods.Where(method => method.Name.StartsWith("On"));

            void BeginInvokeFix(MethodDefinition method) {
                var originalIl = method.Body.Instructions.ToArray();
                var invokeIndex = Array.FindIndex(originalIl, code => code.OpCode == OpCodes.Callvirt && code.Operand.ToString()!.Contains("BeginInvoke"));
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

            memory.WriteOut();
        }

    }
}
