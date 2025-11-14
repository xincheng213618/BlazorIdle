# Step2 技能系统补充设计 - 上篇：技能学习与管理

## 📋 文档说明

本文档是对 Step2 实施计划的重要补充，基于用户反馈增加以下核心功能：

1. **技能学习与管理系统（持久化）**
2. **技能配置UI组件**
3. **职业固定技能差异化**

**版本：** v1.1  
**创建日期：** 2025-11-14  
**状态：** 补充设计

---

## 一、技能学习与管理系统（持久化）

### 1.1 数据模型扩展

#### CharacterData 新增字段

**文件：** `BlazorIdle.Shared/Models/CharacterData.cs`

```csharp
/// <summary>
/// 角色已学习的技能 - 跨职业共享
/// Learned skills - shared across all professions
/// </summary>
[JsonPropertyName("learnedSkills")]
public HashSet<string> LearnedSkills { get; set; } = new HashSet<string>();

/// <summary>
/// 角色已装备的技能 - 按职业分组
/// Equipped skills - grouped by profession
/// Key: professionId, Value: 装备的技能配置
/// </summary>
[JsonPropertyName("equippedSkillsByProfession")]
public Dictionary<string, EquippedSkillsConfig> EquippedSkillsByProfession { get; set; } = new Dictionary<string, EquippedSkillsConfig>();
```

#### EquippedSkillsConfig 新增类

**文件：** `BlazorIdle.Shared/Models/EquippedSkillsConfig.cs`

```csharp
using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 职业装备的技能配置
    /// Equipped skills configuration for a profession
    /// </summary>
    public class EquippedSkillsConfig
    {
        /// <summary>
        /// 职业ID
        /// </summary>
        [JsonPropertyName("professionId")]
        public string ProfessionId { get; set; } = string.Empty;

        /// <summary>
        /// 主动技能槽位 (3个槽位)
        /// Active skill slots (3 slots)
        /// Key: slotId (active_1, active_2, active_3)
        /// Value: skillId
        /// </summary>
        [JsonPropertyName("activeSlots")]
        public Dictionary<string, string?> ActiveSlots { get; set; } = new Dictionary<string, string?>
        {
            { "active_1", null },
            { "active_2", null },
            { "active_3", null }
        };

        /// <summary>
        /// 被动技能槽位 (1个槽位)
        /// Passive skill slot (1 slot)
        /// </summary>
        [JsonPropertyName("passiveSlot")]
        public string? PassiveSlot { get; set; }
    }
}
```

### 1.2 技能学习逻辑

#### SkillLearningManager 新增类

**文件：** `BlazorIdle.Shared/Game/Skills/SkillLearningManager.cs`

