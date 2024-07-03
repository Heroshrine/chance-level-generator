using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ChanceGen.Editor
{
    [InitializeOnLoad]
    internal sealed class VersionChecker
    {
        private const string c_KEY_ASKED = "chancegenversioncheckingupm";
        private const string c_PACKAGE_NAME = "com.heroshrine.chancegen";

        private const string c_URL_PACKAGE =
            "https://api.github.com/repos/Heroshrine/chance-level-generator/contents/package.json";

        private const string c_URL_2022_3 = "?ref=upm-2022-3";
        private const string c_URL_6 = "?ref=upm-6";

        private static ListRequest s_currentRequest;
        private static bool s_listRequestFinished;

        static VersionChecker()
        {
            var settings =
                (UpdateAskType)EditorPrefs.GetInt(ChanceLevelGeneratorPackageSettingsIMGUIRegister.KeySettingFullpath,
                    0);

            if (settings == UpdateAskType.Silent || SessionState.GetBool(c_KEY_ASKED, false)) return;

            CheckVersion().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogException(t.Exception!.InnerException);
            });
        }

        private static async Task CheckVersion()
        {
            // get package.json from github repo
            var json = await GetPackageJsonOnline();
            if (json is null) return;

            // decode content
            var decodedContent = await Task.Run(() => DecodePackageContent(json));
            if (decodedContent == string.Empty) return;

            // get version from decoded content
            var match = await Task.Run(() => Regex.Match(decodedContent,
                @"[^\S\r\n]*""version"":[^\S\r\n]*""(\d+\.\d+\.\d+)""",
                RegexOptions.Multiline | RegexOptions.IgnoreCase, new TimeSpan(0, 2, 0)));
            if (!match.Success) return;
            var onlineVersion = match.Groups[1].Value;

            // get installed version
            s_listRequestFinished = false;
            s_currentRequest = Client.List(true);
            EditorApplication.update += CheckIfListFinished;
            await Task.Run(() =>
            {
                while (!s_listRequestFinished)
                    Thread.Sleep(200);
            });
            EditorApplication.update -= CheckIfListFinished;
            var installedVersion = await Task.Run(() => GetInstalledVersionNumbers(s_currentRequest));
            if (installedVersion is null) return;

            // finally, compare versions
            if (installedVersion != onlineVersion)
                DisplayUpdateDialog();
        }

        private static void CheckIfListFinished() { s_listRequestFinished = s_currentRequest.IsCompleted; }

        private static string GetInstalledVersionNumbers(ListRequest completedListRequest)
        {
            foreach (var package in completedListRequest.Result)
            {
                if (package.source != PackageSource.Git) continue;
                if (package.name != c_PACKAGE_NAME) continue;
                return package.version;
            }

            return null;
        }

        private static string DecodePackageContent(string json)
        {
            var encodedJson = Regex.Match(json, @"""content"":""([A-Za-z0-9+/=\\n]+)").Groups[1].Value;
            var splitEncoded = encodedJson.Split(@"\n");

            var sb = new StringBuilder();

            foreach (var jsonPiece in splitEncoded)
            {
                var decodedBytes = Convert.FromBase64String(jsonPiece);
                var decodedPiece = Encoding.UTF8.GetString(decodedBytes);
                sb.Append(decodedPiece);
            }

            return sb.ToString();
        }

        private static async Task<string> GetPackageJsonOnline()
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("request");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{c_URL_PACKAGE}{c_URL_2022_3}"),
                Method = HttpMethod.Get
            };

            var response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning(
                    $"request failure! chance-level-generator repo api access code: {response.StatusCode}");
                return null;
            }

            // decode content
            var content = await response.Content.ReadAsByteArrayAsync();
            var decoded = Encoding.UTF8.GetString(content);

            return decoded;
        }

        [MenuItem("Tools/ChanceGen/Check for updates")]
        private static void DisplayUpdateDialog()
        {
            var settings =
                (UpdateAskType)EditorPrefs.GetInt(ChanceLevelGeneratorPackageSettingsIMGUIRegister.KeySettingFullpath,
                    0);

            switch (settings)
            {
                case UpdateAskType.Popup:
                    BoxDialog();
                    break;
                case UpdateAskType.Debug:
                    Debug.Log("An update for the installed package chance-level-generator is available.");
                    break;
                case UpdateAskType.Silent:
                default:
                    break;
            }

            SessionState.SetBool(c_KEY_ASKED, true);
        }

        private static void BoxDialog()
        {
            var choice = EditorUtility.DisplayDialogComplex("Chance Level Generator Update",
                "An update for the installed package chance-level-generator was detected. "
                + "Would you like to open the package manager now?",
                "Yes", "Don't Notify Me of Updates", "No");

            switch (choice)
            {
                case 0:
                    UnityEditor.PackageManager.UI.Window.Open("com.heroshrine.chancegen");
                    break;
                case 1:
                    SettingsService.OpenProjectSettings(ChanceLevelGeneratorPackageSettingsIMGUIRegister.SETTINGS_PATH);
                    break;
            }
        }
    }
}