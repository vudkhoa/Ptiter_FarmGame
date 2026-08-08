# Audio module

The root scope registers one `IAudioService` and one `IAudioSettingsProvider`.
The service creates one 2D Music source and a small 2D SFX source pool
automatically; no `AudioSource` or `AudioMixer` needs to be placed in a scene.
UI sounds use the `Sfx` bus, so the settings screen only needs Master, Music,
and SFX rows.

## Setup

1. Create an optional **Farm Game > Audio > Settings** asset and assign it to
   `RootLifetimeScope`. Leaving it empty uses the built-in defaults.
2. Create cues from **Farm Game > Audio > Cue** and assign one or more clips.
3. Add `AudioSettingsReference` to each settings row. Choose its bus, then
   assign that row's Slider and mute Toggle. Toggle ON means muted.
4. Add `AudioUiButton` to a Button and assign an SFX cue for click feedback.

Scene objects must be under a VContainer auto-injected scope. Runtime-created
UI prefabs must be instantiated or injected through VContainer, like the other
game UI controllers.

## Gameplay usage

```csharp
private readonly IAudioService _audio;

public Example(IAudioService audio) => _audio = audio;

public void Harvest()
{
    _audio.Play(_harvestCue);
}
```

Use `PlayMusic(cue)` for background music and `Play(cue)` for UI or 2D SFX.
Volume and mute values are restored automatically from `PlayerPrefs`.
