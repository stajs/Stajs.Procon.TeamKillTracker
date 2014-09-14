using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PRoCon.Core;
using PRoCon.Core.Plugin;

namespace PRoConEvents
{
	public class TeamKillTracker : PRoConPluginAPI, IPRoConPluginInterface
	{
		public const string Author = "stajs";
		public const string Version = "0.2.7";

		private const int PunishWindowMin = 20;
		private const int PunishWindowMax = 120;
		private const int PunishLimitMin = 1;
		private const int PunishLimitMax = 20;

		private struct VariableGroup
		{
			public const string Commands = "Commands|";
			public const string Messages = "Messages|";
			public const string Limits = "Limits|";
			public const string Protection = "Protection|";
		}

		private struct VariableName
		{
			public const string PunishCommand = "Punish";
			public const string ForgiveCommand = "Forgive";
			public const string TeamKillMessage = "Team kill";
			public const string KickCountdownMessage = "Kick countdown";
			public const string KickImminentMessage = "Kick imminent";
			public const string VictimPromptMessage = "Victim prompt";
			public const string ShowVictimStats = "Show victim stats?";
			public const string NoOneToPunishMessage = "No one to punish";
			public const string NoOneToForgiveMessage = "No one to forgive";
			public const string PunishedMessage = "Punished";
			public const string ForgivenMessage = "Forgiven";
			public const string PunishWindow = "Punish window (seconds)";
			public const string HasPunishLimit = "Kick after punish limit reached?";
			public const string PunishLimit = "Punish limit";
			public const string Protected = "Who should be protected?";
			public const string Whitelist = "Whitelist";
		}

		private static readonly Dictionary<string, object> Defaults = new Dictionary<string, object>
		{
			{ VariableName.PunishCommand, "!p"},
			{ VariableName.ForgiveCommand, "!f"},
			{ VariableName.TeamKillMessage, "{killer} TEAM KILLED {victim}. Watch your fire dum-dum!"},
			{ VariableName.KickCountdownMessage, "{killer} will be kicked after {punishesLeft} punishes."},
			{ VariableName.KickImminentMessage, "{killer} will be kicked on next punish!"},
			{ VariableName.VictimPromptMessage, "!p to punish, !f to forgive."},
			{ VariableName.ShowVictimStats, enumBoolYesNo.Yes},
			{ VariableName.NoOneToPunishMessage, "No one to punish (auto-forgive after {window} seconds)."},
			{ VariableName.NoOneToForgiveMessage, "No one to forgive (auto-forgive after {window} seconds)."},
			{ VariableName.PunishedMessage, "Punished {killer}."},
			{ VariableName.ForgivenMessage, "Forgiven {killer}."},
			{ VariableName.PunishWindow, TimeSpan.FromSeconds(45)},
			{ VariableName.HasPunishLimit, enumBoolYesNo.Yes},
			{ VariableName.PunishLimit, 5},
			{ VariableName.Protected, Protect.Admins},
			{ VariableName.Whitelist, new string[] {}},
		};

		private string _punishCommand = Defaults[VariableName.PunishCommand].ToString();
		private string _forgiveCommand = Defaults[VariableName.ForgiveCommand].ToString();
		private string _teamKillMessage = Defaults[VariableName.TeamKillMessage].ToString();
		private string _kickCountdownMessage = Defaults[VariableName.KickCountdownMessage].ToString();
		private string _kickImminentMessage = Defaults[VariableName.KickImminentMessage].ToString();
		private string _victimPromptMessage = Defaults[VariableName.VictimPromptMessage].ToString();
		private enumBoolYesNo _showVictimStats = (enumBoolYesNo)Defaults[VariableName.ShowVictimStats];
		private string _noOneToPunishMessage = Defaults[VariableName.NoOneToPunishMessage].ToString();
		private string _noOneToForgiveMessage = Defaults[VariableName.NoOneToForgiveMessage].ToString();
		private string _punishedMessage = Defaults[VariableName.PunishedMessage].ToString();
		private string _forgivenMessage = Defaults[VariableName.ForgivenMessage].ToString();
		private TimeSpan _punishWindow = (TimeSpan)Defaults[VariableName.PunishWindow];
		private enumBoolYesNo _hasPunishLimit = (enumBoolYesNo)Defaults[VariableName.HasPunishLimit];
		private int _punishLimit = (int)Defaults[VariableName.PunishLimit];
		private Protect _protect = (Protect)Defaults[VariableName.Protected];
		private string[] _whitelist = (string[])Defaults[VariableName.Whitelist];

