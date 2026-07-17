# HsMod

Hearthstone Modification based on BepInEx. The plugin source code is at [github.com/Pik-4/HsMod](https://github.com/Pik-4/HsMod). The plugin does not collect any information about you; the project follows AGPL-3.0 and is used for learning and research only.

[![License](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.x-green.svg)](https://github.com/BepInEx/BepInEx/releases)
[![.NET](https://img.shields.io/badge/.NET%20SDK-8.x-512BD4.svg)](https://dotnet.microsoft.com/download)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)](#installation)

> **Warning.** The Hearthstone client in mainland China starts the anti-cheat SDK by default. The plugin will try to block the related anti-cheat functions, but it cannot guarantee the safety of your account.

HsMod is based on [Simplified Chinese](https://github.com/Pik-4/HsMod/blob/bepinex5/[CHS]ReadMe.md); the English version may contain translation errors. You can edit the JSON files in `HsMod/Languages` and recompile to support your region's language. The plugin is not yet fully translated.

**[Documentation Wiki](../../wiki)** covers installation, every setting, hotkeys and troubleshooting in detail.

## Quick start (Windows)

1. Download `HsMod.dll` from Releases (or [build it](#building-from-source)).
2. Extract [BepInEx x64](https://github.com/BepInEx/BepInEx/releases) into the Hearthstone folder.
3. Copy the `HsMod/UnstrippedCorlib` DLLs into `Hearthstone\BepInEx\unstripped_corlib\`.
4. In `doorstop_config.ini`, set `dll_search_path_override = BepInEx\unstripped_corlib`.
5. Put `HsMod.dll` in `Hearthstone\BepInEx\plugins`, then launch.

Open the settings in-game with `Esc` -> **Mod Settings**. Full steps and other platforms are in [Installation](../../wiki/Installation).

## Contents

- [Features](#features)
- [Installation](#installation)
- [Building from source](#building-from-source)
- [Version scheme](#version-scheme)
- [Additional notes](#additional-notes)
- [client.config](#clientconfig)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [References](#references)

## Features

### Battlegrounds

- Opponent MMR shown under each player's name in the leaderboard overlay (solo and duos, loaded in the background at match start).
- Session statistics: place, MMR change and hero are logged per ranked game to a CSV and shown in a chart with today / 7 days / month / all-time and solo / duos filters.
- Shop hotkeys: refresh, freeze, tavern upgrade, hero power, and teammate board in duos. Rebindable, active only during the shopping phase.
- Auto-squelch all opponents' emotes at the start of a match (off by default).
- Fast battle that skips part of the animation (silkier than the gear, shields the finisher effect on cast; also works in the tavern with Mercenaries PvE, though the final death settlement may lag).

### Interface and quality of life

- In-game settings menu opened from a "Mod Settings" button in the game menu (Esc or gear icon), with categories, search and key-binding capture. The button can be hidden, and a web page fallback is available.
- Gear fast and slow up to 8x speed (configurable up to 32x).
- Display game frame rate information and modify the game frame rate.
- Remove the window focus requirement and window resizing restrictions.
- Block pop-up prompts (for example, cannot match).
- Remove Chinese hints, and hints for nerf patches, advertisements and ladder settlement rewards.
- Block end-of-game promotion tips and settlement tips.
- Block the prompts of battle orders, achievements and other rewards.
- Display the actual number (9+) of cards in favorites.
- Display the DbId when right-clicking a selected card in the collection, hero, card back, hit effect, tavern panel and so on.
- Remove `new!` markers with one click (requires a shortcut; may need re-entering the collection; may not work in Mercenaries after a restart).
- Abandon a duel at 0-0 (with no deck).
- Mute Hearthstone with a shortcut key.

### Automation and AFK

- Log in with VerifyWebCredentials (also supports command-line startup, no Battle.net needed).
- Disable dropouts to allow long periods with no action.
- Auto-collect rewards from Arena, Duels, Mercenaries and similar (opens the reward package at the end).
- Quick pack opening (space opens five packs at a time), with optional auto-disenchant of fully-refundable cards when opening.
- One-click auto-disenchant of fully-refundable decks (requires a shortcut).
- Auto-report opponents; when enabled, game logs can be generated automatically.
- Block error reporting so no error messages are sent to Blizzard, or allow error reporting with auto-exit.
- Auto-rotate decks, and auto-watch both sides of the game when spectating a friend.
- Receive Hearthstone startup parameters, such as a specified resolution.

### Emotes and opponents

- Automatically block opponent emotes or set an emote limit; block thinking emotes; block Bob's voice; skip hero introductions in matchmaking.
- Emote without cooldown (minimum 1.5s interval) and emote shortcut keys.
- Block the opponent's card effects individually.
- Display the opponent's full Battle.net nickname, and add opponents in matchmaking.
- Block streamer mode: force it off and block the accidental Ctrl+Shift+S toggle that replaces player names with hero names.
- Retrieve tavern player nicknames by clicking their avatar.
- Display the opponent's ladder rank before Legend.
- Mark the opponent's known cards.

### Skins and cosmetics

- Modify skins: matchmaking hero skin, tavern hero skin, finisher effect, matchmaking panel, tavern panel, lucky coin and more (configure `HsSkins.cfg` or edit in settings; press F4 to save when updating in a match, then simulate a disconnect).
- Modify card backs (takes effect in-game automatically).
- Auto gold and diamond cards.
- Mercenary random skins, forced diamond skins and similar.
- Block pop-up windows for mercenary treasure chests, ladder rewards and similar.
- Shield the mercenary matchmaking interface zoom.

### Simulation and developer

- Access Hearthstone Developer Mode.
- Simulate positioning for Hearthside parties, and simulate an unplug or disconnect (requires a shortcut).
- Simulate opening packs (random or fixed results; custom type, quantity, rarity and quality).
- Device simulation to collect pack card backs for iOS, Android and other devices (may need one matchmaking game).
- Buy adventures with gold, such as Naxxramas, Blackrock Mountain and League of Explorers (Karazhan is supported but cannot play the prologue).
- Force-open Karazhan (cannot play the prologue, cannot skip a level until it is cleared).
- Information display (Showinfo): requires the plugin, default HTTP on port 58744; shows mercenary progress, pack-opening history and more.
- Webshell at path `/shell` (must be enabled in settings; Chinese display may be garbled).
- Read local files via the web, i.e. parse static pages (under development; uses `Hearthstone/BepInEx/HsMod` as the root).
- Attempt to disable anti-cheat.

<details>
<summary>Removed or patched by Blizzard</summary>

- ~~Lifting the set-recognition restriction to open Manning Hearthstone.~~ Fixed by Blizzard.
- ~~Zero-Dollar Shopping.~~ Fixed by Blizzard.

</details>

## Installation

Choose BepInEx 5 (BepInEx 6 is still pre-release and is not adapted for now). Hearthstone became a 64-bit program on 2026-07-01, so use the x64 BepInEx build. The Hearthstone install path must not contain non-Latin characters.

<details>
<summary>Windows</summary>

1. Compile HsMod (see [Building from source](#building-from-source)) or download `HsMod.dll` from Releases.
2. Download [BepInEx x64](https://github.com/BepInEx/BepInEx/releases) and extract it into the Hearthstone root directory (`Hearthstone\`).
3. Create `Hearthstone\BepInEx\unstripped_corlib\` and copy every DLL from the project directory `HsMod/UnstrippedCorlib` into it.
4. Edit `Hearthstone\doorstop_config.ini`:
   - BepInEx 5.4.23.2 or newer: set `dll_search_path_override = BepInEx\unstripped_corlib`
   - older BepInEx: set `dllSearchPathOverride=BepInEx\unstripped_corlib`
5. Put `HsMod.dll` into `Hearthstone\BepInEx\plugins`.

</details>

<details>
<summary>macOS</summary>

1. Download [BepInEx_macos_universal](https://github.com/BepInEx/BepInEx/releases) (BepInEx 5) and extract it into `Hearthstone/`.
2. Copy every DLL from `HsMod/UnstrippedCorlibUnix` into `/Applications/Hearthstone/BepInEx/unstripped_corlib/`. Mono and Unity versions must match Hearthstone.
3. In `run_bepinex.sh`, set `dll_search_path_override="BepInEx/unstripped_corlib"` and `executable_name="Hearthstone.app"`.
4. Run `chmod u+x run_bepinex.sh`.
5. Obtain a login token from a Battle.net login URL (see [client.config](#clientconfig)): copy the part after `http://localhost:0/?ST=` and before `&accountId=`.
6. Optionally create a `client.config` so the token persists between launches.
7. Download HsMod from Releases and unzip into `Hearthstone/BepInEx/plugins`.

Launch with `./run_bepinex.sh TOKEN`, or `./run_bepinex.sh` when a `client.config` is present.

</details>

<details>
<summary>Linux</summary>

1. Compile HsMod or download `HsMod.dll` from Releases.
2. Install Hearthstone for Linux via [0xf4b1/hearthstone-linux](https://github.com/0xf4b1/hearthstone-linux), which usually sets up `client.config` for you.
3. Download [BepInEx (unix, v5)](https://github.com/BepInEx/BepInEx/releases) and extract it into the Hearthstone root.
4. Create `hearthstone/BepInEx/unstripped_corlib/` and copy the DLLs from `HsMod/UnstrippedCorlibUnix` into it (UniTask is extracted from net48 of OpenMod.UniTask.2021.2.4.1).
5. In `run_bepinex.sh`, set `DOORSTOP_CORLIB_OVERRIDE_PATH="$BASEDIR/BepInEx/unstripped_corlib"` and `executable_name="Bin/Hearthstone.x86_64"`, then normalize line endings with `sed -i "s/\r/ /g" ./run_bepinex.sh`.
6. If `client.config` is not configured, follow the macOS steps to set it up.
7. Put `HsMod.dll` into `hearthstone/BepInEx/plugins` (create it if missing).
8. Run `chmod u+x run_bepinex.sh` and then `./run_bepinex.sh`.

</details>

## Building from source

```bash
# .NET SDK 8.x; output: ./HsMod/Release/HsMod.dll
git clone --depth 1 --branch bepinex5 https://github.com/Pik-4/HsMod
cd HsMod
dotnet build --configuration Release --no-restore
```

The post-build step (`install.bat`) copies the compiled DLL to a default Hearthstone install if BepInEx is already configured there.

## Version scheme

For example, HsMod version `3.0.0.0`:

- First digit (3): the Hearthstone major version, e.g. 3 means 26.
- Second digit (0): how many times Hearthstone has been updated within that version. It does not map to Hearthstone minor versions, and is not incremented when a Hearthstone update does not change files such as `Assembly-CSharp.dll`.
- Third digit (0): incremented by 1 when HsMod gains new features for that Hearthstone version; reset to zero when the second digit changes.
- Fourth digit (0): the compile version, mainly tracking bug fixes for the third digit.

A Hearthstone update does not necessarily break HsMod. If the plugin still works, it does not need to be updated from Releases. Feature changes can be tracked in the commit history.

## Additional notes

1. The plugin path may not contain non-Latin characters, i.e. the Hearthstone install path cannot contain them.
2. HsMod may conflict with mods based on `Assembly-CSharp.dll`. Modifying `Assembly-CSharp.dll` can misalign IL offsets and prevent patches from taking effect. It may also conflict with other BepInEx plugins (such as Mercenaries or MixMod) when the same method is patched more than once; the plugin does not detect whether the original method has been modified.
3. The skin configuration file is `Hearthstone\BepInEx\config\HsSkins.cfg`, created automatically on first run.
4. `F4` is a fixed shortcut that dumps some in-game information (stored under `Hearthstone\BepInEx\`), updates the skin configuration and restarts the web service. All other shortcuts are configurable.
5. By default almost all features must be enabled manually. Most are in the configuration; a few are only mentioned in the patch (such as minimizing restrictions).
6. The web server (Showinfo) default port is 58744 and generally listens on all local IPs. On cloud servers, mind your firewall and security groups.
7. The statistics log is `BepInEx\HsMatch.log` (fields separated by commas), configurable in settings.
8. When a problem occurs, first delete the relevant `.cfg` (usually in `BepInEx\config\`) and reconfigure. If it persists, attach `HsMod.cfg` to an issue, though a timely answer is not guaranteed.
9. `GetHsLib.py` updates Hearthstone's own runtime libraries; `install.bat` copies the compiled `HsMod.dll` to the default Hearthstone directory (provided BepInEx is configured).
10. If skins display incorrectly, check `HsSkins.cfg` and try deleting `HsMod.cfg` to reconfigure.
11. If modified settings do not save, check whether other Hearthstone plugins are enabled.
12. Use BepInEx 5. BepInEx 6 is still pre-release and is not adapted for now.

## client.config

`client.config` lets you start Hearthstone bypassing Battle.net. Place it next to `Hearthstone.exe`:

```ini
[Config]
Version = 3
[Aurora]
VerifyWebCredentials = "TOKEN"
ClientCheck = 0
Env.Override = 1
Env = us.actual.battle.net
```

Token login URLs (pick your region):

```
https://us.battle.net/login/en/?app=wtcg
https://eu.battle.net/login/en/?app=wtcg
https://tw.battle.net/login/zh/?app=wtcg
https://kr.battle.net/login/zh/?app=wtcg
https://account.battlenet.com.cn/login/zh-cn/?app=wtcg
```

The `Env` value for China is `cn.actual.battlenet.com.cn`; `Env` otherwise matches the first two characters of the token. With the plugin enabled, `./Hearthstone.exe VerifyWebCredentials` also works and no longer strictly requires a `client.config`. If the token becomes obsolete and the game stops opening, update it in `client.config`.

## Roadmap

- Organize README and Wiki; organize the relationship between configuration and patches; multi-language support.
- In-game one-click hero skin change (currently only updatable by simulating a disconnect).
- Refactor the Showinfo web pages.
- Adapt to macOS.
- Fix Mercenaries-related features.

## Contributing

Development happens on the upstream repository, [Pik-4/HsMod](https://github.com/Pik-4/HsMod). Bug reports and feature ideas are welcome via its [issues](https://github.com/Pik-4/HsMod/issues); when reporting a bug, attach your `HsMod.cfg`. To add a translation, edit the JSON files in `HsMod/Languages` and open a pull request.

Community:

- Telegram: [@HearthstoneMod](https://t.me/HearthstoneMod) (suggestions and recommendations)
- [Discord](https://discord.gg/BCnhxtXHVq)

## References

1. [MixMod (4pda)](https://4pda.to/forum/index.php?showtopic=870696&st=4780#entry114865283)
2. [MixMod (GitHub)](https://github.com/DeNcHiK3713/MixMod)
3. [Hearthstone Apollo Mod](https://hearthmod.com/)
4. [Making BepInEx plugin mods for Unity games from scratch](https://mod.3dmgame.com/read/3)
5. [BepInEx Docs](https://docs.bepinex.dev/)
6. [Harmony](https://harmony.pardeike.net/articles/intro.html)
7. [List of CIL instructions](https://en.wikipedia.org/wiki/List_of_CIL_instructions)
8. [hearthstone-linux](https://github.com/0xf4b1/hearthstone-linux)
