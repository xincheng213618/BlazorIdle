using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlazorIdle.Game.Config;

namespace BlazorIdle.Server.Services
{
    public interface IGameConfigProvider
    {
        Task EnsureLoadedAsync(CancellationToken ct = default);
        IReadOnlyList<ProfessionDef> Professions { get; }
        IReadOnlyList<MonsterDef> Monsters { get; }
        string Version { get; }
    }
}