		private List<TeamKill> _teamKills = new List<TeamKill>();
		private List<TeamKiller> _kickedPlayers = new List<TeamKiller>();

		private enum TeamKillStatus
		{
			Pending,
			Punished,
			Forgiven,
			AutoForgiven
		}

		private class TeamKill
		{
			public string KillerName { get; set; }
			public string VictimName { get; set; }
			public DateTime At { get; set; }
			public TeamKillStatus Status { get; set; }
		}

		private enum TeamKillerStatus
		{
			Survived,
			Kicked
		}

		private class TeamKiller
		{
			public string Name { get; set; }
			public int TeamKillCount { get; set; }
			public TeamKillerStatus Status { get; set; }
		}

		private enum Protect
		{
			NoOne,
			Admins,
			Whitelist,
			AdminsAndWhitelist
		}

		#region IPRoConPluginInterface

		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
		{
			var events = new[]
			{
				"OnLevelLoaded",
				"OnLoadingLevel",
				"OnLevelStarted",
				"OnRoundOver",
				"OnPlayerKilled",
				"OnGlobalChat",
				"OnTeamChat",
				"OnSquadChat"
			};

			this.RegisterEvents(this.GetType().Name, events);
		}

		public void OnPluginEnable()
		{
			_teamKills = new List<TeamKill>();
			WriteConsole("^2Enabled.^0");
		}

		public void OnPluginDisable()
		{
			WriteConsole("^8Disabled.^0");
		}

		public string GetPluginName()
		{
			return "Team Kill Tracker";
		}

		public string GetPluginVersion()
		{
			return Version;
		}

		public string GetPluginAuthor()
		{
			return Author;
		}

		public string GetPluginWebsite()
		{
			return "battlelog.battlefield.com/bf4/soldier/stajs/stats/904562646/pc/";
		}

		public string GetPluginDescription()
		{
			return GetDescriptionHtml();
		}

		public List<CPluginVariable> GetDisplayPluginVariables()
		{
			return new List<CPluginVariable>
			{
				new CPluginVariable(VariableGroup.Commands + VariableName.PunishCommand, typeof(string), _punishCommand),
				new CPluginVariable(VariableGroup.Commands + VariableName.ForgiveCommand, typeof(string), _forgiveCommand),
				new CPluginVariable(VariableGroup.Messages + VariableName.TeamKillMessage, typeof(string), _teamKillMessage),
				new CPluginVariable(VariableGroup.Messages + VariableName.KickCountdownMessage, typeof(string), _kickCountdownMessage),
				new CPluginVariable(VariableGroup.Messages + VariableName.KickImminentMessage, typeof(string), _kickImminentMessage),
				new CPluginVariable(VariableGroup.Messages + VariableName.VictimPromptMessage, typeof(string), _victimPromptMessage),
				new CPluginVariable(VariableGroup.Messages + VariableName.ShowVictimStats, typeof(enumBoolYesNo), _showVictimStats),
				new CPluginVariable(VariableGroup.Messages + VariableName.PunishedMessage, typeof(string), _punishedMessage),
				new CPluginVariable(VariableGroup.Messages + VariableName.ForgivenMessage, typeof(string), _forgivenMessage),
				new CPluginVariable(VariableGroup.Messages + VariableName.NoOneToPunishMessage, typeof(string), _noOneToPunishMessage),
				new CPluginVariable(VariableGroup.Messages + VariableName.NoOneToForgiveMessage, typeof(string), _noOneToForgiveMessage),
				new CPluginVariable(VariableGroup.Limits + VariableName.PunishWindow, typeof(int), _punishWindow.TotalSeconds),
				new CPluginVariable(VariableGroup.Limits + VariableName.HasPunishLimit, typeof(enumBoolYesNo), _hasPunishLimit),
				new CPluginVariable(VariableGroup.Limits + VariableName.PunishLimit, typeof(int), _punishLimit),
				new CPluginVariable(VariableGroup.Protection + VariableName.Protected, CreateEnumString(typeof(Protect)), _protect.ToString()),
				new CPluginVariable(VariableGroup.Protection + VariableName.Whitelist, typeof(string[]), _whitelist.Select(s => s = CPluginVariable.Decode(s)).ToArray())
			};
		}

