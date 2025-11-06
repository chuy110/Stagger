using System.Collections.Generic;
using UnityEngine;

namespace Stagger.Core.Commands
{
    /// <summary>
    /// Command pattern interface. All commands must implement Execute and Undo.
    /// Commands encapsulate actions for input buffering, replay, and undo functionality.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Execute the command's action.
        /// </summary>
        void Execute();

        /// <summary>
        /// Undo the command's action (optional implementation).
        /// </summary>
        void Undo();

        /// <summary>
        /// Timestamp when the command was created (for frame-perfect timing).
        /// </summary>
        float Timestamp { get; }
    }

    /// <summary>
    /// Base abstract class for commands with common functionality.
    /// </summary>
    public abstract class Command : ICommand
    {
        public float Timestamp { get; private set; }

        protected Command()
        {
            Timestamp = Time.time;
        }

        public abstract void Execute();
        public virtual void Undo() { }
    }

    /// <summary>
    /// Command Invoker manages command execution, buffering, and history.
    /// Allows for input buffering and potential command replay/undo systems.
    /// </summary>
    public class CommandInvoker
    {
        private Queue<ICommand> _commandBuffer;
        private Stack<ICommand> _commandHistory;
        private int _maxBufferSize;
        private int _maxHistorySize;

        public int BufferCount => _commandBuffer.Count;
        public int HistoryCount => _commandHistory.Count;

        public CommandInvoker(int maxBufferSize = 10, int maxHistorySize = 100)
        {
            _commandBuffer = new Queue<ICommand>();
            _commandHistory = new Stack<ICommand>();
            _maxBufferSize = maxBufferSize;
            _maxHistorySize = maxHistorySize;
        }

        /// <summary>
        /// Add a command to the buffer for execution.
        /// </summary>
        public void AddCommand(ICommand command)
        {
            if (_commandBuffer.Count >= _maxBufferSize)
            {
                Debug.LogWarning($"[CommandInvoker] Buffer full ({_maxBufferSize}). Dropping oldest command.");
                _commandBuffer.Dequeue();
            }

            _commandBuffer.Enqueue(command);
        }

        /// <summary>
        /// Execute the next command in the buffer and add it to history.
        /// </summary>
        public void ExecuteNext()
        {
            if (_commandBuffer.Count == 0)
                return;

            ICommand command = _commandBuffer.Dequeue();
            command.Execute();

            // Add to history for potential undo
            _commandHistory.Push(command);
            if (_commandHistory.Count > _maxHistorySize)
            {
                // Remove oldest from history
                var tempStack = new Stack<ICommand>(_maxHistorySize);
                for (int i = 0; i < _maxHistorySize; i++)
                {
                    if (_commandHistory.Count > 0)
                        tempStack.Push(_commandHistory.Pop());
                }
                _commandHistory = tempStack;
            }
        }

        /// <summary>
        /// Execute all commands in the buffer.
        /// </summary>
        public void ExecuteAll()
        {
            while (_commandBuffer.Count > 0)
            {
                ExecuteNext();
            }
        }

        /// <summary>
        /// Undo the last executed command.
        /// </summary>
        public void UndoLast()
        {
            if (_commandHistory.Count == 0)
            {
                Debug.LogWarning("[CommandInvoker] No commands in history to undo.");
                return;
            }

            ICommand command = _commandHistory.Pop();
            command.Undo();
        }

        /// <summary>
        /// Clear the command buffer without executing.
        /// </summary>
        public void ClearBuffer()
        {
            _commandBuffer.Clear();
        }

        /// <summary>
        /// Clear the command history.
        /// </summary>
        public void ClearHistory()
        {
            _commandHistory.Clear();
        }

        /// <summary>
        /// Clear both buffer and history.
        /// </summary>
        public void ClearAll()
        {
            ClearBuffer();
            ClearHistory();
        }
    }
}