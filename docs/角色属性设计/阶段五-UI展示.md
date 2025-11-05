# 阶段五：UI展示

## 阶段目标

在用户界面上展示角色属性信息，让玩家直观地看到属性值和成长变化。

## 展示内容

### 核心属性显示
1. **主属性**：力量/法强/敏捷（根据职业）
2. **生命值**：最大生命
3. **攻击**：每次攻击伤害
4. **攻速**：每秒攻击次数
5. **暴击**：暴击率百分比
6. **急速**：急速百分比
7. **技能**：技能伤害和间隔

### 派生信息显示
1. **DPS**：每秒伤害输出
2. **期望伤害**：单次攻击期望值
3. **生效攻速**：包含急速加成后的实际攻速

## 工作内容

### 任务5.1：创建属性显示组件

**位置**：`BlazorIdle/Shared/Components/CharacterStats.razor`

**说明**：
- 创建可复用的属性显示组件
- 支持实时更新
- 美观的UI设计

**组件代码**：

```razor
@using BlazorIdle.Shared.Game
@inject ICharacterAttributeService AttributeService
@inject IProfessionAttributeService ProfessionService

<div class="character-stats">
    @if (character != null && calculatedAttrs != null)
    {
        <div class="stats-header">
            <h3>@character.Name</h3>
            <span class="profession-badge">@professionName</span>
            <span class="level-badge">Lv.@currentLevel</span>
        </div>

        <div class="stats-sections">
            <!-- 主属性 -->
            <div class="stat-section primary-stat">
                <div class="section-title">主属性</div>
                <div class="stat-item main-stat">
                    <span class="stat-name">@calculatedAttrs.MainStatName</span>
                    <span class="stat-value">@calculatedAttrs.MainStatTotal</span>
                </div>
            </div>

            <!-- 基础属性 -->
            <div class="stat-section">
                <div class="section-title">基础属性</div>
                
                <div class="stat-item">
                    <span class="stat-name">生命值</span>
                    <span class="stat-value">@calculatedAttrs.MaxHp</span>
                </div>

                <div class="stat-item">
                    <span class="stat-name">攻击伤害</span>
                    <span class="stat-value">@calculatedAttrs.DamagePerAttack</span>
                </div>

                <div class="stat-item">
                    <span class="stat-name">基础攻速</span>
                    <span class="stat-value">@calculatedAttrs.AttackRateAPS.ToString("F2") APS</span>
                </div>
            </div>

            <!-- 战斗属性 -->
            <div class="stat-section">
                <div class="section-title">战斗属性</div>
                
                <div class="stat-item">
                    <span class="stat-name">暴击率</span>
                    <span class="stat-value">@((calculatedAttrs.CritChancePercent * 100).ToString("F1"))%</span>
                    <span class="stat-cap">/ 60%</span>
                </div>

                <div class="stat-item">
                    <span class="stat-name">急速</span>
                    <span class="stat-value">@((calculatedAttrs.HastePercent * 100).ToString("F1"))%</span>
                    <span class="stat-cap">/ 40%</span>
                </div>

                <div class="stat-item">
                    <span class="stat-name">暴击倍率</span>
                    <span class="stat-value">@calculatedAttrs.CritMultiplier.ToString("F1")x</span>
                </div>

                <div class="stat-item">
                    <span class="stat-name">伤害浮动</span>
                    <span class="stat-value">±@((calculatedAttrs.VariancePct * 100).ToString("F0"))%</span>
                </div>
            </div>

            <!-- 技能属性 -->
            <div class="stat-section">
                <div class="section-title">技能属性</div>
                
                <div class="stat-item">
                    <span class="stat-name">技能伤害</span>
                    <span class="stat-value">@calculatedAttrs.SpecialDamage</span>
                </div>

                <div class="stat-item">
                    <span class="stat-name">技能间隔</span>
                    <span class="stat-value">@calculatedAttrs.SpecialIntervalSec.ToString("F1")秒</span>
                </div>

                <div class="stat-item">
                    <span class="stat-name">复活时间</span>
                    <span class="stat-value">@calculatedAttrs.ReviveSec.ToString("F1")秒</span>
                </div>
            </div>

            <!-- DPS统计 -->
            <div class="stat-section dps-section">
                <div class="section-title">输出统计</div>
                
                <div class="stat-item">
                    <span class="stat-name">期望伤害</span>
                    <span class="stat-value">@expectedDamage.ToString("F1")</span>
                </div>

                <div class="stat-item">
                    <span class="stat-name">生效攻速</span>
                    <span class="stat-value">@effectiveAPS.ToString("F2") APS</span>
                </div>

                <div class="stat-item">
                    <span class="stat-name">普攻DPS</span>
                    <span class="stat-value highlight">@basicDPS.ToString("F1")</span>
                </div>

                <div class="stat-item">
                    <span class="stat-name">技能DPS</span>
                    <span class="stat-value">@skillDPS.ToString("F1")</span>
                </div>

                <div class="stat-item total-dps">
                    <span class="stat-name">总DPS</span>
                    <span class="stat-value">@totalDPS.ToString("F1")</span>
                </div>
            </div>
        </div>
    }
    else if (isLoading)
    {
        <div class="loading">
            <span class="spinner"></span>
            <span>加载属性中...</span>
        </div>
    }
    else
    {
        <div class="no-data">
            <span>未选择角色</span>
        </div>
    }
</div>

@code {
    [Parameter]
    public CharacterData? character { get; set; }

    private CalculatedAttributes? calculatedAttrs;
    private string professionName = "";
    private int currentLevel = 1;
    
    // 派生信息
    private double expectedDamage = 0;
    private double effectiveAPS = 0;
    private double basicDPS = 0;
    private double skillDPS = 0;
    private double totalDPS = 0;
    
    private bool isLoading = false;

    protected override async Task OnParametersSetAsync()
    {
        await RefreshStats();
    }

    private async Task RefreshStats()
    {
        if (character == null)
        {
            calculatedAttrs = null;
            return;
        }

        try
        {
            isLoading = true;
            StateHasChanged();

            // 计算属性
            calculatedAttrs = await AttributeService.CalculateAttributesAsync(character);

            if (calculatedAttrs != null)
            {
                // 获取职业名称
                var config = await ProfessionService.GetConfigAsync(character.ActiveCombatProfessionId);
                professionName = config?.Name ?? character.ActiveCombatProfessionId;

                // 获取当前等级
                if (character.Professions.TryGetValue(character.ActiveCombatProfessionId, out var progress))
                {
                    currentLevel = progress.Level;
                }

                // 计算派生信息
                expectedDamage = AttributeCalculator.CalculateExpectedHitDamage(calculatedAttrs);
                effectiveAPS = AttributeCalculator.CalculateEffectiveAPS(calculatedAttrs);
                basicDPS = AttributeCalculator.CalculateBasicDPS(calculatedAttrs);
                skillDPS = AttributeCalculator.CalculateSkillDPS(calculatedAttrs);
                totalDPS = AttributeCalculator.CalculateTotalDPS(calculatedAttrs);
            }
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    public async Task Refresh()
    {
        await RefreshStats();
    }
}

<style>
    .character-stats {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        border-radius: 12px;
        padding: 20px;
        color: white;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    }

    .stats-header {
        display: flex;
        align-items: center;
        gap: 12px;
        margin-bottom: 20px;
        padding-bottom: 15px;
        border-bottom: 2px solid rgba(255, 255, 255, 0.2);
    }

    .stats-header h3 {
        margin: 0;
        font-size: 24px;
        font-weight: bold;
    }

    .profession-badge {
        background: rgba(255, 255, 255, 0.2);
        padding: 4px 12px;
        border-radius: 12px;
        font-size: 14px;
    }

    .level-badge {
        background: rgba(255, 215, 0, 0.3);
        padding: 4px 12px;
        border-radius: 12px;
        font-size: 14px;
        font-weight: bold;
    }

    .stats-sections {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: 16px;
    }

    .stat-section {
        background: rgba(255, 255, 255, 0.1);
        border-radius: 8px;
        padding: 16px;
    }

    .stat-section.primary-stat {
        background: rgba(255, 215, 0, 0.2);
        border: 2px solid rgba(255, 215, 0, 0.4);
    }

    .stat-section.dps-section {
        background: rgba(255, 99, 71, 0.2);
        border: 2px solid rgba(255, 99, 71, 0.4);
    }

    .section-title {
        font-size: 16px;
        font-weight: bold;
        margin-bottom: 12px;
        text-transform: uppercase;
        letter-spacing: 1px;
        opacity: 0.9;
    }

    .stat-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 8px 0;
        border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    }

    .stat-item:last-child {
        border-bottom: none;
    }

    .stat-item.main-stat {
        font-size: 20px;
        font-weight: bold;
        padding: 12px 0;
    }

    .stat-item.total-dps {
        font-size: 18px;
        font-weight: bold;
        margin-top: 8px;
        padding-top: 12px;
        border-top: 2px solid rgba(255, 255, 255, 0.3);
    }

    .stat-name {
        opacity: 0.9;
    }

    .stat-value {
        font-weight: bold;
        font-size: 18px;
    }

    .stat-value.highlight {
        color: #FFD700;
        text-shadow: 0 0 10px rgba(255, 215, 0, 0.5);
    }

    .stat-cap {
        opacity: 0.6;
        font-size: 14px;
        margin-left: 4px;
    }

    .loading, .no-data {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 40px;
        gap: 12px;
    }

    .spinner {
        display: inline-block;
        width: 32px;
        height: 32px;
        border: 3px solid rgba(255, 255, 255, 0.3);
        border-top-color: white;
        border-radius: 50%;
        animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
        to { transform: rotate(360deg); }
    }

    @media (max-width: 768px) {
        .stats-sections {
            grid-template-columns: 1fr;
        }
    }
</style>
```

