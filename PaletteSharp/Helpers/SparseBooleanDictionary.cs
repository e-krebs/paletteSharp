using System.Collections.Generic;

namespace PaletteSharp.Helpers
{
    public class SparseBooleanDictionary<T> : Dictionary<T, bool>
    {
        public void Append(T item, bool value)
        {
            if (ContainsKey(item)) this[item] = value;
            else Add(item, value);
        }

        public bool Get(T item)
        {
            return ContainsKey(item) && this[item];
        }
    }
}
