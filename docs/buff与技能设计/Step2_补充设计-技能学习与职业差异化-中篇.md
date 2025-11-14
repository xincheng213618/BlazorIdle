# Step2 技能系统补充设计 - 中篇：技能装备UI与职业固定技能差异化

## 📋 文档说明

本文档为中篇，继续补充 Step2 实施计划的核心功能。

**内容包括：**
1. 技能装备界面UI组件
2. 职业固定技能差异化设计（战士、法师特色机制）

**版本：** v1.1  
**创建日期：** 2025-11-14  

---

## 三、技能装备界面UI组件

### 3.1 SkillEquipmentPanel 组件

**文件：** `BlazorIdle/Components/SkillEquipmentPanel.razor`

```razor
@using BlazorIdle.Shared.Game.Skills
@using BlazorIdle.Shared.Models

<div class="skill-equipment-panel">
    <h3>技能配置 - @ProfessionName</h3>
    
    <div class="equipment-layout">
        <!-- 技能槽位区域 -->
        <div class="skill-slots">
            <h4>主动技能槽位</h4>
            <div class="active-slots">
                @for (int i = 1; i <= 3; i++)
                {
                    var slotId = $"active_{i}";
                    var equippedSkillId = GetEquippedSkillId(slotId);
                    
                    <div class="skill-slot @(SelectedSlot == slotId ? "selected" : "")" 
                         @onclick="() => SelectSlot(slotId)">
                        <div class="slot-number">@i</div>
                        @if (!string.IsNullOrEmpty(equippedSkillId))
                        {
                            var skill = GetSkill(equippedSkillId);
                            if (skill != null)
                            {
                                <div class="equipped-skill">
                                    <div class="skill-icon-small">
                                        @skill.Id.Substring(0, 1).ToUpper()
                                    </div>
                                    <div class="skill-name-small">@skill.Name</div>
                                    <button class="btn-unequip" @onclick:stopPropagation="true" 
                                            @onclick="() => UnequipFromSlot(slotId)">×</button>
                                </div>
                            }
                        }
                        else
                        {
                            <div class="empty-slot">
                                <span>空槽位</span>
                            </div>
                        }
                    </div>
                }
            </div>

            <h4>被动技能槽位</h4>
            <div class="passive-slots">
                @{
                    var passiveSlotId = "passive_1";
                    var equippedPassiveId = GetEquippedSkillId(passiveSlotId);
                }
                <div class="skill-slot passive @(SelectedSlot == passiveSlotId ? "selected" : "")" 
                     @onclick="() => SelectSlot(passiveSlotId)">
                    <div class="slot-number">P</div>
                    @if (!string.IsNullOrEmpty(equippedPassiveId))
                    {
                        var skill = GetSkill(equippedPassiveId);
                        if (skill != null)
                        {
                            <div class="equipped-skill">
                                <div class="skill-icon-small passive">
                                    @skill.Id.Substring(0, 1).ToUpper()
                                </div>
                                <div class="skill-name-small">@skill.Name</div>
                                <button class="btn-unequip" @onclick:stopPropagation="true" 
                                        @onclick="() => UnequipFromSlot(passiveSlotId)">×</button>
                            </div>
                        }
                    }
                    else
                    {
                        <div class="empty-slot">
                            <span>空槽位</span>
                        </div>
                    }
                </div>
            </div>
        </div>

        <!-- 可装备技能列表 -->
        <div class="available-skills">
            <h4>可装备技能 @(string.IsNullOrEmpty(SelectedSlot) ? "" : $"- {GetSlotTypeText()}")</h4>
            <div class="skill-list-equip">
                @if (!string.IsNullOrEmpty(SelectedSlot))
                {
                    var slotType = SelectedSlot.StartsWith("active") ? "active" : "passive";
                    var availableSkills = GetAvailableSkillsForSlot(slotType);
                    
                    @foreach (var skill in availableSkills)
                    {
                        var isEquipped = IsSkillEquipped(skill.Id);
                        <div class="skill-card-equip @(isEquipped ? "equipped" : "")" 
                             @onclick="() => EquipToSelectedSlot(skill.Id)">
                            <div class="skill-icon">
                                @skill.Id.Substring(0, 1).ToUpper()
                            </div>
                            <div class="skill-details">
                                <div class="skill-name">@skill.Name</div>
                                <div class="skill-desc">@GetSkillDescription(skill)</div>
                                @if (skill.Costs != null && skill.Costs.Count > 0)
                                {
                                    <div class="skill-cost">
                                        消耗: @string.Join(", ", skill.Costs.Select(c => $"{c.Amount} {GetResourceName(c.BucketId)}"))
                                    </div>
                                }
                                @if (skill.CooldownSec > 0)
                                {
                                    <div class="skill-cooldown">冷却: @skill.CooldownSec 秒</div>
                                }
                            </div>
                            @if (isEquipped)
                            {
                                <span class="equipped-badge">已装备</span>
                            }
                        </div>
                    }
                    
                    @if (availableSkills.Count == 0)
                    {
                        <div class="no-skills">
                            <p>暂无可装备的@(slotType == "active" ? "主动" : "被动")技能</p>
                            <p class="hint">请先在技能学习界面学习技能</p>
                        </div>
                    }
                }
                else
                {
                    <div class="no-slot-selected">
                        <p>请选择一个技能槽位</p>
                        <p class="hint">点击左侧槽位以查看可装备的技能</p>
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
    public EventCallback<string> OnSkillEquipped { get; set; }

    [Parameter]
    public EventCallback<string> OnSkillUnequipped { get; set; }

    private string? SelectedSlot { get; set; }
    private string ProfessionName => GetProfessionName(ProfessionId);

    private SkillEquipmentManager? _equipmentManager;
    private SkillLearningManager? _learningManager;
    private SkillRepository? _skillRepository;

    private string? GetEquippedSkillId(string slotId)
    {
        if (Character == null) return null;
        
        if (!Character.EquippedSkillsByProfession.TryGetValue(ProfessionId, out var config))
            return null;

        if (slotId == "passive_1")
            return config.PassiveSlot;
        
        return config.ActiveSlots.GetValueOrDefault(slotId);
    }

    private SkillDef? GetSkill(string skillId)
    {
        return _skillRepository?.GetSkillById(skillId);
    }

    private void SelectSlot(string slotId)
    {
        SelectedSlot = slotId;
        StateHasChanged();
    }

    private string GetSlotTypeText()
    {
        if (string.IsNullOrEmpty(SelectedSlot)) return "";
        return SelectedSlot.StartsWith("active") ? "主动技能" : "被动技能";
    }

    private List<SkillDef> GetAvailableSkillsForSlot(string slotType)
    {
        if (Character == null || _learningManager == null || _skillRepository == null)
            return new List<SkillDef>();

        var learnedSkills = _learningManager.GetLearnedSkills(Character, ProfessionId);
        return learnedSkills.Where(s => s.SlotType == slotType).ToList();
    }

    private bool IsSkillEquipped(string skillId)
    {
        if (Character == null) return false;
        
        if (!Character.EquippedSkillsByProfession.TryGetValue(ProfessionId, out var config))
            return false;

        if (config.PassiveSlot == skillId)
            return true;

        return config.ActiveSlots.Values.Any(v => v == skillId);
    }

    private async Task EquipToSelectedSlot(string skillId)
    {
        if (Character == null || _equipmentManager == null || string.IsNullOrEmpty(SelectedSlot))
            return;

        if (_equipmentManager.EquipSkill(skillId, SelectedSlot, Character, ProfessionId))
        {
            await OnSkillEquipped.InvokeAsync(skillId);
            StateHasChanged();
        }
    }

    private async Task UnequipFromSlot(string slotId)
    {
        if (Character == null || _equipmentManager == null) return;

        var skillId = GetEquippedSkillId(slotId);
        if (_equipmentManager.UnequipSkill(slotId, Character, ProfessionId))
        {
            if (!string.IsNullOrEmpty(skillId))
                await OnSkillUnequipped.InvokeAsync(skillId);
            StateHasChanged();
        }
    }

    private string GetSkillDescription(SkillDef skill)
    {
        if (skill.Damage != null)
        {
            var dmgText = $"{skill.Damage.CoefAtk:P0} 攻击力";
            if (skill.Damage.Flat > 0)
                dmgText += $" + {skill.Damage.Flat}";
            if (skill.Damage.IsAoe)
                dmgText += " (AoE)";
            return dmgText;
        }
        return skill.Type == "passive" ? "被动效果" : "特殊技能";
    }

    private string GetResourceName(string bucketId)
    {
        return bucketId switch
        {
            "rage" => "怒气",
            "mana" => "法力",
            "energy" => "能量",
            _ => bucketId
        };
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

**样式文件：** `BlazorIdle/Components/SkillEquipmentPanel.razor.css`

```css
.skill-equipment-panel {
    padding: 20px;
    background: #f5f5f5;
    border-radius: 8px;
}

