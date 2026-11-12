# Inventory Kamera Modern - A Genshin Data Scanner

Fan-made Genshin Impact tool that scans characters, weapons, artifacts, materials and character development items in your inventory using the OCR technique.

**[⬇ Download the latest release](https://github.com/syrusgreycloak/Inventory_Kamera_Modern/releases/latest)** (Windows, 64-bit) · [Installation](#installing-inventory-kamera-modern)

This scanner exports in `.GOOD`, a JSON-based exporting format, which allows you to use it with compatible online Genshin Impact tools. These tools include artifact optimizing tools including [Genshin Optimizer](https://frzyc.github.io/genshin-optimizer/#/), [SEELIE.me](https://seelie.me/) and [Aspirine's Genshin Impact Calculator](https://genshin.aspirine.su/).

## About this project

**Inventory Kamera Modern** is a modernized fork of the original [Inventory Kamera](https://github.com/Andrewthe13th/Inventory_Kamera), which is actively maintained at [taiwenlee/Inventory_Kamera](https://github.com/taiwenlee/Inventory_Kamera). It keeps the same approach, OCR on screenshots of the game menus, with a new UI and a reworked scanner:

- **New Avalonia UI** with Scan, Export, Settings and About tabs and a live console. The old Windows Forms UI is retired; its last version is kept on the [`history`](../../tree/history) branch and the `winforms-final` tag.
- **.NET 8** (from .NET Framework 4.7.2), with the scanning logic split into `InventoryKamera.Core` and `InventoryKamera.Infrastructure` and covered by tests.
- **GOOD v3 export**, including equipped weapons and artifacts, and a one-click upload to Genshin Optimizer.
- **Faster character scans:** talent levels are read from one screenshot of the Talents tab instead of opening each talent (it falls back to clicking when the screenshot is unclear), and the constellation tab is read as it opens.
- **Scanning fixes**, among them:
  - material counts are read from the card's label strip, so the last page (fish included) no longer misreads;
  - artifact and weapon locks are detected by the red padlock, so locks aren't missed on the Fast scan speed;
  - character levels, constellations (including the C3/C5 talent order) and long names such as Kaedehara Kazuha read correctly.
- **Configurable game data source:** if the game data repository moves, point the updater at the new one in Settings.

On the maintainer's account (94 characters, 155 weapons, 684 artifacts, about 600 materials) a full scan at Fast speed matches the game exactly.

## Table of Contents
> **Note**
>
> Please **read the following instructions carefully** and set up before using the scanner.
- Getting Started
  - [Installing Inventory Kamera Modern](#installing-inventory-kamera-modern)
  - [Setting up Genshin Impact](#setting-up-genshin-impact)
  - [Settings and configurations](#how-to-configure-inventory-kamera-modern)
  - [Running Inventory Kamera Modern](#how-to-run-inventory-kamera-modern)
- Scanner
  - [Updating the database](#updating-for-new-game-versions)
- Repository
  - [Reporting a bug or a scanning issue](#reporting-an-issue)
  - [Requesting a new feature](#requesting-a-new-feature)
  - [Asking a question](#asking-a-question)
  - [Frequently Asked Questions (FAQ)](#frequently-asked-questions-faq)
  - [Credits](#credits)
  - [License](#license)

## Installing Inventory Kamera Modern

You need Windows 10 or 11 (64-bit) with Genshin Impact ([GenshinImpact.exe](https://genshin.hoyoverse.com/) or [YuanShen.exe](https://ys.mihoyo.com/)). Nothing else needs to be installed: the release includes .NET and the Visual C++ runtime the OCR engine uses.

1. Download `InventoryKameraModern-<version>-win-x64.zip` from the [latest release](https://github.com/syrusgreycloak/Inventory_Kamera_Modern/releases/latest).
2. Unzip it to a folder of your own, e.g. `Documents\InventoryKameraModern`. Avoid `Program Files`: the app keeps its game data (`inventorylists`) and logs (`logging`) next to the exe.
3. Run `InventoryKamera.Avalonia.exe`.

On first start:

- **"Windows protected your PC"**: the app isn't code-signed (certificates cost money every year), so Windows SmartScreen warns about it. Click **More info**, then **Run anyway**.
- **Administrator prompt**: the app needs administrator rights to send keyboard and mouse input to the game, which runs elevated.
- **Antivirus**: tools that capture the screen and simulate input are sometimes flagged. The source is all here, and the release is built from it by GitHub Actions ([workflow](.github/workflows/release.yml)).
- The app opens on the `Settings` tab; see [configuring](#how-to-configure-inventory-kamera-modern).
- If it says it **couldn't start** because a library couldn't be loaded, install the [Microsoft Visual C++ Redistributable (x64)](https://aka.ms/vs/17/release/vc_redist.x64.exe) and try again (a restart may be needed).

To update, unzip the new release into a new folder and delete the old one. Settings are kept in `%AppData%\InventoryKamera`, so they carry over. The About tab can check for new releases.

Scanning is Windows-only, because it captures and controls the game window.

### Building from source

Needs the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/syrusgreycloak/Inventory_Kamera_Modern.git
cd Inventory_Kamera_Modern
dotnet build InventoryKamera.Avalonia/InventoryKamera.Avalonia.csproj -c Release
# Run:
InventoryKamera.Avalonia/bin/Release/net8.0-windows/InventoryKamera.Avalonia.exe
```

If you run it from an IDE such as Rider, start the IDE as administrator and use the `net8.0-windows` target.

## Setting up Genshin Impact

1. Log in to Genshin Impact and click start.
2. Open [Paimon menu](https://genshin-impact.fandom.com/wiki/Paimon_Menu) (Default shortcut: `ESC`).
3. Go to `Settings` (Cog Icon ⚙) and set these settings:
   - Under `Languages`, set _Game Language_ to English.
   - The game resolution should be _16:9_ or _16:10_. If your screen has a matching native resolution, you may set _Display Mode_ under `Graphics` to fullscreen. Otherwise, you'll have to switch it to any _windowed_ resolution that matches one of these aspect ratios.
     - Some examples of good resolutions are: 1920x1080 (Full HD), 1920x1200, 2560x1440 (QHD), 3840x2160 (4K), etc.
     - Don't know if a resolution is 16:9 or 16:10? [Find out here](https://andrew.hedges.name/experiments/aspect_ratio/).
        > **Warning**
        >
        > Ultrawide screens (21:9) are not supported yet. Run the game windowed at a 16:9 resolution instead.
        > Additionally, HDR is not currently supported. If you experience scanning issues, please make sure this is disabled in Windows Settings
   - Under `Controls`, set _Control Type_ to Keyboard.
     - If you rebound keys (inventory (default: B), character screen (default: C), or selecting character 1 (default: 1)) revert your binding to default or set the new key bindings under `Settings` > `Hotkeys`.

## How to configure Inventory Kamera Modern

The app opens on the `Settings` tab on first start, and whenever no output folder is set (scans aren't saved without one). You can:

- Choose a **Scan Speed**: _Fast_, _Normal_ or _Slow_. Start with Fast; if you see misreads on a slower PC, try Normal.
- Set **Item Filters**: minimum weapon and artifact rarity and level.
- Choose whether to **update equipped weapons and artifacts** during the scan (needed for the `location` field in the export).
- Under **Names**, enter your **Traveler's** name (leave blank to read it from the game) and the **Wanderer's** name if you renamed him. If you renamed your Miliastra Wonderland **mannequins**, enter their names too; mannequins are skipped during scans because Genshin Optimizer doesn't support them.
- Rebind the **Hotkeys** for the inventory, character screen and first character slot.
- Pick the **Output folder** for exports.
- Turn on **Log all scan screenshots** to save what the scanner saw to the `logging` folder (useful for bug reports).
- Under **Advanced**, add a **game executable** name if your game runs under a name other than `GenshinImpact` or `YuanShen`.
- Under **Advanced**, change the **Game data source** (see [Updating for new game versions](#updating-for-new-game-versions)).

## How to run Inventory Kamera Modern

1. Open the game and stand in the **open world** (or open the [Paimon menu](https://genshin-impact.fandom.com/wiki/Paimon_Menu)); the scan opens the menu itself if needed. Don't be in a dialogue, cutscene or other menu.
2. In the `Scan` tab, choose the categories to scan (Weapons, Artifacts, Character Dev Items, Materials, Characters; they're scanned in that order) and click **Start Scan**. Weapon enhancement ores are only shown on the weapon page, so they're read at the end of the weapon scan when both Weapons and Character Dev Items are selected.
> **Warning**
>
> While scanning, **do not use your mouse or keyboard**. The scanner uses keyboard and mouse input to automate scanning, and moving the mouse can make it misread.
>
> To stop a scan early, Alt+Tab to Inventory Kamera Modern and press `Esc` or `Enter`, or click **Stop Scan**. Nothing is exported automatically after a cancelled scan; use **Manual Export** to save what was scanned so far.

3. When the scan finishes, the GOOD file is written to the output folder. Use **Open output folder** in the `Export` tab to find it, or **Upload to Genshin Optimizer** to copy the data and open the optimizer.

## Updating for new game versions

Inventory Kamera Modern uses lists of valid items and characters to assist with text recognition. These lists are kept locally in the `inventorylists` folder. These lists should be updated every time a new version of the game is released, and it can be done either automatically or manually.

### Updating automatically

Click `Update game data` in the `About` tab after each game update. Inventory Kamera Modern syncs with [Dimbreath's Anime Game Data Repo](https://gitlab.com/Dimbreath/animegamedata2) for new game versions. Big thanks for all the hard work done there.

If it doesn't find a newer game version, it offers to run the update anyway.

If the game data repository moves again, you can point the updater at the new one without waiting for an app update: under `Settings` > `Advanced` > `Game data source`, paste the GitLab or GitHub repository URL and branch, then use `Test` to check it. `Reset to default` restores Dimbreath's repository.

### Updating manually

> **Warning**
> 
> This option should only be necessary if `Update game data` reports an error or the generated outputs are invalid.

The materials and weapons lists are kept in a simple key:value JSON-readable format. 'value' is the name of an item in [PascalCase](<https://en.wikipedia.org/wiki/Naming_convention_(programming)#Examples_of_multiple-word_identifier_formats>) and 'key' is whatever 'value' is but in lowercase. 

The format for manually updating characters and artifacts is slightly different. The key for both json files is still the lowercase version of the character/artifact set name in PascalCase, and the value is in the following example formats:

characters.json
```json
"albedo": {
    "GOOD": "Albedo",
    "ConstellationName": [
      "Princeps Cretaceus"
    ],
    "ConstellationOrder": [
      "skill",
      "burst"
    ],
    "Element": [
      "geo"
    ],
    "WeaponType": 0
  },


"traveler": {
    "GOOD": "Traveler",
    "Element": [
      "electro",
      "pyro",
      "dendro",
      "geo",
      "hydro",
      "anemo"
    ],
    "ConstellationOrder": {
      "electro": [
        "burst",
        "skill"
      ],
      "dendro": [
        "skill",
        "burst"
      ],
      "geo": [
        "burst",
        "skill"
      ],
      "hydro": [
        "skill",
        "burst"
      ],
      "anemo": [
        "burst",
        "skill"
      ]
    },
    "WeaponType": 0
  },
```
Note: The ConstellationName value is formatted as an array. This primarily applies to only the traveler as the male and female versions of the characters have different constellation names.
Note: The Traveler is a unique case in which constellation skill orders are be provided for each currently available element.

The character's name is as it appears in the party menu on the right side of the in-world UI or the character's menu screen in the top left corner. The constellation order depends on which talent the third constellation upgrades for each character.

Weapon type values are as follows:

0 = Sword, 1 = Claymore, 2 = Polearm, 3 = Bow, 4 = Catalyst

artifacts.json
```json
"adventurer": {
    "setName": "Adventurer",
    "GOOD": "Adventurer",
    "normalizedName": "adventurer",
    "artifacts": {
      "goblet": {
        "artifactName": "Adventurer's Golden Goblet",
        "GOOD": "AdventurersGoldenGoblet",
        "normalizedName": "adventurersgoldengoblet"
      },
      "plume": {
        "artifactName": "Adventurer's Tail Feather",
        "GOOD": "AdventurersTailFeather",
        "normalizedName": "adventurerstailfeather"
      },
      "circlet": {
        "artifactName": "Adventurer's Bandana",
        "GOOD": "AdventurersBandana",
        "normalizedName": "adventurersbandana"
      },
      "flower": {
        "artifactName": "Adventurer's Flower",
        "GOOD": "AdventurersFlower",
        "normalizedName": "adventurersflower"
      },
      "sands": {
        "artifactName": "Adventurer's Pocket Watch",
        "GOOD": "AdventurersPocketWatch",
        "normalizedName": "adventurerspocketwatch"
      }
    }
  },
```

Consider using [a JSON text validator](https://jsonlint.com/) after following this manual method. Support may or may not be provided if you choose this route.

## Reporting an issue

If you run into a problem with the scanner (e.g. a bug, app crash, invalid export format), please open an issue on this project's GitHub repository and try to fill it out as much as possible. Screenshots, video recordings, and the contents of the `logging` folder are extremely helpful for debugging — attach a zipped copy if relevant. You may turn on 'Log all scan screenshots' in Settings to force screenshot capture in most areas that may concern the developer.

> **Note**
>
> Before submitting an issue, **please check for similar existing issues, especially the ones still open.**

## Requesting a new feature

If you would like to request a new feature, please open a discussion or feature-request issue on this project's GitHub repository.

## Asking a question

For general questions, open a discussion thread on this project's GitHub repository.

## Frequently Asked Questions (FAQ)

#### Can Inventory Kamera Modern get me banned?

According to [HoYoverse's response to Script, Plug-In, and Third-Party Software](https://genshin.hoyoverse.com/en/news/detail/5763), we believe, no.

The scanner does not provide any exploits or game progression. It only takes screenshots of a portion of the game window, processes the image and exports the data. The account will still be yours to keep. We do not provide any account exchanges or primogem top-ups. Game interaction is limited to only menu navigation.

In addition, we have not received any warnings about the application development.
However, that does not mean it will stay that way forever! We are at the mercy of HoYoverse.

#### What Languages does Inventory Kamera Modern support?

Inventory Kamera Modern only supports _**English**_ currently. This limitation is due to only having a trained dataset available for the English language. The in-game "Game Language" option must be set to English. Scanning efficiency is not guaranteed for other languages. More languages may be implemented in the future but there are no current plans.

## Credits

Inventory Kamera Modern builds on [Inventory Kamera](https://github.com/Andrewthe13th/Inventory_Kamera), created by Andrewthe13th and later maintained by [taiwenlee](https://github.com/taiwenlee/Inventory_Kamera). Thanks to everyone who contributed to it:

[amklinv](https://github.com/amklinv), [Andrewthe13th](https://github.com/Andrewthe13th), [b4lesh](https://github.com/b4lesh), [baconeko](https://github.com/baconeko), [blowin](https://github.com/blowin), [BlueSpaceCanary](https://github.com/BlueSpaceCanary), [BrandonXLF](https://github.com/BrandonXLF), [Cupcak3](https://github.com/Cupcak3), [doxxx](https://github.com/doxxx), [FichteFoll](https://github.com/FichteFoll), [hastarin](https://github.com/hastarin), [man90es](https://github.com/man90es), [Marchewcia](https://github.com/Marchewcia), [mineminemine](https://github.com/mineminemine), [nguyentvan7](https://github.com/nguyentvan7), [nikinack](https://github.com/nikinack), [Nikolas-VIII](https://github.com/Nikolas-VIII), [paranoid945](https://github.com/paranoid945), [pconKd](https://github.com/pconKd), [rafsawicki](https://github.com/rafsawicki), [sagelga](https://github.com/sagelga), [taiwenlee](https://github.com/taiwenlee), [TitaniumLexa](https://github.com/TitaniumLexa), [vivienicepig](https://github.com/vivienicepig)

## License

- This project is distributed under the [MIT license](LICENSE).
- This project uses third-party libraries and other resources that may be distributed under other licenses.

---

All rights reserved by © Cognosphere Pte. Ltd. This project is not affiliated with nor endorsed by HoYoverse. Genshin Impact™ and other properties belong to their respective owners.
