namespace slidecraft.Helpers;

/// <summary>
/// A simple undo/redo stack that stores snapshots as byte arrays.
/// Phase-1 stub — full implementation in step 10.
/// </summary>
public sealed class RelayUndoStack
{
    private readonly Stack<byte[]> _undoStack = new();
    private readonly Stack<byte[]> _redoStack = new();

    /// <summary>Whether there is an action to undo.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Whether there is an action to redo.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Push a snapshot onto the undo stack and clear redo.</summary>
    public void Push(byte[] snapshot)
    {
        _undoStack.Push(snapshot);
        _redoStack.Clear();
    }

    /// <summary>Pop the last snapshot from the undo stack.</summary>
    public byte[]? Undo()
    {
        if (_undoStack.Count == 0) return null;
        var snapshot = _undoStack.Pop();
        _redoStack.Push(snapshot);
        return snapshot;
    }

    /// <summary>Pop the last snapshot from the redo stack.</summary>
    public byte[]? Redo()
    {
        if (_redoStack.Count == 0) return null;
        var snapshot = _redoStack.Pop();
        _undoStack.Push(snapshot);
        return snapshot;
    }

    /// <summary>Clear both stacks.</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