.equipment-layout {
    display: flex;
    gap: 20px;
}

.skill-slots {
    width: 300px;
}

.active-slots,
.passive-slots {
    display: flex;
    flex-direction: column;
    gap: 10px;
    margin-bottom: 20px;
}

.skill-slot {
    position: relative;
    padding: 12px;
    background: white;
    border: 3px solid #ddd;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.2s;
    min-height: 80px;
    display: flex;
    align-items: center;
}

.skill-slot:hover {
    border-color: #2196F3;
}

.skill-slot.selected {
    border-color: #4CAF50;
    box-shadow: 0 0 0 3px rgba(76, 175, 80, 0.2);
}

.skill-slot.passive {
    border-color: #9C27B0;
}

.skill-slot.passive.selected {
    border-color: #7B1FA2;
    box-shadow: 0 0 0 3px rgba(156, 39, 176, 0.2);
}

.slot-number {
    width: 32px;
    height: 32px;
    background: #2196F3;
    color: white;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
    margin-right: 12px;
}

.skill-slot.passive .slot-number {
    background: #9C27B0;
}

.equipped-skill {
    flex: 1;
    display: flex;
    align-items: center;
    gap: 8px;
}

.skill-icon-small {
    width: 36px;
    height: 36px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border-radius: 4px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
}

