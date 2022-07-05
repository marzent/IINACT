# IINACT

A minimalistic tool to run the [FFXIV_ACT_Plugin](https://github.com/ravahn/FFXIV_ACT_Plugin) in an ACT-like enviroment with a stripped-down port of Overlay Plugin with Cactbot event sources.

This will **not** render overlays by itself, use something like [Browsingway](https://github.com/Styr1x/Browsingway), [Next UI](https://github.com/kaminaris/Next-UI), [hudkit](https://github.com/valarnin/hudkit) (Linux only) or [Bunny HUD](https://github.com/marzent/Bunny-HUD) (macOS only) to display Overlays.


## Why?

- ACT is too clunky IMHO for just wanting to have the game data parsed and served via a Web Socket server
- Drastically more efficent than ACT, in part to .NET 6.0, in part to a more sane log line processing (disk I/O is not blocking LogLineEvents and happening on a separate lower priority thread)
- Due to the above and Machina optimizations CPU usage will be orders of magnitude (no understatement) lower when running under Wine
- Will generally stay out of your way, doesn't take away focus when launched, only lives as a tray icon and quits itself once the game closes
- Doesn't use legacy technology that hurts Linux and macOS users
- More robust Web Socket server than what Overlay Plugin currently uses
- RPCAP support

## How to build?

Just place the FFXIV_ACT_Plugin.dll and its SDK files in the external_dependencies folder and build the IINACT project.


## Known Issues

- Doesn't have any sort of auto-update system
- Cactbot may work *with issues* (the configuration is currently not saved for sure)
- Not all parse settings exposed in the GUI are properly implemented yet
