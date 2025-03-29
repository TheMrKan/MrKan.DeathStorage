
using System.Collections.Generic;
using System.Xml.Serialization;
using Rocket.API;

namespace MrKan.DeathStorage
{
    public class Config : IRocketPluginConfiguration
    {
        public ushort BarricadeId { get; set; }
        public int BarricadeLifetime { get; set; }
        public float BarricadePositionOffset { get; set; }
        [XmlArrayItem(ElementName = "Item")]
        public List<ushort> DontLoseOnDeath { get; set; } = new();
        public void LoadDefaults()
        {
            BarricadeId = 368;
            BarricadeLifetime = 300;
            BarricadePositionOffset = 0.1f;
            DontLoseOnDeath = new()
            {
                363,
                17
            };
        }
    }
}