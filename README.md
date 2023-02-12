![icon](https://github.com/marzent/IINACT/blob/main/IINACT/Icon-IINACT-512x512@2x.ico?raw=true)

# IINACT

A minimalistic application to run the [FFXIV_ACT_Plugin](https://github.com/ravahn/FFXIV_ACT_Plugin) in an [ACT](https://advancedcombattracker.com/)-like enviroment with a stripped-down port of [Overlay Plugin](https://github.com/OverlayPlugin/OverlayPlugin) with [Cactbot](https://github.com/quisquous/cactbot) event sources.

This will **not** render overlays by itself, use something like [Browsingway](https://github.com/Styr1x/Browsingway), [Next UI](https://github.com/kaminaris/Next-UI), [hudkit](https://github.com/valarnin/hudkit) (Linux only) or [Bunny HUD](https://github.com/marzent/Bunny-HUD) (macOS only) to display Overlays.


## Why?

- ACT is too clunky IMHO for just wanting to have the game data parsed and served via a Web Socket server
- Drastically more efficent than ACT, in part to .NET 7.0, in part to a more sane log line processing (disk I/O is not blocking LogLineEvents and happening on a separate lower priority thread)
- Due to the above and Machina optimizations CPU usage will be orders of magnitude (not exaggerating here) lower when running under Wine
- Will generally stay out of your way, doesn't take away focus when launched, only lives as a tray icon and quits itself once the game closes
- Doesn't use legacy technology that hurts Linux and macOS users
- RPCAP support (for the poor Flatpak users)
- Has a nice dark UI that won't flashbang you in the middle of the night
- Follows the Unix philosophy of just doing one thing and doing it well   

## How to build?

Just clone and run `dotnet build`.

## FAQ

**Where are my logs?**

- In your Documents folder. For Windows users, `C:\Users\[user]\Documents\IINACT`. For Mac/Linux users, same thing, but relative to your wine prefix.

**Are these logs compatible with FFLogs? Can I use the FFLogs Uploader?**

- Yes! 100% compatible.


## Known Issues

- Doesn't have any sort of auto-update system
- TTS is not fully implemented yet


PRs are always welcome :)
