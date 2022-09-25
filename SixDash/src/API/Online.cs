using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace SixDash.API;

/// <summary>
/// APIs related to online levels.
/// </summary>
[PublicAPI]
public static class Online {
    /// <summary>
    /// A singleton of the current <see cref="OnlineLevelsHub"/> instance.
    /// </summary>
    public static OnlineLevelsHub? hubInstance { get; private set; }

    /// <summary>
    /// A singleton of the current <see cref="Submission"/> instance.
    /// </summary>
    public static Submission? submissionInstance { get; private set; }

    /// <summary>
    /// Data of a level in the recent levels list.
    /// </summary>
    /// <param name="id">Level id.</param>
    /// <param name="name">Level name.</param>
    /// <param name="author">Level author.</param>
    /// <param name="difficulty">Level difficulty.</param>
    /// <seealso cref="getRecentLevelsStart"/>
    /// <seealso cref="Online.getRecentLevelsFinish"/>
    public readonly record struct RecentLevelData(int id, string name, string author, int difficulty);

    /// <summary>
    /// Fired when the game sends a <b>get_recent</b> request.
    /// </summary>
    /// <seealso cref="getRecentLevelsFinish"/>
    public static event Action? getRecentLevelsStart;

    /// <summary>
    /// Fired when the game receives a response to a <b>get_recent</b> request.
    /// </summary>
    /// <seealso cref="getRecentLevelsStart"/>
    public static event Action<string, RecentLevelData[]>? getRecentLevelsFinish;

    /// <summary>
    /// Level download start delegate.
    /// </summary>
    /// <param name="levelId">Downloading level ID.</param>
    /// <seealso cref="levelDownloadStart"/>
    public delegate void LevelDownloadStart(int levelId);
    /// <summary>
    /// Fired when the game sends a <b>get_json</b> request.
    /// </summary>
    /// <seealso cref="levelDownloadFinish"/>
    public static event LevelDownloadStart? levelDownloadStart;

    /// <summary>
    /// Level download finish delegate.
    /// </summary>
    /// <param name="levelId">Downloaded level ID.</param>
    /// <param name="responseCode">Response code of the request.</param>
    /// <param name="levelJson">JSON data of the downloaded level.</param>
    /// <seealso cref="levelDownloadFinish"/>
    public delegate void LevelDownloadFinish(int levelId, int responseCode, string levelJson);
    /// <summary>
    /// Fired when the game receives a response to a <b>get_json</b> request.
    /// </summary>
    /// <seealso cref="levelDownloadStart"/>
    public static event LevelDownloadFinish? levelDownloadFinish;

    /// <summary>
    /// Level upload start delegate.
    /// </summary>
    /// <param name="name">Level name.</param>
    /// <param name="author">Level author.</param>
    /// <param name="difficulty">Level difficulty.</param>
    /// <param name="json">JSON data of the uploading level..</param>
    /// <seealso cref="levelUploadStart"/>
    public delegate void LevelUploadStart(string name, string author, int difficulty, string json);
    /// <summary>
    /// Fired when the game sends a <b>push_level_data</b> request.
    /// </summary>
    /// <seealso cref="levelUploadFinish"/>
    public static event LevelUploadStart? levelUploadStart;

    /// <summary>
    /// Level upload finish delegate.
    /// </summary>
    /// <param name="levelId">Uploaded level ID.</param>
    /// <param name="responseCode">Response code of the request.</param>
    /// <seealso cref="levelUploadFinish"/>
    public delegate void LevelUploadFinish(int levelId, int responseCode);
    /// <summary>
    /// Fired when the game receives a response to a <b>push_level_data</b> request.
    /// </summary>
    /// <seealso cref="levelUploadStart"/>
    public static event LevelUploadFinish? levelUploadFinish;

    private static readonly List<RecentLevelData> recentLevels = new();
    private static int _latestDownloadId;

    internal static void Patch() {
        On.OnlineLevelsHub.Awake += (orig, self) => {
            hubInstance = self;
            orig(self);
        };
        On.Submission.Start += (orig, self) => {
            submissionInstance = self;
            orig(self);
        };

        GetRecentLevels();
        LevelDownload();
        LevelUpload();
    }

    private static void GetRecentLevels() {
        On.OnlineLevelsHub.GetRecentRequest += (orig, self, uri) => {
            getRecentLevelsStart?.Invoke();
            return orig(self, uri);
        };
        On.OnlineLevelsHub.PopulateRecentLevels += (orig, self, data) => {
            recentLevels.Clear();
            orig(self, data);
            getRecentLevelsFinish?.Invoke(data, recentLevels.ToArray());
        };
        On.OnlineLevelsHub.AddButton += (orig, self, transform, id, name, author, difficulty) => {
            orig(self, transform, id, name, author, difficulty);
            recentLevels.Add(new RecentLevelData(id, name, author, difficulty));
        };
    }

    private static void LevelDownload() {
        On.OnlineLevelsHub.GetRequest += (orig, self, uri, id) => {
            _latestDownloadId = id;
            levelDownloadStart?.Invoke(id);
            return orig(self, uri, id);
        };
        On.OnlineLevelsHub.LoadLevelRequestFinished += (orig, self, json, responseCode) => {
            orig(self, json, responseCode);
            levelDownloadFinish?.Invoke(_latestDownloadId, responseCode, json);
        };
    }

    private static void LevelUpload() {
        On.Submission.SetRequest += (orig, self, uri, name, author, difficulty, json) => {
            levelUploadStart?.Invoke(name, author, difficulty, json);
            return orig(self, uri, name, author, difficulty, json);
        };
        On.Submission.ManageOutput += (orig, self, outputText, responseCode) => {
            orig(self, outputText, responseCode);
            if(!int.TryParse(outputText, out int levelId))
                levelId = -1;
            levelUploadFinish?.Invoke(levelId, responseCode);
        };
    }
}
