using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Inventory.Commands
{
    /// <summary>
    /// Invoker for inventory commands.
    /// Manages command execution and maintains undo/redo history.
    /// Follows Command Pattern.
    /// </summary>
    public class InventoryCommandInvoker
    {
        private readonly Stack<IInventoryCommand> _undoStack;
        private readonly Stack<IInventoryCommand> _redoStack;
        private readonly int _maxHistorySize;
        private readonly bool _enableDebugLogs;

        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public InventoryCommandInvoker(int maxHistorySize = 20, bool enableDebugLogs = false)
        {
            _maxHistorySize = maxHistorySize;
            _enableDebugLogs = enableDebugLogs;
            _undoStack = new Stack<IInventoryCommand>();
            _redoStack = new Stack<IInventoryCommand>();
        }

        /// <summary>
        /// Execute a command and add it to history if it supports undo
        /// </summary>
        public bool Execute(IInventoryCommand command)
        {
            if (command == null)
            {
                Debug.LogWarning("InventoryCommandInvoker: Cannot execute null command");
                return false;
            }

            bool success = command.Execute();
            
            if (success)
            {
                // Add to undo history if the command supports undo
                if (command.CanUndo)
                {
                    _undoStack.Push(command);
                    
                    // Limit history size
                    if (_undoStack.Count > _maxHistorySize)
                    {
                        // Remove oldest command
                        var temp = new Stack<IInventoryCommand>();
                        for (int i = 0; i < _maxHistorySize; i++)
                        {
                            temp.Push(_undoStack.Pop());
                        }
                        _undoStack.Clear();
                        while (temp.Count > 0)
                        {
                            _undoStack.Push(temp.Pop());
                        }
                    }

                    // Clear redo stack when new command is executed
                    _redoStack.Clear();
                }

                if (_enableDebugLogs)
                {
                    Debug.Log($"Executed: {command.Description}");
                }
            }
            else
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"Failed to execute: {command.Description}");
                }
            }

            return success;
        }

        /// <summary>
        /// Undo the last command
        /// </summary>
        public bool Undo()
        {
            if (_undoStack.Count == 0)
            {
                Debug.LogWarning("InventoryCommandInvoker: Nothing to undo");
                return false;
            }

            IInventoryCommand command = _undoStack.Pop();
            bool success = command.Undo();

            if (success)
            {
                _redoStack.Push(command);
                if (_enableDebugLogs)
                {
                    Debug.Log($"Undone: {command.Description}");
                }
            }
            else
            {
                // Put it back if undo failed
                _undoStack.Push(command);
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"Failed to undo: {command.Description}");
                }
            }

            return success;
        }

        /// <summary>
        /// Redo the last undone command
        /// </summary>
        public bool Redo()
        {
            if (_redoStack.Count == 0)
            {
                Debug.LogWarning("InventoryCommandInvoker: Nothing to redo");
                return false;
            }

            IInventoryCommand command = _redoStack.Pop();
            bool success = command.Execute();

            if (success)
            {
                _undoStack.Push(command);
                if (_enableDebugLogs)
                {
                    Debug.Log($"Redone: {command.Description}");
                }
            }
            else
            {
                // Put it back if redo failed
                _redoStack.Push(command);
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"Failed to redo: {command.Description}");
                }
            }

            return success;
        }

        /// <summary>
        /// Clear all command history
        /// </summary>
        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            if (_enableDebugLogs)
            {
                Debug.Log("InventoryCommandInvoker: History cleared");
            }
        }

        /// <summary>
        /// Get the description of the last command that can be undone
        /// </summary>
        public string GetUndoDescription()
        {
            return _undoStack.Count > 0 ? _undoStack.Peek().Description : "Nothing to undo";
        }

        /// <summary>
        /// Get the description of the last command that can be redone
        /// </summary>
        public string GetRedoDescription()
        {
            return _redoStack.Count > 0 ? _redoStack.Peek().Description : "Nothing to redo";
        }
    }
}
