using System.Collections.Generic;
using System.Linq;

namespace OtterGui.Filesystem;

public partial class FileSystem<T>
{
    private enum Result
    {
        Success,
        SuccessNothingDone,
        InvalidOperation,
        ItemExists,
        PartialSuccess,
        CircularReference,
    }

    // Try to rename a child inside its parent.
    // Will fix invalid symbols in the name.
    // Returns
    //     - InvalidOperation (child is root)
    //     - SuccessNothingDone (fixed newName identical to old name)
    //     - ItemExists (an item of the fixed newName already exists in childs parent)
    //     - Success (item was successfully renamed).
    private Result RenameChild(IWritePath child, string newName)
    {
        if (child.Name.Length == 0)
            return Result.InvalidOperation;

        newName = newName.FixName();
        if (newName == child.Name)
            return Result.SuccessNothingDone;

        var newIdx = Search(child.Parent, newName);
        if (newIdx >= 0)
        {
            if (newIdx != child.IndexInParent)
                return Result.ItemExists;
            child.SetName(newName, false);
            return Result.Success;
        }

        newIdx = ~newIdx;
        if (newIdx > child.IndexInParent)
        {
            for (var i = child.IndexInParent + 1; i < newIdx; ++i)
                child.Parent.Children[i].UpdateIndex(i - 1);
            --newIdx;
        }
        else
        {
            for (var i = newIdx; i < child.IndexInParent; ++i)
                child.Parent.Children[i].UpdateIndex(i + 1);
        }

        child.Parent.Children.Move(child.IndexInParent, newIdx);
        child.UpdateIndex(newIdx);
        child.SetName(newName, false);
        return Result.Success;
    }


    // Try to move a child to newParent, renaming the child if newName is set.
    // Returns
    //     - InvalidOperation (child is root)
    //     - SuccessNothingDone (newParent is the same as childs parent and newName is null or fixed newName identical to old name)
    //     - ItemExists (an item of fixed newName already exists in newParent)
    //     - CircularReference (newParent is a descendant of child)
    //     - Success (the child was correctly moved and renamed)
    private Result MoveChild(IWritePath child, Folder newParent, out Folder oldParent, out int newIdx, string? newName = null)
    {
        newIdx = 0;
        if (child.Name.Length == 0)
        {
            oldParent = Root;
            return Result.InvalidOperation;
        }

        oldParent = child.Parent;
        if (newParent == oldParent || newParent == child)
            return newName == null ? Result.SuccessNothingDone : RenameChild(child, newName);

        if (!CheckHeritage(newParent, child))
            return Result.CircularReference;

        var actualNewName = newName?.FixName() ?? child.Name;
        newIdx = Search(newParent, actualNewName);
        if (newIdx >= 0)
        {
            if (newIdx != child.IndexInParent)
                return Result.ItemExists;

            child.SetName(actualNewName, false);
            return Result.Success;
        }

        RemoveChild(oldParent, child, Search(oldParent, child.Name));
        newIdx = ~newIdx;
        child.SetName(actualNewName, false);
        SetChild(newParent, child, newIdx);
        return Result.Success;
    }

    // Try to create all folders in names as successive subfolders beginning from Root.
    // Returns the topmost available folder and:
    //     - ItemExists (the first name in names already exists in Root and is not a folder)
    //     - SuccessNothingDone (all folders already exist)
    //     - Success (all folders exist and at least one was newly created)
    private (Result, Folder) CreateAllFolders(IEnumerable<string> names)
    {
        var last   = Root;
        var result = Result.SuccessNothingDone;
        foreach (var name in names)
        {
            var folder    = new Folder(last, name, IdCounter++);
            var midResult = SetChild(last, folder, out var idx);
            if (midResult == Result.ItemExists)
            {
                if (last.Children[idx] is not Folder f)
                    return (Result.ItemExists, last);

                last = f;
            }
            else
            {
                result = Result.Success;
                last   = folder;
            }
        }

        return (result, last);
    }

