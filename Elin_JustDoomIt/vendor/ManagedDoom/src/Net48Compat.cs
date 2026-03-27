using System;
using System.Collections.Generic;
using System.IO;

namespace ManagedDoom
{
    internal static class Net48Compat
    {
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}

namespace System
{
    public static class MathF
    {
        public static float Round(float value)
        {
            return (float)Math.Round(value);
        }
    }
}

namespace System.Collections.Generic
{
    internal static class DictionaryNet48Extensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                return false;
            }

            dictionary.Add(key, value);
            return true;
        }
    }
}

namespace System.IO
{
    internal static class StreamNet48Extensions
    {
        public static void ReadExactly(this Stream stream, byte[] buffer)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            var offset = 0;
            while (offset < buffer.Length)
            {
                var read = stream.Read(buffer, offset, buffer.Length - offset);
                if (read <= 0)
                {
                    throw new EndOfStreamException();
                }

                offset += read;
            }
        }
    }
}
