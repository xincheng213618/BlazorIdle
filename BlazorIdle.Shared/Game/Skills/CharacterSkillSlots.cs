using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 角色技能槽位管理器 (Step 2 Phase 2)
    /// Character skill slots manager
    /// </summary>
    public sealed class CharacterSkillSlots
    {
        private readonly Dictionary<string, SkillSlotConfig> _slots = new();
        private readonly SkillRepository _skillRepository;

        /// <summary>
        /// 当前职业ID
        /// Current profession ID
        /// </summary>
        public string ProfessionId { get; private set; } = "";

        public CharacterSkillSlots(SkillRepository skillRepository)
        {
            _skillRepository = skillRepository;
        }

        /// <summary>
        /// 初始化技能槽位（为指定职业创建默认槽位配置）
        /// Initialize skill slots (create default slot configuration for profession)
        /// </summary>
        public void Initialize(string professionId)
        {
            ProfessionId = professionId;
            _slots.Clear();

            // 创建3个主动槽位
            // Create 3 active slots
            for (int i = 1; i <= 3; i++)
            {
                _slots[$"active_{i}"] = new SkillSlotConfig
                {
                    SlotId = $"active_{i}",
                    SlotType = "active",
                    SkillId = null,
                    IsFixed = false
                };
            }

            // 创建1个被动槽位
            // Create 1 passive slot
            _slots["passive_1"] = new SkillSlotConfig
            {
                SlotId = "passive_1",
                SlotType = "passive",
                SkillId = null,
                IsFixed = false
            };

            // 自动装配固定技能
            // Auto-equip fixed skills
            AutoEquipFixedSkills();
        }

        /// <summary>
        /// 自动装配固定技能
        /// Auto-equip fixed skills
        /// </summary>
        private void AutoEquipFixedSkills()
        {
            var fixedSkills = _skillRepository.GetSkillsByProfession(ProfessionId)
                .Where(s => s.Fixed)
                .ToList();

            foreach (var skill in fixedSkills)
            {
                // 找到第一个空的、类型匹配的槽位
                // Find first empty slot of matching type
                var emptySlot = _slots.Values
                    .FirstOrDefault(s => s.SlotType == skill.SlotType && s.SkillId == null);

                if (emptySlot != null)
                {
                    emptySlot.SkillId = skill.Id;
                    emptySlot.IsFixed = true;
                }
            }
        }

        /// <summary>
        /// 装备技能到指定槽位
        /// Equip skill to specified slot
        /// </summary>
        public bool EquipSkill(string slotId, string skillId)
        {
            if (!_slots.TryGetValue(slotId, out var slot))
                return false;

            // 固定槽位不能修改
            // Fixed slots cannot be modified
            if (slot.IsFixed)
                return false;

            var skill = _skillRepository.GetSkill(skillId);
            if (skill == null)
                return false;

            // 验证职业限制
            // Validate profession restriction
            if (skill.AllowedProfessions != null && 
                skill.AllowedProfessions.Count > 0 && 
                !skill.AllowedProfessions.Contains(ProfessionId))
                return false;

            // 验证槽位类型匹配
            // Validate slot type matching
            if (skill.SlotType != slot.SlotType)
                return false;

            // 检查是否已装备在其他槽位（防止重复）
            // Check if already equipped in another slot (prevent duplication)
            if (_slots.Values.Any(s => s.SkillId == skillId && s.SlotId != slotId))
                return false;

            slot.SkillId = skillId;
            return true;
        }

        /// <summary>
        /// 卸载指定槽位的技能
        /// Unequip skill from specified slot
        /// </summary>
        public bool UnequipSkill(string slotId)
        {
            if (!_slots.TryGetValue(slotId, out var slot))
                return false;

            // 固定槽位不能卸载
            // Fixed slots cannot be unequipped
            if (slot.IsFixed)
                return false;

            slot.SkillId = null;
            return true;
        }

        /// <summary>
        /// 获取所有主动技能（按槽位顺序）
        /// Get all active skills (in slot order)
        /// </summary>
        public List<SkillDef> GetActiveSkills()
        {
            var skills = new List<SkillDef>();

            for (int i = 1; i <= 3; i++)
            {
                var slotId = $"active_{i}";
                if (_slots.TryGetValue(slotId, out var slot) && !string.IsNullOrEmpty(slot.SkillId))
                {
                    var skill = _skillRepository.GetSkill(slot.SkillId);
                    if (skill != null)
                        skills.Add(skill);
                }
            }

            return skills;
        }

        /// <summary>
        /// 获取所有被动技能
        /// Get all passive skills
        /// </summary>
        public List<SkillDef> GetPassiveSkills()
        {
            var skills = new List<SkillDef>();

            if (_slots.TryGetValue("passive_1", out var slot) && !string.IsNullOrEmpty(slot.SkillId))
            {
                var skill = _skillRepository.GetSkill(slot.SkillId);
                if (skill != null)
                    skills.Add(skill);
            }

            return skills;
        }

        /// <summary>
        /// 获取所有固定技能
        /// Get all fixed skills
        /// </summary>
        public List<SkillDef> GetFixedSkills()
        {
            var skills = new List<SkillDef>();

            foreach (var slot in _slots.Values.Where(s => s.IsFixed && !string.IsNullOrEmpty(s.SkillId)))
            {
                var skill = _skillRepository.GetSkill(slot.SkillId!);
                if (skill != null)
                    skills.Add(skill);
            }

            return skills;
        }

        /// <summary>
        /// 获取所有可配置技能（非固定）
        /// Get all configurable skills (non-fixed)
        /// </summary>
        public List<SkillDef> GetConfigurableSkills()
        {
            var skills = new List<SkillDef>();

            foreach (var slot in _slots.Values.Where(s => !s.IsFixed && !string.IsNullOrEmpty(s.SkillId)))
            {
                var skill = _skillRepository.GetSkill(slot.SkillId!);
                if (skill != null)
                    skills.Add(skill);
            }

            return skills;
        }

        /// <summary>
        /// 获取指定槽位的配置
        /// Get slot configuration
        /// </summary>
        public SkillSlotConfig? GetSlot(string slotId)
        {
            return _slots.GetValueOrDefault(slotId);
        }

        /// <summary>
        /// 获取所有槽位配置
        /// Get all slot configurations
        /// </summary>
        public IReadOnlyDictionary<string, SkillSlotConfig> GetAllSlots()
        {
            return _slots;
        }
    }
}
