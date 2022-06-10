using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using static ImGuiNET.ImGui;
using Dalamud.Logging;


namespace MacroChain
{
    public class ConfigUI
    {

        private bool m_Visible;
        public bool Enabled
        {
            get => m_Visible;
            set
            {
                if (value)
                {
                    // load config and update UI
                    MacroChain.config = Config.Load();
                }
                m_Visible = value;
            }
        }

        public void Draw()
        {
            if (!m_Visible) return;
            try
            {
                if (!ImGui.Begin("MacroChain", ref m_Visible)) return;
                if (EnumCombo("Watch Channel", ref MacroChain.config.watchChannel))
                {
                    MacroChain.config.Save();
                }
            } catch (Exception ex)
            {
                PluginLog.LogError(ex.ToString());
            } finally
            {
                ImGui.End();
            }
        }


        public static bool EnumCombo<TEnum>(string label, ref TEnum @enum, string[] toolTips, ImGuiComboFlags flags = ImGuiComboFlags.None, bool showValue = false) where TEnum : struct, Enum
        {
            var ret = false;
            var previewValue = showValue ? $"{@enum.ToString()} ({Convert.ChangeType(@enum, @enum.GetTypeCode())})" : @enum.ToString();
            if (BeginCombo(label, previewValue, flags))
            {
                var values = Enum.GetValues<TEnum>();
                for (var i = 0; i < values.Length; i++)
                    try
                    {
                        PushID(i);
                        var s = showValue
                            ? $"{values[i].ToString()} ({Convert.ChangeType(values[i], values[i].GetTypeCode())})"
                            : values[i].ToString();
                        if (Selectable(s, values[i].Equals(@enum)))
                        {
                            ret = true;
                            @enum = values[i];
                        }

                        //if (IsItemHovered())
                        //{
                        //    ToolTip(toolTips[i].Localize());
                        //}

                        PopID();
                    }
                    catch (Exception e)
                    {
                        PluginLog.Error(e.ToString());
                    }

                EndCombo();
            }

            return ret;
        }
        public static bool EnumCombo<TEnum>(string label, ref TEnum @enum, ImGuiComboFlags flags = ImGuiComboFlags.None, bool showValue = false) where TEnum : struct, Enum
        {
            var ret = false;
            var previewValue = showValue ? $"{@enum} ({Convert.ChangeType(@enum, @enum.GetTypeCode())})" : @enum.ToString();
            if (BeginCombo(label, previewValue, flags))
            {
                var values = Enum.GetValues<TEnum>();
                for (var i = 0; i < values.Length; i++)
                    try
                    {
                        PushID(i);
                        var s = showValue
                            ? $"{values[i]} ({Convert.ChangeType(values[i], values[i].GetTypeCode())})"
                            : values[i].ToString();
                        if (Selectable(s, values[i].Equals(@enum)))
                        {
                            ret = true;
                            @enum = values[i];
                        }

                        PopID();
                    }
                    catch (Exception e)
                    {
                        PluginLog.Error(e.ToString());
                    }

                EndCombo();
            }

            return ret;
        }
    }


}
