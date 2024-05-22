## About Mod

An SPT mod that adds a custom in-game map viewer in the place of the BSG map screen. Includes pre-configured maps made by [TarkovData](https://github.com/TarkovTracker/tarkovdata/) and [TarkovDev](https://github.com/the-hideout/tarkov-dev).

![Out of Raid Map with selector](Screenshots/out_of_raid_map.png)

![In raid with player marker](Screenshots/in_raid_map.png)

### Features

- Map organized in stacked layers / levels
  - Automatic selection of layer based on player position
  - Manual control available on left of map screen
- Automatic min/max zoom based on size of map
- Support for coordinate rotation, since BSG decided to make north different direction on many of the maps
- Drag-based map pan and mousewheel-based map zoom controls
- Map labels supported for orientation
- Icon-based map markers placed both statically and dynamically. Currently:
  - Static markers for all extracts for all maps out-of-raid
  - In-raid dynamic current extracts for player
  - Statically-loaded locked door with dynamic icon and color based on key status
    - Out-of-raid, green with key means player has it in inventory, yellow with key means key in stash, red with lock otherwise
    - In-raid, green with key means palyer has the key, red with lock means player doesn't have key
  - Static markers for switches and levers
  - In-raid dynamic player icon
  - In-raid dynamic quest icons (based on [Prop's GTFO](https://github.com/dvize/GTFO))
  - Disabled in-raid bot markers (waiting for configuration and filter feature)

See [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md) for known current issues and [`FEATURE_WISHLIST.md`](FEATURE_WISHLIST.md) for a list of things that I would like to work on in the future.

## Configuration

There is no current configuration.

## Installation

[Releases are here](https://github.com/mpstark/SPT-InGameMap/releases). Open zip file and drag `BepInEx` folder into root of your SPT-AKI install.

## License

Distributed under the MIT license. See [`LICENSE.txt`](LICENSE.txt) for more details.

## Acknowledgments

- [CJ](https://github.com/CJ-SPT) for letting me hack on [StashSearch](https://github.com/CJ-SPT/StashSearch) as my first SPT-AKI modding experience
- [DrakiaXYZ](https://github.com/DrakiaXYZ) for having multiple great mods to look at for examples
- [Arys](https://github.com/Nympfonic) for being awesome
- Multiple people in the SPT Discord for suggestions and encouragement