    // Split path into folders and a final file and try to create all folders as successive subfolders beginning from Root.
    // Returns the topmost available folder, the name of the file in the path and:
    //     - SuccessNothingDone (path was empty, path contained no forward-slashes or all folders in path already existed)
    //     - ItemExists (a folder could not be created due to a non-folder item of that name already existing. Does not check for existence of the final file.)
    //     - Success (all folders exist and at least one was newly created)
    private (Result, Folder, string) CreateAllFoldersAndFile(string path)
    {
        if (path.Length == 0)
            return (Result.SuccessNothingDone, Root, string.Empty);

        var split = path.SplitDirectories();
        if (split.Length == 1)
            return (Result.SuccessNothingDone, Root, split[0]);

        var (result, folder) = CreateAllFolders(path.SplitDirectories().SkipLast(1));
        return (result, folder, split[^1]);
    }


    private static void ApplyDescendantChanges(Folder parent, IWritePath child, int idx, bool removed)
    {
        var (descendants, leaves) = (child, removed) switch
        {
            (Folder f, false) => (f.TotalDescendants + 1, f.TotalLeaves),
            (Folder f, true)  => (-f.TotalDescendants - 1, -f.TotalLeaves),
            (_, true)         => (-1, -1),
            _                 => (1, 1),
        };

        for (var i = idx; i < parent.Children.Count; i++)
            parent.Children[i].UpdateIndex(i);

        while (true)
        {
            parent.TotalDescendants += descendants;
            parent.TotalLeaves      += leaves;
            if (parent.IsRoot)
                break;

            parent = parent.Parent;
        }
    }

    // Remove a child at position idx from its parent. Does not change child.Parent.
    private static void RemoveChild(Folder parent, IWritePath child, int idx)
    {
        parent.Children.RemoveAt(idx);
        ApplyDescendantChanges(parent, child, idx, true);
    }

    // Add a child to its new parent at position idx
    private static void SetChild(Folder parent, IWritePath child, int idx)
    {
        parent.Children.Insert(idx, child);
        child.SetParent(parent);
        child.UpdateDepth();
        ApplyDescendantChanges(parent, child, idx, false);
    }

    // Add a child to its new parent and return its new idx.
    // Returns ItemExists if a child of that name already exists in parent or Success otherwise.
    private Result SetChild(Folder parent, IWritePath child, out int idx)
    {
        idx = Search(parent, child.Name);
        if (idx >= 0)
            return Result.ItemExists;

        idx = ~idx;
        SetChild(parent, child, idx);
        return Result.Success;
    }

    // Remove a child from its parent.
    // Returns:
    //     - InvalidOperation (child is Root)
    //     - SuccessNothingDone (child is not set as a child of child.Parent, should not happen as its invalid state)
    //     - Success (child was successfully removed from its Parent. Does not change child.Parent)
    private Result RemoveChild(IWritePath child)
    {
        if (child.Name.Length == 0)
            return Result.InvalidOperation;

        var idx = Search(child.Parent, child.Name);
        if (idx < 0)
            return Result.SuccessNothingDone;

        RemoveChild(child.Parent, child, idx);
        return Result.Success;
    }

    // Try to merge all children of folder from into folder to and remove from if it is empty at the end.
    // Returns:
    //     - SuccessNothingDone (from is the same as to)
    //     - InvalidOperation (from is Root)
    //     - CircularReference (to is a descendant of from)
    //     - PartialSuccess (Some Items could not be moved because they already existed in to, so from was not deleted, but some items were moved)
    //     - Success (all items were successfully moved and from was deleted)
    private Result MergeFolders(Folder from, Folder to)
    {
        if (from == to)
            return Result.SuccessNothingDone;
        if (from.Name.Length == 0)
            return Result.InvalidOperation;
        if (!CheckHeritage(to, from))
            return Result.CircularReference;

        var result = Result.Success;
        for (var i = 0; i < from.Children.Count;)
            (i, result) = MoveChild(from.Children[i], to, out _, out _) == Result.Success ? (i, result) : (i + 1, Result.PartialSuccess);

        return result == Result.Success ? RemoveChild(from) : result;
    }

    // Check that child is not contained in potentialParent.
    // Returns true if potentialParent is not anywhere up the tree from child, false otherwise.
    private static bool CheckHeritage(Folder potentialParent, IPath child)
    {
        var parent = potentialParent;
        while (parent.Name.Length > 0)
        {
            if (parent == child)
                return false;

            parent = parent.Parent;
        }

        return true;
    }
}
