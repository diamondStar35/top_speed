using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed class InputState
    {
        private readonly bool[] _keys;

        public InputState()
        {
            _keys = new bool[256];
        }

        public bool IsDown(Key key)
        {
            var index = (int)key;
            if (index < 0 || index >= _keys.Length)
                return false;
            return _keys[index];
        }

        internal void Clear()
        {
            for (var i = 0; i < _keys.Length; i++)
                _keys[i] = false;
        }

        internal void Set(Key key, bool value)
        {
            var index = (int)key;
            if (index < 0 || index >= _keys.Length)
                return;
            _keys[index] = value;
        }

        internal void CopyFrom(InputState other)
        {
            for (var i = 0; i < _keys.Length; i++)
                _keys[i] = other._keys[i];
        }
    }
}
