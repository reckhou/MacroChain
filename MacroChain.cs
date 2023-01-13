using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;

namespace MacroChain {
    public sealed unsafe class MacroChain : IDalamudPlugin {

        [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; } = null!;

        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static ChatGui Chat { get; private set; } = null!;

        public string Name => "Macro Chain-Ori";

        private delegate void MacroCallDelegate(RaptureShellModule* raptureShellModule, RaptureMacroModule.Macro* macro);

        private Hook<MacroCallDelegate> macroCallHook;

        ConfigUI configUI;
        public static Config config;

        public MacroChain(DalamudPluginInterface pi) {
            DalamudApi.Initialize(this, pi);
            configUI = new ConfigUI();
            macroCallHook = Hook<MacroCallDelegate>.FromAddress(new IntPtr(RaptureShellModule.MemberFunctionPointers.ExecuteMacro), MacroCallDetour);
            macroCallHook?.Enable();

            CommandManager.AddHandler("/nextmacro", new Dalamud.Game.Command.CommandInfo(OnMacroCommandHandler) {
                HelpMessage = "Executes the next macro.",
                ShowInHelp = true
            });
            CommandManager.AddHandler("/runmacro", new Dalamud.Game.Command.CommandInfo(OnRunMacroCommand) {
                HelpMessage = "Execute a macro (Not usable inside macros). - /runmacro ## [individual|shared].",
                ShowInHelp = true
            });

            Framework.Update += FrameworkUpdate;
            Framework.Update += LoadConfig;
            Chat.ChatMessage += ChatCommand.OnChatMessage;

            PluginInterface.UiBuilder.Draw += OnDraw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        }

        private void LoadConfig(Framework framework)
        {
            if (DalamudApi.ClientState != null && DalamudApi.ClientState.IsLoggedIn && DalamudApi.ClientState.LocalPlayer != null)
            {
                config = Config.Load();
                Framework.Update -= LoadConfig;
                PluginLog.LogWarning("Config.Load()! " + config.watchChannel.ToString());
            }
        }

        public void Dispose() {
            CommandManager.RemoveHandler("/nextmacro");
            CommandManager.RemoveHandler("/runmacro");
            CommandManager.RemoveHandler("/mchain");
            macroCallHook?.Disable();
            macroCallHook?.Dispose();
            macroCallHook = null;
            Framework.Update -= FrameworkUpdate;
            Framework.Update -= LoadConfig;
            Chat.ChatMessage -= ChatCommand.OnChatMessage;
            PluginInterface.UiBuilder.Draw -= OnDraw;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        }

        public void OnDraw()
        {
            configUI.Draw();
        }

        public void OpenConfigUi()
        {
            configUI.Enabled = !configUI.Enabled;
        }

        private RaptureMacroModule.Macro* lastExecutedMacro = null;
        private RaptureMacroModule.Macro* nextMacro = null;
        private RaptureMacroModule.Macro* downMacro = null;
        private readonly Stopwatch paddingStopwatch = new Stopwatch();

        private void MacroCallDetour(RaptureShellModule* raptureShellModule, RaptureMacroModule.Macro* macro) {
            macroCallHook?.Original(raptureShellModule, macro);
            if (RaptureShellModule.Instance->MacroLocked) return;
            lastExecutedMacro = macro;
            nextMacro = null;
            downMacro = null;
            if (lastExecutedMacro == RaptureMacroModule.Instance->Individual[99] || lastExecutedMacro == RaptureMacroModule.Instance->Shared[99]) {
                return;
            }

            nextMacro = macro + 1;
            for (var i = 90; i < 100; i++) {
                if (lastExecutedMacro == RaptureMacroModule.Instance->Individual[i] || lastExecutedMacro == RaptureMacroModule.Instance->Shared[i]) {
                    return;
                }
            }

            downMacro = macro + 10;
        }
        
        public void OnMacroCommandHandler(string command, string args) {
            try {
                if (lastExecutedMacro == null) {
                    Chat.PrintError("No macro is running.");
                    return;
                }

                if (args.ToLower() == "down") {
                    if (downMacro != null) {
                        RaptureShellModule.Instance->MacroLocked = false;
                        RaptureShellModule.Instance->ExecuteMacro(downMacro);
                    } else
                        Chat.PrintError("Can't use `/nextmacro down` on macro 90+");
                } else {
                    if (nextMacro != null) {
                        RaptureShellModule.Instance->MacroLocked = false;
                        RaptureShellModule.Instance->ExecuteMacro(nextMacro);
                    } else
                        Chat.PrintError("Can't use `/nextmacro` on macro 99.");
                }
                RaptureShellModule.Instance->MacroLocked = false;
                
            } catch (Exception ex) {
                PluginLog.LogError(ex.ToString());
            }
        }

