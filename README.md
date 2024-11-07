# SRC-ERS Patcher

Synthesis patcher for [Skyrim Realistic Conquering](https://www.nexusmods.com/skyrimspecialedition/mods/26396), specifically the [exclude from radiant system](https://www.nexusmods.com/skyrimspecialedition/mods/41881) add-on. The patcher forwards the formlist condition that ERS adds to radiant quests to ensure subsequent quests are not assigned to a cleared location. This patcher is intended to allow SRC to work alongside quest mods such as [At Your Own Pace](https://www.nexusmods.com/skyrimspecialedition/mods/52704) and [Quests are in Skyrim](https://www.nexusmods.com/skyrimspecialedition/mods/18416), which also edit radiant quests, without having to make complex combination patches manually for all affected quests.

## Usage
- Add to your Synthesis pipeline using the patcher browser
- In addition to the base game, the patcher will automatically detect and patch the following radiant quest mods:
  - Missives
  - Notice Board
  - Bounty Hunter
- If you have multiple Synthesis groups, run this patcher in the same group as other patchers that also modify quests to ensure changes are merged properly.
- The patcher will log which quests and aliases it forwarded the condition to. These can be viewed in the Synthesis log files or in the UI itself.


## Reporting Bugs/Issues
Please include the following to help me help you:
- Synthesis log file(s)
- `Plugins.txt`
- Specific record(s) that are problematic
  - xEdit screenshots not required, but appreciated


## Examples:
**Note** that xEdit tends to show conflicts with lists like conditions, even if they contain the same items. What's important is that the condition is present in the winning record as marked in the screenshots. In a future update I might add logic to try and place the condition in the same spot in the list, but there's no guarantee that will remove the visual conflicts in xedit.

<details>
  <summary> Base game</summary>

  ![Dawnguard Tweaks and Enhancements](/examples/example1.jpg)
  ![At Your Own Pace](/examples/example2.jpg)
</details>

<details>
  <summary>Radiant quest mods</summary>

  ![Missives](/examples/missives.jpg)
  ![Notice Board](/examples/noticeboard.jpg)
  ![BountyHunter](/examples/bountyhunter.jpg)
</details>
