using UnityEditor;
using UnityEngine;
using static ChanceGen.Editor.ChanceLevelGeneratorPackageSettings;

namespace ChanceGen.Editor
{
    internal class ChanceLevelGeneratorPackageSettings : ScriptableObject
    {
        public const string SETTINGS_ASSET_PATH =
            "Packages/com.heroshrine.chancegen/Edit/ChanceLevelGeneratorSettings.asset";

        public const string SETTINGS_PATH = "Project/ChanceGeneratorSettings";

        internal static ChanceLevelGeneratorPackageSettings GetOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ChanceLevelGeneratorPackageSettings>(SETTINGS_ASSET_PATH);

            if (settings != null) return settings;

            settings = CreateInstance<ChanceLevelGeneratorPackageSettings>();
            AssetDatabase.CreateAsset(settings, SETTINGS_ASSET_PATH);
            AssetDatabase.SaveAssets();

            return settings;
        }

        public UpdateAskType askUpdateType;
    }


    internal static class ChanceLevelGeneratorPackageSettingsIMGUIRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateSetingsProvider()
        {
            var provider = new SettingsProvider(SETTINGS_PATH, SettingsScope.Project)
            {
                label = "Chance Level Generator Settings",
                guiHandler = _ =>
                {
                    var settings = new SerializedObject(ChanceLevelGeneratorPackageSettings.GetOrCreate());
                    EditorGUILayout.PropertyField(settings.FindProperty("askUpdateType"),
                        new GUIContent("Ask For Updates"));
                    settings.ApplyModifiedPropertiesWithoutUndo();
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