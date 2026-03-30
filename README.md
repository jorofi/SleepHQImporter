# SleepHQ Importer

An ASP.NET Core Web API (.NET 10) that accepts ZIP file uploads via iOS Shortcuts and imports the contained files into [SleepHQ](https://sleephq.com).

## Projects

| Project | Description |
|---|---|
| **SleepHQImporter.Api** | Web API that handles file uploads, extracts ZIP archives, and forwards individual files to the SleepHQ API. |
| **SleepHQImporter.Tests** | Unit tests for the API using MSTest. |

## Features

- **Shortcut Upload Endpoint** – `POST /api/shortcuts/upload` accepts a ZIP file, extracts its contents, and uploads each file to SleepHQ via their API.
- **API Key Authentication** – Requests are authorized using an `X-Uplink-ApiKey` header validated against configuration.
- **SleepHQ OAuth Integration** – Authenticates with the SleepHQ API using OAuth client credentials.
- **Content Hashing** – Computes an MD5 content hash per file for SleepHQ's deduplication.

## Configuration

The application uses the standard ASP.NET Core configuration system. Configure the following settings via `appsettings.json` or user secrets:

```json
{
  "SleepHQ": {
    "BaseUrl": "https://sleephq.com",
    "ClientId": "<your-client-id>",
    "ClientSecret": "<your-client-secret>",
    "Scope": "read write delete"
  },
  "ShortcutUpload": {
    "ApiKey": "<your-api-key>",
    "RootPath": "<optional-local-storage-path>"
  }
}
```

## Running

```bash
cd SleepHQImporter.Api
dotnet run
```

## Testing

```bash
dotnet test
```
