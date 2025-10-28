using System.Collections.Generic;
using BlazorIdle.Game.Config;

namespace BlazorIdle.Shared.DTOs
{
    public sealed class GameConfigResponse
    {
        public string Version { get; set; } = "unloaded";
        public List<ProfessionDef> Professions { get; set; } = new();
        public List<MonsterDef> Monsters { get; set; } = new();
    }
}