**检查点**：
- [ ] 组件文件创建完成
- [ ] 显示所有核心属性
- [ ] 显示DPS统计
- [ ] 样式美观
- [ ] 支持响应式布局

---

### 任务5.2：集成到角色面板

**位置**：主界面或角色管理页面

**说明**：
- 将属性显示组件集成到主界面
- 确保组件会随角色切换更新

**集成示例**：

```razor
@page "/"
@inject ICharacterStateService CharacterStateService
@implements IDisposable

<div class="home-page">
    <div class="left-panel">
        @* 角色选择等其他内容 *@
    </div>

    <div class="main-panel">
        @if (CurrentCharacter != null)
        {
            <CharacterStats character="@CurrentCharacter" @ref="statsComponent" />
        }
        
        @* 其他内容 *@
    </div>

    <div class="right-panel">
        @* 其他面板 *@
    </div>
</div>

@code {
    private CharacterData? CurrentCharacter => CharacterStateService.CurrentCharacter;
    private CharacterStats? statsComponent;

    protected override void OnInitialized()
    {
        // 订阅角色切换事件
        CharacterStateService.OnCharacterChanged += OnCharacterChanged;
    }

    private async void OnCharacterChanged()
    {
        // 刷新属性显示
        if (statsComponent != null)
        {
            await statsComponent.Refresh();
        }
        
        await InvokeAsync(() => StateHasChanged());
    }

    public void Dispose()
    {
        CharacterStateService.OnCharacterChanged -= OnCharacterChanged;
    }
}
```