		public List<CPluginVariable> GetPluginVariables()
		{
			return new List<CPluginVariable>
			{
				new CPluginVariable(VariableName.PunishCommand, typeof(string), _punishCommand),
				new CPluginVariable(VariableName.ForgiveCommand, typeof(string), _forgiveCommand),
				new CPluginVariable(VariableName.TeamKillMessage, typeof(string), _teamKillMessage),
				new CPluginVariable(VariableName.KickCountdownMessage, typeof(string), _kickCountdownMessage),
				new CPluginVariable(VariableName.KickImminentMessage, typeof(string), _kickImminentMessage),
				new CPluginVariable(VariableName.VictimPromptMessage, typeof(string), _victimPromptMessage),
				new CPluginVariable(VariableName.ShowVictimStats, typeof(enumBoolYesNo), _showVictimStats),
				new CPluginVariable(VariableName.PunishedMessage, typeof(string), _punishedMessage),
				new CPluginVariable(VariableName.ForgivenMessage, typeof(string), _forgivenMessage),
				new CPluginVariable(VariableName.NoOneToPunishMessage, typeof(string), _noOneToPunishMessage),
				new CPluginVariable(VariableName.NoOneToForgiveMessage, typeof(string), _noOneToForgiveMessage),
				new CPluginVariable(VariableName.PunishWindow, typeof(int), _punishWindow.TotalSeconds),
				new CPluginVariable(VariableName.HasPunishLimit, typeof(enumBoolYesNo), _hasPunishLimit),
				new CPluginVariable(VariableName.PunishLimit, typeof(int), _punishLimit),
				new CPluginVariable(VariableName.Protected, CreateEnumString(typeof(Protect)), _protect.ToString()),
				new CPluginVariable(VariableName.Whitelist, typeof(string[]), _whitelist.ToArray())
			};
		}

		public void SetPluginVariable(string variable, string value)
		{
			int i;

			switch (variable)
			{
				case VariableName.TeamKillMessage:
					_teamKillMessage = value;
					break;

				case VariableName.KickCountdownMessage:
					_kickCountdownMessage = value;
					break;

				case VariableName.KickImminentMessage:
					_kickImminentMessage = value;
					break;

				case VariableName.ShowVictimStats:
					_showVictimStats = value == "Yes" ? enumBoolYesNo.Yes : enumBoolYesNo.No;
					break;

				case VariableName.PunishCommand:
					_punishCommand = value;
					break;

				case VariableName.ForgiveCommand:
					_forgiveCommand = value;
					break;

				case VariableName.VictimPromptMessage:
					_victimPromptMessage = value;
					break;

				case VariableName.NoOneToPunishMessage:
					_noOneToPunishMessage = value;
					break;

				case VariableName.NoOneToForgiveMessage:
					_noOneToForgiveMessage = value;
					break;

				case VariableName.PunishedMessage:
					_punishedMessage = value;
					break;

				case VariableName.ForgivenMessage:
					_forgivenMessage = value;
					break;

				case VariableName.PunishWindow:

					if (!int.TryParse(value, out i))
						return;

					if (i < PunishWindowMin)
						i = PunishWindowMin;

					if (i > PunishWindowMax)
						i = PunishWindowMax;

					_punishWindow = TimeSpan.FromSeconds(i);

					break;

				case VariableName.HasPunishLimit:
					_hasPunishLimit = value == "Yes" ? enumBoolYesNo.Yes : enumBoolYesNo.No;
					break;

				case VariableName.PunishLimit:

					if (!int.TryParse(value, out i))
						return;

					if (i < PunishLimitMin)
						i = PunishLimitMin;

					if (i > PunishLimitMax)
						i = PunishLimitMax;

					_punishLimit = i;

					break;

				case VariableName.Protected:
					_protect = (Protect)Enum.Parse(typeof(Protect), value);
					break;

				case VariableName.Whitelist:
					_whitelist = value.Split(new [] { "|" }, StringSplitOptions.RemoveEmptyEntries);
					break;
			}
		}

