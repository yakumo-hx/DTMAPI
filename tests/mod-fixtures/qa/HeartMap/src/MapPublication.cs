using System;
using System.Collections.Generic;

namespace DTMAPI.Internal.HeartMap
{
    internal sealed class MapPublication : IDisposable
    {
        private readonly Stack<Action> undo = new Stack<Action>();
        internal void Apply(Action publish, Action rollback) { undo.Push(rollback); publish(); }
        internal void Commit() => undo.Clear();
        public void Dispose()
        {
            Exception? failure = null;
            while (undo.Count != 0) try { undo.Pop()(); } catch (Exception ex) { failure = ex; }
            if (failure != null) throw new InvalidOperationException("native-registration-rollback-failed", failure);
        }
    }
}
