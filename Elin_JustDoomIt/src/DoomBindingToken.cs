namespace Elin_JustDoomIt
{
    internal enum DoomBindingKind
    {
        Key,
        MouseButton,
        WheelUp,
        WheelDown
    }

    internal readonly struct DoomBindingToken
    {
        public DoomBindingKind Kind { get; }
        public string KeyName { get; }
        public int MouseButton { get; }

        private DoomBindingToken(DoomBindingKind kind, string keyName, int mouseButton)
        {
            Kind = kind;
            KeyName = keyName ?? string.Empty;
            MouseButton = mouseButton;
        }

        public static DoomBindingToken ForKey(string keyName)
        {
            return new DoomBindingToken(DoomBindingKind.Key, keyName ?? string.Empty, -1);
        }

        public static DoomBindingToken ForMouseButton(int mouseButton)
        {
            return new DoomBindingToken(DoomBindingKind.MouseButton, string.Empty, mouseButton);
        }

        public static DoomBindingToken ForWheelUp()
        {
            return new DoomBindingToken(DoomBindingKind.WheelUp, string.Empty, -1);
        }

        public static DoomBindingToken ForWheelDown()
        {
            return new DoomBindingToken(DoomBindingKind.WheelDown, string.Empty, -1);
        }
    }
}
