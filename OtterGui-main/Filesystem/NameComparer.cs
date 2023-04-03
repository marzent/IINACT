using System.Collections.Generic;

namespace OtterGui.Filesystem;

public partial class FileSystem<T>
{
    // Compare paths only by their name, using the submitted string comparer.
    private readonly struct NameComparer : IComparer<IPath>
    {
        private readonly IComparer<string> _baseComparer;

        public NameComparer(IComparer<string> baseComparer)
            => _baseComparer = baseComparer;

        public int Compare(IPath? x, IPath? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (y is null)
                return 1;
            if (x is null)
                return -1;

            return _baseComparer.Compare(x.Name, y.Name);
        }
    }
}
