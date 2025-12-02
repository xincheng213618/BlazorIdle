using BlazorIdle.Game.Professions;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 角色属性系统 Phase 1 单元测试 - 职业基础属性配置
    /// Character Attribute System Phase 1 Unit Tests - Profession Base Stats Configuration
    /// </summary>
    public class CharacterAttributePhase1Tests
    {
        #region ProfessionBaseStats Tests

        [Fact]
        public void ProfessionBaseStats_CreateDefault_ReturnsValidDefaults()
        {
            // Act
            var stats = ProfessionBaseStats.CreateDefault();

            // Assert
            Assert.Equal(100, stats.BaseAttack);
            Assert.Equal(500, stats.BaseHP);
            Assert.Equal(0.4, stats.AttackRateAPS);
            Assert.Equal(0.05, stats.Variance);
            Assert.Equal(5.0, stats.ReviveSec);
        }

        [Fact]
        public void ProfessionBaseStats_AllPropertiesCanBeSet()
        {
            // Arrange & Act
            var stats = new ProfessionBaseStats
            {
                BaseAttack = 150,
                BaseHP = 600,
                AttackRateAPS = 0.5,
                Variance = 0.06,
                ReviveSec = 4.0
            };

            // Assert
            Assert.Equal(150, stats.BaseAttack);
            Assert.Equal(600, stats.BaseHP);
            Assert.Equal(0.5, stats.AttackRateAPS);
            Assert.Equal(0.06, stats.Variance);
            Assert.Equal(4.0, stats.ReviveSec);
        }

        #endregion

        #region InitialCombatStats Tests

        [Fact]
        public void InitialCombatStats_CreateDefault_ReturnsAllZeros()
        {
            // Act
            var stats = InitialCombatStats.CreateDefault();

            // Assert
            Assert.Equal(0, stats.CritChancePercent);
            Assert.Equal(0, stats.CritDamageBonusPercent);
            Assert.Equal(0, stats.HastePercent);
            Assert.Equal(0, stats.AttackPercent);
            Assert.Equal(0, stats.SpecialAttackPercent);
            Assert.Equal(0, stats.HPPercent);
            Assert.Equal(0, stats.FortifyMaxPercent);
            Assert.Equal(0, stats.BackwaterMaxPercent);
            Assert.Equal(0, stats.ChasePercent);
            Assert.Equal(0, stats.KenChasePercent);
            Assert.Equal(0, stats.ChaseFlat);
            Assert.Equal(0, stats.DamageReductionPercent);
        }

        [Fact]
        public void InitialCombatStats_AllPropertiesCanBeSet()
        {
            // Arrange & Act
            var stats = new InitialCombatStats
            {
                CritChancePercent = 15.0,
                CritDamageBonusPercent = 10.0,
                HastePercent = 5.0,
                AttackPercent = 10.0,
                SpecialAttackPercent = 5.0,
                HPPercent = 8.0,
                FortifyMaxPercent = 10.0,
                BackwaterMaxPercent = 10.0,
                ChasePercent = 5.0,
                KenChasePercent = 3.0,
                ChaseFlat = 50,
                DamageReductionPercent = 5.0
            };

            // Assert
            Assert.Equal(15.0, stats.CritChancePercent);
            Assert.Equal(10.0, stats.CritDamageBonusPercent);
            Assert.Equal(5.0, stats.HastePercent);
            Assert.Equal(10.0, stats.AttackPercent);
            Assert.Equal(5.0, stats.SpecialAttackPercent);
            Assert.Equal(8.0, stats.HPPercent);
            Assert.Equal(10.0, stats.FortifyMaxPercent);
            Assert.Equal(10.0, stats.BackwaterMaxPercent);
            Assert.Equal(5.0, stats.ChasePercent);
            Assert.Equal(3.0, stats.KenChasePercent);
            Assert.Equal(50, stats.ChaseFlat);
            Assert.Equal(5.0, stats.DamageReductionPercent);
        }

        #endregion

        #region ProfessionConfig Tests

        [Fact]
        public void ProfessionConfig_DefaultValues_AreEmpty()
        {
            // Act
            var config = new ProfessionConfig();

            // Assert
            Assert.Equal("", config.Id);
            Assert.Equal("", config.Name);
            Assert.Equal("", config.Description);
            Assert.NotNull(config.BaseStats);
            Assert.NotNull(config.InitialCombatStats);
            Assert.NotNull(config.Resource);
            Assert.NotNull(config.DefaultSkills);
        }

        [Fact]
        public void ProfessionConfig_AllPropertiesCanBeSet()
        {
            // Arrange & Act
            var config = new ProfessionConfig
            {
                Id = "warrior",
                Name = "战士",
                Description = "高生命、稳定输出",
                BaseStats = new ProfessionBaseStats
                {
                    BaseAttack = 100,
                    BaseHP = 500
                },
                InitialCombatStats = new InitialCombatStats
                {
                    CritChancePercent = 5.0
                },
                Resource = new ProfessionResource
                {
                    Id = "rage",
                    Name = "怒气",
                    Max = 5
                },
                DefaultSkills = new ProfessionDefaultSkills
                {
                    NormalAttack = "warrior_attack_basic",
                    SpecialAttack = "warrior_special_pulse"
                }
            };

            // Assert
            Assert.Equal("warrior", config.Id);
            Assert.Equal("战士", config.Name);
            Assert.Equal("高生命、稳定输出", config.Description);
            Assert.Equal(100, config.BaseStats.BaseAttack);
            Assert.Equal(500, config.BaseStats.BaseHP);
            Assert.Equal(5.0, config.InitialCombatStats.CritChancePercent);
            Assert.Equal("rage", config.Resource.Id);
            Assert.Equal("怒气", config.Resource.Name);
            Assert.Equal(5, config.Resource.Max);
            Assert.Equal("warrior_attack_basic", config.DefaultSkills.NormalAttack);
            Assert.Equal("warrior_special_pulse", config.DefaultSkills.SpecialAttack);
        }

        #endregion

        #region ProfessionResource Tests

        [Fact]
        public void ProfessionResource_AllPropertiesCanBeSet()
        {
            // Arrange & Act
            var resource = new ProfessionResource
            {
                Id = "mana",
                Name = "法力",
                Max = 10,
                Initial = 5,
                GainPerAttack = 1,
                GainPerCritExtra = 1
            };

            // Assert
            Assert.Equal("mana", resource.Id);
            Assert.Equal("法力", resource.Name);
            Assert.Equal(10, resource.Max);
            Assert.Equal(5, resource.Initial);
            Assert.Equal(1, resource.GainPerAttack);
            Assert.Equal(1, resource.GainPerCritExtra);
        }

        #endregion

        #region ProfessionDefaultSkills Tests

        [Fact]
        public void ProfessionDefaultSkills_AllPropertiesCanBeSet()
        {
            // Arrange & Act
            var skills = new ProfessionDefaultSkills
            {
                NormalAttack = "mage_attack_basic",
                SpecialAttack = "mage_special_pulse"
            };

            // Assert
            Assert.Equal("mage_attack_basic", skills.NormalAttack);
            Assert.Equal("mage_special_pulse", skills.SpecialAttack);
        }

        #endregion

        #region ProfessionStatsRepository Tests

        [Fact]
        public void ProfessionStatsRepository_Shared_ReturnsSameInstance()
        {
            // Act
            var instance1 = ProfessionStatsRepository.Shared;
            var instance2 = ProfessionStatsRepository.Shared;

            // Assert
            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void ProfessionStatsRepository_IsLoaded_ReturnsTrue()
        {
            // Act
            var repo = ProfessionStatsRepository.Shared;

            // Assert
            Assert.True(repo.IsLoaded);
        }

        [Fact]
        public void ProfessionStatsRepository_Count_Returns4Professions()
        {
            // Act
            var repo = ProfessionStatsRepository.Shared;

            // Assert
            Assert.Equal(4, repo.Count);
        }

        [Fact]
        public void ProfessionStatsRepository_GetAllProfessionIds_Returns4Ids()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var ids = repo.GetAllProfessionIds();

            // Assert
            Assert.Equal(4, ids.Count);
            Assert.Contains("warrior", ids);
            Assert.Contains("mage", ids);
            Assert.Contains("rogue", ids);
            Assert.Contains("ranger", ids);
        }

        [Fact]
        public void ProfessionStatsRepository_GetProfession_Warrior_ReturnsValidConfig()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var config = repo.GetProfession("warrior");

            // Assert
            Assert.NotNull(config);
            Assert.Equal("warrior", config.Id);
            Assert.Equal("战士", config.Name);
            Assert.Equal(100, config.BaseStats.BaseAttack);
            Assert.Equal(500, config.BaseStats.BaseHP);
            Assert.Equal(0.4, config.BaseStats.AttackRateAPS);
            Assert.Equal(5.0, config.InitialCombatStats.CritChancePercent);
            Assert.Equal(0.0, config.InitialCombatStats.HastePercent);
        }

        [Fact]
        public void ProfessionStatsRepository_GetProfession_Mage_ReturnsValidConfig()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var config = repo.GetProfession("mage");

            // Assert
            Assert.NotNull(config);
            Assert.Equal("mage", config.Id);
            Assert.Equal("法师", config.Name);
            Assert.Equal(80, config.BaseStats.BaseAttack);
            Assert.Equal(300, config.BaseStats.BaseHP);
            Assert.Equal(0.35, config.BaseStats.AttackRateAPS);
            Assert.Equal(10.0, config.InitialCombatStats.CritChancePercent);
            Assert.Equal(10.0, config.InitialCombatStats.CritDamageBonusPercent);
            Assert.Equal(5.0, config.InitialCombatStats.SpecialAttackPercent);
        }

        [Fact]
        public void ProfessionStatsRepository_GetProfession_Rogue_ReturnsValidConfig()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var config = repo.GetProfession("rogue");

            // Assert
            Assert.NotNull(config);
            Assert.Equal("rogue", config.Id);
            Assert.Equal("盗贼", config.Name);
            Assert.Equal(70, config.BaseStats.BaseAttack);
            Assert.Equal(350, config.BaseStats.BaseHP);
            Assert.Equal(0.5, config.BaseStats.AttackRateAPS);
            Assert.Equal(15.0, config.InitialCombatStats.CritChancePercent);
            Assert.Equal(5.0, config.InitialCombatStats.CritDamageBonusPercent);
            Assert.Equal(10.0, config.InitialCombatStats.HastePercent);
        }

        [Fact]
        public void ProfessionStatsRepository_GetProfession_Ranger_ReturnsValidConfig()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var config = repo.GetProfession("ranger");

            // Assert
            Assert.NotNull(config);
            Assert.Equal("ranger", config.Id);
            Assert.Equal("游侠", config.Name);
            Assert.Equal(85, config.BaseStats.BaseAttack);
            Assert.Equal(400, config.BaseStats.BaseHP);
            Assert.Equal(0.45, config.BaseStats.AttackRateAPS);
            Assert.Equal(8.0, config.InitialCombatStats.CritChancePercent);
            Assert.Equal(5.0, config.InitialCombatStats.HastePercent);
        }

        [Fact]
        public void ProfessionStatsRepository_GetProfession_InvalidId_ReturnsNull()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var config = repo.GetProfession("invalid");

            // Assert
            Assert.Null(config);
        }

        [Fact]
        public void ProfessionStatsRepository_GetProfession_NullId_ReturnsNull()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var config = repo.GetProfession(null!);

            // Assert
            Assert.Null(config);
        }

        [Fact]
        public void ProfessionStatsRepository_GetProfession_CaseInsensitive()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var config1 = repo.GetProfession("warrior");
            var config2 = repo.GetProfession("WARRIOR");
            var config3 = repo.GetProfession("Warrior");

            // Assert
            Assert.NotNull(config1);
            Assert.NotNull(config2);
            Assert.NotNull(config3);
            Assert.Equal(config1.Id, config2.Id);
            Assert.Equal(config1.Id, config3.Id);
        }

        [Fact]
        public void ProfessionStatsRepository_GetBaseStats_ValidId_ReturnsStats()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var stats = repo.GetBaseStats("warrior");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(100, stats.BaseAttack);
            Assert.Equal(500, stats.BaseHP);
        }

        [Fact]
        public void ProfessionStatsRepository_GetBaseStats_InvalidId_ReturnsDefault()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var stats = repo.GetBaseStats("invalid");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(100, stats.BaseAttack); // Default value
            Assert.Equal(500, stats.BaseHP);     // Default value
        }

        [Fact]
        public void ProfessionStatsRepository_GetInitialCombatStats_ValidId_ReturnsStats()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var stats = repo.GetInitialCombatStats("rogue");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(15.0, stats.CritChancePercent);
            Assert.Equal(10.0, stats.HastePercent);
        }

        [Fact]
        public void ProfessionStatsRepository_GetInitialCombatStats_InvalidId_ReturnsDefault()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var stats = repo.GetInitialCombatStats("invalid");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(0, stats.CritChancePercent); // Default value
            Assert.Equal(0, stats.HastePercent);       // Default value
        }

        [Fact]
        public void ProfessionStatsRepository_GetResource_ValidId_ReturnsResource()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var resource = repo.GetResource("warrior");

            // Assert
            Assert.NotNull(resource);
            Assert.Equal("rage", resource.Id);
            Assert.Equal("怒气", resource.Name);
            Assert.Equal(5, resource.Max);
        }

        [Fact]
        public void ProfessionStatsRepository_GetResource_InvalidId_ReturnsNull()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var resource = repo.GetResource("invalid");

            // Assert
            Assert.Null(resource);
        }

        [Fact]
        public void ProfessionStatsRepository_GetDefaultSkills_ValidId_ReturnsSkills()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var skills = repo.GetDefaultSkills("mage");

            // Assert
            Assert.NotNull(skills);
            Assert.Equal("mage_attack_basic", skills.NormalAttack);
            Assert.Equal("mage_special_pulse", skills.SpecialAttack);
        }

        [Fact]
        public void ProfessionStatsRepository_GetDefaultSkills_InvalidId_ReturnsNull()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var skills = repo.GetDefaultSkills("invalid");

            // Assert
            Assert.Null(skills);
        }

        [Fact]
        public void ProfessionStatsRepository_HasProfession_ValidId_ReturnsTrue()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act & Assert
            Assert.True(repo.HasProfession("warrior"));
            Assert.True(repo.HasProfession("mage"));
            Assert.True(repo.HasProfession("rogue"));
            Assert.True(repo.HasProfession("ranger"));
        }

        [Fact]
        public void ProfessionStatsRepository_HasProfession_InvalidId_ReturnsFalse()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act & Assert
            Assert.False(repo.HasProfession("invalid"));
            Assert.False(repo.HasProfession(""));
            Assert.False(repo.HasProfession(null!));
        }

        [Fact]
        public void ProfessionStatsRepository_GetAllProfessions_Returns4Configs()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var professions = repo.GetAllProfessions();

            // Assert
            Assert.Equal(4, professions.Count);
            Assert.Contains(professions, p => p.Id == "warrior");
            Assert.Contains(professions, p => p.Id == "mage");
            Assert.Contains(professions, p => p.Id == "rogue");
            Assert.Contains(professions, p => p.Id == "ranger");
        }

        #endregion

        #region Profession Differentiation Tests

        [Fact]
        public void Professions_HaveDifferentBaseAttack()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var warrior = repo.GetBaseStats("warrior");
            var mage = repo.GetBaseStats("mage");
            var rogue = repo.GetBaseStats("rogue");
            var ranger = repo.GetBaseStats("ranger");

            // Assert
            Assert.Equal(100, warrior.BaseAttack); // Highest
            Assert.Equal(80, mage.BaseAttack);
            Assert.Equal(70, rogue.BaseAttack);     // Lowest
            Assert.Equal(85, ranger.BaseAttack);
        }

        [Fact]
        public void Professions_HaveDifferentBaseHP()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var warrior = repo.GetBaseStats("warrior");
            var mage = repo.GetBaseStats("mage");
            var rogue = repo.GetBaseStats("rogue");
            var ranger = repo.GetBaseStats("ranger");

            // Assert
            Assert.Equal(500, warrior.BaseHP);  // Highest
            Assert.Equal(300, mage.BaseHP);     // Lowest
            Assert.Equal(350, rogue.BaseHP);
            Assert.Equal(400, ranger.BaseHP);
        }

        [Fact]
        public void Professions_HaveDifferentAttackSpeed()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var warrior = repo.GetBaseStats("warrior");
            var mage = repo.GetBaseStats("mage");
            var rogue = repo.GetBaseStats("rogue");
            var ranger = repo.GetBaseStats("ranger");

            // Assert
            Assert.Equal(0.4, warrior.AttackRateAPS);
            Assert.Equal(0.35, mage.AttackRateAPS);   // Slowest
            Assert.Equal(0.5, rogue.AttackRateAPS);   // Fastest
            Assert.Equal(0.45, ranger.AttackRateAPS);
        }

        [Fact]
        public void Professions_HaveDifferentInitialCritChance()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var warrior = repo.GetInitialCombatStats("warrior");
            var mage = repo.GetInitialCombatStats("mage");
            var rogue = repo.GetInitialCombatStats("rogue");
            var ranger = repo.GetInitialCombatStats("ranger");

            // Assert
            Assert.Equal(5.0, warrior.CritChancePercent);   // Lowest
            Assert.Equal(10.0, mage.CritChancePercent);
            Assert.Equal(15.0, rogue.CritChancePercent);    // Highest
            Assert.Equal(8.0, ranger.CritChancePercent);
        }

        [Fact]
        public void Professions_HaveDifferentInitialHaste()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var warrior = repo.GetInitialCombatStats("warrior");
            var mage = repo.GetInitialCombatStats("mage");
            var rogue = repo.GetInitialCombatStats("rogue");
            var ranger = repo.GetInitialCombatStats("ranger");

            // Assert
            Assert.Equal(0.0, warrior.HastePercent);
            Assert.Equal(0.0, mage.HastePercent);
            Assert.Equal(10.0, rogue.HastePercent);  // Highest
            Assert.Equal(5.0, ranger.HastePercent);
        }

        [Fact]
        public void Mage_HasSpecialAttackBonus()
        {
            // Arrange
            var repo = ProfessionStatsRepository.Shared;

            // Act
            var mage = repo.GetInitialCombatStats("mage");
            var warrior = repo.GetInitialCombatStats("warrior");

            // Assert
            Assert.Equal(5.0, mage.SpecialAttackPercent);
            Assert.Equal(0.0, warrior.SpecialAttackPercent);
        }

        #endregion
    }
}
