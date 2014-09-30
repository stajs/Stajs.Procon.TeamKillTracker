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
		public const string Version = "2.3.0";

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
			public const string Shame = "Shame|";
		}

		private struct VariableName
		{
			public const string PunishCommand = "Punish";
			public const string ForgiveCommand = "Forgive";
			public const string ShameCommand = "Shame";
			public const string KillerMessages = "Killer";
			public const string VictimMessages = "Victim";
			public const string NoOneToPunishMessage = "No one to punish";
			public const string NoOneToForgiveMessage = "No one to forgive";
			public const string PunishedMessage = "Punished";
			public const string ForgivenMessage = "Forgiven";
			public const string PunishWindow = "Punish window (seconds)";
			public const string HasPunishLimit = "Kick after punish limit reached?";
			public const string PunishLimit = "Punish limit";
			public const string Protected = "Who should be protected?";
			public const string Whitelist = "Whitelist";
			public const string ShameAllOnRoundEnd = "Shame all on round end?";
		}

		private class Stats
		{
			public int TeamKillsOnTeamCount { get; set; }
			public int TeamKillsOnVictimCount { get; set; }
			public int VictimPunishedKillerCount { get; set; }
			public int VictimForgivenKillerCount { get; set; }
			public int VictimAutoForgivenKillerCount { get; set; }
		}

		private static readonly Dictionary<string, object> Defaults = new Dictionary<string, object>
		{
			{ VariableName.PunishCommand, "!p"},
			{ VariableName.ForgiveCommand, "!f"},
			{ VariableName.ShameCommand, "!shame"},
			{
				VariableName.KillerMessages, new []
				{
					"You TEAM KILLED {victim}.",
					"@You TEAM KILLED {victim}.",
					"Watch your fire! Punishes left before kick: {punishesLeft}."
				}
			},
			{
				VariableName.VictimMessages, new []
				{
					"TEAM KILLED by {killer}.",
					"@TEAM KILLED by {killer}.",
					"Their TK's: you ({victimCount}) team ({teamCount}).",
					"You have: punished ({punishedCount}) forgiven ({forgivenCount}) auto-forgiven ({autoForgivenCount}).",
					"Punishes left before kick: {punishesLeft}.",
					"!p to punish, !f to forgive.",
					"@!p to punish, !f to forgive."
				}
			},
			{ VariableName.NoOneToPunishMessage, "No one to punish (auto-forgive after {window} seconds)."},
			{ VariableName.NoOneToForgiveMessage, "No one to forgive (auto-forgive after {window} seconds)."},
			{ VariableName.PunishedMessage, "{killer} punished by {victim}."},
			{ VariableName.ForgivenMessage, "{killer} forgiven by {victim}."},
			{ VariableName.PunishWindow, TimeSpan.FromSeconds(45)},
			{ VariableName.HasPunishLimit, enumBoolYesNo.Yes},
			{ VariableName.PunishLimit, 5},
			{ VariableName.Protected, Protect.Admins},
			{ VariableName.Whitelist, new string[] {}},
			{ VariableName.ShameAllOnRoundEnd, enumBoolYesNo.Yes}
		};

		private string _punishCommand = Defaults[VariableName.PunishCommand].ToString();
		private string _forgiveCommand = Defaults[VariableName.ForgiveCommand].ToString();
		private string _shameCommand = Defaults[VariableName.ShameCommand].ToString();
		private string[] _killerMessages = (string[])Defaults[VariableName.KillerMessages];
		private string[] _victimMessages = (string[])Defaults[VariableName.VictimMessages];
		private string _noOneToPunishMessage = Defaults[VariableName.NoOneToPunishMessage].ToString();
		private string _noOneToForgiveMessage = Defaults[VariableName.NoOneToForgiveMessage].ToString();
		private string _punishedMessage = Defaults[VariableName.PunishedMessage].ToString();
		private string _forgivenMessage = Defaults[VariableName.ForgivenMessage].ToString();
		private TimeSpan _punishWindow = (TimeSpan)Defaults[VariableName.PunishWindow];
		private enumBoolYesNo _hasPunishLimit = (enumBoolYesNo)Defaults[VariableName.HasPunishLimit];
		private int _punishLimit = (int)Defaults[VariableName.PunishLimit];
		private Protect _protect = (Protect)Defaults[VariableName.Protected];
		private string[] _whitelist = (string[])Defaults[VariableName.Whitelist];
		private enumBoolYesNo _shameAllOnRoundEnd = (enumBoolYesNo)Defaults[VariableName.ShameAllOnRoundEnd];

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
				new CPluginVariable(VariableGroup.Commands + VariableName.ShameCommand, typeof(string), _shameCommand),
				new CPluginVariable(VariableGroup.Messages + VariableName.KillerMessages, typeof(string[]), _killerMessages.Select(s => s = CPluginVariable.Decode(s)).ToArray()),
				new CPluginVariable(VariableGroup.Messages + VariableName.VictimMessages, typeof(string[]), _victimMessages.Select(s => s = CPluginVariable.Decode(s)).ToArray()),
				new CPluginVariable(VariableGroup.Messages + VariableName.PunishedMessage, typeof(string), _punishedMessage),
				new CPluginVariable(VariableGroup.Messages + VariableName.ForgivenMessage, typeof(string), _forgivenMessage),
				new CPluginVariable(VariableGroup.Messages + VariableName.NoOneToPunishMessage, typeof(string), _noOneToPunishMessage),
				new CPluginVariable(VariableGroup.Messages + VariableName.NoOneToForgiveMessage, typeof(string), _noOneToForgiveMessage),
				new CPluginVariable(VariableGroup.Limits + VariableName.PunishWindow, typeof(int), _punishWindow.TotalSeconds),
				new CPluginVariable(VariableGroup.Limits + VariableName.HasPunishLimit, typeof(enumBoolYesNo), _hasPunishLimit),
				new CPluginVariable(VariableGroup.Limits + VariableName.PunishLimit, typeof(int), _punishLimit),
				new CPluginVariable(VariableGroup.Protection + VariableName.Protected, CreateEnumString(typeof(Protect)), _protect.ToString()),
				new CPluginVariable(VariableGroup.Protection + VariableName.Whitelist, typeof(string[]), _whitelist.Select(s => s = CPluginVariable.Decode(s)).ToArray()),
				new CPluginVariable(VariableGroup.Shame + VariableName.ShameAllOnRoundEnd, typeof(enumBoolYesNo), _shameAllOnRoundEnd)
			};
		}

		public List<CPluginVariable> GetPluginVariables()
		{
			return new List<CPluginVariable>
			{
				new CPluginVariable(VariableName.PunishCommand, typeof(string), _punishCommand),
				new CPluginVariable(VariableName.ForgiveCommand, typeof(string), _forgiveCommand),
				new CPluginVariable(VariableName.ShameCommand, typeof(string), _shameCommand),
				new CPluginVariable(VariableName.KillerMessages, typeof(string[]), _killerMessages.Select(s => s = CPluginVariable.Decode(s)).ToArray()),
				new CPluginVariable(VariableName.VictimMessages, typeof(string[]), _victimMessages.Select(s => s = CPluginVariable.Decode(s)).ToArray()),
				new CPluginVariable(VariableName.PunishedMessage, typeof(string), _punishedMessage),
				new CPluginVariable(VariableName.ForgivenMessage, typeof(string), _forgivenMessage),
				new CPluginVariable(VariableName.NoOneToPunishMessage, typeof(string), _noOneToPunishMessage),
				new CPluginVariable(VariableName.NoOneToForgiveMessage, typeof(string), _noOneToForgiveMessage),
				new CPluginVariable(VariableName.PunishWindow, typeof(int), _punishWindow.TotalSeconds),
				new CPluginVariable(VariableName.HasPunishLimit, typeof(enumBoolYesNo), _hasPunishLimit),
				new CPluginVariable(VariableName.PunishLimit, typeof(int), _punishLimit),
				new CPluginVariable(VariableName.Protected, CreateEnumString(typeof(Protect)), _protect.ToString()),
				new CPluginVariable(VariableName.Whitelist, typeof(string[]), _whitelist.Select(s => s = CPluginVariable.Decode(s)).ToArray()),
				new CPluginVariable(VariableName.ShameAllOnRoundEnd, typeof(enumBoolYesNo), _shameAllOnRoundEnd)
			};
		}

		public void SetPluginVariable(string variable, string value)
		{
			int i;

			switch (variable)
			{
				case VariableName.KillerMessages:
					_killerMessages = value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
					break;

				case VariableName.VictimMessages:
					_victimMessages = value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
					break;

				case VariableName.PunishCommand:
					_punishCommand = value;
					break;

				case VariableName.ForgiveCommand:
					_forgiveCommand = value;
					break;

				case VariableName.ShameCommand:
					_shameCommand = value;
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

				case VariableName.ShameAllOnRoundEnd:
					_shameAllOnRoundEnd = value == "Yes" ? enumBoolYesNo.Yes : enumBoolYesNo.No;
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
					_whitelist = value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
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

		private List<TeamKill> GetAllTeamKillsByPlayer(string player)
		{
			return _teamKills
				.Where(tk => tk.KillerName == player)
				.ToList();
		}

		private Stats GetStats(string killer, string victim)
		{
			var allKillsByKiller = GetAllTeamKillsByPlayer(killer);

			var victimKillsByKiller = allKillsByKiller
				.Where(tk => tk.VictimName == victim)
				.ToList();

			return new Stats
			{
				TeamKillsOnTeamCount = allKillsByKiller.Count(),
				TeamKillsOnVictimCount = victimKillsByKiller.Count(),
				VictimPunishedKillerCount = victimKillsByKiller.Count(tk => tk.Status == TeamKillStatus.Punished),
				VictimForgivenKillerCount = victimKillsByKiller.Count(tk => tk.Status == TeamKillStatus.Forgiven),
				VictimAutoForgivenKillerCount = victimKillsByKiller.Count(tk => tk.Status == TeamKillStatus.AutoForgiven)
			};
		}

		private void Notify(string to, string[] messages, string killer, string victim)
		{
			const int yellInterval = 2;
			const int yellDuration = 10;

			var yellCount = 0;

			var stats = GetStats(killer, victim);

			foreach (var message in messages)
			{
				var m = CPluginVariable.Decode(message);

				var shouldYell = m.StartsWith("@");

				if (shouldYell)
					m = m.Substring(1).Trim();

				if (string.IsNullOrEmpty(m))
					continue;

				m = m
					.Replace("{killer}", killer)
					.Replace("{victim}", victim)
					.Replace("{victimCount}", stats.TeamKillsOnVictimCount.ToString())
					.Replace("{teamCount}", stats.TeamKillsOnTeamCount.ToString())
					.Replace("{punishedCount}", stats.VictimPunishedKillerCount.ToString())
					.Replace("{forgivenCount}", stats.VictimForgivenKillerCount.ToString())
					.Replace("{autoForgivenCount}", stats.VictimAutoForgivenKillerCount.ToString())
					.Replace("{punishesLeft}", GetPunishesLeftBeforeKick(killer).ToString());

				if (shouldYell)
				{
					var delay = (yellInterval + yellDuration) * yellCount++;
					AdminYellPlayer(to, m, delay, yellDuration);
				}
				else
				{
					AdminSayPlayer(to, m);
				}
			}
		}

		private void NotifyKiller(string killer, string victim)
		{
			Notify(killer, _killerMessages, killer, victim);
		}

		private void NotifyVictim(string killer, string victim)
		{
			Notify(victim, _victimMessages, killer, victim);
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

			NotifyKiller(killerName, victimName);
			NotifyVictim(killerName, victimName);
		}

		private void OnChat(string player, string message)
		{
			if (message == _shameCommand)
				ShamePlayer(player);

			if (message == _punishCommand)
				PunishKillerOf(player);

			if (message == _forgiveCommand)
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
			var victim = kill.VictimName;

			var shouldKick =
				_hasPunishLimit == enumBoolYesNo.Yes		// Limit is active.
				&& GetPunishesLeftBeforeKick(killer) == 1;	// This is their last chance.

			if (shouldKick)
			{
				Kick(killer);
				return;
			}

			var message = _punishedMessage
				.Replace("{killer}", killer)
				.Replace("{victim}", victim);

			AdminSayPlayer(killer, message);
			AdminSayPlayer(victim, message);

			kill.Status = TeamKillStatus.Punished;

			if (IsProtected(killer))
				AdminSayPlayer(killer, "Protected from kill.");
			else
				ExecuteCommand("procon.protected.send", "admin.killPlayer", killer);
		}

		private void Forgive(TeamKill kill)
		{
			var killer = kill.KillerName;
			var victim = kill.VictimName;

			var message = _forgivenMessage
				.Replace("{killer}", killer)
				.Replace("{victim}", victim);

			AdminSayPlayer(killer, message);
			AdminSayPlayer(victim, message);

			kill.Status = TeamKillStatus.Forgiven;
		}

		private List<TeamKiller> GetWorstTeamKillers()
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

			return worstTeamKillers;
		}

		private string GetWorstTeamKillersMessage(List<TeamKiller> killers)
		{
			var sb = new StringBuilder();

			for (int i = 0; i < killers.Count; i++)
			{
				var killer = killers[i];

				sb.AppendFormat("{0} ({1}){2}{3}",
					  killer.Name,
					  killer.TeamKillCount,
					  killer.Status == TeamKillerStatus.Kicked ? " kicked" : string.Empty,
					  i + 1 < killers.Count ? ", " : ".");
			}

			return "Worst team killers: " + sb;
		}

		private void ShameAll()
		{
			if (_shameAllOnRoundEnd == enumBoolYesNo.No)
				return;

			var killers = GetWorstTeamKillers();

			var message = killers.Any()
				? GetWorstTeamKillersMessage(killers)
				: "Wow! We got through a round without a single teamkill!";

			AdminSayAll(message);
		}

		private void ShamePlayer(string player)
		{
			var killers = GetWorstTeamKillers();

			var message = killers.Any()
				? GetWorstTeamKillersMessage(killers)
				: "Amazing. No team kills so far...";

			AdminSayPlayer(player, message);

			if (killers.Any(tk => tk.Name == player))
				return;

			var playerTeamKills = GetAllTeamKillsByPlayer(player);

			if (!playerTeamKills.Any())
				return;

			AdminSayPlayer(player, "Your team kills: " + playerTeamKills.Count + ".");
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

		private void AdminYellPlayer(string player, string message, int delay, int duration)
		{
			message = ReplaceStaches(message);
			ExecuteCommand("procon.protected.tasks.add", "TeamKillTracker", delay.ToString(), "1", "1", "procon.protected.send", "admin.yell", message, duration.ToString(), "player", player);
			ExecuteCommand("procon.protected.tasks.add", "TeamKillTracker", delay.ToString(), "1", "1", "procon.protected.chat.write", "(AdminYellPlayer " + player + ") " + message);

		}

		public string GetDescriptionHtml()
		{
			return @"
<h2>Description</h2>
<p>Track team kill statistics and allow victims to punish their killers.</p>
<p>See project site for full documentation: <a href=""https://github.com/stajs/Stajs.Procon.TeamKillTracker"">https://github.com/stajs/Stajs.Procon.TeamKillTracker</a></p>
";
		}
	}
}