```csharp
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Shared.Game.Skills
{
    /// <summary>
    /// 技能学习管理器
    /// Manages skill learning and unlocking
    /// </summary>
    public class SkillLearningManager
    {
        private readonly SkillRepository _skillRepository;

        public SkillLearningManager(SkillRepository skillRepository)
        {
            _skillRepository = skillRepository;
        }

        /// <summary>
        /// 检查技能是否可学习
        /// Check if a skill can be learned
        /// </summary>
        public bool CanLearnSkill(string skillId, CharacterData character)
        {
            var skill = _skillRepository.GetSkillById(skillId);
            if (skill == null) return false;

            // 固定技能不需要学习（自动拥有）
            if (skill.Fixed) return false;

            // 已学习过
            if (character.LearnedSkills.Contains(skillId)) return false;

            // 检查职业限制
            if (skill.AllowedProfessions != null && skill.AllowedProfessions.Count > 0)
            {
                // 检查角色是否有任一允许的职业
                bool hasProfession = false;
                foreach (var profId in skill.AllowedProfessions)
                {
                    if (character.Professions.ContainsKey(profId))
                    {
                        hasProfession = true;
                        break;
                    }
                }
                if (!hasProfession) return false;
            }

            // 检查解锁条件
            if (skill.Unlock != null)
            {
                // 检查等级条件（使用对应职业的等级）
                if (skill.Unlock.MinLevel.HasValue)
                {
                    // 如果技能有职业限制，检查该职业的等级
                    if (skill.AllowedProfessions != null && skill.AllowedProfessions.Count > 0)
                    {
                        bool meetsLevelRequirement = false;
                        foreach (var profId in skill.AllowedProfessions)
                        {
                            if (character.Professions.TryGetValue(profId, out var profProgress))
                            {
                                if (profProgress.Level >= skill.Unlock.MinLevel.Value)
                                {
                                    meetsLevelRequirement = true;
                                    break;
                                }
                            }
                        }
                        if (!meetsLevelRequirement) return false;
                    }
                }

                // 检查账户标记（预留）
                if (skill.Unlock.AccountFlags != null && skill.Unlock.AccountFlags.Count > 0)
                {
                    // TODO: 实现账户标记检查
                    // 暂时返回 true（后续实现账户系统时完善）
                }

                // 检查职业等级（预留）
                if (skill.Unlock.RequiresProfessionLevel.HasValue)
                {
                    // TODO: 实现职业等级检查
                }
            }

            return true;
        }

        /// <summary>
        /// 学习技能
        /// Learn a skill
        /// </summary>
        public bool LearnSkill(string skillId, CharacterData character)
        {
            if (!CanLearnSkill(skillId, character))
                return false;

            character.LearnedSkills.Add(skillId);
            return true;
        }

        /// <summary>
        /// 获取角色可学习的所有技能
        /// Get all skills that can be learned by the character
        /// </summary>
        public List<SkillDef> GetLearnableSkills(CharacterData character, string professionId)
        {
            var allSkills = _skillRepository.GetSkillsByProfession(professionId);
            var learnableSkills = new List<SkillDef>();

            foreach (var skill in allSkills)
            {
                // 排除固定技能
                if (skill.Fixed) continue;

                // 排除已学习的
                if (character.LearnedSkills.Contains(skill.Id)) continue;

                learnableSkills.Add(skill);
            }

            return learnableSkills;
        }

        /// <summary>
        /// 获取角色已学习的技能
        /// Get all learned skills by the character
        /// </summary>
        public List<SkillDef> GetLearnedSkills(CharacterData character, string professionId)
        {
            var learned = new List<SkillDef>();

            foreach (var skillId in character.LearnedSkills)
            {
                var skill = _skillRepository.GetSkillById(skillId);
                if (skill != null)
                {
                    // 检查是否属于指定职业
                    if (skill.AllowedProfessions == null || 
                        skill.AllowedProfessions.Count == 0 || 
                        skill.AllowedProfessions.Contains(professionId))
                    {
                        learned.Add(skill);
                    }
                }
            }

            return learned;
        }
    }
}
```

### 1.3 技能装备逻辑

#### SkillEquipmentManager 新增类

**文件：** `BlazorIdle.Shared/Game/Skills/SkillEquipmentManager.cs`