**检查点**：
- [ ] 组件成功集成到界面
- [ ] 切换角色时属性更新
- [ ] 升级时属性更新
- [ ] 布局合理美观

---

### 任务5.3：添加属性变化提示

**说明**：
- 当属性变化时显示动画提示
- 让玩家感知到成长

**组件实现**：

创建 `StatChangeIndicator.razor`：

```razor
<div class="stat-change-container">
    @foreach (var change in activeChanges)
    {
        <div class="stat-change @change.Type" 
             style="animation-delay: @(change.Delay)ms"
             @key="change.Id">
            <span class="icon">@(change.IsIncrease ? "↑" : "↓")</span>
            <span class="name">@change.StatName</span>
            <span class="value">@change.ChangeText</span>
        </div>
    }
</div>

@code {
    private List<StatChange> activeChanges = new();
    private int changeIdCounter = 0;

    public void ShowChange(string statName, double oldValue, double newValue, bool isPercentage = false)
    {
        var change = oldValue - newValue;
        var isIncrease = change > 0;
        
        string changeText;
        if (isPercentage)
        {
            changeText = $"{Math.Abs(change * 100):F1}%";
        }
        else
        {
            changeText = $"{Math.Abs(change):F0}";
        }

        var statChange = new StatChange
        {
            Id = changeIdCounter++,
            StatName = statName,
            ChangeText = changeText,
            IsIncrease = isIncrease,
            Type = isIncrease ? "increase" : "decrease",
            Delay = activeChanges.Count * 100
        };

        activeChanges.Add(statChange);
        StateHasChanged();

        // 2秒后移除
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            activeChanges.Remove(statChange);
            await InvokeAsync(() => StateHasChanged());
        });
    }

    private class StatChange
    {
        public int Id { get; set; }
        public string StatName { get; set; } = "";
        public string ChangeText { get; set; } = "";
        public bool IsIncrease { get; set; }
        public string Type { get; set; } = "";
        public int Delay { get; set; }
    }
}

<style>
    .stat-change-container {
        position: fixed;
        top: 20%;
        right: 20px;
        z-index: 1000;
        pointer-events: none;
    }

    .stat-change {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 12px 16px;
        margin-bottom: 8px;
        border-radius: 8px;
        font-weight: bold;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.2);
        animation: slideInRight 0.3s ease-out, fadeOut 0.3s ease-in 1.7s forwards;
    }

    .stat-change.increase {
        background: linear-gradient(135deg, #4CAF50, #45a049);
        color: white;
    }

    .stat-change.decrease {
        background: linear-gradient(135deg, #f44336, #da190b);
        color: white;
    }

    .stat-change .icon {
        font-size: 24px;
    }

    .stat-change .name {
        font-size: 14px;
    }

    .stat-change .value {
        font-size: 18px;
    }

    @keyframes slideInRight {
        from {
            transform: translateX(100px);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }

    @keyframes fadeOut {
        to {
            opacity: 0;
            transform: translateX(50px);
        }
    }
</style>
```

