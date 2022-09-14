using Mono.Cecil;

namespace FetchDependencies {
    public class TargetAssembly
    {
        public AssemblyDefinition Assembly { get; }
        private string Path { get; }

        public TargetAssembly(string assemblyPath)
        {
            Path = assemblyPath;
            Assembly = AssemblyDefinition.ReadAssembly(Path);
        }

        public MethodDefinition GetMethod(string name) =>
            Assembly.MainModule.Types
                .Where(o => o.IsClass == true)
                .SelectMany(type => type.Methods)
                .First(o => o.FullName.Contains(name));

        public void WriteOut() {
            var patchedPath = Path + ".patched";
            Assembly.Write(patchedPath);
            Assembly.Dispose();
            File.Move(patchedPath, Path, true);
        }
    }
}