```csharp
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Shared.Game.Skills
{
    /// <summary>
    /// 技能装备管理器
    /// Manages skill equipment to slots
    /// </summary>
    public class SkillEquipmentManager
    {
        private readonly SkillRepository _skillRepository;

        public SkillEquipmentManager(SkillRepository skillRepository)
        {
            _skillRepository = skillRepository;
        }

        /// <summary>
        /// 装备技能到指定槽位
        /// Equip a skill to a specific slot
        /// </summary>
        public bool EquipSkill(string skillId, string slotId, CharacterData character, string professionId)
        {
            var skill = _skillRepository.GetSkillById(skillId);
            if (skill == null) return false;

            // 检查技能是否已学习（固定技能除外）
            if (!skill.Fixed && !character.LearnedSkills.Contains(skillId))
                return false;

            // 检查职业限制
            if (skill.AllowedProfessions != null && 
                skill.AllowedProfessions.Count > 0 && 
                !skill.AllowedProfessions.Contains(professionId))
                return false;

            // 确保职业配置存在
            if (!character.EquippedSkillsByProfession.ContainsKey(professionId))
            {
                character.EquippedSkillsByProfession[professionId] = new EquippedSkillsConfig
                {
                    ProfessionId = professionId
                };
            }

            var config = character.EquippedSkillsByProfession[professionId];

            // 检查槽位类型匹配
            if (slotId == "passive_1")
            {
                if (skill.SlotType != "passive")
                    return false;
                config.PassiveSlot = skillId;
            }
            else if (slotId.StartsWith("active_"))
            {
                if (skill.SlotType != "active")
                    return false;
                config.ActiveSlots[slotId] = skillId;
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 卸载技能
        /// Unequip a skill from a slot
        /// </summary>
        public bool UnequipSkill(string slotId, CharacterData character, string professionId)
        {
            if (!character.EquippedSkillsByProfession.ContainsKey(professionId))
                return false;

            var config = character.EquippedSkillsByProfession[professionId];

            if (slotId == "passive_1")
            {
                config.PassiveSlot = null;
            }
            else if (config.ActiveSlots.ContainsKey(slotId))
            {
                config.ActiveSlots[slotId] = null;
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取角色当前装备的所有技能
        /// Get all currently equipped skills for the character
        /// </summary>
        public List<SkillDef> GetEquippedSkills(CharacterData character, string professionId)
        {
            var equipped = new List<SkillDef>();

            if (!character.EquippedSkillsByProfession.TryGetValue(professionId, out var config))
                return equipped;

            // 添加主动技能
            foreach (var kvp in config.ActiveSlots)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    var skill = _skillRepository.GetSkillById(kvp.Value);
                    if (skill != null)
                        equipped.Add(skill);
                }
            }

            // 添加被动技能
            if (!string.IsNullOrEmpty(config.PassiveSlot))
            {
                var skill = _skillRepository.GetSkillById(config.PassiveSlot);
                if (skill != null)
                    equipped.Add(skill);
            }

            return equipped;
        }

        /// <summary>
        /// 初始化职业的固定技能
        /// Initialize fixed skills for a profession
        /// </summary>
        public void InitializeFixedSkills(CharacterData character, string professionId)
        {
            var fixedSkills = _skillRepository.GetSkillsByProfession(professionId)
                .Where(s => s.Fixed)
                .ToList();

            // 确保职业配置存在
            if (!character.EquippedSkillsByProfession.ContainsKey(professionId))
            {
                character.EquippedSkillsByProfession[professionId] = new EquippedSkillsConfig
                {
                    ProfessionId = professionId
                };
            }

            var config = character.EquippedSkillsByProfession[professionId];

            // 固定技能自动装备到对应槽位
            // 注意：固定技能不占用可配置槽位，它们有独立的处理逻辑
            // 这里仅做记录，实际战斗中固定技能会被特殊处理
        }
    }
}
```

### 1.4 技能学习与使用机制

**核心设计决策：学会即可用**

技能学习系统采用简化的"学会即可用"原则，降低玩家学习成本：

#### 1.4.1 学习即拥有，永久有效

1. **永久性保证**
   - 技能一旦学习成功，永久添加到 `LearnedSkills` 集合
   - 即使角色等级降低（如死亡惩罚），已学习的技能不会失效
   - 已学习的技能不会因为任何原因被移除（除非玩家删除角色）

2. **解锁条件的作用范围**
   - `unlock.minLevel` 等条件**仅在学习时检查**
   - **学习后，技能立即可以装备和使用，无需其他条件**
   - 这确保了玩家的进度不会倒退，提供流畅的游戏体验

3. **职业切换的影响**
   - `LearnedSkills` 跨职业共享 - 所有职业共用同一个已学习技能池
   - 但技能的**装备**受职业限制（`allowedProfessions`）
   - 切换职业时，已学习的技能保留，但只能装备当前职业允许的技能

#### 1.4.2 使用条件 vs 学习条件

**明确区分两种条件：**

| 条件类型 | 检查时机 | 影响范围 | 示例 |
|---------|---------|---------|------|
| **学习条件** | 学习技能时 | 是否可学习 | minLevel=7（需要7级才能学） |
| **使用条件** | 战斗中施放时 | 是否可施放 | hpBelowPct=50（HP<50%才能用） |

- **学习条件（unlock）：** 
  - 只在学习时检查一次
  - 学习后不再检查
  - 包括：minLevel, requiresProfessionLevel, accountFlags