		#endregion

		#region PRoConPluginAPI

		public override void OnGlobalChat(string speaker, string message)
		{
			OnChat(speaker, message);
		}

		public override void OnTeamChat(string speaker, string message, int teamId)
		{
			OnChat(speaker, message);
		}

		public override void OnSquadChat(string speaker, string message, int teamId, int squadId)
		{
			OnChat(speaker, message);
		}

		public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
		{
			WriteConsole("OnLoadingLevel " + mapFileName);
		}

		public override void OnLevelLoaded(string mapFileName, string gamemode, int roundsPlayed, int roundsTotal)
		{
			WriteConsole("OnLevelLoaded " + mapFileName + ", " + gamemode);
			_teamKills = new List<TeamKill>();
		}

		// TODO: Confirm that this is round start.
		public override void OnLevelStarted()
		{
			WriteConsole("OnLevelStarted");
		}

		public override void OnRoundOver(int winningTeamId)
		{
			ShameAll();
		}

		public override void OnPlayerKilled(Kill kill)
		{
			OnTeamKill(kill);
		}

		#endregion

		private void AutoForgive(string killer, string victim)
		{
			_teamKills
				.Where(tk => tk.KillerName == killer && tk.VictimName == victim && tk.Status == TeamKillStatus.Pending)
				.ToList()
				.ForEach(tk => tk.Status = TeamKillStatus.AutoForgiven);
		}

		private int GetPunishesLeftBeforeKick(string player)
		{
			if (_hasPunishLimit == enumBoolYesNo.No)
				return 9999;

			var totalPunishCount = GetAllTeamKillsByPlayer(player).Count(tk => tk.Status == TeamKillStatus.Punished);
			var punishesLeft = _punishLimit - totalPunishCount;

			return punishesLeft < 1 ? 1 : punishesLeft;
		}

		private string GetTeamKillMessage(string killer, string victim)
		{
			var ret = _teamKillMessage
				.Replace("{killer}", killer)
				.Replace("{victim}", victim);

			return ReplaceStaches(ret);
		}

		private List<TeamKill> GetAllTeamKillsByPlayer(string player)
		{
			return _teamKills
				.Where(tk => tk.KillerName == player)
				.ToList();
		}

		private string GetVictimStatsMessage(string killer, string victim)
		{
			var allKillsByKiller = GetAllTeamKillsByPlayer(killer);

			var victimKillsByKiller = allKillsByKiller
				.Where(tk => tk.VictimName == victim)
				.ToList();

			var punishedCount = victimKillsByKiller.Count(tk => tk.Status == TeamKillStatus.Punished);
			var forgivenCount = victimKillsByKiller.Count(tk => tk.Status == TeamKillStatus.Forgiven);
			var autoForgivenCount = victimKillsByKiller.Count(tk => tk.Status == TeamKillStatus.AutoForgiven);

			var sb = new StringBuilder()
				.AppendFormat("TK's by {0}: you ({1}) team ({2}).", killer, victimKillsByKiller.Count, allKillsByKiller.Count);

			if (victimKillsByKiller.Count > 1)
			{
				sb.Append(" Previously you have:");

				if (punishedCount > 0)
					sb.AppendFormat(" punished ({0})", punishedCount);

				if (forgivenCount > 0)
					sb.AppendFormat(" forgiven ({0})", forgivenCount);

				if (autoForgivenCount > 0)
					sb.AppendFormat(" auto-forgiven ({0})", autoForgivenCount);

				sb.Append(".");
			}

			return sb.ToString();
		}

