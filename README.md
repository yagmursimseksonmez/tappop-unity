# TapPop: Unity project

A tap-to-pop puzzle made as a UI work sample. The tiles and UI were designed in Figma, put together in Unity 6.3 LTS with uGUI and TextMeshPro, and exported to WebGL.

- **Play:** https://yagmursimseksonmez.github.io/tappop/play/
- **Style guide:** https://yagmursimseksonmez.github.io/tappop/

Tap two or more touching tiles of one colour to pop them. Where tiles land, four or more of one colour pop by themselves. Ten levels, each asking for a few more tiles in a few fewer moves. Progress and stars are saved.

## Open it

1. Install Unity **6000.3.27f2** (6.3 LTS). Add WebGL Build Support only if you want the web build.
2. In Unity Hub: **Add → Add project from disk**, then pick the `TapPop` folder.
3. Open `Assets/_TapPop/Scenes/Game.unity` and press Play.

Each level is a `LevelConfig` asset in `Assets/_TapPop/Data`. Change moves, goal or star scores in the Inspector and press Play again.

## What's where

```
TapPop/Assets/_TapPop/
  Art/           tiles, icons and the spark, imported from figma-export
  Atlases/       one sprite atlas for the UI
  Audio/         pop sounds
  Data/          Level_01 to Level_10 and the tile set
  Fonts/         Geist, as TextMeshPro font assets
  Placeholders/  simple shapes for cards, pills, cells and circles
  Prefabs/       tile and button
  Scenes/        Game.unity
  Scripts/       game code
  Scripts/Editor/ import, scene and build tools
figma-export/    the @2x PNGs Unity imports: tiles from Figma, the rest from tools/make_art.py
tools/           scripts that drew the logo, stars and spark, and made the pop sounds
```

## How it fits together

- `GameController` runs a level: moves, goal, score, stars, and saved progress.
- `Board` owns the grid. A tap pops the group, tiles fall and new ones drop in. A group of four or more that a landing tile touches then pops by itself. Chains are free and count toward the goal.
- `LevelConfig` holds one level's moves, goal colour and count, star scores and chain rule.
- UI: `Hud`, `StartScreen` with `LevelPath` and `LevelNode`, `ResultPopup`, `PressableButton`, and `SafeArea` for phone notches.
- The **TapPop** menu in the editor:
  - **Import From figma-export** copies the PNGs into `Art`, applies the sprite import settings and relinks them in the scene.
  - **Build Game Scene** rebuilds `Game.unity` from code, so the scene and its wiring stay reproducible.
  - **Build WebGL** writes the web build next to the project.

## Credits

Design, tiles and UI: Yağmur Şimşek. Claude (Anthropic) helped with the C# scripts, the editor tools and the WebGL build.
Font: [Geist](https://github.com/vercel/geist-font), SIL Open Font License 1.1 (`TapPop/Assets/_TapPop/Fonts/Geist-OFL.txt`).
