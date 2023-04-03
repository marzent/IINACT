using System;
using System.Collections.Generic;

namespace OtterGui.Filesystem;

public enum FileSystemChangeType
{
    ObjectRenamed,
    ObjectRemoved,
    FolderAdded,
    LeafAdded,
    ObjectMoved,
    FolderMerged,
    PartialMerge,
    Reload,
}

// The public facing filesystem methods all throw descriptive exceptions if they are unsuccessful.
public partial class FileSystem<T> where T : class
{
    // Any filesystem change triggers the event after the actual change has taken place.
    public delegate void         ChangeDelegate(FileSystemChangeType type, IPath changedObject, IPath? previousParent, IPath? newParent);
    public event ChangeDelegate? Changed;

    private readonly NameComparer      _nameComparer;
    private readonly IComparer<string> _stringComparer;
    public           Folder            Root      = Folder.CreateRoot();
    public           uint              IdCounter = 1;

    // The string comparer passed will be used to compare the names of siblings.
    // If none is supplied, they will be compared with OrdinalIgnoreCase.
    public FileSystem(IComparer<string>? comparer = null)
    {
        _stringComparer = comparer ?? StringComparer.OrdinalIgnoreCase;
        _nameComparer   = new NameComparer(_stringComparer);
    }

    // Find a child-index inside a folder using the given comparer.
    private int Search(Folder parent, string name)
        => parent.Children.BinarySearch((SearchPath)name, _nameComparer);

    /// <summary> Check if two paths compare equal completely. </summary>
    public bool Equal(string lhs, string rhs)
        => _stringComparer.Compare(lhs, rhs) == 0;

    // Find a specific child by its path from Root.
    // Returns true if the folder was found, and false if not.
    // The out parameter will contain the furthest existing folder.
    public bool Find(string fullPath, out IPath child)
    {
        var split  = fullPath.SplitDirectories();
        var folder = Root;
        child = Root;
        foreach (var part in split)
        {
            var idx = Search(folder, part);
            if (idx < 0)
            {
                child = folder;
                return false;
            }

            child = folder.Children[idx];
            if (child is not Folder f)
                return part == split[^1];

            folder = f;
        }

        return true;
    }

    // Create a new leaf of name name and with data data in parent.
    // Throws if a child of that name already exists in parent.
    // Returns the leaf and its index in parent.
    public (Leaf, int) CreateLeaf(Folder parent, string name, T data)
    {
        var leaf = new Leaf(parent, name, data, IdCounter++);
        if (SetChild(parent, leaf, out var idx) == Result.ItemExists)
            throw new Exception($"Could not add leaf {leaf.Name} to {parent.FullName()}: Child of that name already exists.");

        Changed?.Invoke(FileSystemChangeType.LeafAdded, leaf, null, parent);
        return (leaf, idx);
    }

    // Create a new folder of name name in parent.
    // Throws if a child of that name already exists in parent (even if it is a folder).
    // Returns the folder and its index in parent.
    public (Folder, int) CreateFolder(Folder parent, string name)
    {
        var folder = new Folder(parent, name, IdCounter++);
        if (SetChild(parent, folder, out var idx) == Result.ItemExists)
            throw new Exception($"Could not add folder {folder.Name} to {parent.FullName()}: Child of that name already exists.");

        Changed?.Invoke(FileSystemChangeType.FolderAdded, folder, null, parent);
        return (folder, idx);
    }

    // Finds or create the folder of name name in parent.
    // Throws if a child of that name already exists in parent and is not a folder.
    // Returns the pre-existing or newly created folder and its index in parent.
    public (Folder, int) FindOrCreateFolder(Folder parent, string name)
    {
        var folder = new Folder(parent, name, IdCounter++);
        if (SetChild(parent, folder, out var idx) == Result.ItemExists)
        {
            if (parent.Children[idx] is Folder f)
                return (f, idx);

            throw new Exception(
                $"Could not add folder {folder.Name} to {parent.FullName()}: Child of that name already exists, but is not a folder.");
        }

        Changed?.Invoke(FileSystemChangeType.FolderAdded, folder, null, parent);
        return (folder, idx);
    }

    // Splits path into successive subfolders of root and finds or creates the topmost folder.
    // Throws if a folder can not be found or created due to a non-folder child of that name already existing.
    // Returns the topmost folder.
    public Folder FindOrCreateAllFolders(string path)
    {
        var (res, folder) = CreateAllFolders(path.SplitDirectories());
        switch (res)
        {
            case Result.Success:
                Changed?.Invoke(FileSystemChangeType.FolderAdded, folder, null, folder.Parent);
                break;
            case Result.ItemExists:
                throw new Exception(
                    $"Could not create new folder for {path}: {folder.FullName()} already contains an object with a required name.");
            case Result.PartialSuccess:
                Changed?.Invoke(FileSystemChangeType.FolderAdded, folder, null, folder.Parent);
                throw new Exception(
                    $"Could not create all new folders for {path}: {folder.FullName()} already contains an object with a required name.");
        }

        return folder;
    }

