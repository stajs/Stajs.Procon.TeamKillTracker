# Stajs.Procon.TeamKillTracker

## Description

Track team kill statistics and allow self-management of team killers.

## Features

- Allows victims to forgive or punish (slay) their killers.
- Optional kick for team killers after reaching a punish limit.
- Configurable commands and messages.
- Optionally allow killers to apologize to avoid punishment.
- Optionally protect admins or whitelisted players from being punished.
- Shame the worst team killers at the end of the round or on demand.
- Integration with [AdKats](https://github.com/AdKats/AdKats).

## Install

0. Download and extract the latest release: https://github.com/stajs/Stajs.Procon.TeamKillTracker/releases.
1. Move the plugin file `TeamKillTracker.cs` to your Procon layer under `Plugins\BF4` and restart your layer.
2. Turn off the built-in team killing management:

> ![image](https://cloud.githubusercontent.com/assets/2253814/4515372/77fea896-4bb9-11e4-872d-bd9f818e129b.png)

### Compatibility

This plugin has only been tested against BF4. It is reported to work with [BF3](https://forum.myrcon.com/showthread.php?8690-Team-Kill-Tracker&p=109517&viewfull=1#post109517) and [Hardline](https://forum.myrcon.com/showthread.php?8690-Team-Kill-Tracker-(3-5-0)-12-Dec-2014&p=119082&viewfull=1#post119082) and _may_ work for other games.

## Settings

### Commands

Commands are case-insensitive and can be issued in global, team, or squad chat. A command can be triggered by more than one matching player message.

|Punish|&nbsp;|
|:--|:---|
|Description|Punish a team killer.|
|Default&nbsp;value|- `!p`<br />- `!punish`|

|Forgive|&nbsp;|
|:--|:---|
|Description|Forgive a team killer.|
|Default&nbsp;value|- `!f`<br />- `!forgive`|

|Allow killers to apologize to avoid punishment?|&nbsp;|
|:--|:---|
|Description|If set to `Yes`, the `Sorry` command is enabled.|
|Default&nbsp;value|`No`|

|Sorry|&nbsp;|
|:--|:---|
|Description|Allows a killer to avoid punishment.|
|Default&nbsp;value|- `!sorry`<br />- `!mybad`|

|Shame|&nbsp;|
|:--|:---|
|Description|List the worst team killers.|
|Default&nbsp;value|`!shame`|

### Debug

|Should suicide count as a team kill?|&nbsp;|
|:--|:---|
|Description|Useful for testing other plugin settings and messages. Allows a player to trigger a "team kill" by suiciding.|
|Default&nbsp;value|`No`|

|Output to Chat|&nbsp;|
|:--|:---|
|Description|Determines what messages (if any) should appear in the Procon `Chat` tab.|
|Default&nbsp;value|`SayAndYell`|

### Limits

|Victim window before auto-action (seconds)|&nbsp;|
|:--|:---|
|Description|How long (in seconds) to allow a victim to apply an action to their killer before an auto-action is applied. The method used to check for punish actions is updated every ~30 seconds so the actual window may be ~30 seconds longer than specified.<br />Minimum: 20<br />Maximum: 120|
|Default&nbsp;value|`45`|

|Auto-action|&nbsp;|
|:--|:---|
|Description|What should happen to the killer after the victim action window expires.|
|Default&nbsp;value|`Forgiven`|

|Kick after punish limit reached?|&nbsp;|
|:--|:---|
|Description|Should a team killer be kicked after reaching the punish limit? If set to `Yes`, the following settings are enabled:<br />- `Player count threshold for kick`<br />- `Punish limit`|
|Default&nbsp;value|`Yes`|

|Player count threshold for kick|&nbsp;|
|:--|:---|
|Description|How many players are required before kicking is active. The method used to count the players is updated every ~30 seconds and includes players joining, but not yet visible in game.<br />Minimum: 1<br />Maximum: 64|
|Default&nbsp;value|`1`|

|Punish limit|&nbsp;|
|:--|:---|
|Description|How many times a killer is allowed to be punished before being kicked.<br />Minimum: 1<br />Maximum: 20|
|Default&nbsp;value|`5`|

### Messages

For `Killer` and `Victim` messages, the following prefixes may be used.

- `>` to say the message if a threshold is required for kick and the threshold has been reached.
- `<` to say the message if a threshold is required for kick and the threshold has not been reached.
- `@` to yell the message.
- `>@` to yell the message if a threshold is required for kick and the threshold has been reached.
- `<@` to yell the message if a threshold is required for kick and the threshold has not been reached.

|Killer|&nbsp;|
|:--|:---|
|Description|Sent to the killer when a team kill is detected (one line per message). See note about prefixes above.|
|Default&nbsp;value|- `You TEAM KILLED {victim}. Watch your fire!`<br />- `@You TEAM KILLED {victim}. Watch your fire!`<br />- `>Punishes left before kick: {punishesLeft}.`|
|`{killer}`|Player name of killer.|
|`{victim}`|Player name of victim.|
|`{victimCount}`|The number of times the killer has team killed the victim.|
|`{teamCount}`|The number of times the killer has team killed the team.|
|`{punishedCount}`|The number of times the victim has previously punished the killer.|
|`{forgivenCount}`|The number of times the victim has previously forgiven the killer.|
|`{autoForgivenCount}`|The number of times the victim has previously auto-forgiven the killer.|
|`{sorryCount}`|The number of times the killer has apologized to the victim.|
|`{punishesLeft}`|The number of punishes left before the killer is kicked.|

|Victim|&nbsp;|
|:--|:---|
|Description|Sent to the victim when a team kill is detected (one line per message). See note about prefixes above.|
|Default&nbsp;value|- `TEAM KILLED by {killer}.`<br />- `@TEAM KILLED by {killer}.`<br />- `Their TK's: you ({victimCount}) team ({teamCount}).`<br />- `punished ({punishedCount}) forgiven ({forgivenCount}) auto-forgiven ({autoForgivenCount}) sorry ({sorryCount})`<br />- `>Punishes left before kick: {punishesLeft}.`<br />- `<Waiting on more players to join before enabling kick.`<br />- `!p to punish, !f to forgive.`<br />- `@!p to punish, !f to forgive.`|
|`{killer}`|Player name of killer.|
|`{victim}`|Player name of victim.|
|`{victimCount}`|The number of times the killer has team killed the victim.|
|`{teamCount}`|The number of times the killer has team killed the team.|
|`{punishedCount}`|The number of times the victim has previously punished the killer.|
|`{forgivenCount}`|The number of times the victim has previously forgiven the killer.|
|`{autoForgivenCount}`|The number of times the victim has previously auto-forgiven the killer.|
|`{sorryCount}`|The number of times the killer has apologized to the victim.|
|`{punishesLeft}`|The number of punishes left before the killer is kicked.|

|Punished|&nbsp;|
|:--|:---|
|Description|Sent to both the killer and victim when a punish command is successful.|
|Default&nbsp;value|`{killer} punished by {victim}.`|
|`{killer}`|Player name of killer.|
|`{victim}`|Player name of victim.|

|Forgiven|&nbsp;|
|:--|:---|
|Description|Sent to both the killer and victim when a forgive command is successful.|
|Default&nbsp;value|`{killer} forgiven by {victim}.`|
|`{killer}`|Player name of killer.|
|`{victim}`|Player name of victim.|

|Apologized|&nbsp;|
|:--|:---|
|Description|Sent to both the killer and victim when a sorry command is successful.|
|Default&nbsp;value|`{killer} apologized to {victim}.`|
|`{killer}`|Player name of killer.|
|`{victim}`|Player name of victim.|

|Kick|&nbsp;|
|:--|:---|
|Description|Sent to all players when a player is kicked for reaching the punish limit.|
|Default&nbsp;value|`Too many team kills for {killer}. Boot incoming!`|
|`{killer}`|Player name of killer.|

|No one to punish|&nbsp;|
|:--|:---|
|Description|Sent to the player who issued a punish command that was unsuccessful.|
|Default&nbsp;value|`No one to punish ({window} second window).`|
|`{window}`|Length (in seconds) of the punish window.|

|No one to forgive|&nbsp;|
|:--|:---|
|Description|Sent to the player who issued a forgive command that was unsuccessful.|
|Default&nbsp;value|`No one to forgive ({window} second window).`|
|`{window}`|Length (in seconds) of the punish window.|

|No one to apologize to|&nbsp;|
|:--|:---|
|Description|Sent to the player who issued a sorry command that was unsuccessful.|
|Default&nbsp;value|`Apology rejected! No recent kills.`|

|Shame all on round end?|&nbsp;|
|:--|:---|
|Description|Should a list of the worst team killers be displayed to all players at the end of the round?|
|Default&nbsp;value|`Yes`|

|No one to shame on round end message|&nbsp;|
|:--|:---|
|Description|Displayed to all players if `Shame all on round end?` is `Yes`, but no team kills are recorded.|
|Default&nbsp;value|`Wow! We got through a round without a single teamkill!`|

|No one to shame message|&nbsp;|
|:--|:---|
|Description|Sent to the player who issued a shame command.|
|Default&nbsp;value|`No team kills so far...`|

### Protection

|Who should be protected?|&nbsp;|
|:--|:---|
|Description|Who, if any, should be protected from punishment. If set to `Whitelist` or `AdminsAndWhitelist`, `Whitelist` is enabled.|
|Default&nbsp;value|`Admins`|

|Whitelist|&nbsp;|
|:--|:---|
|Description|A list of players (one per line) that are protected from punishment.|

|Use AdKats|&nbsp;|
|:--|:---|
|Description|Should punishment be handed off to AdKats to handle. This (obviously) requires a working installation of [AdKats](https://github.com/AdKats/AdKats) and only hands off to AdKats once it is fully initialized and ready to receive commands.|
|Default&nbsp;value|`No`|
