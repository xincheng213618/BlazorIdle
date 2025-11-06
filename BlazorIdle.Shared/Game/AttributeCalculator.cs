using BlazorIdle.Shared.Models;

namespace BlazorIdle.Shared.Game
{
    /// <summary>
    /// 属性计算器 - 负责根据职业配置和角色等级计算派生属性
    /// </summary>
    public static class AttributeCalculator
    {
        /// <summary>
        /// 计算角色的所有属性
        /// </summary>
        /// <param name="level">角色等级</param>
        /// <param name="config">职业属性配置</param>
        /// <param name="equipBonuses">装备提供的额外属性（预留，MVP可传null）</param>
        /// <returns>计算后的属性结果</returns>
        public static CalculatedAttributes Calculate(
            int level, 
            ProfessionAttributeConfig config, 
            EquipmentBonuses? equipBonuses = null)
        {
            equipBonuses ??= new EquipmentBonuses();

            // 步骤1：计算各基础属性总值
            var mainStatTotal = CalculateAttributeValue(level, config.MainStat, equipBonuses.MainStat);
            var staminaTotal = CalculateAttributeValue(level, config.Stamina, equipBonuses.Stamina);
            var hasteTotal = CalculateAttributeValue(level, config.Haste, equipBonuses.Haste);
            var critTotal = CalculateAttributeValue(level, config.Crit, equipBonuses.Crit);

            // 步骤2：计算派生属性
            var result = new CalculatedAttributes
            {
                MainStatTotal = (int)mainStatTotal,
                StaminaTotal = (int)staminaTotal,
                HasteRating = (int)hasteTotal,
                CritRating = (int)critTotal,
                MainStatName = config.DisplayMainStat
            };

            // 基于主属性计算伤害
            result.DamagePerAttack = CalculateDamage(
                config.Baseline.BaseDamage,
                mainStatTotal,
                config.Weights.DamagePerAttackPerMain
            );

            result.SpecialDamage = CalculateDamage(
                config.Baseline.BaseSpecialDamage,
                mainStatTotal,
                config.Weights.SpecialDamagePerMain
            );

            // 基于耐力计算生命值
            result.MaxHp = CalculateHp(
                config.Baseline.BaseHp,
                staminaTotal,
                config.Weights.MaxHpPerStamina
            );

            // 基于Rating计算百分比
            result.HastePercent = CalculateHastePercent(
                hasteTotal,
                config.Weights.HasteRatingToPercent,
                config.Weights.HasteCap
            );

            result.CritChancePercent = CalculateCritPercent(
                critTotal,
                config.Weights.CritRatingToPercent,
                config.Weights.CritCap
            );

            // 步骤3：设置固定属性
            result.AttackRateAPS = config.Baseline.BaseAPS;
            result.SpecialIntervalSec = config.Baseline.BaseSpecialIntervalSec;
            result.CritMultiplier = config.Baseline.CritMultiplier;
            result.VariancePct = config.Baseline.Variance;
            result.ReviveSec = config.Baseline.ReviveSec;

            return result;
        }

        /// <summary>
        /// 计算属性总值（通用方法）
        /// </summary>
        private static double CalculateAttributeValue(int level, AttributeGrowthConfig growthConfig, double equipBonus)
        {
            if (level < 1)
            {
                throw new ArgumentException("Level must be at least 1", nameof(level));
            }

            // 公式：Attribute = base + perLevel × (level - 1) + equipBonus
            var value = growthConfig.Base + growthConfig.PerLevel * (level - 1) + equipBonus;
            
            return Math.Max(0, value); // 确保非负
        }

        /// <summary>
        /// 计算伤害属性（普攻或技能）
        /// </summary>
        private static int CalculateDamage(int baseDamage, double mainStat, double perMainCoeff)
        {
            // 公式：Damage = baseDamage + mainStat × perMainCoeff
            var damage = baseDamage + mainStat * perMainCoeff;
            
            return Math.Max(1, (int)Math.Round(damage)); // 至少为1
        }

        /// <summary>
        /// 计算生命值
        /// </summary>
        private static int CalculateHp(int baseHp, double stamina, double perStaminaCoeff)
        {
            // 公式：HP = baseHp + stamina × perStaminaCoeff
            var hp = baseHp + stamina * perStaminaCoeff;
            
            return Math.Max(1, (int)Math.Round(hp)); // 至少为1
        }

