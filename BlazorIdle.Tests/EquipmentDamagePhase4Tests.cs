using BlazorIdle.Game.Items.Equipment;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 4 单元测试 - 装备与槽位系统
    /// Phase 4 Unit Tests - Equipment & Slot System
    /// </summary>
    public class EquipmentDamagePhase4Tests
    {
        #region QualityDef Tests

        [Fact]
        public void QualityDef_Weight_ReturnsCorrectOrder()
        {
            var white = new QualityDef { Id = "white" };
            var green = new QualityDef { Id = "green" };
            var blue = new QualityDef { Id = "blue" };
            var purple = new QualityDef { Id = "purple" };
            var orange = new QualityDef { Id = "orange" };

            Assert.Equal(1, white.Weight);
            Assert.Equal(2, green.Weight);
            Assert.Equal(3, blue.Weight);
            Assert.Equal(4, purple.Weight);
            Assert.Equal(5, orange.Weight);
        }

        [Fact]
        public void QualityDef_AffixCount_DefaultValues()
        {
            var white = new QualityDef { Id = "white", AffixCount = 0 };
            var green = new QualityDef { Id = "green", AffixCount = 1 };
            var blue = new QualityDef { Id = "blue", AffixCount = 2 };
            var purple = new QualityDef { Id = "purple", AffixCount = 3 };
            var orange = new QualityDef { Id = "orange", AffixCount = 4 };

            Assert.Equal(0, white.AffixCount);
            Assert.Equal(1, green.AffixCount);
            Assert.Equal(2, blue.AffixCount);
            Assert.Equal(3, purple.AffixCount);
            Assert.Equal(4, orange.AffixCount);
        }

        #endregion

        #region TierDef Tests

        [Fact]
        public void TierDef_BaseAttack_DefaultValues()
        {
            var tier1 = new TierDef { Tier = 1, BaseAttack = 100, BaseHp = 50 };
            var tier2 = new TierDef { Tier = 2, BaseAttack = 200, BaseHp = 100 };
            var tier3 = new TierDef { Tier = 3, BaseAttack = 320, BaseHp = 160 };
            var tier4 = new TierDef { Tier = 4, BaseAttack = 450, BaseHp = 225 };

            Assert.Equal(100, tier1.BaseAttack);
            Assert.Equal(200, tier2.BaseAttack);
            Assert.Equal(320, tier3.BaseAttack);
            Assert.Equal(450, tier4.BaseAttack);
        }

        [Fact]
        public void TierConfig_GetTier_ReturnsCorrectTier()
        {
            var config = new TierConfig
            {
                Tiers = new List<TierDef>
                {
                    new TierDef { Tier = 1, BaseAttack = 100, BaseHp = 50 },
                    new TierDef { Tier = 2, BaseAttack = 200, BaseHp = 100 }
                }
            };

            var tier1 = config.GetTier(1);
            var tier2 = config.GetTier(2);
            var tier3 = config.GetTier(3);

            Assert.NotNull(tier1);
            Assert.Equal(100, tier1.BaseAttack);
            Assert.NotNull(tier2);
            Assert.Equal(200, tier2.BaseAttack);
            Assert.Null(tier3);
        }

        #endregion

        #region SlotConfig Tests

        [Fact]
        public void SlotConfig_IsMainHandSlot_ReturnsCorrectly()
        {
            var config = new SlotConfig
            {
                MainHand = new MainHandSlotConfig { Index = 0 },
                SubSlots = new List<SubSlotConfig>
                {
                    new SubSlotConfig { Index = 1, Name = "副槽1" },
                    new SubSlotConfig { Index = 2, Name = "副槽2" }
                }
            };

            Assert.True(config.IsMainHandSlot(0));
            Assert.False(config.IsMainHandSlot(1));
            Assert.False(config.IsMainHandSlot(2));
        }

        [Fact]
        public void SlotConfig_GetSlotName_ReturnsCorrectName()
        {
            var config = new SlotConfig
            {
                MainHand = new MainHandSlotConfig { Index = 0, Name = "主手" },
                SubSlots = new List<SubSlotConfig>
                {
                    new SubSlotConfig { Index = 1, Name = "副槽1" },
                    new SubSlotConfig { Index = 2, Name = "副槽2" }
                }
            };

            Assert.Equal("主手", config.GetSlotName(0));
            Assert.Equal("副槽1", config.GetSlotName(1));
            Assert.Equal("副槽2", config.GetSlotName(2));
        }

        #endregion

        #region EquipmentTemplate Tests

        [Fact]
        public void WeaponTypes_All_ContainsAllTypes()
        {
            Assert.Equal(10, WeaponTypes.All.Count);
            Assert.Contains("sword", WeaponTypes.All);
            Assert.Contains("dagger", WeaponTypes.All);
            Assert.Contains("spear", WeaponTypes.All);
            Assert.Contains("axe", WeaponTypes.All);
            Assert.Contains("staff", WeaponTypes.All);
            Assert.Contains("gun", WeaponTypes.All);
            Assert.Contains("melee", WeaponTypes.All);
            Assert.Contains("bow", WeaponTypes.All);
            Assert.Contains("harp", WeaponTypes.All);
            Assert.Contains("katana", WeaponTypes.All);
        }

        [Fact]
        public void WeaponTypes_GetDisplayName_ReturnsChineseNames()
        {
            Assert.Equal("剑", WeaponTypes.GetDisplayName("sword"));
            Assert.Equal("匕首", WeaponTypes.GetDisplayName("dagger"));
            Assert.Equal("枪", WeaponTypes.GetDisplayName("spear"));
            Assert.Equal("斧", WeaponTypes.GetDisplayName("axe"));
            Assert.Equal("杖", WeaponTypes.GetDisplayName("staff"));
            Assert.Equal("铳", WeaponTypes.GetDisplayName("gun"));
            Assert.Equal("拳套", WeaponTypes.GetDisplayName("melee"));
            Assert.Equal("弓", WeaponTypes.GetDisplayName("bow"));
            Assert.Equal("乐器", WeaponTypes.GetDisplayName("harp"));
            Assert.Equal("刀", WeaponTypes.GetDisplayName("katana"));
        }

        [Fact]
        public void WeaponTypes_IsValid_ValidatesCorrectly()
        {
            Assert.True(WeaponTypes.IsValid("sword"));
            Assert.True(WeaponTypes.IsValid("SWORD")); // Case insensitive
            Assert.True(WeaponTypes.IsValid("Dagger"));
            Assert.False(WeaponTypes.IsValid("unknown"));
            Assert.False(WeaponTypes.IsValid(""));
        }

        [Fact]
        public void EquipmentTemplate_AffixSequence_ContainsFourAffixes()
        {
            var template = new EquipmentTemplate
            {
                Id = "sword_fire_t1",
                Name = "烈焰剑",
                WeaponType = "sword",
                Element = "fire",
                Tier = 1,
                AffixSequence = new List<string> { "attack", "crit", "hp", "assault" }
            };

            Assert.Equal(4, template.AffixSequence.Count);
            Assert.Equal("attack", template.AffixSequence[0]);
            Assert.Equal("crit", template.AffixSequence[1]);
            Assert.Equal("hp", template.AffixSequence[2]);
            Assert.Equal("assault", template.AffixSequence[3]);
        }

        #endregion

        #region EquipmentItem Tests

        [Fact]
        public void EquipmentItem_Create_GeneratesUniqueId()
        {
            var item1 = EquipmentItem.Create("sword_fire_t1", "blue");
            var item2 = EquipmentItem.Create("sword_fire_t1", "blue");

            Assert.NotEqual(item1.InstanceId, item2.InstanceId);
        }

        [Fact]
        public void EquipmentItem_AffixCount_BasedOnQuality()
        {
            var whiteItem = new EquipmentItem { Quality = "white" };
            var greenItem = new EquipmentItem { Quality = "green" };
            var blueItem = new EquipmentItem { Quality = "blue" };
            var purpleItem = new EquipmentItem { Quality = "purple" };
            var orangeItem = new EquipmentItem { Quality = "orange" };

            Assert.Equal(0, whiteItem.AffixCount);
            Assert.Equal(1, greenItem.AffixCount);
            Assert.Equal(2, blueItem.AffixCount);
            Assert.Equal(3, purpleItem.AffixCount);
            Assert.Equal(4, orangeItem.AffixCount);
        }

        [Fact]
        public void EquipmentItem_BaseAttack_BasedOnTier()
        {
            var tier1Item = new EquipmentItem 
            { 
                Template = new EquipmentTemplate { Tier = 1 },
                TierDef = new TierDef { Tier = 1, BaseAttack = 100, BaseHp = 50 }
            };
            var tier4Item = new EquipmentItem 
            { 
                Template = new EquipmentTemplate { Tier = 4 },
                TierDef = new TierDef { Tier = 4, BaseAttack = 450, BaseHp = 225 }
            };

            Assert.Equal(100, tier1Item.BaseAttack);
            Assert.Equal(450, tier4Item.BaseAttack);
        }

        [Fact]
        public void EquipmentItem_Clone_CreatesCopy()
        {
            var original = EquipmentItem.Create("sword_fire_t1", "blue");
            original.ReinforceLevel = 5;

            var clone = original.Clone();

            Assert.NotEqual(original.InstanceId, clone.InstanceId);
            Assert.Equal(original.TemplateId, clone.TemplateId);
            Assert.Equal(original.Quality, clone.Quality);
            Assert.Equal(original.ReinforceLevel, clone.ReinforceLevel);
        }

        #endregion

        #region EquipmentSlot Tests

        [Fact]
        public void EquipmentSlot_IsEmpty_WhenNoEquipment()
        {
            var slot = EquipmentSlot.Create(0);
            Assert.True(slot.IsEmpty);
        }

        [Fact]
        public void EquipmentSlot_Equip_SetsEquipment()
        {
            var slot = EquipmentSlot.Create(0);
            var item = EquipmentItem.Create("sword_fire_t1", "blue");

            slot.Equip(item);

            Assert.False(slot.IsEmpty);
            Assert.Equal(item.InstanceId, slot.EquipmentInstanceId);
            Assert.Same(item, slot.Equipment);
        }

        [Fact]
        public void EquipmentSlot_Unequip_ReturnsItem()
        {
            var slot = EquipmentSlot.Create(0);
            var item = EquipmentItem.Create("sword_fire_t1", "blue");
            slot.Equip(item);

            var unequipped = slot.Unequip();

            Assert.True(slot.IsEmpty);
            Assert.Same(item, unequipped);
        }

        [Fact]
        public void EquipmentSlot_IsMainHand_ChecksIndex()
        {
            var mainHand = EquipmentSlot.Create(0);
            var subSlot = EquipmentSlot.Create(1);

            Assert.True(mainHand.IsMainHand);
            Assert.False(subSlot.IsMainHand);
        }

        #endregion

        #region EquipmentLoadout Tests

        [Fact]
        public void EquipmentLoadout_CreateDefault_HasTenSlots()
        {
            var loadout = EquipmentLoadout.CreateDefault();

            Assert.Equal(10, loadout.Slots.Count);
            Assert.True(loadout.Slots[0].IsMainHand);
        }

        [Fact]
        public void EquipmentLoadout_MainElement_FromMainHand()
        {
            var loadout = EquipmentLoadout.CreateDefault();
            var fireItem = EquipmentItem.Create("sword_fire_t1", "blue");
            fireItem.Template = new EquipmentTemplate { Element = "fire" };

            loadout.Equip(0, fireItem);

            Assert.Equal("fire", loadout.MainElement);
        }

        [Fact]
        public void EquipmentLoadout_Equip_AddsToSlot()
        {
            var loadout = EquipmentLoadout.CreateDefault();
            var item = EquipmentItem.Create("sword_fire_t1", "blue");

            var result = loadout.Equip(0, item);

            Assert.True(result);
            Assert.Same(item, loadout.MainHand.Equipment);
        }

        [Fact]
        public void EquipmentLoadout_Unequip_RemovesFromSlot()
        {
            var loadout = EquipmentLoadout.CreateDefault();
            var item = EquipmentItem.Create("sword_fire_t1", "blue");
            loadout.Equip(0, item);

            var unequipped = loadout.Unequip(0);

            Assert.Same(item, unequipped);
            Assert.True(loadout.MainHand.IsEmpty);
        }

        [Fact]
        public void EquipmentLoadout_Swap_ExchangesItems()
        {
            var loadout = EquipmentLoadout.CreateDefault();
            var item1 = EquipmentItem.Create("sword_fire_t1", "blue");
            var item2 = EquipmentItem.Create("sword_water_t2", "purple");

            loadout.Equip(0, item1);
            loadout.Equip(1, item2);

            loadout.Swap(0, 1);

            Assert.Same(item2, loadout.GetSlot(0)?.Equipment);
            Assert.Same(item1, loadout.GetSlot(1)?.Equipment);
        }

        [Fact]
        public void EquipmentLoadout_GetSameElementCount_CountsCorrectly()
        {
            var loadout = EquipmentLoadout.CreateDefault();
            
            var mainHand = EquipmentItem.Create("sword_fire_t1", "blue");
            mainHand.Template = new EquipmentTemplate { Element = "fire" };
            
            var sameElement = EquipmentItem.Create("dagger_fire_t1", "blue");
            sameElement.Template = new EquipmentTemplate { Element = "fire" };
            
            var differentElement = EquipmentItem.Create("sword_water_t1", "blue");
            differentElement.Template = new EquipmentTemplate { Element = "water" };

            loadout.Equip(0, mainHand);
            loadout.Equip(1, sameElement);
            loadout.Equip(2, differentElement);

            Assert.Equal(2, loadout.GetSameElementCount());
            Assert.Equal(1, loadout.GetDifferentElementCount());
        }

        #endregion

        #region AggregatedStats Tests

        [Fact]
        public void AggregatedStats_CreateEmpty_AllZeros()
        {
            var stats = AggregatedStats.CreateEmpty();

            Assert.Equal(0, stats.TotalBaseAttack);
            Assert.Equal(0, stats.TotalBaseHp);
            Assert.Equal(0, stats.AttackPercent);
            Assert.Equal(0, stats.CritChancePercent);
        }

        [Fact]
        public void AggregatedStats_AddStat_AccumulatesValues()
        {
            var stats = AggregatedStats.CreateEmpty();

            stats.AddStat("AttackPercent", 10);
            stats.AddStat("AttackPercent", 5);
            stats.AddStat("CritChancePercent", 3);

            Assert.Equal(15, stats.AttackPercent);
            Assert.Equal(3, stats.CritChancePercent);
        }

        [Fact]
        public void AggregatedStats_AddStat_UniqueStats_TakesMax()
        {
            var stats = AggregatedStats.CreateEmpty();

            stats.AddStat("FortifyMaxPercent", 10);
            stats.AddStat("FortifyMaxPercent", 15);
            stats.AddStat("FortifyMaxPercent", 5);

            Assert.Equal(15, stats.FortifyMaxPercent);
        }

        [Fact]
        public void AggregatedStats_MergeWith_CombinesStats()
        {
            var stats1 = new AggregatedStats
            {
                TotalBaseAttack = 100,
                AttackPercent = 10,
                FortifyMaxPercent = 5
            };
            var stats2 = new AggregatedStats
            {
                TotalBaseAttack = 200,
                AttackPercent = 15,
                FortifyMaxPercent = 10
            };

            stats1.MergeWith(stats2);

            Assert.Equal(300, stats1.TotalBaseAttack);
            Assert.Equal(25, stats1.AttackPercent);
            Assert.Equal(10, stats1.FortifyMaxPercent); // Max
        }

        #endregion

        #region EquipmentCalculator Tests

        [Fact]
        public void EquipmentCalculator_Calculate_SumsBaseStats()
        {
            var loadout = EquipmentLoadout.CreateDefault();
            
            var item1 = new EquipmentItem
            {
                InstanceId = "1",
                TierDef = new TierDef { BaseAttack = 100, BaseHp = 50 },
                Template = new EquipmentTemplate { Tier = 1, Element = "fire" }
            };
            var item2 = new EquipmentItem
            {
                InstanceId = "2",
                TierDef = new TierDef { BaseAttack = 200, BaseHp = 100 },
                Template = new EquipmentTemplate { Tier = 2, Element = "fire" }
            };

            loadout.Equip(0, item1);
            loadout.Equip(1, item2);

            var calculator = new EquipmentCalculator();
            var stats = calculator.Calculate(loadout);

            Assert.Equal(300, stats.TotalBaseAttack);
            Assert.Equal(150, stats.TotalBaseHp);
        }

        [Fact]
        public void EquipmentCalculator_CalculateSingle_ReturnsSingleItemStats()
        {
            var item = new EquipmentItem
            {
                InstanceId = "1",
                TierDef = new TierDef { BaseAttack = 100, BaseHp = 50 },
                Template = new EquipmentTemplate { Tier = 1, Element = "fire" }
            };

            var calculator = new EquipmentCalculator();
            var stats = calculator.CalculateSingle(item, "fire");

            Assert.Equal(100, stats.TotalBaseAttack);
            Assert.Equal(50, stats.TotalBaseHp);
        }

        #endregion

        #region EquipmentRepository Tests

        [Fact]
        public void EquipmentRepository_Shared_ReturnsSameInstance()
        {
            var repo1 = EquipmentRepository.Shared;
            var repo2 = EquipmentRepository.Shared;

            Assert.Same(repo1, repo2);
        }

        [Fact]
        public void EquipmentRepository_GetAllQualities_ReturnsQualities()
        {
            var repo = EquipmentRepository.Shared;
            var qualities = repo.GetAllQualities().ToList();

            Assert.True(qualities.Count >= 5); // white, green, blue, purple, orange
        }

        [Fact]
        public void EquipmentRepository_GetAllTiers_ReturnsTiers()
        {
            var repo = EquipmentRepository.Shared;
            var tiers = repo.GetAllTiers().ToList();

            Assert.True(tiers.Count >= 4); // T1-T4
        }

        [Fact]
        public void EquipmentRepository_GetAllTemplates_ReturnsTemplates()
        {
            var repo = EquipmentRepository.Shared;
            var templates = repo.GetAllTemplates().ToList();

            // 7 elements × 10 weapon types × 4 tiers = 280 templates
            Assert.True(templates.Count >= 280);
        }

        [Fact]
        public void EquipmentRepository_GetTemplatesByElement_FiltersCorrectly()
        {
            var repo = EquipmentRepository.Shared;
            var fireTemplates = repo.GetTemplatesByElement("fire").ToList();

            Assert.True(fireTemplates.Count >= 40); // 10 weapon types × 4 tiers
            Assert.All(fireTemplates, t => Assert.Equal("fire", t.Element));
        }

        [Fact]
        public void EquipmentRepository_GetTemplatesByWeaponType_FiltersCorrectly()
        {
            var repo = EquipmentRepository.Shared;
            var swordTemplates = repo.GetTemplatesByWeaponType("sword").ToList();

            Assert.True(swordTemplates.Count >= 28); // 7 elements × 4 tiers
            Assert.All(swordTemplates, t => Assert.Equal("sword", t.WeaponType));
        }

        [Fact]
        public void EquipmentRepository_GetTemplate_ReturnsCorrectTemplate()
        {
            var repo = EquipmentRepository.Shared;
            var template = repo.GetTemplate("sword_fire_t1");

            Assert.NotNull(template);
            Assert.Equal("sword_fire_t1", template.Id);
            Assert.Equal("sword", template.WeaponType);
            Assert.Equal("fire", template.Element);
            Assert.Equal(1, template.Tier);
        }

        [Fact]
        public void EquipmentRepository_CreateEquipment_CreatesItemWithAffixes()
        {
            var equipRepo = EquipmentRepository.Shared;
            var affixRepo = AffixRepository.Shared;
            
            // 创建橙装（4词条）
            var item = equipRepo.CreateEquipment("sword_fire_t1", "orange", affixRepo);

            Assert.NotNull(item);
            Assert.Equal("sword_fire_t1", item.TemplateId);
            Assert.Equal("orange", item.Quality);
            Assert.Equal(4, item.Affixes.Count); // 橙装应该有4个词条
        }

        [Fact]
        public void EquipmentRepository_CreateEquipment_WhiteHasNoAffixes()
        {
            var equipRepo = EquipmentRepository.Shared;
            var affixRepo = AffixRepository.Shared;
            
            // 创建白装（0词条）
            var item = equipRepo.CreateEquipment("sword_fire_t1", "white", affixRepo);

            Assert.NotNull(item);
            Assert.Equal("white", item.Quality);
            Assert.Empty(item.Affixes); // 白装没有词条
        }

        #endregion
    }
}
