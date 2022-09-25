using System.IO;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;

namespace SixDash.Enhancements;

[UsedImplicitly]
internal class CacheDownloadedLevels : IPatch {
    public void Apply() {
        Online.levelDownloadFinish += (levelId, responseCode, levelJson) => {
            if(responseCode != 200)
                return;
            File.WriteAllText(GetFilePath(levelId), levelJson);
        };

        On.OnlineLevelsHub.LoadLevel += (orig, self, id) => {
            string cachedFilePath = GetFilePath(id);
            if(File.Exists(cachedFilePath) && !self.activated) {
                self.activated = true;
                LevelEditor.currentID = id;
                try {
                    LevelEditor.ImportFromLevelObject(self.editor.JSONToLevel(File.ReadAllText(cachedFilePath)));
                    self.LoadGamePage();
                    return;
                }
                catch {
                    self.activated = false;
                }
            }
            orig(self, id);
        };
    }

    private static string GetFilePath(int levelId) =>
        Path.Combine(Application.persistentDataPath, $"level_{levelId.ToString()}_data.json");
}
