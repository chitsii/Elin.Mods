namespace Elin_CommonDrama
{
    /// <summary>
    /// Pluggable resolver for drama dependency keys.
    /// </summary>
    public interface IDramaDependencyResolver
    {
        bool TryResolveBool(string key, out bool value);
        bool TryExecute(string key);
    }

    public sealed class NullDramaDependencyResolver : IDramaDependencyResolver
    {
        public bool TryResolveBool(string key, out bool value)
        {
            value = false;
            return false;
        }

        public bool TryExecute(string key)
        {
            return false;
        }
    }
}
