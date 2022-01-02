#### Allows you to skip intros and outros of songs that are longer than 5 seconds without any significant objects.
#### Requires BeatSaberMarkupLanguage and SiraUtil
- Skips song to 2 seconds before first significant object if skipping intro.
- Skips song to one second before the end of the song if skipping outro

- Can be toggled in `UserData/IntroSkip.json`, as well as on modifiers menu in mods section.
#### Changelog 4.0.0
- Complete Internal Plugin Rewrite
- Moved Settings from Gameplay tab to Settings Tab

#### Changelog 3.0.0
- Rewrite to not be a giant mess
- Updated for Beat Saber 1.8.0, BSIPA 4
- Now uses BeatSaberMarkupLanguage for UI
#### Changelog 2.2.2
- Removed excess logging
#### Changelog 2.2.0
- Update For beat saber 0.13.0
#### Changelog 2.1.0
- Can now Skip Empty outros of songs as well

```csharp
{
  "AllowIntroSkip": true,
  "AllowOutroSkip": true
}
```