**使用方式**：

```razor
<StatChangeIndicator @ref="changeIndicator" />

@code {
    private StatChangeIndicator? changeIndicator;

    private void OnLevelUp()
    {
        // 显示属性变化
        changeIndicator?.ShowChange("伤害", oldDamage, newDamage);
        changeIndicator?.ShowChange("生命", oldHp, newHp);
        changeIndicator?.ShowChange("暴击", oldCrit, newCrit, isPercentage: true);
    }
}
```

**检查点**：
- [ ] 变化提示组件创建完成
- [ ] 集成到主界面
- [ ] 升级时显示提示
- [ ] 动画流畅美观

---

### 任务5.4：添加属性对比功能

**说明**：
- 在切换职业前显示属性对比
- 帮助玩家做出决策

**组件实现**：

创建 `StatComparison.razor`：

```razor
@using BlazorIdle.Shared.Game

<div class="stat-comparison">
    <div class="comparison-header">
        <div class="profession-label">
            <span>@currentProfessionName</span>
            <span class="level">Lv.@currentLevel</span>
        </div>
        <div class="vs">VS</div>
        <div class="profession-label">
            <span>@targetProfessionName</span>
            <span class="level">Lv.@targetLevel</span>
        </div>
    </div>

    <div class="comparison-stats">
        @foreach (var stat in comparisonStats)
        {
            <div class="stat-row">
                <span class="stat-name">@stat.Name</span>
                <div class="stat-values">
                    <span class="value current">@stat.CurrentValue</span>
                    <span class="arrow @stat.ChangeDirection">
                        @(stat.ChangeDirection == "increase" ? "→" : stat.ChangeDirection == "decrease" ? "←" : "=")
                    </span>
                    <span class="value target">@stat.TargetValue</span>
                    @if (stat.ChangePercent != 0)
                    {
                        <span class="change-percent @stat.ChangeDirection">
                            (@(stat.ChangePercent > 0 ? "+" : "")@stat.ChangePercent.ToString("F1")%)
                        </span>
                    }
                </div>
            </div>
        }
    </div>
</div>

@code {
    [Parameter]
    public CalculatedAttributes? CurrentStats { get; set; }

    [Parameter]
    public CalculatedAttributes? TargetStats { get; set; }

    [Parameter]
    public string CurrentProfessionName { get; set; } = "";

    [Parameter]
    public string TargetProfessionName { get; set; } = "";

    [Parameter]
    public int CurrentLevel { get; set; }

    [Parameter]
    public int TargetLevel { get; set; }

    private List<ComparisonStat> comparisonStats = new();

    protected override void OnParametersSet()
    {
        if (CurrentStats != null && TargetStats != null)
        {
            BuildComparison();
        }
    }

    private void BuildComparison()
    {
        comparisonStats = new List<ComparisonStat>
        {
            CreateComparison("生命", CurrentStats!.MaxHp, TargetStats!.MaxHp),
            CreateComparison("伤害", CurrentStats.DamagePerAttack, TargetStats.DamagePerAttack),
            CreateComparison("暴击", CurrentStats.CritChancePercent * 100, TargetStats.CritChancePercent * 100, "%"),
            CreateComparison("急速", CurrentStats.HastePercent * 100, TargetStats.HastePercent * 100, "%"),
            CreateComparison("技能伤害", CurrentStats.SpecialDamage, TargetStats.SpecialDamage),
            CreateComparison("总DPS", 
                AttributeCalculator.CalculateTotalDPS(CurrentStats),
                AttributeCalculator.CalculateTotalDPS(TargetStats),
                "", 1)
        };
    }

    private ComparisonStat CreateComparison(string name, double current, double target, string suffix = "", int decimals = 0)
    {
        var changePercent = current != 0 ? ((target - current) / current * 100) : 0;
        var direction = target > current ? "increase" : target < current ? "decrease" : "same";

        return new ComparisonStat
        {
            Name = name,
            CurrentValue = current.ToString($"F{decimals}") + suffix,
            TargetValue = target.ToString($"F{decimals}") + suffix,
            ChangePercent = changePercent,
            ChangeDirection = direction
        };
    }

    private class ComparisonStat
    {
        public string Name { get; set; } = "";
        public string CurrentValue { get; set; } = "";
        public string TargetValue { get; set; } = "";
        public double ChangePercent { get; set; }
        public string ChangeDirection { get; set; } = "";
    }
}

<style>
    .stat-comparison {
        background: white;
        border-radius: 8px;
        padding: 20px;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .comparison-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 20px;
        padding-bottom: 15px;
        border-bottom: 2px solid #eee;
    }

    .profession-label {
        display: flex;
        flex-direction: column;
        gap: 4px;
        font-weight: bold;
        font-size: 16px;
    }

    .profession-label .level {
        font-size: 12px;
        color: #666;
    }

    .vs {
        font-weight: bold;
        font-size: 20px;
        color: #999;
    }

    .comparison-stats {
        display: flex;
        flex-direction: column;
        gap: 12px;
    }

    .stat-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 10px;
        background: #f8f9fa;
        border-radius: 4px;
    }

    .stat-name {
        font-weight: 500;
        min-width: 80px;
    }

    .stat-values {
        display: flex;
        align-items: center;
        gap: 12px;
    }

    .value {
        font-weight: bold;
        min-width: 60px;
        text-align: right;
    }

    .value.current {
        color: #666;
    }

    .value.target {
        color: #333;
    }

    .arrow {
        font-size: 20px;
        font-weight: bold;
    }

    .arrow.increase {
        color: #4CAF50;
    }

    .arrow.decrease {
        color: #f44336;
    }

    .arrow.same {
        color: #999;
    }

    .change-percent {
        font-size: 12px;
        font-weight: bold;
        padding: 2px 6px;
        border-radius: 4px;
    }

    .change-percent.increase {
        color: #4CAF50;
        background: rgba(76, 175, 80, 0.1);
    }

    .change-percent.decrease {
        color: #f44336;
        background: rgba(244, 67, 54, 0.1);
    }
</style>
```

