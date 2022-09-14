using Mono.Cecil;
using Mono.Cecil.Cil;

namespace FetchDependencies {
    public class Patcher {
        private readonly string _workPath;

        public Patcher(string workPath) {
            _workPath = workPath;
        }

        public void mainPlugin() {
            var plugin = new TargetAssembly(Path.Combine(_workPath, "FFXIV_ACT_Plugin.dll"));
            var resources = plugin.Assembly.MainModule.Resources.ToArray();

            foreach (var resource in resources) {
                if (Costura.CheckForPlugin(resource.Name)) {
                    using var stream = (resource as EmbeddedResource)!.GetResourceStream();
                    var dllPath = Path.Combine(_workPath, Costura.Fix(resource.Name));
                    Costura.Decompress(stream, dllPath);
                }
                plugin.Assembly.MainModule.Resources.Remove(resource);
            }

            {
                var method = plugin.GetMethod(
                    "System.Void FFXIV_ACT_Plugin.ACTWrapper::RunOnACTUIThread(System.Action)");
                var ilProcessor = method.Body.GetILProcessor();
                ilProcessor.Replace(0, Instruction.Create(OpCodes.Ret));
            }

            plugin.WriteOut();
        }

        public void logFilePlugin() {
            var logfile = new TargetAssembly(Path.Combine(_workPath, "FFXIV_ACT_Plugin.Logfile.dll"));
            {
                var method = logfile.GetMethod(
                    "System.Void FFXIV_ACT_Plugin.Logfile.LogOutput::WriteLine(FFXIV_ACT_Plugin.Logfile.LogMessageType,System.DateTime,System.String)");
                var targetMethod =
                    typeof(Advanced_Combat_Tracker.ActGlobals).GetMethod("LogLineWritten", new[] { typeof(string) });
                var importedMethod = logfile.Assembly.MainModule.ImportReference(targetMethod);
                var enqueueInstructionIndex = Array.FindIndex(method.Body.Instructions.ToArray(),
                    code => code.OpCode == OpCodes.Callvirt &&
                            code.Operand.ToString()!.Equals(
                                "System.Void System.Collections.Generic.Queue`1<System.String>::Enqueue(!0)"));
                if (enqueueInstructionIndex == -1)
                    throw new Exception("Could not patch LogOutput.WriteLine");
                var ilProcessor = method.Body.GetILProcessor();
                ilProcessor.Replace(enqueueInstructionIndex, Instruction.Create(OpCodes.Call, importedMethod));
                var enqueueInstructionArgIndex = enqueueInstructionIndex - 3;
                ilProcessor.RemoveAt(enqueueInstructionArgIndex);
                ilProcessor.RemoveAt(enqueueInstructionArgIndex);
            }
            {
                var method = logfile.GetMethod(
                    "System.Void FFXIV_ACT_Plugin.Logfile.LogOutput::Run(System.Object)");
                var ilProcessor = method.Body.GetILProcessor();
                ilProcessor.Replace(0, Instruction.Create(OpCodes.Ret));
            }
            logfile.WriteOut();
        }
    }
}