		private string GetKickWarningMessage(string killer)
		{
			var punishesLeft = GetPunishesLeftBeforeKick(killer);

			string ret;

			if (punishesLeft == 1)
				ret = _kickImminentMessage
					.Replace("{killer}", killer);
			else
				ret = _kickCountdownMessage
					.Replace("{killer}", killer)
					.Replace("{punishesLeft}", punishesLeft.ToString());

			return ReplaceStaches(ret);
		}

		private void OnTeamKill(Kill kill)
		{
			if (kill == null || kill.Victim == null || kill.Killer == null)
				return;

			if (kill.IsSuicide)
				return;

			var victimName = kill.Victim.SoldierName;
			var killerName = kill.Killer.SoldierName;

			if (string.IsNullOrEmpty(victimName) || string.IsNullOrEmpty(killerName))
				return;

			var isTeamKill = kill.Killer.TeamID == kill.Victim.TeamID;

			if (!isTeamKill)
				return;

			// Auto-forgive any previous pending TKs for this killer and victim - there should only be one pending
			// at a time for this combination, and it's about to be added.
			AutoForgive(killerName, victimName);

			_teamKills.Add(new TeamKill
			{
				KillerName = killerName,
				VictimName = victimName,
				At = DateTime.UtcNow,
				Status = TeamKillStatus.Pending
			});

			var teamKillMessage = GetTeamKillMessage(killerName, victimName);

			AdminSayPlayer(killerName, teamKillMessage);
			AdminSayPlayer(victimName, teamKillMessage);

			if (_hasPunishLimit == enumBoolYesNo.Yes)
			{
				var warningMessage = GetKickWarningMessage(killerName);

				AdminSayPlayer(killerName, warningMessage);
				AdminSayPlayer(victimName, warningMessage);
			}

			if (_showVictimStats == enumBoolYesNo.Yes)
				AdminSayPlayer(victimName, GetVictimStatsMessage(killerName, victimName));

			AdminSayPlayer(victimName, _victimPromptMessage);
		}

		private void OnChat(string player, string message)
		{
			if (message == "!shame")
				AdminSayPlayer(player, GetWorstTeamKillerMessage());

			if (message == (_punishCommand))
				PunishKillerOf(player);

			if (message == (_forgiveCommand))
				ForgiveKillerOf(player);
		}

		private void AutoForgivePastPunishWindow()
		{
			var punishWindowStart = DateTime.UtcNow.Add(_punishWindow.Negate());

			_teamKills
				.Where(tk => tk.Status == TeamKillStatus.Pending && tk.At < punishWindowStart)
				.ToList()
				.ForEach(tk => tk.Status = TeamKillStatus.AutoForgiven);
		}

		private List<TeamKill> GetPendingTeamKillsForVictim(string victim)
		{
			return _teamKills
				.Where(tk => tk.Status == TeamKillStatus.Pending && tk.VictimName == victim)
				.ToList();
		}

		private void PunishKillerOf(string victim)
		{
			AutoForgivePastPunishWindow();

			var kills = GetPendingTeamKillsForVictim(victim);

			if (!kills.Any())
				AdminSayPlayer(victim, ReplaceStaches(_noOneToPunishMessage.Replace("{window}", _punishWindow.TotalSeconds.ToString())));

			if (kills.Count > 1)
				WriteConsole("Players found to punish: " + kills.Count);

			foreach (var kill in kills)
				Punish(kill);
		}

		private void ForgiveKillerOf(string victim)
		{
			AutoForgivePastPunishWindow();

			var kills = GetPendingTeamKillsForVictim(victim);

			if (!kills.Any())
				AdminSayPlayer(victim, ReplaceStaches(_noOneToForgiveMessage.Replace("{window}", _punishWindow.TotalSeconds.ToString())));

			if (kills.Count > 1)
				WriteConsole("Players found to forgive: " + kills.Count);

			foreach (var kill in kills)
				Forgive(kill);
		}

