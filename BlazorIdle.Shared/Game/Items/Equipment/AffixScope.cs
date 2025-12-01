namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 词条作用域枚举 - 定义词条生效条件
    /// Affix scope enum - defines when an affix is active
    /// </summary>
    public enum AffixScope
    {
        /// <summary>
        /// 全局生效，不受元素限制
        /// Always active, not affected by element matching
        /// 典型词条：HP%, Crit%, CritDmgBonus%, DR%, 固定追击
        /// Typical affixes: HP%, Crit%, CritDmgBonus%, DR%, ChaseFlat
        /// </summary>
        Global,

        /// <summary>
        /// 元素限定生效，装备元素必须与主元素匹配
        /// Active only when equipment element matches main element
        /// 典型词条：Attack%, SpecialAttack%, Chase%, KenChase%
        /// Typical affixes: Attack%, SpecialAttack%, Chase%, KenChase%
        /// </summary>
        Element,

        /// <summary>
        /// 混合作用域，各字段有独立的作用域定义
        /// Hybrid scope, each field has its own scope definition
        /// 典型词条：assault (Attack%=element, Crit%=global)
        /// Typical affixes: assault (Attack%=element, Crit%=global)
        /// </summary>
        Hybrid
    }
}