    // Moves and renames a child to a new path given as full path.
    // Throws if the new path is empty, not all folders in the path could be found/created or the child could not be named.
    public void RenameAndMove(IPath child, string newPath)
    {
        if (newPath.Length == 0)
            throw new Exception($"Could not change path of {child.FullName()} to an empty path.");

        var oldPath = child.FullName();
        if (newPath == oldPath)
            return;

        var (res, folder, fileName) = CreateAllFoldersAndFile(newPath);
        var oldParent = child.Parent;
        switch (res)
        {
            case Result.Success:
                MoveChild((IWritePath)child, folder, out _, out _, fileName); // Can not fail since the parent folder is new.
                Changed?.Invoke(FileSystemChangeType.ObjectMoved, child, oldParent, folder);
                break;
            case Result.SuccessNothingDone:
                res = MoveChild((IWritePath)child, folder, out _, out _, fileName);
                if (res == Result.ItemExists)
                    throw new Exception($"Could not move {oldPath} to {newPath}: An object of name {fileName} already exists.");

                Changed?.Invoke(FileSystemChangeType.ObjectMoved, child, oldParent, folder);
                return;
            case Result.ItemExists:
                throw new Exception(
                    $"Could not create {newPath} for {oldPath}: A pre-existing folder contained an object of the same name as a required folder.");
        }
    }

    // Rename a child to newName.
    // Throws if child is Root or an item of that name already exists in child's parent.
    public void Rename(IPath child, string newName)
    {
        switch (RenameChild((IWritePath)child, newName))
        {
            case Result.InvalidOperation: throw new Exception("Can not rename root directory.");
            case Result.ItemExists:
                throw new Exception(
                    $"Could not rename {child.Name} to {newName}: Child of that name already exists in {child.Parent.FullName()}.");
            case Result.Success:
                Changed?.Invoke(FileSystemChangeType.ObjectRenamed, child, child.Parent, child.Parent);
                return;
        }
    }

    // Delete a child from its parent.
    // Throws if child is Root.
    public void Delete(IPath child)
    {
        switch (RemoveChild((IWritePath)child))
        {
            case Result.InvalidOperation: throw new Exception("Can not delete root directory.");
            case Result.Success:
                Changed?.Invoke(FileSystemChangeType.ObjectRemoved, child, child.Parent, null);
                return;
        }
    }

    // Move child to newParent.
    // Throws if child is Root, newParent is a descendant of child or a child of child's name already exists in newParent and is not a folder.
    // If a child of child's name already exists in newParent and is a folder, it will try to merge child into this folder instead.
    public void Move(IPath child, Folder newParent)
    {
        switch (MoveChild((IWritePath)child, newParent, out var oldParent, out var newIdx))
        {
            case Result.Success:
                Changed?.Invoke(FileSystemChangeType.ObjectMoved, child, oldParent, newParent);
                break;
            case Result.SuccessNothingDone: return;
            case Result.InvalidOperation:   throw new Exception("Can not move root directory.");
            case Result.CircularReference:
                throw new Exception($"Can not move {child.FullName()} into {newParent.FullName()} since folders can not contain themselves.");
            case Result.ItemExists:
                if (child is Folder childFolder && newParent.Children[newIdx] is Folder preFolder)
                {
                    Merge(childFolder, preFolder);
                    return;
                }
                else
                {
                    throw new Exception(
                        $"Can not move {child.Name} into {newParent.FullName()} because {newParent.Children[newIdx].FullName()} already exists.");
                }
        }
    }

    // Merge all children of from into to.
    // Throws if from is Root.
    // If all children can be moved, from is deleted.
    // If some children can not be moved, from and the unmoved children are kept where they are.
    public void Merge(Folder from, Folder to)
    {
        switch (MergeFolders(from, to))
        {
            case Result.SuccessNothingDone: return;
            case Result.InvalidOperation:   throw new Exception($"Can not merge root directory into {to.FullName()}.");
            case Result.Success:
                Changed?.Invoke(FileSystemChangeType.FolderMerged, from, from, to);
                return;
            case Result.PartialSuccess:
                Changed?.Invoke(FileSystemChangeType.PartialMerge, from, from, to);
                return;
        }
    }
}
