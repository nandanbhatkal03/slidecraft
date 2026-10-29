namespace slidecraft.Helpers;

/// <summary>
/// A reliable undo/redo stack that stores snapshots as byte arrays (Memento pattern).
/// </summary>
public sealed class RelayUndoStack
{
    private readonly Stack<byte[]> _undoStack = new();
    private readonly Stack<byte[]> _redoStack = new();

    /// <summary>Whether there is an action to undo.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Whether there is an action to redo.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Push a snapshot of the current state before a change onto the undo stack and clear redo.</summary>
    public void Push(byte[] snapshot)
    {
        _undoStack.Push(snapshot);
        _redoStack.Clear();
    }

    /// <summary>
    /// Undo the last action. Saves <paramref name="currentSnapshot"/> onto the redo stack
    /// and returns the previous state snapshot to restore.
    /// </summary>
    public byte[]? Undo(byte[] currentSnapshot)
    {
        if (_undoStack.Count == 0) return null;
        _redoStack.Push(currentSnapshot);
        return _undoStack.Pop();
    }

    /// <summary>
    /// Redo the last undone action. Saves <paramref name="currentSnapshot"/> onto the undo stack
    /// and returns the next state snapshot to restore.
    /// </summary>
    public byte[]? Redo(byte[] currentSnapshot)
    {
        if (_redoStack.Count == 0) return null;
        _undoStack.Push(currentSnapshot);
        return _redoStack.Pop();
    }

    /// <summary>Clear both stacks.</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
