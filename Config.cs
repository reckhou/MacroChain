using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Configuration;
using Newtonsoft.Json;
using System.IO;
using Dalamud;

namespace MacroChain
{
    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public enum eWatchChannel
        {
            Party,
            LS1,
            LS2,
            LS3,
            LS4,
            LS5,
            LS6,
            LS7,
            LS8,
            CWLS1,
            CWLS2,
            CWLS3,
            CWLS4,
            CWLS5,
            CWLS6,
            CWLS7,
            CWLS8
        }

        public eWatchChannel watchChannel = eWatchChannel.Party;
        public ulong LocalContentID;
        public string CharacterName;
        public string World;

        public void Save()
        {
            if (DalamudApi.ClientState.IsLoggedIn)
            {
                if (LocalContentID != 0)
                {
                    PluginLog.LogWarning($"Saving {DateTime.Now} - {CharacterName} Saved");

                    var configFileInfo = GetConfigFileInfo(LocalContentID);

                    var serializedContents = JsonConvert.SerializeObject(this, Formatting.Indented);

                    File.WriteAllText(configFileInfo.FullName, serializedContents);
                }
            }
        }

        public static Config Load()
        {
            Config ret = new Config();
            if (DalamudApi.ClientState != null && DalamudApi.ClientState.IsLoggedIn)
            {
                var playerData = DalamudApi.ClientState.LocalPlayer;
                var contentId = DalamudApi.ClientState.LocalContentId;

                if (playerData != null && playerData.HomeWorld.GameData != null)
                {
                    var playerName = playerData.Name.TextValue;
                    var playerWorld = playerData.HomeWorld.GameData.Name.ToString();

                    var configFileInfo = GetConfigFileInfo(contentId);

                    if (configFileInfo.Exists)
                    {
                        ret = LoadConfiguration(contentId);
                        ret.CharacterName = playerName;
                        ret.World = playerWorld;
                    }
                    else
                    {
                        ret.CharacterName = playerName;
                        ret.LocalContentID = contentId;
                        ret.World = playerWorld;
                        ret.Save();
                    }
                }
            }

            return ret;
        }

        private static Config LoadConfiguration(ulong contentID)
        {
            var configFileInfo = GetConfigFileInfo(contentID);

            if (configFileInfo.Exists)
            {
                var fileText = File.ReadAllText(configFileInfo.FullName);

                var loadedCharacterConfiguration = JsonConvert.DeserializeObject<Config>(fileText);

                return loadedCharacterConfiguration ?? new Config();
            }
            else
            {
                return new Config();
            }
        }

        public static FileInfo GetConfigFileInfo(ulong contentID)
        {
            var pluginConfigDirectory = DalamudApi.PluginInterface.ConfigDirectory;

            return new FileInfo(pluginConfigDirectory.FullName + $@"\{contentID}.json");
        }
    }
}
