using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Elin_SukutsuArena
{
    public sealed class ArenaReturnPointState
    {
        private int _returnX;
        private int _returnZ;

        [JsonProperty("returnX")]
        public int ReturnX
        {
            get => _returnX;
            set
            {
                _returnX = value;
                BaseX = value;
            }
        }

        [JsonProperty("returnZ")]
        public int ReturnZ
        {
            get => _returnZ;
            set
            {
                _returnZ = value;
                BaseZ = value;
            }
        }

        [JsonProperty("x")]
        public int BaseX { get; private set; }

        [JsonProperty("z")]
        public int BaseZ { get; private set; }

        public static ArenaReturnPointState FromEntryPoint(int x, int z)
        {
            return new ArenaReturnPointState
            {
                ReturnX = x,
                ReturnZ = z
            };
        }

        public void SyncBaseCoordinates()
        {
            BaseX = ReturnX;
            BaseZ = ReturnZ;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SyncBaseCoordinates();
        }
    }
}
