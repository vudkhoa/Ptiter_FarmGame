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
4. Register the Settings module after Audio. Its Music and Sound toggles map to
   the Music and SFX buses automatically; toggle ON means enabled.

`AudioSettingsReference` remains available for screens that expose per-bus
volume sliders or additional mute toggles.

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

Attach `AudioUiButton` to clickable `Button` objects. It handles click/error
feedback directly and does not require the prefab to be instantiated through
VContainer. UI controllers can still call `PlaySfx` for non-button cues.

## Feature integration

`Audio` is independent from gameplay feature modules. `Audio.Integration` owns
the adapters from game events to audio cues:

- `AudioUiButton`: component-driven click and disabled-button error feedback;
- `FarmAudioBridge`: plant, care, and harvest;
- `MapAudioBridge`: successful player placement only;
- `EconomyAudioBridge`: coin, reward, and transaction errors;
- `FarmBgmController`: starts the farm music cue.

Register them once with `RegisterAudioIntegration()` after all event-owning
modules have registered their MessagePipe brokers. Feature modules should keep
publishing domain events and must not depend directly on the audio module.