		// TODO: figure out how to get a list of admins rather than check on demand.
		private bool IsAdmin(string player)
		{
			var privileges = GetAccountPrivileges(player);

			if (privileges == null)
				return false;

			return privileges.CanKillPlayers;
		}

		private bool IsWhitelisted(string player)
		{
			return _whitelist.Any(p => p == player);
		}

		private bool IsProtected(string player)
		{
			if (player == Author)
				return true;

			switch (_protect)
			{
				case Protect.Admins:
					return IsAdmin(player);

				case Protect.Whitelist:
					return IsWhitelisted(player);

				case Protect.AdminsAndWhitelist:
					return IsAdmin(player) || IsWhitelisted(player);

				default: // No one is protected.
					return false;
			}
		}

		private void Kick(string player)
		{
			_kickedPlayers.Add(new TeamKiller
			{
				Name = player,
				TeamKillCount = _teamKills.Count(tk => tk.KillerName == player),
				Status = TeamKillerStatus.Kicked
			});

			// Re-set their team kills in case they re-join.
			_teamKills.RemoveAll(tk => tk.KillerName == player);

			AdminSayAll("Too many team kills for " + player + ". Boot incoming!");

			if (IsProtected(player))
			{
				AdminSayPlayer(player, "Protected from kick.");
				return;
			}

			ExecuteCommand("procon.protected.tasks.add", "TeamKillTracker", "20", "1", "1", "procon.protected.send", "admin.kickPlayer", player, "Kicked for reaching team kill limit.");
		}

		private void Punish(TeamKill kill)
		{
			var killer = kill.KillerName;

			var shouldKick =
				_hasPunishLimit == enumBoolYesNo.Yes		// Limit is active.
				&& GetPunishesLeftBeforeKick(killer) == 1;	// This is their last chance.

			if (shouldKick)
			{
				Kick(killer);
				return;
			}

			var message = ReplaceStaches(_punishedMessage.Replace("{killer}", killer));

			AdminSayPlayer(killer, message);
			AdminSayPlayer(kill.VictimName, message);

			kill.Status = TeamKillStatus.Punished;

			if (IsProtected(killer))
				AdminSayPlayer(killer, "Protected from kill.");
			else
				ExecuteCommand("procon.protected.send", "admin.killPlayer", killer);
		}

		private void Forgive(TeamKill kill)
		{
			var message = ReplaceStaches(_forgivenMessage.Replace("{killer}", kill.KillerName));

			AdminSayPlayer(kill.KillerName, message);
			AdminSayPlayer(kill.VictimName, message);

			kill.Status = TeamKillStatus.Forgiven;
		}

		private string GetWorstTeamKillerMessage()
		{
			var worstTeamKillers = _teamKills
				 .GroupBy(tk => tk.KillerName)
				 .Select(g => new TeamKiller
				 {
					 Name = g.Key,
					 TeamKillCount = g.Count(),
					 Status = TeamKillerStatus.Survived
				 })
				 .OrderByDescending(tk => tk.TeamKillCount)
				 .Take(3)
				 .ToList();

			worstTeamKillers
				 .AddRange(_kickedPlayers);

			worstTeamKillers = worstTeamKillers
				 .OrderByDescending(tk => tk.TeamKillCount)
				 .Take(3)
				 .ToList();

			if (!worstTeamKillers.Any())
				return "Wow! We got through a round without a single teamkill!";

			var sb = new StringBuilder();

			for (int i = 0; i < worstTeamKillers.Count; i++)
			{
				var killer = worstTeamKillers[i];

				sb.AppendFormat("{0} ({1}){2}{3}",
					  killer.Name,
					  killer.TeamKillCount,
					  killer.Status == TeamKillerStatus.Kicked ? " kicked" : string.Empty,
					  i + 1 < worstTeamKillers.Count ? ", " : ".");
			}

			return "Worst team killers: " + sb;
		}

		private void ShameAll()
		{
			AdminSayAll(GetWorstTeamKillerMessage());
		}

		private string ReplaceStaches(string s)
		{
			return s.Replace("{", "~(").Replace("}", ")~");
		}

