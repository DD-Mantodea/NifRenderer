using System;
using System.Collections.Generic;

namespace NifRenderer.Editing
{
    public sealed class History(int limit = 64)
    {
        private readonly List<EditRecord> _done = [];

        private readonly List<EditRecord> _undone = [];

        public event Action? Changed;

        public bool CanUndo => _done.Count > 0;

        public bool CanRedo => _undone.Count > 0;

        public int UndoDepth => _done.Count;

        public int RedoDepth => _undone.Count;

        public IReadOnlyList<EditRecord> Applied => _done;

        public void Record(EditRecord record)
        {
            if (record == null)
                return;

            _done.Add(record);

            _undone.Clear();

            while (_done.Count > limit)
                _done.RemoveAt(0);

            Changed?.Invoke();
        }

        public EditRecord? Undo()
        {
            if (_done.Count == 0)
                return null;

            var record = _done[^1];

            _done.RemoveAt(_done.Count - 1);

            record.Undo();

            _undone.Add(record);

            Changed?.Invoke();

            return record;
        }

        public EditRecord? Redo()
        {
            if (_undone.Count == 0)
                return null;

            var record = _undone[^1];

            _undone.RemoveAt(_undone.Count - 1);

            record.Redo();

            _done.Add(record);

            Changed?.Invoke();

            return record;
        }

        public void Clear()
        {
            _done.Clear();

            _undone.Clear();

            Changed?.Invoke();
        }
    }
}
