using System;
using System.Collections;
using UnityEngine.Networking;

namespace PaulMapper
{
    internal class GitHubUtils
    {
        public static IEnumerator GetLatestReleaseTag(Action<string> onResponse)
        {
            UnityWebRequest request = UnityWebRequest.Get("https://api.github.com/repos/HypersonicSharkz/PaulMapper/releases");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onResponse?.Invoke(null);
            }
            else
            {
                // Get the response as a string
                string response = request.downloadHandler.text;
                SimpleJSON.JSONArray releases = SimpleJSON.JSONObject.Parse(response).AsArray;
                onResponse?.Invoke(releases[0]["tag_name"]);
            }
        }
    }
}