- **使用条件（conditions）：**
  - 每次施放时都检查
  - 战斗中动态判定
  - 包括：hpBelowPct, hpAbovePct, requireBuffId, forbidBuffId, requireResource

#### 1.4.3 固定技能初始化策略

**防御式设计，确保固定技能总是可用：**

- 在任何访问技能槽位的方法中，先检查当前职业配置是否存在
- 如果不存在或为空，自动调用 `InitializeFixedSkills()` 初始化
- 这样确保无论在何种情况下，固定技能都能正确初始化

**建议检查位置：**
- `GetEquippedSkills()` - 获取装备技能时检查
- `EquipSkill()` - 装备技能时检查
- `Character` 构造函数 - 创建时检查
- `ChangeProfession()` - 切换职业时检查

**实现示例：**
```csharp
public List<string> GetEquippedSkills(string professionId)
{
    // 防御式检查：如果职业配置不存在，自动初始化
    if (!EquippedSkillsByProfession.ContainsKey(professionId) || 
        EquippedSkillsByProfession[professionId] == null)
    {
        InitializeFixedSkills(professionId);
    }
    
    // 返回技能列表
    var config = EquippedSkillsByProfession[professionId];
    var skills = new List<string>();
    skills.AddRange(config.ActiveSlots.Values.Where(s => !string.IsNullOrEmpty(s)));
    if (!string.IsNullOrEmpty(config.PassiveSlot))
        skills.Add(config.PassiveSlot);
    return skills;
}
```

#### 1.4.4 示例场景

**场景 1：学习和使用**
```
1. 角色在战士职业 7 级时达到学习"雷霆一击"的条件（minLevel=7）
2. 玩家点击"学习"，技能添加到 LearnedSkills
3. 玩家立即可以将"雷霆一击"装备到技能槽位
4. 战斗中，"雷霆一击"可以正常使用（无额外限制）
```

**场景 2：等级降低不影响使用**
```
1. 角色 10 级学习了"雷霆一击"（需要 7 级）
2. 角色死亡惩罚，等级降至 6 级
3. "雷霆一击"仍然在 LearnedSkills 中
4. 仍然可以装备和使用"雷霆一击"（不受等级影响）
```

**场景 3：职业切换**
```
1. 战士职业学习了"雷霆一击"（allowedProfessions=["warrior"]）
2. 角色切换到法师职业
3. "雷霆一击"仍在 LearnedSkills 中（不丢失）
4. 但无法装备"雷霆一击"（职业限制）
5. 切换回战士后，可以重新装备
```

**场景 4：使用条件动态检查**
```
1. 学习了"肾上腺素"（条件：hpBelowPct=50）
2. 可以装备到技能槽位（无学习后限制）
3. 战斗中 HP > 50% 时，"肾上腺素"不会触发（使用条件不满足）
4. 战斗中 HP < 50% 时，"肾上腺素"可以触发
```

#### 1.4.5 设计优势

**简化玩家体验：**
- ✅ 学会即可用，无需多余的装备前置条件
- ✅ 玩家进度不会倒退（等级降低不影响已学技能）
- ✅ 鼓励玩家探索和学习更多技能
- ✅ 职业切换更加灵活（技能不丢失）
- ✅ 降低了玩家的挫败感

**技术实现清晰：**
- ✅ `LearnedSkills` 只增不减（HashSet<string>）
- ✅ 学习条件和使用条件分离明确
- ✅ 固定技能防御式初始化
- ✅ 序列化持久化简单可靠

---

### 1.5 实施任务更新

需要在原 Step2_实施进度追踪.md 中插入新的阶段：

**新增阶段 2.5：技能学习与装备系统（P0 - 必须）**

**状态：** ⬜ 未开始

**目标：** 实现技能学习和装备的持久化管理。

**任务清单：**

- [ ] 2.5.1 扩展 CharacterData 数据模型
  - 添加 LearnedSkills 字段（HashSet<string>）
  - 添加 EquippedSkillsByProfession 字段（Dictionary）

