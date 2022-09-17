using Mono.Cecil;

namespace FetchDependencies {
    internal class TargetAssembly {
        public AssemblyDefinition Assembly { get; }
        private string Path { get; }

        public TargetAssembly(string assemblyPath) {
            Path = assemblyPath;
            Assembly = AssemblyDefinition.ReadAssembly(Path);
        }

        public MethodDefinition GetMethod(string name) =>
            GetAllTypes()
                .Where(o => o.IsClass == true)
                .SelectMany(type => type.Methods)
                .First(o => o.FullName.Contains(name));

        public void MakePublic() {
            bool CheckCompilerGeneratedAttribute(IMemberDefinition member) {
                return member.CustomAttributes.Any(x =>
                    x.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
            }

            foreach (var type in GetAllTypes()) {
                if (CheckCompilerGeneratedAttribute(type))
                    continue;

                if (type.IsNested)
                    type.IsNestedPublic = true;
                else
                    type.IsPublic = true;

                foreach (var method in type.Methods.Where(method =>
                             !CheckCompilerGeneratedAttribute(method) && !method.IsCompilerControlled))
                    method.IsPublic = true;

                foreach (var field in type.Fields.Where(field =>
                             !CheckCompilerGeneratedAttribute(field) && !field.IsCompilerControlled))
                    field.IsPublic = true;
            }
        }

        public void RemoveStrongNaming() {
            var name = Assembly.Name;
            name.HasPublicKey = false;
            name.PublicKey = Array.Empty<byte>();

            foreach (var module in Assembly.Modules) {
                module.Attributes &= ~ModuleAttributes.StrongNameSigned;
                var coreLibs = new[] { "netstandard", "mscorlib", "System" };
                foreach (var reference in module.AssemblyReferences) {
                    if (coreLibs.Any(coreLib => reference.Name == coreLib))
                        continue;
                    reference.HasPublicKey = false;
                    reference.PublicKey = Array.Empty<byte>();
                }
            }
        }

        private IEnumerable<TypeDefinition> GetAllTypes() {
            var types = new Queue<TypeDefinition>(Assembly.MainModule.Types);

            while (types.Count > 0) {
                var type = types.Dequeue();

                yield return type;

                foreach (var nestedType in type.NestedTypes)
                    types.Enqueue(nestedType);
            }
        }

        public void WriteOut() {
            var patchedPath = Path + ".patched";
            Assembly.Write(patchedPath);
            Assembly.Dispose();
            File.Move(patchedPath, Path, true);
        }
    }
}