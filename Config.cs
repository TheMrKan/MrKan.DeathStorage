
using System.Collections.Generic;
using Rocket.API;

namespace MrKan.DeathStorage
{
    public class Config : IRocketPluginConfiguration
    {
        public ushort BarricadeId { get; set; }
        public int BarricadeLifetime { get; set; }
        public List<ushort> DontLoseOnDeath { get; set; } = new();
        public void LoadDefaults()
        {
            BarricadeId = 368;
            BarricadeLifetime = 300;
            DontLoseOnDeath = new()
            {
                363,
                17
            };
        }
    }
}