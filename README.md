![icon](https://github.com/marzent/IINACT/blob/main/images/icon.ico?raw=true)

# IINACT

A Dalamud plugin to run the [FFXIV_ACT_Plugin](https://github.com/ravahn/FFXIV_ACT_Plugin) in an [ACT](https://advancedcombattracker.com/)-like enviroment with a heavily modified port of [Overlay Plugin](https://github.com/OverlayPlugin/OverlayPlugin) for modern .NET.

The data source here is only based on `Dalamud.Game.Network` and does not require any extra injection with [Deucalion](https://github.com/ff14wed/deucalion) or network capture with elevated privileges.

This will **not** render overlays by itself, use something like [Browsingway](https://github.com/Styr1x/Browsingway), [Next UI](https://github.com/kaminaris/Next-UI), [hudkit](https://github.com/valarnin/hudkit) (Linux only) or [Bunny HUD](https://github.com/marzent/Bunny-HUD) (macOS only) to display Overlays.


## Why

- ACT is too inconvenient IMHO for just wanting to have the game data parsed and served via a WebSocket server
- Drastically more efficent than ACT, in part to .NET 7.0, in part to a more sane log line processing (disk I/O is not blocking LogLineEvents and happening on a separate lower priority thread)
- Due to the above and running fully inside the game process CPU usage will be orders of magnitude (not exaggerating here) lower when running under Wine compared to network-based capture
- Uses an ultra fast and low latency WebSocket server based on [NetCoreServer](https://github.com/chronoxor/NetCoreServer)
- Doesn't use legacy technology that hurts Linux and macOS users
- Follows the Unix philosophy of just doing one thing and doing it well   

## Installing 

While this project is still a work in progress, you can use it by adding the following URL to the custom plugin repositories list in your Dalamud settings

1. `/xlsettings` -> Experimental tab
2. Copy and paste the repo.json link below
3. Click on the + button
4. Click on the "Save and Close" button
5. You will now see IINACT listed in the Available Plugins tab in the Dalamud Plugin Installer
6. Do not forget to actually install IINACT from this tab.

Please do not install IINACT manually by downloading a release zip and unpacking it into your devPlugins folder. That will require manually updating IINACT and you will miss out on features and bug fixes as you won't get update notifications automatically. Any manually installed copies of IINACT should be removed before switching to the custom plugin respository method, as they will conflict.
- https://raw.githubusercontent.com/marzent/IINACT/main/repo.json

## How to build

Just clone and run `dotnet build`.

## FAQ

**Where are my logs?**

- In your Documents folder. For Windows users, `C:\Users\[user]\Documents\IINACT`. For Mac/Linux users, same thing, but relative to your wine prefix.

**Are these logs compatible with FFLogs? Can I use the FFLogs Uploader?**

- Yes! 100% compatible.


## Known Issues

- TTS is not fully implemented yet


PRs are always welcome :)
