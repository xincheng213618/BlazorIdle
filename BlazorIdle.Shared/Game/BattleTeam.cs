using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Game
{
    /// <summary>
    /// 战斗队伍 - 管理多个战斗单位的通用容器
    /// Battle team - generic container for managing multiple combat units
    /// </summary>
    public class BattleTeam<T> where T : class
    {
        private readonly List<BattleMember<T>> _members = new();
        private readonly Dictionary<string, BattleMember<T>> _memberById = new();

        /// <summary>
        /// 队伍唯一标识
        /// Team unique identifier
        /// </summary>
        public string TeamId { get; }

        /// <summary>
        /// 队伍名称
        /// Team name
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// 队伍类型（玩家队伍/敌人队伍）
        /// Team type (player team / enemy team)
        /// </summary>
        public TeamType Type { get; }

        /// <summary>
        /// 所有队伍成员（只读）
        /// All team members (read-only)
        /// </summary>
        public IReadOnlyList<BattleMember<T>> Members => _members;

        /// <summary>
        /// 队伍是否全部阵亡
        /// Whether the entire team is dead
        /// </summary>
        public bool IsAllDead => _members.All(m => m.IsDead);

        /// <summary>
        /// 存活成员数量
        /// Number of alive members
        /// </summary>
        public int AliveCount => _members.Count(m => !m.IsDead);

        /// <summary>
        /// 总成员数量
        /// Total member count
        /// </summary>
        public int TotalCount => _members.Count;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public BattleTeam(string teamId, string teamName, TeamType type)
        {
            TeamId = teamId ?? throw new ArgumentNullException(nameof(teamId));
            TeamName = teamName ?? string.Empty;
            Type = type;
        }

        /// <summary>
        /// 添加成员到队伍
        /// Add member to team
        /// </summary>
        public void AddMember(string memberId, T member, int maxHp, int currentHp = -1)
        {
            if (string.IsNullOrWhiteSpace(memberId))
                throw new ArgumentException("Member ID cannot be empty", nameof(memberId));

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            if (_memberById.ContainsKey(memberId))
                throw new InvalidOperationException($"Member with ID '{memberId}' already exists in team");

            var battleMember = new BattleMember<T>(memberId, member, maxHp, currentHp < 0 ? maxHp : currentHp);
            _members.Add(battleMember);
            _memberById[memberId] = battleMember;
        }

        /// <summary>
        /// 移除队伍成员
        /// Remove team member
        /// </summary>
        public bool RemoveMember(string memberId)
        {
            if (_memberById.TryGetValue(memberId, out var member))
            {
                _members.Remove(member);
                _memberById.Remove(memberId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取成员
        /// Get member
        /// </summary>
        public BattleMember<T>? GetMember(string memberId)
        {
            return _memberById.GetValueOrDefault(memberId);
        }

        /// <summary>
        /// 获取成员实体
        /// Get member entity
        /// </summary>
        public T? GetMemberEntity(string memberId)
        {
            return _memberById.GetValueOrDefault(memberId)?.Entity;
        }

        /// <summary>
        /// 对成员造成伤害
        /// Deal damage to member
        /// </summary>
        public int DealDamage(string memberId, int damage)
        {
            var member = GetMember(memberId);
            if (member == null || member.IsDead)
                return 0;

            return member.TakeDamage(damage);
        }

        /// <summary>
        /// 治疗成员
        /// Heal member
        /// </summary>
        public int HealMember(string memberId, int amount)
        {
            var member = GetMember(memberId);
            if (member == null || member.IsDead)
                return 0;

            return member.Heal(amount);
        }

        /// <summary>
        /// 复活成员
        /// Revive member
        /// </summary>
        public void ReviveMember(string memberId, int hpAmount = -1)
        {
            var member = GetMember(memberId);
            if (member == null)
                return;

            member.Revive(hpAmount);
        }

        /// <summary>
        /// 复活所有成员
        /// Revive all members
        /// </summary>
        public void ReviveAll(bool fullHp = true)
        {
            foreach (var member in _members)
            {
                member.Revive(fullHp ? member.MaxHp : member.MaxHp / 2);
            }
        }

        /// <summary>
        /// 获取所有存活成员ID列表
        /// Get all alive member IDs
        /// </summary>
        public List<string> GetAliveMemberIds()
        {
            return _members.Where(m => !m.IsDead)
                          .Select(m => m.Id)
                          .ToList();
        }

        /// <summary>
        /// 获取所有存活成员
        /// Get all alive members
        /// </summary>
        public List<BattleMember<T>> GetAliveMembers()
        {
            return _members.Where(m => !m.IsDead).ToList();
        }

        /// <summary>
        /// 从存活成员中随机选择一个
        /// Randomly select one from alive members
        /// </summary>
        public string? GetRandomAliveMemberId(RngContext rng)
        {
            var aliveIds = GetAliveMemberIds();
            if (aliveIds.Count == 0)
                return null;

            var index = rng.NextRange(0, aliveIds.Count - 1);
            return aliveIds[index];
        }

        /// <summary>
        /// 获取血量最低的存活成员
        /// Get alive member with lowest HP
        /// </summary>
        public string? GetLowestHpMemberId()
        {
            var aliveMember = _members.Where(m => !m.IsDead)
                                      .OrderBy(m => m.CurrentHp)
                                      .FirstOrDefault();
            return aliveMember?.Id;
        }

        /// <summary>
        /// 获取血量百分比最低的存活成员
        /// Get alive member with lowest HP percentage
        /// </summary>
        public string? GetLowestHpPercentMemberId()
        {
            var aliveMember = _members.Where(m => !m.IsDead)
                                      .OrderBy(m => (double)m.CurrentHp / m.MaxHp)
                                      .FirstOrDefault();
            return aliveMember?.Id;
        }

        /// <summary>
        /// 重置队伍状态（复活所有成员并恢复满血）
        /// Reset team state (revive all members with full HP)
        /// </summary>
        public void Reset()
        {
            foreach (var member in _members)
            {
                member.Reset();
            }
        }

        /// <summary>
        /// 获取队伍总生命值信息
        /// Get team total HP info
        /// </summary>
        public (int currentTotal, int maxTotal) GetTotalHp()
        {
            int currentTotal = _members.Sum(m => m.CurrentHp);
            int maxTotal = _members.Sum(m => m.MaxHp);
            return (currentTotal, maxTotal);
        }
    }

    /// <summary>
    /// 战斗成员 - 包装实体并管理其战斗状态
    /// Battle member - wraps entity and manages its battle state
    /// </summary>
    public class BattleMember<T> where T : class
    {
        /// <summary>
        /// 成员唯一标识
        /// Member unique identifier
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 实际的实体对象（Character或Enemy）
        /// Actual entity object (Character or Enemy)
        /// </summary>
        public T Entity { get; }

        /// <summary>
        /// 最大生命值
        /// Maximum HP
        /// </summary>
        public int MaxHp { get; private set; }

        /// <summary>
        /// 当前生命值
        /// Current HP
        /// </summary>
        public int CurrentHp { get; private set; }

        /// <summary>
        /// 是否已阵亡
        /// Whether the member is dead
        /// </summary>
        public bool IsDead => CurrentHp <= 0;

        /// <summary>
        /// 生命值百分比
        /// HP percentage
        /// </summary>
        public double HpPercent => MaxHp > 0 ? (double)CurrentHp / MaxHp : 0;

        /// <summary>
        /// 累计受到的伤害
        /// Total damage taken
        /// </summary>
        public int TotalDamageTaken { get; private set; }

        /// <summary>
        /// 累计造成的伤害
        /// Total damage dealt
        /// </summary>
        public int TotalDamageDealt { get; private set; }

        /// <summary>
        /// 击杀数
        /// Kill count
        /// </summary>
        public int Kills { get; private set; }

        /// <summary>
        /// 死亡次数
        /// Death count
        /// </summary>
        public int Deaths { get; private set; }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public BattleMember(string id, T entity, int maxHp, int currentHp)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
            MaxHp = Math.Max(1, maxHp);
            CurrentHp = Math.Max(0, Math.Min(currentHp, MaxHp));
        }

        /// <summary>
        /// 承受伤害
        /// Take damage
        /// </summary>
        /// <returns>实际承受的伤害值</returns>
        public int TakeDamage(int damage)
        {
            if (IsDead || damage <= 0)
                return 0;

            int actualDamage = Math.Min(damage, CurrentHp);
            CurrentHp -= actualDamage;
            TotalDamageTaken += actualDamage;

            if (IsDead)
            {
                Deaths++;
            }

            return actualDamage;
        }

        /// <summary>
        /// 接受治疗
        /// Receive healing
        /// </summary>
        /// <returns>实际恢复的生命值</returns>
        public int Heal(int amount)
        {
            if (IsDead || amount <= 0)
                return 0;

            int actualHeal = Math.Min(amount, MaxHp - CurrentHp);
            CurrentHp += actualHeal;
            return actualHeal;
        }

        /// <summary>
        /// 复活
        /// Revive
        /// </summary>
        public void Revive(int hpAmount = -1)
        {
            CurrentHp = hpAmount < 0 ? MaxHp : Math.Min(hpAmount, MaxHp);
        }

        /// <summary>
        /// 重置状态
        /// Reset state
        /// </summary>
        public void Reset()
        {
            CurrentHp = MaxHp;
            TotalDamageTaken = 0;
            TotalDamageDealt = 0;
            Kills = 0;
            Deaths = 0;
        }

        /// <summary>
        /// 记录造成的伤害
        /// Record damage dealt
        /// </summary>
        public void RecordDamageDealt(int damage, bool isKill = false)
        {
            TotalDamageDealt += damage;
            if (isKill)
            {
                Kills++;
            }
        }

        /// <summary>
        /// 更新最大生命值（用于Buff/Debuff效果）
        /// Update max HP (for buff/debuff effects)
        /// </summary>
        public void UpdateMaxHp(int newMaxHp)
        {
            int oldMaxHp = MaxHp;
            MaxHp = Math.Max(1, newMaxHp);

            // 按比例调整当前生命值
            if (oldMaxHp > 0)
            {
                CurrentHp = (int)Math.Ceiling(CurrentHp * ((double)MaxHp / oldMaxHp));
                CurrentHp = Math.Min(CurrentHp, MaxHp);
            }
        }
    }

    /// <summary>
    /// 队伍类型枚举
    /// Team type enumeration
    /// </summary>
    public enum TeamType
    {
        /// <summary>
        /// 玩家队伍
        /// Player team
        /// </summary>
        Player = 1,

        /// <summary>
        /// 敌人队伍
        /// Enemy team
        /// </summary>
        Enemy = 2
    }
}