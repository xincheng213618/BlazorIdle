using BlazorIdle.Shared.Models;
using BlazorIdle.Game.Config;
using Blazored.LocalStorage;

namespace BlazorIdle.Services
{
    /// <summary>
    /// 角色管理服务 - 暂时使用本地存储，将来可接入数据库
    /// Character management service - uses local storage temporarily, can be connected to database later
    /// </summary>
    public interface ICharacterService
    {
        Task<List<CharacterData>> GetCharactersAsync();
        Task<CharacterData?> GetCharacterAsync(string id);
        Task<CharacterData> CreateCharacterAsync(string name, ProfessionDef profession);
        Task DeleteCharacterAsync(string id);
        Task<CharacterData?> GetSelectedCharacterAsync();
        Task SetSelectedCharacterAsync(string? characterId);
    }

    public class CharacterService : ICharacterService
    {
        private const string StorageKey = "blazoridle_characters";
        private const string SelectedCharacterKey = "blazoridle_selected_character";
        private readonly ILocalStorageService _localStorage;
        private List<CharacterData>? _cachedCharacters;

        public CharacterService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<List<CharacterData>> GetCharactersAsync()
        {
            if (_cachedCharacters != null)
            {
                return _cachedCharacters;
            }

            var characters = await _localStorage.GetItemAsync<List<CharacterData>>(StorageKey);
            if (characters == null)
            {
                _cachedCharacters = new List<CharacterData>();
                return _cachedCharacters;
            }

            _cachedCharacters = characters;
            return _cachedCharacters;
        }

        public async Task<CharacterData?> GetCharacterAsync(string id)
        {
            var characters = await GetCharactersAsync();
            return characters.FirstOrDefault(c => c.Id == id);
        }

        public async Task<CharacterData> CreateCharacterAsync(string name, ProfessionDef profession)
        {
            var character = new CharacterData
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                ProfessionId = profession.Id,
                CreatedAt = DateTime.UtcNow,
                // 从职业模板复制初始属性
                MaxHp = profession.MaxHp,
                AttackRateAPS = profession.AttackRateAPS,
                DamagePerAttack = profession.DamagePerAttack,
                HastePercent = profession.HastePercent,
                SpecialIntervalSec = profession.SpecialIntervalSec,
                SpecialDamage = profession.SpecialDamage,
                CritChancePercent = profession.CritChancePercent,
                CritMultiplier = profession.CritMultiplier,
                VariancePct = profession.VariancePct,
                ReviveSec = profession.ReviveSec
            };

            var characters = await GetCharactersAsync();
            characters.Add(character);
            await SaveCharactersAsync(characters);

            return character;
        }

        public async Task DeleteCharacterAsync(string id)
        {
            var characters = await GetCharactersAsync();
            var character = characters.FirstOrDefault(c => c.Id == id);
            if (character != null)
            {
                characters.Remove(character);
                await SaveCharactersAsync(characters);

                // 如果删除的是当前选中的角色，清除选中状态
                var selectedId = await _localStorage.GetItemAsync<string>(SelectedCharacterKey);
                if (selectedId == id)
                {
                    await _localStorage.RemoveItemAsync(SelectedCharacterKey);
                }
            }
        }

        public async Task<CharacterData?> GetSelectedCharacterAsync()
        {
            var selectedId = await _localStorage.GetItemAsync<string>(SelectedCharacterKey);
            if (string.IsNullOrWhiteSpace(selectedId))
            {
                return null;
            }

            return await GetCharacterAsync(selectedId);
        }

        public async Task SetSelectedCharacterAsync(string? characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                await _localStorage.RemoveItemAsync(SelectedCharacterKey);
            }
            else
            {
                await _localStorage.SetItemAsync(SelectedCharacterKey, characterId);
            }
        }

        private async Task SaveCharactersAsync(List<CharacterData> characters)
        {
            _cachedCharacters = characters;
            await _localStorage.SetItemAsync(StorageKey, characters);
        }
    }
}