        public void FrameworkUpdate(Framework framework) {
            if (lastExecutedMacro == null) return;
            if (ClientState == null) return;
            if (!ClientState.IsLoggedIn) {
                lastExecutedMacro = null;
                paddingStopwatch.Stop();
                paddingStopwatch.Reset();
                return;
            }
            if (RaptureShellModule.Instance->MacroCurrentLine >= 0) {
                paddingStopwatch.Restart();
                return;
            }

            if (paddingStopwatch.ElapsedMilliseconds > 2000) {
                lastExecutedMacro = null;
                paddingStopwatch.Stop();
                paddingStopwatch.Reset();
            }
        }

        public void OnRunMacroCommand(string command, string args) {
            try {
                //if (lastExecutedMacro != null) {
                //    Chat.PrintError("/runmacro is not usable while macros are running. Please use /nextmacro");
                //    return;
                //}
                var argSplit = args.Split(' ');
                var num = byte.Parse(argSplit[0]);

                if (num > 99) {
                    Chat.PrintError("Invalid Macro number.\nShould be 0 - 99");
                    return;
                }

                var shared = false;
                foreach (var arg in argSplit.Skip(1)) {
                    switch (arg.ToLower()) {
                        case "shared":
                        case "share":
                        case "s": {
                            shared = true;
                            break;
                        }
                        case "individual":
                        case "i": {
                            shared = false;
                            break;
                        }
                    }
                }
                RaptureShellModule.Instance->ExecuteMacro((shared ? RaptureMacroModule.Instance->Shared : RaptureMacroModule.Instance->Individual)[num]);
            } catch (Exception ex) {
                PluginLog.LogError(ex.ToString());
            }
        }


        [Command("/mchain")]
        [HelpMessage("toggle MidiBard window\n" +
                     "/mchain [channel name] → to watch specified channel. e.g. /mchain LS1 -> Watch link shell 1")]
        public void Command(string command, string args) => OnCommand(command, args);

        async Task OnCommand(string command, string args)
        {
            var argStrings = args.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            PluginLog.Debug($"command: {command}, {string.Join('|', argStrings)}");
            if (argStrings.Any())
            {
                switch (argStrings[0])
                {
                    case "party":
                        config.watchChannel = Config.eWatchChannel.Party;
                        break;
                    case "ls1":
                        config.watchChannel = Config.eWatchChannel.LS1;
                        break;
                    case "ls2":
                        config.watchChannel = Config.eWatchChannel.LS2;
                        break;
                    case "ls3":
                        config.watchChannel = Config.eWatchChannel.LS3;
                        break;
                    case "ls4":
                        config.watchChannel = Config.eWatchChannel.LS4;
                        break;
                    case "ls5":
                        config.watchChannel = Config.eWatchChannel.LS5;
                        break;
                    case "ls6":
                        config.watchChannel = Config.eWatchChannel.LS6;
                        break;
                    case "ls7":
                        config.watchChannel = Config.eWatchChannel.LS7;
                        break;
                    case "ls8":
                        config.watchChannel = Config.eWatchChannel.LS8;
                        break;
                    case "cwls1":
                        config.watchChannel = Config.eWatchChannel.CWLS1;
                        break;
                    case "cwls2":
                        config.watchChannel = Config.eWatchChannel.CWLS2;
                        break;
                    case "cwls3":
                        config.watchChannel = Config.eWatchChannel.CWLS3;
                        break;
                    case "cwls4":
                        config.watchChannel = Config.eWatchChannel.CWLS4;
                        break;
                    case "cwls5":
                        config.watchChannel = Config.eWatchChannel.CWLS5;
                        break;
                    case "cwls6":
                        config.watchChannel = Config.eWatchChannel.CWLS6;
                        break;
                    case "cwls7":
                        config.watchChannel = Config.eWatchChannel.CWLS7;
                        break;
                    case "cwls8":
                        config.watchChannel = Config.eWatchChannel.CWLS8;
                        break;

                    default:
                        break;

                }

                config.Save();
            }
            else
            {
                configUI.Enabled = !configUI.Enabled;
            }
        }
    }
}
