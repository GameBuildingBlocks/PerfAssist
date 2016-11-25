using UnityEngine;
using System.Collections;
using MemoryProfilerWindow;
using System.Collections.Generic;

public class MemObjHistory 
{
    public void Clear()
    {
        _history.Clear();
        _cursor = -1;
    }

    public void OnObjSelected(ThingInMemory thing)
    {
        if (_cursor != -1)
        {
            if (_history[_cursor] == thing)
                return;

            if (_history.Count > 50)
                _history.RemoveRange(0, 10);

            if (_cursor != _history.Count - 1)
            {
                _history.RemoveRange(_cursor + 1, _history.Count - (_cursor + 1));
            }
        }

        _history.Add(thing);
        _cursor = _history.Count - 1;
    }

    public ThingInMemory TryGetPrev()
    {
        if (_history.Count == 0 || _cursor == 0 || _cursor == -1)
            return null;

        return _history[_cursor - 1];
    }

    public ThingInMemory MovePrev()
    {
        ThingInMemory thing = TryGetPrev();
        if (thing != null)
            _cursor--;
        return thing;
    }

    public ThingInMemory TryGetNext()
    {
        if (_history.Count == 0 || _cursor == _history.Count - 1 || _cursor == -1)
            return null;

        return _history[_cursor + 1];
    }

    public ThingInMemory MoveNext()
    {
        ThingInMemory thing = TryGetNext();
        if (thing != null)
            _cursor++;
        return thing;
    }

    private List<ThingInMemory> _history = new List<ThingInMemory>();
    private int _cursor = -1;
}