- [ ] 2.5.2 创建 EquippedSkillsConfig 类
  - ActiveSlots 字典（3个主动槽位）
  - PassiveSlot 字符串（1个被动槽位）

- [ ] 2.5.3 实现 SkillLearningManager
  - CanLearnSkill() 方法（检查等级、职业、已学习）
  - LearnSkill() 方法
  - GetLearnableSkills() 方法
  - GetLearnedSkills() 方法

- [ ] 2.5.4 实现 SkillEquipmentManager
  - EquipSkill() 方法（验证槽位类型、职业限制）
  - UnequipSkill() 方法
  - GetEquippedSkills() 方法
  - InitializeFixedSkills() 方法

- [ ] 2.5.5 数据持久化
  - 确保 CharacterData 序列化包含新字段
  - 测试存档加载和保存

- [ ] 2.5.6 单元测试（18 个）
  - SkillLearningManager 测试（8 个）
  - SkillEquipmentManager 测试（8 个）
  - 持久化测试（2 个）

**验收标准：**
- ✅ CharacterData 正确序列化技能数据
- ✅ 技能学习条件正确判定
- ✅ 技能装备验证正确工作
- ✅ 固定技能自动初始化
- ✅ 18 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 3-4 小时

---

## 二、技能配置UI组件

### 2.1 技能学习界面组件

#### SkillLearningPanel 新增组件

**文件：** `BlazorIdle/Components/SkillLearningPanel.razor`

```razor
@using BlazorIdle.Shared.Game.Skills
@using BlazorIdle.Shared.Models

<div class="skill-learning-panel">
    <h3>技能学习 - @ProfessionName</h3>
    
    <div class="skill-categories">
        <!-- 可学习的技能 -->
        <div class="learnable-skills">
            <h4>可学习技能</h4>
            <div class="skill-list">
                @foreach (var skill in LearnableSkills)
                {
                    var canLearn = CanLearnSkill(skill);
                    var cssClass = canLearn ? "skill-card learnable" : "skill-card locked";
                    
                    <div class="@cssClass" @onclick="() => OnLearnSkill(skill)">
                        <div class="skill-icon">
                            <span class="skill-letter">@skill.Id.Substring(0, 1).ToUpper()</span>
                        </div>
                        <div class="skill-info">
                            <div class="skill-name">@skill.Name</div>
                            <div class="skill-level">需要等级: @GetRequiredLevel(skill)</div>
                            @if (!canLearn)
                            {
                                <div class="skill-locked-reason">
                                    @GetLockReason(skill)
                                </div>
                            }
                        </div>
                        @if (canLearn)
                        {
                            <button class="btn-learn">学习</button>
                        }
                    </div>
                }
            </div>
        </div>

        <!-- 已学习的技能 -->
        <div class="learned-skills">
            <h4>已学习技能</h4>
            <div class="skill-list">
                @foreach (var skill in LearnedSkills)
                {
                    <div class="skill-card learned">
                        <div class="skill-icon">
                            <span class="skill-letter">@skill.Id.Substring(0, 1).ToUpper()</span>
                        </div>
                        <div class="skill-info">
                            <div class="skill-name">@skill.Name</div>
                            <div class="skill-type">@GetSkillTypeText(skill)</div>
                        </div>
                        <span class="learned-badge">✓</span>
                    </div>
                }
            </div>
        </div>
    </div>
</div>

@code {
    [Parameter]
    public CharacterData? Character { get; set; }

    [Parameter]
    public string ProfessionId { get; set; } = "warrior";

    [Parameter]
    public EventCallback<string> OnSkillLearned { get; set; }

    private string ProfessionName => GetProfessionName(ProfessionId);
    private List<SkillDef> LearnableSkills { get; set; } = new();
    private List<SkillDef> LearnedSkills { get; set; } = new();

    private SkillLearningManager? _learningManager;
    private SkillRepository? _skillRepository;

    protected override void OnParametersSet()
    {
        if (Character != null && _skillRepository != null && _learningManager != null)
        {
            LearnableSkills = _learningManager.GetLearnableSkills(Character, ProfessionId);
            LearnedSkills = _learningManager.GetLearnedSkills(Character, ProfessionId);
        }
    }

    private bool CanLearnSkill(SkillDef skill)
    {
        if (Character == null || _learningManager == null) return false;
        return _learningManager.CanLearnSkill(skill.Id, Character);
    }

    private int GetRequiredLevel(SkillDef skill)
    {
        return skill.Unlock?.MinLevel ?? 1;
    }

    private string GetLockReason(SkillDef skill)
    {
        if (Character == null) return "角色未加载";
        
        if (skill.Unlock?.MinLevel.HasValue == true)
        {
            var profProgress = Character.Professions.GetValueOrDefault(ProfessionId);
            if (profProgress != null && profProgress.Level < skill.Unlock.MinLevel.Value)
            {
                return $"需要等级 {skill.Unlock.MinLevel.Value}（当前 {profProgress.Level}）";
            }
        }

        return "条件未满足";
    }

    private string GetSkillTypeText(SkillDef skill)
    {
        var type = skill.Type == "active" ? "主动" : "被动";
        var release = skill.ReleaseType == "instant" ? "瞬发" : "施法";
        return $"{type} · {release}";
    }

    private async Task OnLearnSkill(SkillDef skill)
    {
        if (Character == null || _learningManager == null) return;
        
        if (_learningManager.LearnSkill(skill.Id, Character))
        {
            // 刷新列表
            LearnableSkills = _learningManager.GetLearnableSkills(Character, ProfessionId);
            LearnedSkills = _learningManager.GetLearnedSkills(Character, ProfessionId);
            
            // 触发回调
            await OnSkillLearned.InvokeAsync(skill.Id);
            
            StateHasChanged();
        }
    }

    private string GetProfessionName(string professionId)
    {
        return professionId switch
        {
            "warrior" => "战士",
            "mage" => "法师",
            "ranger" => "游侠",
            "rogue" => "盗贼",
            _ => professionId
        };
    }
}
```

