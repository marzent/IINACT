# Disclaimer

This project implements the [ACT](https://advancedcombattracker.com/)-like environment mentioned in the main readme.

It is the result of reverse engineering, analyzing dependencies of FFXIV_ACT_Plugin at runtime and decompilation output of the original `Advanced Combat Tracker.exe`.

Ideally this component is supposed to be kept to a minimum and only represent the used API surface by OverlayPlugin and FFXIV_ACT_Plugin. If you can find anything in here that is not strictly needed, any PRs to reimplement or remove parts are greatly appreciated.

I believe this is fine due to being mainly an API surface with almost all calculations re-implemented in LINQ (also see [Google vs Oracle](https://en.wikipedia.org/wiki/Google_LLC_v._Oracle_America,_Inc.)). Under most jurisdictions reverse engineering of software is permissible, especially for compatibility purposes.

There is no copyright claim being made on the public ACT API here or on any of the implementations and behaviour of it.