        /// <summary>
        /// 将急速Rating转换为百分比
        /// </summary>
        private static double CalculateHastePercent(double hasteRating, double ratingToPercentCoeff, double cap)
        {
            if (ratingToPercentCoeff <= 0)
            {
                return 0;
            }

            // 公式：Haste% = min(hasteRating / ratingToPercentCoeff, cap)
            var hastePercent = hasteRating / ratingToPercentCoeff;
            
            return Math.Clamp(hastePercent, 0.0, cap);
        }

        /// <summary>
        /// 将暴击Rating转换为百分比
        /// </summary>
        private static double CalculateCritPercent(double critRating, double ratingToPercentCoeff, double cap)
        {
            if (ratingToPercentCoeff <= 0)
            {
                return 0;
            }

            // 公式：Crit% = min(critRating / ratingToPercentCoeff, cap)
            var critPercent = critRating / ratingToPercentCoeff;
            
            return Math.Clamp(critPercent, 0.0, cap);
        }

        /// <summary>
        /// 计算期望伤害（用于校准和显示）
        /// </summary>
        public static double CalculateExpectedHitDamage(CalculatedAttributes attrs)
        {
            // 期望 = 基础伤害 × [1 + 暴击率 × (暴击倍率 - 1)]
            return attrs.DamagePerAttack * (1 + attrs.CritChancePercent * (attrs.CritMultiplier - 1));
        }

        /// <summary>
        /// 计算生效攻击速度
        /// </summary>
        public static double CalculateEffectiveAPS(CalculatedAttributes attrs)
        {
            // 生效攻速 = 基础攻速 × (1 + 急速%)
            return attrs.AttackRateAPS * (1 + attrs.HastePercent);
        }

        /// <summary>
        /// 计算普攻DPS
        /// </summary>
        public static double CalculateBasicDPS(CalculatedAttributes attrs)
        {
            var expectedHit = CalculateExpectedHitDamage(attrs);
            var effectiveAPS = CalculateEffectiveAPS(attrs);
            
            return expectedHit * effectiveAPS;
        }

        /// <summary>
        /// 计算技能DPS（假设技能可以无限释放）
        /// </summary>
        public static double CalculateSkillDPS(CalculatedAttributes attrs)
        {
            if (attrs.SpecialIntervalSec <= 0)
            {
                return 0;
            }
            
            return attrs.SpecialDamage / attrs.SpecialIntervalSec;
        }

        /// <summary>
        /// 计算总DPS
        /// </summary>
        public static double CalculateTotalDPS(CalculatedAttributes attrs)
        {
            return CalculateBasicDPS(attrs) + CalculateSkillDPS(attrs);
        }
    }

    /// <summary>
    /// 装备加成（预留接口）
    /// </summary>
    public class EquipmentBonuses
    {
        public double MainStat { get; set; }
        public double Stamina { get; set; }
        public double Haste { get; set; }
        public double Crit { get; set; }
    }

    /// <summary>
    /// 计算后的属性结果
    /// </summary>
    public class CalculatedAttributes
    {
        // 基础属性
        public int MainStatTotal { get; set; }
        public int StaminaTotal { get; set; }
        public int HasteRating { get; set; }
        public int CritRating { get; set; }
        public string MainStatName { get; set; } = string.Empty;

        // 派生属性
        public int DamagePerAttack { get; set; }
        public int SpecialDamage { get; set; }
        public double HastePercent { get; set; }
        public double CritChancePercent { get; set; }
        public int MaxHp { get; set; }

        // 固定属性
        public double AttackRateAPS { get; set; }
        public double SpecialIntervalSec { get; set; }
        public double CritMultiplier { get; set; }
        public double VariancePct { get; set; }
        public double ReviveSec { get; set; }

        /// <summary>
        /// 将计算结果应用到CharacterData
        /// </summary>
        public void ApplyToCharacterData(CharacterData character)
        {
            character.MaxHp = MaxHp;
            character.AttackRateAPS = AttackRateAPS;
            character.DamagePerAttack = DamagePerAttack;
            character.HastePercent = HastePercent;
            character.SpecialIntervalSec = SpecialIntervalSec;
            character.SpecialDamage = SpecialDamage;
            character.CritChancePercent = CritChancePercent;
            character.CritMultiplier = CritMultiplier;
            character.VariancePct = VariancePct;
            character.ReviveSec = ReviveSec;
        }
    }
}
