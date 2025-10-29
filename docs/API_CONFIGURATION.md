# API Configuration

## Overview
The frontend application now uses a centralized configuration file to manage server IP addresses and API endpoints. This makes it easier to update the server URL without modifying code.

## Configuration File
The configuration is stored in `/BlazorIdle/wwwroot/appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7056"
  }
}
```

## How to Change the Server URL

### For Development
Edit `/BlazorIdle/wwwroot/appsettings.json` and change the `BaseUrl` value:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-dev-server:port"
  }
}
```

### For Production
Before deploying to production, update the `BaseUrl` in `appsettings.json` to point to your production server:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-production-server.com"
  }
}
```

## Technical Details

### ApiConfiguration Class
The `ApiConfiguration` class (`/BlazorIdle/Configuration/ApiConfiguration.cs`) provides:
- `BaseUrl`: The base URL of the API server
- `AuthApiUrl`: Full URL for authentication endpoints (`{BaseUrl}/api/auth`)
- `CharacterApiUrl`: Full URL for character endpoints (`{BaseUrl}/api/character`)
- `GameConfigApiUrl`: Full URL for game config endpoints (`{BaseUrl}/api/game-config`)

### Services Using Configuration
The following services have been updated to use the centralized configuration:
- **AuthService**: Authentication and user management
- **CharacterService**: Character CRUD operations
- **GameConfigService**: Game configuration loading

### Loading Process
The configuration is loaded during application startup in `Program.cs`:
1. An HttpClient fetches `appsettings.json` from the wwwroot folder
2. The JSON is deserialized into an `ApiConfiguration` object
3. The configuration is registered as a singleton service
4. All services receive the configuration via dependency injection

## Benefits
- **Single source of truth**: All API URLs are defined in one place
- **Easy updates**: Change the server URL without modifying code
- **Type safety**: Configuration is accessed through strongly-typed properties
- **Maintainability**: Reduces code duplication and potential errors
