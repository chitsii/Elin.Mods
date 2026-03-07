using DoomNetFrameworkEngine.DoomEntity.World;
using System.Collections.Generic;

namespace Elin_JustDoomIt
{
    public static class DoomKillFeed
    {
        private static readonly Queue<string> EnemyQueue = new Queue<string>();
        private const int MaxQueueSize = 1024;

        public static void Reset()
        {
            EnemyQueue.Clear();
        }

        public static void EnqueueEnemy(MobjType type)
        {
            if (EnemyQueue.Count >= MaxQueueSize)
            {
                EnemyQueue.Dequeue();
            }

            EnemyQueue.Enqueue(ToEnemyName(type));
        }

        public static bool TryDequeueEnemy(out string enemyName)
        {
            if (EnemyQueue.Count > 0)
            {
                enemyName = EnemyQueue.Dequeue();
                return true;
            }

            enemyName = null;
            return false;
        }

        private static string ToEnemyName(MobjType type)
        {
            switch (type)
            {
                case MobjType.Possessed: return "Zombieman";
                case MobjType.Shotguy: return "Shotgun Guy";
                case MobjType.Chainguy: return "Heavy Weapon Dude";
                case MobjType.Troop: return "Imp";
                case MobjType.Sergeant: return "Demon";
                case MobjType.Shadows: return "Spectre";
                case MobjType.Head: return "Cacodemon";
                case MobjType.Bruiser: return "Baron of Hell";
                case MobjType.Knight: return "Hell Knight";
                case MobjType.Skull: return "Lost Soul";
                case MobjType.Spider: return "Spider Mastermind";
                case MobjType.Baby: return "Arachnotron";
                case MobjType.Cyborg: return "Cyberdemon";
                case MobjType.Pain: return "Pain Elemental";
                case MobjType.Vile: return "Arch-vile";
                case MobjType.Undead: return "Revenant";
                case MobjType.Fatso: return "Mancubus";
                case MobjType.Wolfss: return "Wolfenstein SS";
                case MobjType.Keen: return "Commander Keen";
                case MobjType.Bossbrain: return "Icon of Sin";
                default: return type.ToString();
            }
        }
    }
}

