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

## Install

0. Download and extract the latest release: https://github.com/stajs/Stajs.Procon.TeamKillTracker/releases.
1. Move the plugin file `TeamKillTracker.cs` to your Procon layer under `Plugins\BF4` and restart your layer.
2. Turn off the built-in team killing management:

> ![image](https://cloud.githubusercontent.com/assets/2253814/4515372/77fea896-4bb9-11e4-872d-bd9f818e129b.png)

### Compatibility

This plugin has only been tested against BF4. It is reported to work with [BF3](https://forum.myrcon.com/showthread.php?8690-Team-Kill-Tracker&p=109517&viewfull=1#post109517) and _may_ work for other games.

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
|Description|If this is set to `Yes`, the `Sorry` command is enabled.|
|Default&nbsp;value|`No`|

|Sorry|&nbsp;|
|:--|:---|
|Description|If `Allow killers to apologize to avoid punishment?` is set to `Yes` a killer can avoid punishment with this command.|
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

|Trace level|&nbsp;|
|:--|:---|
|Description|Determines what messages (if any) should appear in the Procon `Chat` tab.|
|Default&nbsp;value|`SayAndYell`|

### Limits

|Kick after punish limit reached?|&nbsp;|
|:--|:---|
|Description|Should a team killer be kicked after reaching the punish limit?|
|Default&nbsp;value|`Yes`|

|Player count threshold for kick|&nbsp;|
|:--|:---|
|Description|How many players are required before kicking is active. The method used to count the players is updated every 30 seconds and includes players joining, but not yet visible in game.<br />Minimum: 1<br />Maximum: 64|
|Default&nbsp;value|`1`|

|Punish limit|&nbsp;|
|:--|:---|
|Description|How many times a killer is allowed to be punished before being kicked.<br />Minimum: 1<br />Maximum: 20|
|Default&nbsp;value|`5`|

|Punish window (seconds)|&nbsp;|
|:--|:---|
|Description|How long (in seconds) to allow a victim to punish or forgive before the killer is auto-forgiven.<br />Minimum: 20<br />Maximum: 120|
|Default&nbsp;value|`45`|

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
|`{punishesLeft}`|The number of punishes left before the killer is kicked.|

|Victim|&nbsp;|
|:--|:---|
|Description|Sent to the victim when a team kill is detected (one line per message). See note about prefixes above.|
|Default&nbsp;value|- `TEAM KILLED by {killer}.`<br />- `@TEAM KILLED by {killer}.`<br />- `Their TK's: you ({victimCount}) team ({teamCount}).`<br />- `You have: punished ({punishedCount}) forgiven ({forgivenCount}) auto-forgiven ({autoForgivenCount}).`<br />- `>Punishes left before kick: {punishesLeft}.`<br />- `<Waiting on more players to join before enabling kick.`<br />- `!p to punish, !f to forgive.`<br />- `@!p to punish, !f to forgive.`|
|`{killer}`|Player name of killer.|
|`{victim}`|Player name of victim.|
|`{victimCount}`|The number of times the killer has team killed the victim.|
|`{teamCount}`|The number of times the killer has team killed the team.|
|`{punishedCount}`|The number of times the victim has previously punished the killer.|
|`{forgivenCount}`|The number of times the victim has previously forgiven the killer.|
|`{autoForgivenCount}`|The number of times the victim has previously auto-forgiven the killer.|
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

|No one to punish|&nbsp;|
|:--|:---|
|Description|Sent to the player who issued a punish command that was unsuccessful.|
|Default&nbsp;value|`No one to punish (auto-forgive after {window} seconds).`|
|`{window}`|Length (in seconds) of the punish window.|

|No one to forgive|&nbsp;|
|:--|:---|
|Description|Sent to the player who issued a forgive command that was unsuccessful.|
|Default&nbsp;value|`No one to forgive (auto-forgive after {window} seconds).`|
|`{window}`|Length (in seconds) of the punish window.|

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
|Description|Who, if any, should be protected from punishment.|
|Default&nbsp;value|`Admins`|

|Whitelist|&nbsp;|
|:--|:---|
|Description|A list of players (one per line) that are protected from punishment.|
