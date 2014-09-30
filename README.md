# Stajs.Procon.TeamKillTracker

## Description

Track team kill statistics and allow self-management of team killers.

## Features

- Allows victims to forgive or punish (slay) their killers.
- Optional kick for team killers after reaching a punish limit.
- Configurable commands and messages.
- Optionally protect admins or whitelisted players from being punished.
- Shame the worst team killers at the end of the round or on demand.

## Install

0. Download and extract the latest release: https://github.com/stajs/Stajs.Procon.TeamKillTracker/releases.
1. Move the plugin file `TeamKillTracker.cs` to your Procon layer under `Plugins\BF4` and restart your layer.

This plugin _may_ work for other games, but has only been tested against BF4.

## Settings

### Commands

Commands can be issued in global, team, or squad chat.

|Command|Punish|
|:--|:---|
|Description|Punish a team killer.|
|Default&nbsp;value|`!p`|

|Command|Forgive|
|:--|:---|
|Description|Forgive a team killer.|
|Default&nbsp;value|`!f`|

|Command|Shame|
|:--|:---|
|Description|List the worst team killers.|
|Default&nbsp;value|`!shame`|

### Messages

|Message|Killer|
|:--|:---|
|Description|Sent to the killer when a team kill is detected (one line per message). Prefix the line with `@` to yell.|
|Default&nbsp;value|- `You TEAM KILLED {victim}.`<br />- `@You TEAM KILLED {victim}.`<br />- `Watch your fire! Punishes left before kick: {punishesLeft}.`|
|`{killer}`|Player name of killer.|
|`{victim}`|Player name of victim.|
|`{victimCount}`|The number of times the killer has team killed the victim.|
|`{teamCount}`|The number of times the killer has team killed the team.|
|`{punishedCount}`|The number of times the victim has previously punished the killer.|
|`{forgivenCount}`|The number of times the victim has previously forgiven the killer.|
|`{autoForgivenCount}`|The number of times the victim has previously auto-forgiven the killer.|
|`{punishesLeft}`|The number of punishes left before the killer is kicked.|

|Message|Victim|
|:--|:---|
|Description|Sent to the victim when a team kill is detected (one line per message). Prefix the line with `@` to yell.|
|Default&nbsp;value|- `TEAM KILLED by {killer}.`<br />- `@TEAM KILLED by {killer}.`<br />- `Their TK's: you ({victimCount}) team ({teamCount}).`<br />- `You have: punished ({punishedCount}) forgiven ({forgivenCount}) auto-forgiven ({autoForgivenCount}).`<br />- `Punishes left before kick: {punishesLeft}.`<br />- `!p to punish, !f to forgive.`<br />- `@!p to punish, !f to forgive.`|
|`{killer}`|Player name of killer.|
|`{victim}`|Player name of victim.|
|`{victimCount}`|The number of times the killer has team killed the victim.|
|`{teamCount}`|The number of times the killer has team killed the team.|
|`{punishedCount}`|The number of times the victim has previously punished the killer.|
|`{forgivenCount}`|The number of times the victim has previously forgiven the killer.|
|`{autoForgivenCount}`|The number of times the victim has previously auto-forgiven the killer.|
|`{punishesLeft}`|The number of punishes left before the killer is kicked.|

|Message|Punished|
|:--|:---|
|Description|Sent to both the killer and victim when a punish command is successful.|
|Default&nbsp;value|`{killer} punished by {victim}.`|
|`{killer}`|Player name of killer.|
|`{victim}`|Player name of victim.|

|Message|Forgiven|
|:--|:---|
|Description|Sent to both the killer and victim when a forgive command is successful.|
|Default&nbsp;value|`{killer} forgiven by {victim}.`|
|`{killer}`|Player name of killer.|
|`{victim}`|Player name of victim.|

|Message|No one to punish|
|:--|:---|
|Description|Sent to the player who issued a punish command that was unsuccessful.|
|Default&nbsp;value|`No one to punish (auto-forgive after {window} seconds).`|
|`{window}`|Length (in seconds) of the punish window.|

|Message|No one to forgive|
|:--|:---|
|Description|Sent to the player who issued a forgive command that was unsuccessful.|
|Default&nbsp;value|`No one to forgive (auto-forgive after {window} seconds).`|
|`{window}`|Length (in seconds) of the punish window.|

### Limits

|Punish window (seconds)|&nbsp;|
|:--|:---|
|Description|How long (in seconds) to allow a victim to punish or forgive before the killer is auto-forgiven.<br />Minimum: 20<br />Maximum	120|
|Default&nbsp;value|`45`|

|Kick after punish limit reached?|&nbsp;|
|:--|:---|
|Description|Should a team killer be kicked after reaching the punish limit?|
|Default&nbsp;value|`Yes`|

|Punish limit|&nbsp;|
|:--|:---|
|Description|How many times a killer is allowed to be punished before being kicked.<br />Minimum: 1<br />Maximum	20|
|Default&nbsp;value|`5`|

### Protection

|Who should be protected?|&nbsp;|
|:--|:---|
|Description|Who, if any, should be protected from punishment.|
|Default&nbsp;value|`Admins`|

|Whitelist|&nbsp;|
|:--|:---|
|Description|A list of players (one per line) that are protected from punishment.|


### Shame

|Shame all on round end?|&nbsp;|
|:--|:---|
|Description|Should a list of the worst team killers be displayed to all players at the end of the round?|
|Default&nbsp;value|`Yes`|

|No one to shame on round end message|&nbsp;|
|:--|:---|
|Description|Displayed to all players if `Shame all on round end?` is `Yes`, but no team kills are recorded.|
|Default&nbsp;value|`Wow! We got through a round without a single teamkill!`|

|No one to shame on shame command message|&nbsp;|
|:--|:---|
|Description|Sent to the player who issued a shame command.|
|Default&nbsp;value|`Amazing. No team kills so far...`|