**样式文件：** `BlazorIdle/Components/SkillLearningPanel.razor.css`

```css
.skill-learning-panel {
    padding: 20px;
    background: #f5f5f5;
    border-radius: 8px;
}

.skill-learning-panel h3 {
    margin-top: 0;
    color: #333;
}

.skill-categories {
    display: flex;
    gap: 20px;
}

.learnable-skills,
.learned-skills {
    flex: 1;
}

.skill-list {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.skill-card {
    display: flex;
    align-items: center;
    padding: 12px;
    background: white;
    border-radius: 6px;
    border: 2px solid #ddd;
    transition: all 0.2s;
}

.skill-card.learnable {
    cursor: pointer;
    border-color: #4CAF50;
}

.skill-card.learnable:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 8px rgba(0,0,0,0.1);
}

.skill-card.locked {
    opacity: 0.6;
    border-color: #ccc;
    background: #f9f9f9;
}

.skill-card.learned {
    border-color: #2196F3;
}

.skill-icon {
    width: 48px;
    height: 48px;
    border-radius: 4px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    margin-right: 12px;
}

.skill-letter {
    font-size: 24px;
    font-weight: bold;
    color: white;
}

.skill-info {
    flex: 1;
}

.skill-name {
    font-weight: bold;
    color: #333;
    margin-bottom: 4px;
}

.skill-level {
    font-size: 12px;
    color: #666;
}

.skill-locked-reason {
    font-size: 11px;
    color: #f44336;
    margin-top: 2px;
}

.skill-type {
    font-size: 12px;
    color: #666;
}

.btn-learn {
    padding: 6px 16px;
    background: #4CAF50;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-weight: bold;
}

.btn-learn:hover {
    background: #45a049;
}

.learned-badge {
    width: 24px;
    height: 24px;
    background: #4CAF50;
    color: white;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 16px;
}
```

---

## 文档说明

本文档为上篇，包含：
- 技能学习与管理系统的数据模型和逻辑
- 技能学习界面UI组件

**下一篇内容预告：**
- 技能装备界面UI组件
- 职业固定技能差异化设计（战士架势系统、法师/盗贼特色机制）

**最后更新：** 2025-11-14  
**维护者：** @copilot  
**状态：** 进行中
