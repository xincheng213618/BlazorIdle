# Character System Implementation

## Overview

This implementation introduces a character system that separates character instances from profession templates. Previously, the battle system used profession templates directly. Now, users must create a character based on a profession template, and battles use the created character.

## Architecture

### Data Model Location
- **CharacterData.cs** is placed in `BlazorIdle.Shared/Models/` for future database persistence
- This allows the same model to be used by both client and server when persistence is implemented

### Components

1. **CharacterData Model** (`BlazorIdle.Shared/Models/CharacterData.cs`)
   - Stores character information (ID, Name, ProfessionId)
   - Contains all character stats (copied from profession template on creation)
   - JSON serializable for storage

2. **CharacterService** (`BlazorIdle/Services/CharacterService.cs`)
   - Manages character CRUD operations
   - Currently uses browser LocalStorage (via Blazored.LocalStorage)
   - Designed to be easily converted to API calls for server persistence
   - Maintains a selected character state

3. **CharacterCreation Component** (`BlazorIdle/Components/CharacterCreation.razor`)
   - UI for creating new characters
   - Allows user to input name and select profession
   - Shows profession stats preview
   - Validates input (name required, min 2 characters)
   - Fires event when character is created

4. **CharacterSelector Component** (`BlazorIdle/Components/CharacterSelector.razor`)
   - Displays list of all created characters
   - Allows selection of active character
   - Shows character basic info (name, profession, stats, creation date)
   - Supports character deletion
   - Fires event when character is selected/deselected

5. **Updated Home Page** (`BlazorIdle/Pages/Home.razor`)
   - Contains both CharacterCreation and CharacterSelector
   - Only shows BattleDemo when a character is selected
   - Passes selected character to BattleDemo

6. **Updated BattleDemo** (`BlazorIdle/Components/BattleDemo.razor`)
   - Now accepts a `CharacterData` parameter
   - Removed profession selection UI
   - Uses character stats instead of profession template
   - Shows character info at the top

## Data Flow

```
User creates character → CharacterCreation
                     ↓
                CharacterService (stores in LocalStorage)
                     ↓
User selects character → CharacterSelector
                     ↓
                    Home
                     ↓
                 BattleDemo (uses character stats)
```

## Future Persistence

The character system is designed for easy migration to database persistence:

1. **CharacterData** is already in the Shared project
2. **CharacterService** interface can be implemented with API calls:
   ```csharp
   // Future server implementation
   public class ServerCharacterService : ICharacterService
   {
       private readonly HttpClient _httpClient;
       
       public async Task<CharacterData> CreateCharacterAsync(...)
       {
           return await _httpClient.PostAsJsonAsync("/api/characters", ...);
       }
       // ... other methods
   }
   ```

3. Add server-side endpoints in `BlazorIdle.Server`:
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class CharactersController : ControllerBase
   {
       // CRUD operations with database
   }
   ```

4. Replace service registration in Program.cs:
   ```csharp
   // Change from:
   builder.Services.AddScoped<ICharacterService, CharacterService>();
   
   // To:
   builder.Services.AddScoped<ICharacterService, ServerCharacterService>();
   ```

## Key Design Decisions

1. **Character stats are copied on creation**: Characters get a snapshot of profession stats at creation time. This allows future character progression without affecting the base profession template.

2. **Minimal changes to existing code**: The BattleInstance class remains unchanged - it still works with the Character class from `Game/Actors.cs`. We only changed how that Character instance is created (from CharacterData instead of ProfessionDef).

3. **LocalStorage for now**: Using browser storage allows the feature to work without backend changes. Data will persist across browser sessions but is local to each browser.

4. **Selected character state**: Only one character can be active at a time. This is stored separately in LocalStorage for easy access.

## Testing

To test the implementation:

1. Start the application with authentication enabled
2. Navigate to the Home page after login
3. Create a character:
   - Enter a name (min 2 characters)
   - Select a profession
   - View the profession stats preview
   - Click "创建角色" (Create Character)
4. Select the created character from the list
5. The BattleDemo component will appear below
6. Start a battle to verify the character's stats are used

## Notes

- Character deletion is immediate without confirmation for simplicity
- Character names are not validated for uniqueness
- No character level or progression system yet (stats are static after creation)
- The profession attribute values are shown in Chinese in the UI
