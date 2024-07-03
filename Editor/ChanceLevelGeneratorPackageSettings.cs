using System;
using UnityEditor;
using UnityEngine;

namespace ChanceGen.Editor
{
    internal static class ChanceLevelGeneratorPackageSettingsIMGUIRegister
    {
        public const string SETTINGS_PATH = "Project/ChanceGeneratorSettings";
        private const string c_KEY_SETTINGS = "chancegeneratorsettingsnotifyupdate";

        public static string KeySettingFullpath =>
            $"{c_KEY_SETTINGS}{Application.companyName}{Application.productName}";

        [SettingsProvider]
        public static SettingsProvider CreateSetingsProvider()
        {
            var provider = new SettingsProvider(SETTINGS_PATH, SettingsScope.Project)
            {
                label = "Chance Level Generator Settings",
                guiHandler = _ =>
                {
                    var setting =
                        EditorPrefs.GetInt(KeySettingFullpath, 0);

                    var newSetting = (int)(UpdateAskType)EditorGUILayout.EnumPopup((UpdateAskType)setting);

                    if (newSetting != setting)
                        EditorPrefs.SetInt(KeySettingFullpath, newSetting);
                },
                keywords = new[]
                    { "Chance", "Level", "Generator", "Update", "Stop", "No", "Type", "Popup", "Message", "Heroshrine" }
            };

            return provider;
        }
    }

    internal enum UpdateAskType
    {
        Popup = 0,
        Debug = 1,
        Silent = 2
    }
}