.skill-icon-small.passive {
    background: linear-gradient(135deg, #9C27B0 0%, #7B1FA2 100%);
}

.skill-name-small {
    flex: 1;
    font-weight: 500;
    color: #333;
}

.btn-unequip {
    width: 24px;
    height: 24px;
    background: #f44336;
    color: white;
    border: none;
    border-radius: 50%;
    cursor: pointer;
    font-size: 16px;
    line-height: 1;
    padding: 0;
}

.btn-unequip:hover {
    background: #d32f2f;
}

.empty-slot {
    flex: 1;
    text-align: center;
    color: #999;
    font-style: italic;
}

.available-skills {
    flex: 1;
}

.skill-list-equip {
    display: flex;
    flex-direction: column;
    gap: 10px;
    max-height: 600px;
    overflow-y: auto;
}

.skill-card-equip {
    display: flex;
    padding: 12px;
    background: white;
    border: 2px solid #ddd;
    border-radius: 6px;
    cursor: pointer;
    transition: all 0.2s;
}

.skill-card-equip:hover {
    border-color: #2196F3;
    transform: translateY(-2px);
}

.skill-card-equip.equipped {
    border-color: #4CAF50;
    background: #f1f8f4;
}

.skill-card-equip .skill-icon {
    width: 56px;
    height: 56px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 28px;
    font-weight: bold;
    margin-right: 12px;
}

.skill-details {
    flex: 1;
}

.skill-details .skill-name {
    font-weight: bold;
    color: #333;
    margin-bottom: 4px;
}

.skill-desc {
    font-size: 13px;
    color: #666;
    margin-bottom: 4px;
}

.skill-cost,
.skill-cooldown {
    font-size: 12px;
    color: #2196F3;
}

.equipped-badge {
    padding: 4px 12px;
    background: #4CAF50;
    color: white;
    border-radius: 12px;
    font-size: 12px;
    font-weight: bold;
    align-self: center;
}

.no-skills,
.no-slot-selected {
    text-align: center;
    padding: 40px 20px;
    color: #999;
}

.hint {
    font-size: 14px;
    color: #bbb;
    margin-top: 8px;
}
```

---

## 四、职业固定技能差异化设计

### 4.1 战士职业 - 架势系统

#### 设计理念

战士通过脉冲技能积累架势层数，满3层后获得必定暴击buff，强化下一次技能释放。

#### 相关Buff配置

需要在 `BlazorIdle.Shared/Config/buffs.json` 中添加：

```json
{
  "id": "warrior_stance",
  "name": "战士架势",
  "description": "脉冲技能获得，可叠加3层",
  "icon": "⚔️",
  "kind": "buff",
  "durationSec": null,
  "stackingPolicy": "stack",
  "maxStacks": 3,
  "tickIntervalSec": null,
  "effects": [
    {
      "type": "StatMultiplier",
      "target": "DamagePerAttack",
      "value": 0.05
    }
  ],
  "defaultTarget": "self"
},
{
  "id": "warrior_guaranteed_crit",
  "name": "破釜沉舟",
  "description": "下一次技能必定暴击",
  "icon": "💥",
  "kind": "buff",
  "durationSec": 10.0,
  "stackingPolicy": "refresh",
  "maxStacks": 1,
  "tickIntervalSec": null,
  "effects": [
    {
      "type": "ForceCrit",
      "target": "",
      "value": 1.0
    }
  ],
  "defaultTarget": "self"
}
```

#### 战士固定技能定义

**warrior_attack_basic** (普通攻击)
```json
{
  "id": "warrior_attack_basic",
  "name": "战士普通攻击",
  "type": "passive",
  "slotType": "passive",
  "fixed": true,
  "releaseType": "instant",
  "targetPolicy": "current_target",
  "cooldownSec": 0,
  "allowedProfessions": ["warrior"],
  "damage": {
    "coefAtk": 1.0,
    "flat": 0,
    "isAoe": false
  },
  "resourceGains": [
    {
      "bucketId": "rage",
      "amount": 1
    }
  ]
}
```

**warrior_special_pulse** (脉冲技能 - 架势系统)
```json
{
  "id": "warrior_special_pulse",
  "name": "战士战吼",
  "description": "发出战吼，积累架势。满3层架势时获得必定暴击buff",
  "type": "passive",
  "slotType": "passive",
  "fixed": true,
  "releaseType": "instant",
  "targetPolicy": "self",
  "cooldownSec": 0,
  "allowedProfessions": ["warrior"],
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "warrior_stance",
      "targetOverride": "self"
    }
  ],
  "triggers": [
    {
      "when": "OnPostCastWindow",
      "procChance": 1.0,
      "fireSkillId": "warrior_check_stance",
      "priority": 1
    }
  ]
}
```

**warrior_check_stance** (检查架势层数的触发技能)
```json
{
  "id": "warrior_check_stance",
  "name": "架势检查",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "self",
  "cooldownSec": 0,
  "allowedProfessions": ["warrior"],
  "conditions": {
    "requireBuffId": "warrior_stance"
  },
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "warrior_guaranteed_crit",
      "targetOverride": "self"
    },
    {
      "op": "remove",
      "buffIdToRemove": "warrior_stance"
    }
  ]
}
```

**注意：** 架势检查需要在Buff系统中实现层数判定，当warrior_stance达到3层时才触发guaranteed_crit。

### 4.2 法师职业 - 奥术充能系统

#### 设计理念

法师的资源上限确实是10点（已在professionAttributes.json中正确配置）。法师脉冲会产生奥术充能，提升法术强度。

#### 相关Buff配置

```json
{
  "id": "mage_arcane_power",
  "name": "奥术充能",
  "description": "法术伤害提升",
  "icon": "🔮",
  "kind": "buff",
  "durationSec": 8.0,
  "stackingPolicy": "stack",
  "maxStacks": 5,
  "tickIntervalSec": null,
  "effects": [
    {
      "type": "StatMultiplier",
      "target": "DamagePerAttack",
      "value": 0.08
    },
    {
      "type": "StatMultiplier",
      "target": "SpecialDamage",
      "value": 0.12
    }
  ],
  "defaultTarget": "self"
}
```

#### 法师固定技能定义

**mage_attack_basic** (普通攻击 - 火球术)
```json
{
  "id": "mage_attack_basic",
  "name": "火球术",
  "description": "法师的基础攻击，消耗少量法力",
  "type": "passive",
  "slotType": "passive",
  "fixed": true,
  "releaseType": "instant",
  "targetPolicy": "current_target",
  "cooldownSec": 0,
  "allowedProfessions": ["mage"],
  "damage": {
    "coefAtk": 0.9,
    "flat": 5,
    "isAoe": false
  },
  "resourceGains": [
    {
      "bucketId": "mana",
      "amount": 1
    }
  ]
}
```

**mage_special_pulse** (脉冲技能 - 奥术脉冲)
```json
{
  "id": "mage_special_pulse",
  "name": "奥术脉冲",
  "description": "释放奥术能量，获得奥术充能buff",
  "type": "passive",
  "slotType": "passive",
  "fixed": true,
  "releaseType": "instant",
  "targetPolicy": "self",
  "cooldownSec": 0,
  "allowedProfessions": ["mage"],
  "damage": {
    "coefAtk": 0.5,
    "flat": 3,
    "isAoe": true
  },
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "mage_arcane_power",
      "targetOverride": "self"
    }
  ],
  "resourceGains": [
    {
      "bucketId": "mana",
      "amount": 2
    }
  ]
}
```

### 4.3 游侠职业 - 集中射击系统

#### 相关Buff配置

```json
{
  "id": "ranger_focus",
  "name": "集中",
  "description": "提升暴击率和急速",
  "icon": "🎯",
  "kind": "buff",
  "durationSec": 6.0,
  "stackingPolicy": "stack",
  "maxStacks": 3,
  "tickIntervalSec": null,
  "effects": [
    {
      "type": "StatAdditive",
      "target": "CritChancePercent",
      "value": 5.0
    },
    {
      "type": "StatAdditive",
      "target": "HastePercent",
      "value": 3.0
    }
  ],
  "defaultTarget": "self"
}
```

#### 游侠固定技能

**ranger_attack_basic** (普通攻击 - 精准射击)
```json
{
  "id": "ranger_attack_basic",
  "name": "精准射击",
  "description": "游侠的基础射击攻击",
  "type": "passive",
  "slotType": "passive",
  "fixed": true,
  "releaseType": "instant",
  "targetPolicy": "current_target",
  "cooldownSec": 0,
  "allowedProfessions": ["ranger"],
  "damage": {
    "coefAtk": 1.0,
    "flat": 0,
    "isAoe": false
  },
  "resourceGains": [
    {
      "bucketId": "energy",
      "amount": 1
    }
  ]
}
```

**ranger_special_pulse** (脉冲技能 - 集中瞄准)
```json
{
  "id": "ranger_special_pulse",
  "name": "集中瞄准",
  "description": "进入集中状态，提升暴击率和攻击速度",
  "type": "passive",
  "slotType": "passive",
  "fixed": true,
  "releaseType": "instant",
  "targetPolicy": "self",
  "cooldownSec": 0,
  "allowedProfessions": ["ranger"],
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "ranger_focus",
      "targetOverride": "self"
    }
  ]
}
```

### 4.4 盗贼职业 - 连击系统

#### 相关Buff配置

```json
{
  "id": "rogue_combo",
  "name": "连击点",
  "description": "积累连击点数，用于释放终结技",
  "icon": "🗡️",
  "kind": "buff",
  "durationSec": 15.0,
  "stackingPolicy": "stack",
  "maxStacks": 5,
  "tickIntervalSec": null,
  "effects": [],
  "defaultTarget": "self"
}
```

#### 盗贼固定技能

**rogue_attack_basic** (普通攻击 - 邪恶攻击)
```json
{
  "id": "rogue_attack_basic",
  "name": "邪恶攻击",
  "description": "盗贼的基础攻击，有几率获得连击点",
  "type": "passive",
  "slotType": "passive",
  "fixed": true,
  "releaseType": "instant",
  "targetPolicy": "current_target",
  "cooldownSec": 0,
  "allowedProfessions": ["rogue"],
  "damage": {
    "coefAtk": 0.95,
    "flat": 0,
    "isAoe": false
  },
  "resourceGains": [
    {
      "bucketId": "energy",
      "amount": 1
    }
  ],
  "triggers": [
    {
      "when": "OnAttackHit",
      "procChance": 0.4,
      "fireSkillId": "rogue_gain_combo",
      "priority": 10
    }
  ]
}
```

**rogue_special_pulse** (脉冲技能 - 切割)
```json
{
  "id": "rogue_special_pulse",
  "name": "切割",
  "description": "快速切割敌人，获得连击点",
  "type": "passive",
  "slotType": "passive",
  "fixed": true,
  "releaseType": "instant",
  "targetPolicy": "current_target",
  "cooldownSec": 0,
  "allowedProfessions": ["rogue"],
  "damage": {
    "coefAtk": 0.8,
    "flat": 5,
    "isAoe": false
  },
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "rogue_combo",
      "targetOverride": "self"
    }
  ]
}
```

**rogue_gain_combo** (触发技能 - 获得连击点)
```json
{
  "id": "rogue_gain_combo",
  "name": "连击点获得",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "self",
  "cooldownSec": 0,
  "allowedProfessions": ["rogue"],
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "rogue_combo",
      "targetOverride": "self"
    }
  ]
}
```

---

## 文档说明

本文档为中篇，包含：
- 技能装备界面UI组件的完整实现
- 四个职业（战士、法师、游侠、盗贼）的固定技能差异化设计

**下一篇内容预告：**
- 完整的技能库（战士、法师、盗贼、游侠 1-10级技能）
- 实施阶段更新汇总

**最后更新：** 2025-11-14  
**维护者：** @copilot  
**状态：** 进行中
