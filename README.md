## About Mod

An SPT mod that adds a custom in-game map viewer in the place of the BSG map screen. Includes pre-configured maps made by [TarkovData](https://github.com/TarkovTracker/tarkovdata/) and [TarkovDev](https://github.com/the-hideout/tarkov-dev).

#### Full screen out of raid maps with selector
![Out of Raid Map with selector](Screenshots/out_of_raid_map.png)

#### Full screen in-raid with player marker
![In raid with player marker](Screenshots/in_raid_map.png)

#### Dynamic Locks
![Dynamic Locks](Screenshots/dynamic_locks.png)

#### Quest Indicators
![Quest Indicators](Screenshots/quest_markers.png)

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

### General

- **Enabled**: If the map should replace the BSG default map screen, requires swapping away from modded map to refresh

### Dynamic Markers

- **Show Player Marker**: If the player marker should be shown in raid
- **Show Friendly Player Markers**: If friendly player markers should be shown
- **Show Enemy Player Markers**: If enemy player markers should be shown (generally for debug)
- **Show Scav Markers**: If enemy scav markers should be shown (generally for debug)
- **Show Locked Door Status**: If locked door markers should be updated with status based on key acquisition
- **Show Quests In Raid**: If quests should be shown in raid
- **Show Extracts In Raid**: If extracts should be shown in raid
- **Show Extracts Status In Raid**: If extracts should be colored according to their status in raid

### In-Raid

- **Auto Select Level**: If the level should be automatically selected based on the players position in raid
- **Auto Center On Player Marker**: If the player marker should be centered when showing the map in raid
- **Reset Zoom On Center**: If the zoom level should be reset each time that the map is opened while in raid
- **Centering On Player Zoom Level**: What zoom level should be used as while centering on the player (0 is fully zoomed out, and 1 is fully zoomed in)

## Installation

[Releases are here](https://github.com/mpstark/SPT-DynamicMaps/releases). Open zip file and drag `BepInEx` folder into root of your SPT-AKI install.

## License

Distributed under the MIT license. See [`LICENSE.txt`](LICENSE.txt) for more details.

## Acknowledgments

- [CJ](https://github.com/CJ-SPT) for letting me hack on [StashSearch](https://github.com/CJ-SPT/StashSearch) as my first SPT-AKI modding experience
- [DrakiaXYZ](https://github.com/DrakiaXYZ) for having multiple great mods to look at for examples
- [Arys](https://github.com/Nympfonic) for being awesome
- Multiple people in the SPT Discord for suggestions and encouragement
