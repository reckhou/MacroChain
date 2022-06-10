using System;
using System.Diagnostics;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Dalamud.Logging;

namespace MacroChain
{
	public class ChatCommand
	{
		public static void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
		{
			if (isHandled)
				return;

			if (!CorrectType(type))
			{
				return;
			}

			string cmd = "/" + message;
			MacroChain.CommandManager.ProcessCommand(cmd);
		}

		static bool CorrectType(XivChatType type)
        {
			if (MacroChain.config == null)
				MacroChain.config = Config.Load();

			if (type == XivChatType.Party)
				return MacroChain.config.watchChannel == Config.eWatchChannel.Party;

			if (type == XivChatType.CrossLinkShell1)
				return MacroChain.config.watchChannel == Config.eWatchChannel.CWLS1;
			if (type == XivChatType.CrossLinkShell2)
				return MacroChain.config.watchChannel == Config.eWatchChannel.CWLS2;
			if (type == XivChatType.CrossLinkShell3)
				return MacroChain.config.watchChannel == Config.eWatchChannel.CWLS3;
			if (type == XivChatType.CrossLinkShell4)
				return MacroChain.config.watchChannel == Config.eWatchChannel.CWLS4;
			if (type == XivChatType.CrossLinkShell5)
				return MacroChain.config.watchChannel == Config.eWatchChannel.CWLS5;
			if (type == XivChatType.CrossLinkShell6)
				return MacroChain.config.watchChannel == Config.eWatchChannel.CWLS6;
			if (type == XivChatType.CrossLinkShell7)
				return MacroChain.config.watchChannel == Config.eWatchChannel.CWLS7;
			if (type == XivChatType.CrossLinkShell8)
				return MacroChain.config.watchChannel == Config.eWatchChannel.CWLS8;

			if (type == XivChatType.Ls1)
				return MacroChain.config.watchChannel == Config.eWatchChannel.LS1;
			if (type == XivChatType.Ls2)
				return MacroChain.config.watchChannel == Config.eWatchChannel.LS2;
			if (type == XivChatType.Ls3)
				return MacroChain.config.watchChannel == Config.eWatchChannel.LS3;
			if (type == XivChatType.Ls4)
				return MacroChain.config.watchChannel == Config.eWatchChannel.LS4;
			if (type == XivChatType.Ls5)
				return MacroChain.config.watchChannel == Config.eWatchChannel.LS5;
			if (type == XivChatType.Ls6)
				return MacroChain.config.watchChannel == Config.eWatchChannel.LS6;
			if (type == XivChatType.Ls7)
				return MacroChain.config.watchChannel == Config.eWatchChannel.LS7;
			if (type == XivChatType.Ls8)
				return MacroChain.config.watchChannel == Config.eWatchChannel.LS8;


			return false;
        }
	}
}