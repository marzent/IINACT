namespace OtterGui.Filesystem;

public partial class FileSystem<T>
{
    public sealed class Leaf : IWritePath
    {
        public T Value { get; }

        internal Leaf(Folder parent, string name, T value, uint identifier)
        {
            Parent = parent;
            Value  = value;
            SetName(name);
            Identifier = identifier;
        }

        public string FullName()
            => IPath.BaseFullName(this);

        public override string ToString()
            => FullName();

        public Folder Parent        { get; internal set; }
        public string Name          { get; private set; } = string.Empty;
        public uint   Identifier    { get; }
        public byte   Depth         { get; internal set; }
        public ushort IndexInParent { get; internal set; }

        void IWritePath.SetParent(Folder parent)
            => Parent = parent;

        internal void SetName(string name, bool fix = true)
            => Name = fix ? name.FixName() : name;

        void IWritePath.SetName(string name, bool fix)
            => SetName(name, fix);

        void IWritePath.UpdateDepth()
            => Depth = unchecked((byte)(Parent.Depth + 1));

        void IWritePath.UpdateIndex(int index)
        {
            if (index < 0)
                index = Parent.Children.IndexOf(this);
            IndexInParent = (ushort)(index < 0 ? 0 : index);
        }
    }
}