		private void WriteConsole(string message)
		{
			message = ReplaceStaches(message);
			ExecuteCommand("procon.protected.pluginconsole.write", "Team Kill Tracker: " + message);
		}

		private void AdminSayAll(string message)
		{
			message = ReplaceStaches(message);
			ExecuteCommand("procon.protected.send", "admin.say", message, "all");
			ExecuteCommand("procon.protected.chat.write", "(AdminSayAll) " + message);
		}

		private void AdminSayPlayer(string player, string message)
		{
			message = ReplaceStaches(message);
			ExecuteCommand("procon.protected.send", "admin.say", message, "player", player);
			ExecuteCommand("procon.protected.chat.write", "(AdminSayPlayer " + player + ") " + message);
		}

		public string GetDescriptionHtml()
		{
			return @"

<style type=""text/css"">
	p { font-size: 1em; }
	p.default-value { color: #5e5e5e; }
	table th { text-transform: none; }
	h2 { margin-top: 1.1em; }
	h2.group { color: #111; font-size: 1.6em; margin-top: 1.6em; margin-bottom: 0; }
	h3 { font-size: 1.4em; font-family: Vera, Helvetica, Georgia; margin-top: 1.6em; font-weight: 400; padding-bottom: 6px; border-bottom: 1px solid #dcdcdb; }
	h4 { color: #555; font-size: 1em; margin-top: 0; }
</style>

<h2>Description</h2>
<p>Track team kill statistics and allow victims to punish their killers.</p>

<h2>Plugin Settings</h2>

<h2 class=""group"">Commands</h2>

<h3>" + VariableName.PunishCommand + @"</h3>
<p>The command to punish a team killer. This can be issued in global, team, or squad chat.</p>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.PunishCommand] + @"</p>

<h3>" + VariableName.ForgiveCommand + @"</h3>
<p>The command to forgive a team killer. This can be issued in global, team, or squad chat.</p>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.ForgiveCommand] + @"</p>

<h2 class=""group"">Messages</h2>

<h3>" + VariableName.TeamKillMessage + @"</h3>
<p>Sent to both the killer and victim when a team kill is detected.</p>

<h4>Available substitutions</h4>
<table>
<tr>
	<th>Placeholder</th>
	<th>Description</th>
</tr>
<tr>
	<td><strong>{killer}</strong></td>
	<td>Player name of killer.</td>
</tr>
<tr>
	<td><strong>{victim}</strong></td>
	<td>Player name of victim.</td>
</tr>
</table>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.TeamKillMessage] + @"</p>

<h3>" + VariableName.KickCountdownMessage + @"</h3>
<p>Sent to the killer and victim when a team kill is detected, if:</p>
<ol>
	<li><em>" + VariableName.HasPunishLimit + @"</em> is set to <em>Yes</em>.</li>
	<li>The killer is more than one punish away from the limit.</li>
</ol>

<h4>Available substitutions</h4>
<table>
<tr>
	<th>Placeholder</th>
	<th>Description</th>
</tr>
<tr>
	<td><strong>{killer}</strong></td>
	<td>Player name of killer.</td>
</tr>
<tr>
	<td><strong>{punishesLeft}</strong></td>
	<td>The number of punishes left before the killer is kicked.</td>
</tr>
</table>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.KickCountdownMessage] + @"</p>

<h3>" + VariableName.KickImminentMessage + @"</h3>
<p>Sent to the killer and victim when a team kill is detected, if:</p>
<ol>
	<li><em>" + VariableName.HasPunishLimit + @"</em> is set to <em>Yes</em>.</li>
	<li>The killer is one punish away from the limit.</li>
</ol>

<h4>Available substitutions</h4>
<table>
<tr>
	<th>Placeholder</th>
	<th>Description</th>
</tr>
<tr>
	<td><strong>{killer}</strong></td>
	<td>Player name of killer.</td>
</tr>
</table>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.KickImminentMessage] + @"</p>

<h3>" + VariableName.VictimPromptMessage + @"</h3>
<p>Sent to the victim when a team kill is detected.</p>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.VictimPromptMessage] + @"</p>

<h3>" + VariableName.ShowVictimStats + @"</h3>
<p>Show statistics with the victim prompt. This includes the number of times the:</p>
<ul>
	<li>killer has killed the victim.</li>
	<li>killer has killed a team member.</li>
	<li>victim has punished or forgiven the killer.</li>
	<li>killer has been auto-forgiven for a team kill on this victim.</li>
</ul>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.ShowVictimStats] + @"</p>

