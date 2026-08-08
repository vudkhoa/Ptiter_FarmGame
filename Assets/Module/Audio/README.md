# Audio module

The module is intentionally small and fully 2D:

- one looping Music source;
- a configurable SFX pool shared by gameplay and UI;
- Master, Music, and SFX volume/mute settings saved in `PlayerPrefs`;
- one `AudioCatalogSO` containing the game's clips.

## Setup

1. Create **Farm Game > Audio > Settings** and **Farm Game > Audio > Catalog**.
2. Assign both assets to `RootLifetimeScope`.
3. Fill the catalog's Music, UI, Farm, Map, and Quest clip fields.
4. Add `AudioSettingsReference` to each Master, Music, and Sound settings row.

## Usage

Inject `IAudioService` and `AudioCatalogSO` into the controller that owns the
action:

```csharp
private readonly IAudioService _audio;
private readonly AudioCatalogSO _catalog;

public Example(IAudioService audio, AudioCatalogSO catalog)
{
    _audio = audio;
    _catalog = catalog;
}

public void Harvest()
{
    _audio.PlaySfx(_catalog.Harvest);
}

public void StartMusic()
{
    _audio.PlayMusic(_catalog.FarmMusic);
}
```

Pass an optional `0..1` volume only when a particular call needs adjustment:

```csharp
_audio.PlaySfx(_catalog.ButtonClick, 0.7f);
```

`AudioUiButton` is optional; UI controllers can call `PlaySfx` directly.