**使用场景**：

在职业切换对话框中显示对比：

```razor
<div class="switch-profession-dialog">
    <h3>切换职业</h3>
    
    <StatComparison
        CurrentStats="@currentStats"
        TargetStats="@targetStats"
        CurrentProfessionName="@currentProfession"
        TargetProfessionName="@targetProfession"
        CurrentLevel="@currentLevel"
        TargetLevel="@targetLevel" />
    
    <div class="actions">
        <button @onclick="ConfirmSwitch">确认切换</button>
        <button @onclick="Cancel">取消</button>
    </div>
</div>
```

**检查点**：
- [ ] 对比组件创建完成
- [ ] 集成到切换对话框
- [ ] 显示所有关键属性对比
- [ ] 变化方向和百分比清晰

---

### 任务5.5：添加简化属性显示

**说明**：
- 在不需要详细信息的地方显示简化属性
- 例如角色列表、战斗界面等

**组件实现**：

创建 `CharacterStatsCompact.razor`：

```razor
<div class="stats-compact">
    <div class="stat-item">
        <span class="icon">❤️</span>
        <span class="value">@character.MaxHp</span>
    </div>
    <div class="stat-item">
        <span class="icon">⚔️</span>
        <span class="value">@character.DamagePerAttack</span>
    </div>
    <div class="stat-item">
        <span class="icon">💥</span>
        <span class="value">@((character.CritChancePercent * 100).ToString("F0"))%</span>
    </div>
    <div class="stat-item">
        <span class="icon">⚡</span>
        <span class="value">@totalDPS.ToString("F0") DPS</span>
    </div>
</div>

@code {
    [Parameter]
    public CharacterData character { get; set; } = null!;

    private double totalDPS => CalculateDPS();

    private double CalculateDPS()
    {
        // 简化计算
        var basicDPS = character.DamagePerAttack * character.AttackRateAPS * 
                      (1 + character.HastePercent) * 
                      (1 + character.CritChancePercent * (character.CritMultiplier - 1));
        
        var skillDPS = character.SpecialDamage / character.SpecialIntervalSec;
        
        return basicDPS + skillDPS;
    }
}

<style>
    .stats-compact {
        display: flex;
        gap: 12px;
        align-items: center;
        padding: 8px;
        background: rgba(0, 0, 0, 0.05);
        border-radius: 4px;
    }

    .stat-item {
        display: flex;
        align-items: center;
        gap: 4px;
    }

    .stat-item .icon {
        font-size: 16px;
    }

    .stat-item .value {
        font-weight: bold;
        font-size: 14px;
    }
</style>
```