<h3>" + VariableName.PunishedMessage + @"</h3>
<p>Sent to both the killer and victim when a <em>punish</em> command is successful.</p>

<h4>Available substitutions</h4>
<table>
<tr>
	<th>Placeholder</th>
	<th>Description</th>
</tr>
<tr>
	<td><strong>{killer}</strong></td>
	<td>Player name of killer.</td>
</tr>
</table>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.PunishedMessage] + @"</p>

<h3>" + VariableName.ForgivenMessage + @"</h3>
<p>Sent to both the killer and victim when a <em>forgive</em> command is successful.</p>

<h4>Available substitutions</h4>
<table>
<tr>
	<th>Placeholder</th>
	<th>Description</th>
</tr>
<tr>
	<td><strong>{killer}</strong></td>
	<td>Player name of killer.</td>
</tr>
</table>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.ForgivenMessage] + @"</p>

<h3>" + VariableName.NoOneToPunishMessage + @"</h3>
<p>Sent to the player who issued a <em>punish</em> command that was unsuccessful.</p>

<h4>Available substitutions</h4>
<table>
<tr>
	<th>Placeholder</th>
	<th>Description</th>
</tr>
<tr>
	<td><strong>{window}</strong></td>
	<td>Length (in seconds) of the punish window.</td>
</tr>
</table>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.NoOneToPunishMessage] + @"</p>

<h3>" + VariableName.NoOneToForgiveMessage + @"</h3>
<p>Sent to the player who issued a <em>forgive</em> command that was unsuccessful.</p>

<h4>Available substitutions</h4>
<table>
<tr>
	<th>Placeholder</th>
	<th>Description</th>
</tr>
<tr>
	<td><strong>{window}</strong></td>
	<td>Length (in seconds) of the punish window.</td>
</tr>
</table>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.NoOneToForgiveMessage] + @"</p>

<h2 class=""group"">Limits</h2>

<h3>" + VariableName.PunishWindow + @"</h3>
<p>How long (in seconds) to allow a victim to punish or forgive before the killer is auto-forgiven.</p>

<h4>Range</h4>
<table>
<tr>
	<th>Minimum</th>
	<td>" + PunishWindowMin + @"</td>
</tr>
<tr>
	<th>Maximum</th>
	<td>" + PunishWindowMax + @"</td>
</tr>
</table>

<h4>Default value</h4>
<p class=""default-value"">" + ((TimeSpan)Defaults[VariableName.PunishWindow]).TotalSeconds + @"</p>

<h3>" + VariableName.HasPunishLimit + @"</h3>
<p>Should a team killer be kicked after reaching the <em>punish limit</em>?</p>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.HasPunishLimit] + @"</p>

<h3>" + VariableName.PunishLimit + @"</h3>
<p>How many times a killer is allowed to be punished before being kicked.</p>

<h4>Range</h4>
<table>
<tr>
	<th>Minimum</th>
	<td>" + PunishLimitMin + @"</td>
</tr>
<tr>
	<th>Maximum</th>
	<td>" + PunishLimitMax + @"</td>
</tr>
</table>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.PunishLimit] + @"</p>

<h2 class=""group"">Protection</h2>

<h3>" + VariableName.Protected + @"</h3>
<p>Who, if any, should be protected from punishment.</p>

<h4>Default value</h4>
<p class=""default-value"">" + Defaults[VariableName.Protected] + @"</p>

<h3>" + VariableName.Whitelist + @"</h3>
<p>A list of players (one per line) that are protected from punishment.</p>";
		}
	}
}