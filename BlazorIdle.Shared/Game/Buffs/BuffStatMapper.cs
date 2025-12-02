using System;
using System.Collections.Generic;

namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Buff 属性映射器 - 处理旧属性名到新属性名的映射
    /// Buff stat mapper - handles legacy stat name to new stat name mapping
    /// </summary>
    public static class BuffStatMapper
    {
        /// <summary>
        /// 旧属性名 → 新属性名映射
        /// Legacy stat name → new stat name mapping
        /// </summary>
        private static readonly Dictionary<string, string> LegacyMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { "DamagePerAttack", "AttackFinal" },
            { "SpecialDamage", "AttackFinal" },
            { "DamageReduction", "DamageReductionPercent" },
            { "CritMultiplier", "CritDamageBonusPercent" }
        };

        /// <summary>
        /// 所有有效的 CombatStats 属性名
        /// All valid CombatStats property names
        /// </summary>
        private static readonly HashSet<string> ValidStatNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "AttackFinal",
            "AttackPercent",
            "SpecialAttackPercent",
            "HPPercent",
            "HastePercent",
            "CritChancePercent",
            "CritDamageBonusPercent",
            "FortifyMaxPercent",
            "BackwaterMaxPercent",
            "ChasePercent",
            "KenChasePercent",
            "ChaseFlat",
            "DamageReductionPercent"
        };

        /// <summary>
        /// 获取规范化的属性名（处理旧名称兼容）
        /// Get normalized stat name (handles legacy name compatibility)
        /// </summary>
        /// <param name="statName">原始属性名 / Original stat name</param>
        /// <returns>规范化后的属性名 / Normalized stat name</returns>
        public static string NormalizeStatName(string? statName)
        {
            if (string.IsNullOrEmpty(statName))
                return string.Empty;

            // 检查是否为旧属性名
            if (LegacyMapping.TryGetValue(statName, out var newName))
                return newName;

            return statName;
        }

        /// <summary>
        /// 检查是否为有效的属性名（包括旧名称）
        /// Check if stat name is valid (including legacy names)
        /// </summary>
        /// <param name="statName">属性名 / Stat name</param>
        /// <returns>是否有效 / Whether valid</returns>
        public static bool IsValidStatName(string? statName)
        {
            if (string.IsNullOrEmpty(statName))
                return false;

            // 检查旧名称
            if (LegacyMapping.ContainsKey(statName))
                return true;

            // 检查新名称
            return ValidStatNames.Contains(statName);
        }

        /// <summary>
        /// 检查是否为旧属性名
        /// Check if stat name is a legacy name
        /// </summary>
        /// <param name="statName">属性名 / Stat name</param>
        /// <returns>是否为旧属性名 / Whether it's a legacy name</returns>
        public static bool IsLegacyStatName(string? statName)
        {
            if (string.IsNullOrEmpty(statName))
                return false;

            return LegacyMapping.ContainsKey(statName);
        }

        /// <summary>
        /// 检查是否需要值转换（如 CritMultiplier 1.2 → 20%）
        /// Check if value conversion is needed (e.g., CritMultiplier 1.2 → 20%)
        /// </summary>
        /// <param name="statName">原始属性名 / Original stat name</param>
        /// <param name="converter">转换函数 / Converter function</param>
        /// <returns>是否需要转换 / Whether conversion is needed</returns>
        public static bool NeedsValueConversion(string? statName, out Func<double, double>? converter)
        {
            if (string.Equals(statName, "CritMultiplier", StringComparison.OrdinalIgnoreCase))
            {
                // CritMultiplier 1.2 倍率 → 20% 加成
                // CritMultiplier 1.2 multiplier → 20% bonus
                converter = v => (v - 1.0) * 100;
                return true;
            }

            converter = null;
            return false;
        }

        /// <summary>
        /// 获取所有有效的属性名列表
        /// Get list of all valid stat names
        /// </summary>
        /// <returns>有效属性名集合 / Collection of valid stat names</returns>
        public static IReadOnlySet<string> GetValidStatNames()
        {
            return ValidStatNames;
        }

        /// <summary>
        /// 获取所有旧属性名到新属性名的映射
        /// Get all legacy to new stat name mappings
        /// </summary>
        /// <returns>映射字典 / Mapping dictionary</returns>
        public static IReadOnlyDictionary<string, string> GetLegacyMappings()
        {
            return LegacyMapping;
        }
    }
}