**检查点**：
- [ ] 简化组件创建完成
- [ ] 可以在各处复用
- [ ] 显示核心信息
- [ ] 样式简洁

---

### 任务5.6：UI测试

**测试场景**：

1. **显示测试**
   - [ ] 所有属性正确显示
   - [ ] 数值格式正确（小数位数）
   - [ ] 百分比显示正确
   - [ ] DPS计算正确

2. **交互测试**
   - [ ] 切换角色时属性更新
   - [ ] 升级时属性更新
   - [ ] 切换职业时属性更新

3. **响应式测试**
   - [ ] 桌面端显示正常
   - [ ] 平板显示正常
   - [ ] 手机显示正常

4. **性能测试**
   - [ ] 组件渲染快速
   - [ ] 更新不卡顿

---

## 阶段五验收标准

完成以下所有检查点后，阶段五即告完成：

- [ ] 属性显示组件创建完成且美观
- [ ] 成功集成到主界面
- [ ] 显示所有核心属性
- [ ] 显示DPS统计
- [ ] 属性变化提示正常
- [ ] 属性对比功能正常
- [ ] 简化显示组件可用
- [ ] 所有测试通过

## 预计工时

- 任务5.1：2小时
- 任务5.2：0.5小时
- 任务5.3：1小时
- 任务5.4：1.5小时
- 任务5.5：0.5小时
- 任务5.6：0.5小时

**总计**：6小时（约一个工作日）

## 注意事项

1. **性能优化**：避免过度渲染，使用 `ShouldRender` 优化
2. **数值格式**：统一小数位数格式
3. **UI一致性**：保持与现有界面风格一致
4. **响应式**：确保在各种屏幕尺寸下正常显示
5. **可访问性**：考虑色盲用户，不仅用颜色表示增减

## 下一阶段

完成阶段五后，进入**阶段六：测试与调优**，进行全面的测试和数值平衡调整。
