using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlazorIdle.Game;
using BlazorIdle.Game.Config;
using BlazorIdle.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorIdle.Components
{
    public partial class BattleDemo
    {
        // ===== �����ó��� - Configurable Constants =====

        // ս��ѭ����������룩- ������Ϸ����Ƶ��
        // Battle loop interval (ms) - controls game update frequency
        private const int TickMs = 100;

        // �Զ�ѭ���ӳ�ʱ�䣨���룩- ������ɺ��´ο�ʼ�ĵȴ�ʱ��
        // Auto-repeat delay time (ms) - waiting time between dungeon completion and next start
        private const int AutoRepeatDelayMs = 2000;

        // AOE�˺����� - Ⱥ�幥�����ܶ�ÿ��Ŀ����˺�����
        // AOE damage multiplier - damage ratio of area attacks to each target
        private const double AoeDamageMultiplier = 0.7;

        // Ĭ�ϵ���ˢ��ʱ�䣨���룩- ������������û��ָ��ˢ��ʱ��ʱʹ��
        // Default enemy respawn time (ms) - used when monster config doesn't specify respawn time
        private const int DefaultEnemyRespawnMs = 3000;

        // �����־��Ŀ�� - ��־�б�����������¼��
        // Max log entries - maximum number of log records to keep
        private const int MaxLogEntries = 200;

        [Parameter]
        public CharacterData? SelectedCharacter { get; set; }

        /// <summary>
        /// ս��ģʽö�� - ������ͨս���͸���ս��
        /// Battle mode enum - differentiates normal and dungeon battle
        /// </summary>
        private enum BattleMode
        {
            Normal,    // ��ͨս��ģʽ
            Dungeon    // ����ս��ģʽ
        }

        // ս��ģʽ���
        // Battle mode related
        private BattleMode currentBattleMode = BattleMode.Normal;

        // ����ѡ��
        // Configuration selection
        private List<ProfessionDef> professions = new();
        private List<MonsterDef> monsters = new();
        private List<BattleScenarioDef> battleScenarios = new();
        private string? selectedScenarioId;
        private BattleScenarioDef? currentScenario => battleScenarios.FirstOrDefault(s => s.Id == selectedScenarioId);
        private string configVersion = "loading";
        private bool configReady = false;

        // ����ս���������
        // Dungeon battle related configuration
        private List<DungeonDef> dungeons = new();
        private string? selectedDungeonId;
        private DungeonDef? currentDungeon => dungeons.FirstOrDefault(d => d.Id == selectedDungeonId);
        private DungeonManager? dungeonManager;
        private DungeonSnapshot? dungeonSnapshot;

        // �൥λս��ϵͳ��������ͨս���͸���ս����
        // Multi-unit battle system (used for normal battle and dungeon battle)
        private MultiBattleInstance? battle;
        private MultiBattleSnapshot snapshot = new MultiBattleSnapshot();

        // ս������ - ��װ������ɫ�͹���
        // Battle teams - wrapping single character and monster
        private BattleTeam<Character>? playerTeam;
        private BattleTeam<Enemy>? enemyTeam;

        private BattleDigest? digest;
        private bool isRunning = false;

        // ����DPS���㣨���ڽ�ɫ���ԣ�
        // Theoretical DPS calculation (based on character attributes)
        private double theoreticalDps
        {
            get
            {
                if (SelectedCharacter == null) return 0.0;

                var hasteFactor = 1.0 + SelectedCharacter.HastePercent / 100.0;
                var attackDps = SelectedCharacter.DamagePerAttack * SelectedCharacter.AttackRateAPS * hasteFactor;
                var specialDps = SelectedCharacter.SpecialDamage / Math.Max(0.1, SelectedCharacter.SpecialIntervalSec);
                return attackDps + specialDps;
            }
        }

        // ״̬�ı� - ����ս��״̬��ʾ��ͬ����ʾ��Ϣ
        // Status text - displays different prompt messages based on battle state
        private string PlayerStatusText
        {
            get
            {
                if (snapshot.State == MultiBattleState.PlayerTeamDeadCooldown)
                    return $"�ȴ����� {(snapshot.TimeToResumeMs / 1000.0):0.00}s";
                return "ս����";
            }
        }

        private string EnemyStatusText
        {
            get
            {
                if (snapshot.State == MultiBattleState.EnemyTeamDeadCooldown)
                    return $"�ȴ�ˢ�� {(snapshot.TimeToResumeMs / 1000.0):0.00}s";
                return "ս����";
            }
        }

        // ��ս��ʵ����ȡ��ɫ��������
        // Get character attack progress from battle instance
        private double attackProgress01
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return 0.0;
                return battle.GetCharacterAttackProgress(SelectedCharacter.Id);
            }
        }

        // ��ȡ��ɫ���⼼�ܽ���
        // Get character special skill progress
        private double specialProgress01
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return 0.0;
                return battle.GetCharacterSpecialProgress(SelectedCharacter.Id);
            }
        }

        // ��ȡ���˹������� - ��ʱ����ʾ�����������£�
        // Get enemy attack progress - not displayed for now (in multi-enemy scenario)
        private double enemyProgress01 => 0.0;

        // ��ȡ��ɫ����ʣ��ʱ��
        // Get character attack time remaining
        private double attackRemainMs
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return 0.0;
                return battle.GetCharacterAttackTimeRemaining(SelectedCharacter.Id);
            }
        }

        // ��ȡ��ɫ���⼼��ʣ��ʱ��
        // Get character special skill time remaining
        private double specialRemainMs
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return 0.0;
                return battle.GetCharacterSpecialTimeRemaining(SelectedCharacter.Id);
            }
        }

        // ��ȡ���˹���ʣ��ʱ�� - ��ʱ����ʾ�����������£�
        // Get enemy attack time remaining - not displayed for now (in multi-enemy scenario)
        private double enemyRemainMs => 0.0;

        // ��־�б� - �洢ս����־
        // Log list - stores battle logs
        private readonly List<string> logs = new();

        private CancellationTokenSource? _cts;

        /// <summary>
        /// �����ʼ�� - ������Ϸ��������
        /// Component initialization - load game configuration data
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            await GameConfig.EnsureLoadedAsync();
            professions = GameConfig.Professions.ToList();
            monsters = GameConfig.Monsters.ToList();
            battleScenarios = GameConfig.BattleScenarios.ToList();
            dungeons = GameConfig.Dungeons.ToList(); // ���ظ������� - Load dungeon configuration
            configVersion = GameConfig.Version;

            selectedScenarioId = battleScenarios.FirstOrDefault()?.Id;
            selectedDungeonId = dungeons.FirstOrDefault()?.Id;

            configReady = battleScenarios.Count > 0 && monsters.Count > 0;

            if (SelectedCharacter != null)
            {
                ResetBattle();
            }
        }

        protected override void OnParametersSet()
        {
            if (SelectedCharacter != null && configReady && battle == null)
            {
                ResetBattle();
            }
        }

        /// <summary>
        /// ѡ�����ص� - �����򸱱�ѡ����ʱ����ս��
        /// Selection change callback - reset battle when scenario or dungeon selection changes
        /// </summary>
        private void OnSelectionChanged(ChangeEventArgs _)
        {
            if (!isRunning) ResetBattle();
        }

        /// <summary>
        /// ս��ģʽ������� - �л���ͨս���͸���ս��
        /// Battle mode change handler - switch between normal and dungeon battle
        /// </summary>
        private void OnBattleModeChanged(BattleMode mode)
        {
            if (isRunning) return; // ս���в������л� - Don't allow switching during battle

            currentBattleMode = mode;
            ResetBattle();
            StateHasChanged();
        }

        private string GetProfessionName(string professionId)
        {
            return professions.FirstOrDefault(p => p.Id == professionId)?.Name ?? professionId;
        }

        /// <summary>
        /// ����ս��ʵ�� - ʹ��monsterGroups������Զ�ս��
        /// Build battle instance - creates multi-unit battle using monsterGroups
        /// </summary>
        private void BuildBattle()
        {
            if (SelectedCharacter == null || currentScenario == null) return;

            // ȡ�����ľ�ս��ʵ�����¼�
            if (battle is not null)
            {
                battle.CombatEventFired -= OnCombatEvent;
                battle.LootDropped -= OnLootDropped;
            }

            // ����ʱ�Ӻ������������
            var clock = new SimClock();
            int seed = HashSeed(SelectedCharacter.Id, currentScenario.Id, configVersion);
            var rng = new RngContext(seed);

            // ������ɫʵ��
            var character = new Character
            {
                MaxHp = Math.Max(1, SelectedCharacter.MaxHp),
                Hp = Math.Max(1, SelectedCharacter.MaxHp),
                AttackRateAPS = SelectedCharacter.AttackRateAPS,
                DamagePerAttack = SelectedCharacter.DamagePerAttack,
                HastePercent = SelectedCharacter.HastePercent,
                SpecialIntervalSec = SelectedCharacter.SpecialIntervalSec,
                SpecialDamage = SelectedCharacter.SpecialDamage,
                CritChancePercent = SelectedCharacter.CritChancePercent,
                CritMultiplier = SelectedCharacter.CritMultiplier,
                VariancePct = SelectedCharacter.VariancePct,
                ReviveMs = (int)Math.Round(Math.Max(0, SelectedCharacter.ReviveSec) * 1000.0)
            };

            // ��Ҷ���
            playerTeam = new BattleTeam<Character>("player_team", "��Ҷ���", TeamType.Player);
            playerTeam.AddMember(SelectedCharacter.Id, character, character.MaxHp);

            // ���˶���
            enemyTeam = new BattleTeam<Enemy>("enemy_team", currentScenario.Name, TeamType.Enemy);

            // ������
            int enemyIndex = 0;
            foreach (var monsterGroup in currentScenario.MonsterGroups)
            {
                var monsterDef = GameConfig.GetMonster(monsterGroup.MonsterId);
                if (monsterDef == null) continue;

                for (int i = 0; i < monsterGroup.Count; i++)
                {
                    enemyIndex++;

                    int maxHp = (int)Math.Round(Math.Max(1, monsterDef.MaxHp) * monsterGroup.HpMultiplier);
                    int damagePerHit = (int)Math.Round(Math.Max(1, monsterDef.DamagePerHit) * monsterGroup.DamageMultiplier);
                    double attackInterval = Math.Max(0.1, monsterDef.AttackIntervalSec / monsterGroup.AttackSpeedMultiplier);

                    var enemy = new Enemy
                    {
                        MaxHp = maxHp,
                        Hp = maxHp,
                        AttackIntervalSec = attackInterval,
                        DamagePerHit = damagePerHit,
                        VariancePct = monsterDef.VariancePct,
                        RespawnMs = (int)Math.Round(Math.Max(0, monsterDef.RespawnSec) * 1000.0),
                        LootDrops = monsterGroup.SpecialDrops?.ToList() ?? monsterDef.LootDrops?.ToList() ?? new List<Game.Config.LootDrop>()
                    };

                    string enemyId = $"{monsterGroup.MonsterId}_{enemyIndex}";
                    enemyTeam.AddMember(enemyId, enemy, enemy.MaxHp);
                }
            }

            // 战斗配置 - 优先使用场景配置，否则使用默认值
            // Battle config - use scenario config if available, otherwise use defaults
            var config = currentScenario.BattleConfig ?? new MultiBattleConfig
            {
                PlayerTargetStrategy = TargetStrategy.LowestHp,
                EnemyTargetStrategy = TargetStrategy.Random,
                SpecialIsAoe = enemyTeam.TotalCount > 1,
                AoeDamageMultiplier = AoeDamageMultiplier,
                AllowPlayerRevive = true,
                AllowEnemyRespawn = true,
                PlayerReviveCooldownMs = character.ReviveMs,
                EnemyRespawnCooldownMs = enemyTeam.Members.Any()
                    ? enemyTeam.Members.First().Entity.RespawnMs
                    : DefaultEnemyRespawnMs,
                ReviveWithFullHp = true
            };

            // 如果使用了配置文件的 battleConfig，仍需根据实际情况覆盖某些属性
            // If using battleConfig from config file, still override certain properties based on actual values
            if (currentScenario.BattleConfig != null)
            {
                config.PlayerReviveCooldownMs = character.ReviveMs;
                config.EnemyRespawnCooldownMs = enemyTeam.Members.Any()
                    ? enemyTeam.Members.First().Entity.RespawnMs
                    : DefaultEnemyRespawnMs;
            }

            // ����ս��ʵ���������¼�
            battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, config);
            battle.CombatEventFired += OnCombatEvent;
            battle.LootDropped += OnLootDropped;

            digest = null;
        }

        /// <summary>
        /// ��������ս�� - ������������������Ҷ���
        /// Build dungeon battle - create dungeon manager and player team
        /// </summary>
        private void BuildDungeonBattle()
        {
            if (SelectedCharacter == null || currentDungeon == null) return;

            // �����ɵĸ���������
            if (dungeonManager != null)
            {
                dungeonManager.CombatEventFired -= OnCombatEvent;
                dungeonManager.LootDropped -= OnLootDropped;
                dungeonManager.WaveChanged -= OnDungeonWaveChanged;
                dungeonManager.DungeonCompleted -= OnDungeonCompleted;
            }

            var clock = new SimClock();
            int seed = HashSeed(SelectedCharacter.Id, currentDungeon.Id, configVersion);
            var rng = new RngContext(seed);

            var character = new Character
            {
                MaxHp = Math.Max(1, SelectedCharacter.MaxHp),
                Hp = Math.Max(1, SelectedCharacter.MaxHp),
                AttackRateAPS = SelectedCharacter.AttackRateAPS,
                DamagePerAttack = SelectedCharacter.DamagePerAttack,
                HastePercent = SelectedCharacter.HastePercent,
                SpecialIntervalSec = SelectedCharacter.SpecialIntervalSec,
                SpecialDamage = SelectedCharacter.SpecialDamage,
                CritChancePercent = SelectedCharacter.CritChancePercent,
                CritMultiplier = SelectedCharacter.CritMultiplier,
                VariancePct = SelectedCharacter.VariancePct,
                ReviveMs = (int)Math.Round(Math.Max(0, SelectedCharacter.ReviveSec) * 1000.0)
            };

            playerTeam = new BattleTeam<Character>("player_team", "��Ҷ���", TeamType.Player);
            playerTeam.AddMember(SelectedCharacter.Id, character, character.MaxHp);

            dungeonManager = new DungeonManager(currentDungeon, clock, rng, playerTeam, GameConfig);

            // Ĭ�Ͽ����Զ�ѭ��
            dungeonManager.EnableAutoRepeat(AutoRepeatDelayMs);

            dungeonManager.CombatEventFired += OnCombatEvent;
            dungeonManager.LootDropped += OnLootDropped;
            dungeonManager.WaveChanged += OnDungeonWaveChanged;
            dungeonManager.DungeonCompleted += OnDungeonCompleted;

            dungeonSnapshot = null;
        }

        /// <summary>
        /// ���ɹ�ϣ���� - ����ȷ�������������
        /// Generate hash seed - for deterministic random number generation
        /// </summary>
        private int HashSeed(params object[] arr)
        {
            unchecked
            {
                int h = 17;
                foreach (var o in arr) h = h * 31 + (o?.GetHashCode() ?? 0);
                return h == 0 ? 1234567 : h;
            }
        }

        /// <summary>
        /// ��ʼս�� - ���ݵ�ǰģʽ������ͨս���򸱱�ս��
        /// Start battle - starts normal battle or dungeon battle based on current mode
        /// </summary>
        private void StartBattle()
        {
            if (!configReady || isRunning) return;

            logs.Clear();

            if (currentBattleMode == BattleMode.Normal)
            {
                BuildBattle();
                battle!.Start();
                snapshot = battle!.GetSnapshot();
                digest = null;
            }
            else
            {
                BuildDungeonBattle();
                dungeonManager!.StartDungeon();
                dungeonSnapshot = dungeonManager.GetSnapshot();
            }

            isRunning = true;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = RunLoopAsync(_cts.Token);
        }

        /// <summary>
        /// ֹͣս�� - ֹͣ��ǰս����������Դ
        /// Stop battle - stops current battle and cleans up resources
        /// </summary>
        private void StopBattle()
        {
            if (!isRunning) return;
            isRunning = false;
            _cts?.Cancel();

            if (currentBattleMode == BattleMode.Normal)
            {
                battle?.Stop();
                if (battle != null)
                {
                    snapshot = battle.GetSnapshot();
                    digest ??= battle.BuildDigest();
                }
            }
            else
            {
                dungeonManager?.StopDungeon();
                battle = null;

                if (dungeonManager != null)
                {
                    dungeonSnapshot = dungeonManager.GetSnapshot();
                }
            }

            StateHasChanged();
        }

        /// <summary>
        /// ����ս�� - ֹͣ��ǰս�������¹���
        /// Reset battle - stop current battle and rebuild
        /// </summary>
        private void ResetBattle()
        {
            StopBattle();

            battle = null;

            if (currentBattleMode == BattleMode.Normal)
            {
                BuildBattle();
                if (battle != null)
                {
                    snapshot = battle.GetSnapshot();
                }
            }
            else
            {
                BuildDungeonBattle();
                if (dungeonManager != null)
                {
                    dungeonSnapshot = dungeonManager.GetSnapshot();
                }
            }

            StateHasChanged();
        }

        /// <summary>
        /// ս��ѭ�� - ���ڸ���ս��״̬
        /// Battle loop - periodically updates battle state
        /// </summary>
        private async Task RunLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && isRunning)
                {
                    await Task.Delay(TickMs, token);

                    if (currentBattleMode == BattleMode.Normal)
                    {
                        battle!.AdvanceTick(TickMs);
                        snapshot = battle!.GetSnapshot();
                    }
                    else
                    {
                        dungeonManager!.AdvanceTick(TickMs);
                        dungeonSnapshot = dungeonManager.GetSnapshot();

                        if (dungeonSnapshot?.CurrentEnemyTeam != null)
                        {
                            enemyTeam = dungeonSnapshot.CurrentEnemyTeam;

                            if (dungeonManager.CurrentBattle is not null)
                            {
                                battle = dungeonManager.CurrentBattle;
                            }

                            if (dungeonSnapshot.BattleSnapshot != null)
                            {
                                snapshot = dungeonSnapshot.BattleSnapshot;
                            }
                        }
                    }

                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (TaskCanceledException) { }
        }

        /// <summary>
        /// ս���¼����� - ����ս���е��˺��¼�����¼��־
        /// Combat event handler - handles damage events in battle and logs them
        /// </summary>
        private void OnCombatEvent(MultiCombatEvent ev)
        {
            var sec = ev.TimeMs / 1000.0;

            var src = ev.Source switch
            {
                EventSource.Attack => "�չ�",
                EventSource.Special => "�ؼ�",
                EventSource.EnemyAttack => "����",
                _ => "δ֪"
            };

            var attackerName = ev.Attacker == ActorType.Player
                ? (SelectedCharacter?.Name ?? ev.AttackerName ?? ev.AttackerId)
                : (GetEnemyDisplayName(ev.AttackerId) ?? ev.AttackerName ?? ev.AttackerId);

            var defenderName = ev.Defender == ActorType.Player
                ? (SelectedCharacter?.Name ?? ev.DefenderName ?? ev.DefenderId)
                : (GetEnemyDisplayName(ev.DefenderId) ?? ev.DefenderName ?? ev.DefenderId);

            var line = $"[{sec:0.00}s] {attackerName} {src} ���С�{defenderName}���˺� {ev.Damage}��{defenderName}HP�� {ev.DefenderHpAfter}";

            if (ev.IsAoe) line += " [AOE]";
            if (ev.IsKill) line += " [��ɱ!]";

            logs.Add(line);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);

            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// ��ȡ������ʾ���� - ��ID����ȡ�������Ͳ���ȡ��Ӧ����
        /// Get enemy display name - extracts monster type from ID and gets corresponding name
        /// </summary>
        private string GetEnemyDisplayName(string enemyId)
        {
            var parts = enemyId.Split('_');
            if (parts.Length >= 2)
            {
                var monsterId = parts[0];
                var index = parts[1];
                var monsterDef = GameConfig.GetMonster(monsterId);
                if (monsterDef != null)
                {
                    return $"{monsterDef.Name}#{index}";
                }
            }
            return enemyId;
        }

        /// <summary>
        /// ���ս����־
        /// Clear combat logs
        /// </summary>
        private void ClearLogs()
        {
            logs.Clear();
            StateHasChanged();
        }

        /// <summary>
        /// ��ȡ��ҵ�ǰ����ֵ - ����Ҷ����л�ȡ
        /// Get player current HP - retrieved from player team
        /// </summary>
        private int GetPlayerHp()
        {
            if (playerTeam == null || SelectedCharacter == null) return 0;
            var member = playerTeam.GetMember(SelectedCharacter.Id);
            return member?.CurrentHp ?? 0;
        }

        /// <summary>
        /// ��ȡ����������ֵ - ����Ҷ����л�ȡ
        /// Get player max HP - retrieved from player team
        /// </summary>
        private int GetPlayerMaxHp()
        {
            if (playerTeam == null || SelectedCharacter == null) return 1;
            var member = playerTeam.GetMember(SelectedCharacter.Id);
            return member?.MaxHp ?? 1;
        }

        /// <summary>
        /// ��ȡ���˵�ǰ����ֵ - ����˳����·����ܺ�
        /// Get enemy current HP - returns sum in multi-enemy scenario
        /// </summary>
        private int GetEnemyHp()
        {
            if (enemyTeam == null) return 0;
            return enemyTeam.Members.Sum(m => m.CurrentHp);
        }

        /// <summary>
        /// ��ȡ�����������ֵ - ����˳����·����ܺ�
        /// Get enemy max HP - returns sum in multi-enemy scenario
        /// </summary>
        private int GetEnemyMaxHp()
        {
            if (enemyTeam == null) return 1;
            var total = enemyTeam.Members.Sum(m => m.MaxHp);
            return total > 0 ? total : 1;
        }

        /// <summary>
        /// �����������¼� - �����������ӵ���ɫ��沢��¼ͳһ������־
        /// Handle loot drop event - adds loot to character inventory and logs it
        /// </summary>
        private void OnLootDropped(LootDropEvent lootEvent)
        {
            if (SelectedCharacter == null) return;

            SelectedCharacter.Inventory.AddItem(lootEvent.ItemId, lootEvent.Quantity);

            var sec = lootEvent.TimeMs / 1000.0;
            var charName = string.IsNullOrWhiteSpace(SelectedCharacter?.Name) ? "δ֪��ɫ" : SelectedCharacter!.Name;
            var itemDef = GameConfig.GetItem(lootEvent.ItemId);
            var itemName = itemDef?.Name ?? lootEvent.ItemId;

            var logLine = $"[{sec:0.00}s] {charName} ��õ��� {itemName} x{lootEvent.Quantity}";

            logs.Add(logLine);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);

            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// �������α���¼����� - ��¼����״̬���
        /// Dungeon wave change event handler - logs wave status changes
        /// </summary>
        private void OnDungeonWaveChanged(DungeonWaveEvent ev)
        {
            var changeText = ev.ChangeType switch
            {
                WaveChangeType.Preparing => "׼��",
                WaveChangeType.Started => "��ʼ",
                WaveChangeType.Completed => "���",
                _ => ""
            };

            AddLog("����", $"{changeText} {ev.WaveName}");
        }

        /// <summary>
        /// ��������¼����� - ��¼������ɻ�ʧ��
        /// Dungeon complete event handler - logs dungeon completion or failure
        /// </summary>
        private void OnDungeonCompleted(DungeonCompleteEvent ev)
        {
            if (ev.Success)
            {
                AddLog("ϵͳ", $"����ͨ�أ��� {ev.CompletionCount} �����");
            }
            else
            {
                AddLog("ϵͳ", "����ʧ�ܣ�");
            }
        }

        /// <summary>
        /// ������־ - ͳһ����־���ӷ���
        /// Add log - unified log adding method
        /// </summary>
        private void AddLog(string source, string message)
        {
            var time = currentBattleMode == BattleMode.Dungeon && dungeonSnapshot != null
                ? dungeonSnapshot.ElapsedMs
                : snapshot.ElapsedMs;

            var timeStr = $"[{time / 1000.0:F1}s]";
            var logLine = $"{timeStr} [{source}] {message}";

            logs.Add(logLine);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);
        }

        /// <summary>
        /// ��ȡ�������ν�����ʾ�ı�
        /// Get dungeon wave progress display text
        /// </summary>
        private string GetDungeonWaveProgress()
        {
            if (dungeonSnapshot == null) return "0 / 0";

            int currentWave = dungeonSnapshot.CurrentWaveIndex + 1;
            int totalWaves = dungeonSnapshot.TotalWaves;

            if (dungeonSnapshot.State == DungeonState.Completed ||
                dungeonSnapshot.State == DungeonState.CompletionDelay)
            {
                return $"{totalWaves} / {totalWaves}";
            }

            currentWave = Math.Min(currentWave, totalWaves);
            return $"{currentWave} / {totalWaves}";
        }

        /// <summary>
        /// ��ȡ�������Ȱٷֱ�
        /// Get dungeon progress percentage
        /// </summary>
        private int GetDungeonProgressPercent()
        {
            if (dungeonSnapshot == null || dungeonSnapshot.TotalWaves == 0) return 0;

            int currentWave = dungeonSnapshot.CurrentWaveIndex + 1;
            if (dungeonSnapshot.State == DungeonState.Completed ||
                dungeonSnapshot.State == DungeonState.CompletionDelay)
            {
                return 100;
            }

            return Math.Min(100, (int)(currentWave * 100.0 / dungeonSnapshot.TotalWaves));
        }

        /// <summary>
        /// ��ȡ����״̬�ı�
        /// Get dungeon state text
        /// </summary>
        private string GetDungeonStateText()
        {
            if (dungeonSnapshot == null) return "δ��ʼ";

            return dungeonSnapshot.State switch
            {
                DungeonState.NotStarted => "δ��ʼ",
                DungeonState.Preparing => "׼����",
                DungeonState.WaveStartDelay => "����׼��",
                DungeonState.Fighting => "ս����",
                DungeonState.WaveEndDelay => "���ν���",
                DungeonState.CompletionDelay => "�ȴ����¿�ʼ",
                DungeonState.Completed => "�����",
                DungeonState.Failed => "ʧ��",
                DungeonState.Stopped => "��ֹͣ",
                _ => dungeonSnapshot.State.ToString()
            };
        }

        /// <summary>
        /// ��ȡ����״̬������ʽ��
        /// Get dungeon state badge CSS class
        /// </summary>
        private string GetDungeonStateBadgeClass()
        {
            if (dungeonSnapshot == null) return "bg-secondary";

            return dungeonSnapshot.State switch
            {
                DungeonState.Fighting => "bg-danger",
                DungeonState.Completed => "bg-success",
                DungeonState.Failed => "bg-dark",
                DungeonState.Preparing or DungeonState.WaveStartDelay or DungeonState.WaveEndDelay => "bg-warning",
                DungeonState.CompletionDelay => "bg-info",
                _ => "bg-secondary"
            };
        }

        /// <summary>
        /// �Ѻ���ʾ��ʱ - ��ʽ��ʱ����ʾ
        /// Format duration display - formats time display
        /// </summary>
        private static string FormatDuration(int ms)
        {
            var ts = TimeSpan.FromMilliseconds(ms);
            if (ts.TotalHours >= 1)
            {
                return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            }
            return $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
        }

        // ��Դ���� - ȡ�������¼����ͷ���Դ
        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (battle is not null)
            {
                battle.CombatEventFired -= OnCombatEvent;
                battle.LootDropped -= OnLootDropped;
            }

            if (dungeonManager is not null)
            {
                dungeonManager.CombatEventFired -= OnCombatEvent;
                dungeonManager.LootDropped -= OnLootDropped;
                dungeonManager.WaveChanged -= OnDungeonWaveChanged;
                dungeonManager.DungeonCompleted -= OnDungeonCompleted;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }
    }
}