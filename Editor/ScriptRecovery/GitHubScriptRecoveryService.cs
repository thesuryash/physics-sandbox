#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ImportExport.Import;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class GitHubScriptRecoveryService
{
    private const string OwnerKey = "PhysicsSandbox.ScriptRecovery.Owner";
    private const string RepoKey = "PhysicsSandbox.ScriptRecovery.Repo";
    private const string BranchKey = "PhysicsSandbox.ScriptRecovery.Branch";
    private const string IndexPathKey = "PhysicsSandbox.ScriptRecovery.IndexPath";
    private const string DestinationKey = "PhysicsSandbox.ScriptRecovery.Destination";

    private const string DefaultOwner = "thesuryash";
    private const string DefaultRepo = "physics-sandbox";
    private const string DefaultBranch = "lts";
    private const string DefaultIndexPath = "ScriptRecovery~/script-index.json";
    private const string DefaultDestination = "Assets/PhysicsSandbox/RecoveredScripts";

    static GitHubScriptRecoveryService()
    {
        ImportManager.MissingComponentTypesDetected += OnMissingComponentTypesDetected;
    }

    [MenuItem("Physics Sandbox/Script Recovery/Configure GitHub Source")]
    private static void ConfigureSource()
    {
        ScriptRecoverySettingsWindow.Open();
    }

    [MenuItem("Physics Sandbox/Script Recovery/Generate Local Script Index")]
    private static void GenerateLocalScriptIndex()
    {
        string packageRoot = PackageRoot();
        string runtimeRoot = Path.Combine(packageRoot, "Runtime");
        if (!Directory.Exists(runtimeRoot))
        {
            EditorUtility.DisplayDialog("Script Recovery", "Runtime folder was not found for this package.", "OK");
            return;
        }

        var entries = Directory.GetFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(ReadScriptTypes)
            .OrderBy(entry => entry.type, StringComparer.Ordinal)
            .ToList();

        string outputPath = Path.Combine(packageRoot, DefaultIndexPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, ToJson(entries), Encoding.UTF8);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Script Recovery",
            $"Generated {entries.Count} entries at {DefaultIndexPath}. Commit this file on the LTS branch.",
            "OK");
    }

    private static void OnMissingComponentTypesDetected(IReadOnlyList<string> missingTypes)
    {
        var uniqueTypes = missingTypes
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (uniqueTypes.Count == 0) return;

        string message = $"The JSON import skipped {uniqueTypes.Count} missing script type(s).\n\n" +
                         $"Download matching scripts from {Owner}/{Repo}:{Branch}?";

        if (!EditorUtility.DisplayDialog("Recover Missing Scripts", message, "Download", "Skip"))
        {
            return;
        }

        RecoverMissingTypes(uniqueTypes);
    }

    private static void RecoverMissingTypes(IReadOnlyList<string> missingTypes)
    {
        ScriptRecoveryManifest manifest = DownloadManifest();
        if (manifest?.scripts == null || manifest.scripts.Length == 0)
        {
            Debug.LogWarning($"[Script Recovery] No script index found at {RawUrl(IndexPath)}.");
            return;
        }

        int downloaded = 0;
        var unresolved = new List<string>();

        foreach (string missingType in missingTypes)
        {
            string typeName = StripAssemblyName(missingType);
            string className = LastTypeSegment(typeName);
            ScriptRecoveryEntry entry = FindEntry(manifest.scripts, typeName, className);

            if (entry == null || string.IsNullOrWhiteSpace(entry.path))
            {
                unresolved.Add(typeName);
                continue;
            }

            if (TryDownloadScript(entry, className))
            {
                downloaded++;
            }
            else
            {
                unresolved.Add(typeName);
            }
        }

        if (downloaded > 0)
        {
            AssetDatabase.Refresh();
        }

        string summary = $"Downloaded {downloaded} script(s). Unity will recompile them; re-run the JSON import after compilation.";
        if (unresolved.Count > 0)
        {
            summary += "\n\nUnresolved:\n" + string.Join("\n", unresolved);
        }

        EditorUtility.DisplayDialog("Script Recovery", summary, "OK");
    }

    private static ScriptRecoveryManifest DownloadManifest()
    {
        string json = DownloadText(RawUrl(IndexPath));
        return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<ScriptRecoveryManifest>(json);
    }

    private static bool TryDownloadScript(ScriptRecoveryEntry entry, string className)
    {
        string remotePath = NormalizeRemotePath(entry.path);
        if (string.IsNullOrEmpty(remotePath) || !remotePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"[Script Recovery] Refused invalid script path: {entry.path}");
            return false;
        }

        string source = DownloadText(RawUrl(remotePath));
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(className) && !source.Contains($"class {className}"))
        {
            Debug.LogWarning($"[Script Recovery] Downloaded {remotePath}, but it does not appear to declare class {className}.");
        }

        string destinationRelativePath = $"{Destination}/{remotePath}";
        string absolutePath = Path.GetFullPath(destinationRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        File.WriteAllText(absolutePath, source, Encoding.UTF8);
        Debug.Log($"[Script Recovery] Downloaded {remotePath} to {destinationRelativePath}");
        return true;
    }

    private static string DownloadText(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 30;
            var operation = request.SendWebRequest();
            while (!operation.isDone) { }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Script Recovery] Download failed: {url}\n{request.error}");
                return null;
            }

            return request.downloadHandler.text;
        }
    }

    private static ScriptRecoveryEntry FindEntry(IEnumerable<ScriptRecoveryEntry> entries, string typeName, string className)
    {
        return entries.FirstOrDefault(entry =>
            string.Equals(entry.type, typeName, StringComparison.Ordinal) ||
            string.Equals(entry.className, className, StringComparison.Ordinal) ||
            (entry.aliases != null && entry.aliases.Any(alias =>
                string.Equals(alias, typeName, StringComparison.Ordinal) ||
                string.Equals(alias, className, StringComparison.Ordinal))));
    }

    private static IEnumerable<ScriptRecoveryEntry> ReadScriptTypes(string absolutePath)
    {
        string text = File.ReadAllText(absolutePath);
        string packageRoot = PackageRoot().Replace("\\", "/");
        string relativePath = absolutePath.Replace("\\", "/").Replace(packageRoot + "/", "");
        string currentNamespace = null;

        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.StartsWith("namespace ", StringComparison.Ordinal))
            {
                currentNamespace = line.Substring("namespace ".Length).Trim('{', ' ', '\t', '\r');
                continue;
            }

            string className = TryReadTypeName(line);
            if (string.IsNullOrEmpty(className)) continue;

            string typeName = string.IsNullOrEmpty(currentNamespace)
                ? className
                : $"{currentNamespace}.{className}";

            yield return new ScriptRecoveryEntry
            {
                type = typeName,
                className = className,
                path = relativePath
            };
        }
    }

    private static string TryReadTypeName(string line)
    {
        string[] markers = { " class ", " struct ", " enum " };
        foreach (string marker in markers)
        {
            int markerIndex = line.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0) continue;

            string rest = line.Substring(markerIndex + marker.Length).Trim();
            int end = rest.IndexOfAny(new[] { ' ', ':', '<', '{', '\r' });
            return end >= 0 ? rest.Substring(0, end) : rest;
        }

        return null;
    }

    private static string ToJson(IReadOnlyList<ScriptRecoveryEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine("  \"scripts\": [");

        for (int i = 0; i < entries.Count; i++)
        {
            ScriptRecoveryEntry entry = entries[i];
            builder.AppendLine("    {");
            builder.AppendLine($"      \"type\": \"{EscapeJson(entry.type)}\",");
            builder.AppendLine($"      \"className\": \"{EscapeJson(entry.className)}\",");
            builder.AppendLine($"      \"path\": \"{EscapeJson(entry.path)}\"");
            builder.Append("    }");
            builder.AppendLine(i == entries.Count - 1 ? "" : ",");
        }

        builder.AppendLine("  ]");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string EscapeJson(string value)
    {
        return value?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }

    private static string RawUrl(string path)
    {
        return $"https://raw.githubusercontent.com/{Owner}/{Repo}/{Branch}/{NormalizeRemotePath(path)}";
    }

    private static string NormalizeRemotePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        string normalized = path.Replace('\\', '/').TrimStart('/');
        return normalized.Contains("..") ? null : normalized;
    }

    private static string StripAssemblyName(string typeName)
    {
        return typeName.Split(',')[0].Trim();
    }

    private static string LastTypeSegment(string typeName)
    {
        int dot = typeName.LastIndexOf('.');
        return dot >= 0 ? typeName.Substring(dot + 1) : typeName;
    }

    private static string PackageRoot()
    {
        var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GitHubScriptRecoveryService).Assembly);
        return info?.assetPath ?? "Packages/com.thesuryash.physics-sandbox";
    }

    private static string Owner => EditorPrefs.GetString(OwnerKey, DefaultOwner);
    private static string Repo => EditorPrefs.GetString(RepoKey, DefaultRepo);
    private static string Branch => EditorPrefs.GetString(BranchKey, DefaultBranch);
    private static string IndexPath => EditorPrefs.GetString(IndexPathKey, DefaultIndexPath);
    private static string Destination => EditorPrefs.GetString(DestinationKey, DefaultDestination).TrimEnd('/');

    [Serializable]
    private sealed class ScriptRecoveryManifest
    {
        public ScriptRecoveryEntry[] scripts;
    }

    [Serializable]
    private sealed class ScriptRecoveryEntry
    {
        public string type;
        public string className;
        public string path;
        public string[] aliases;
    }

    private sealed class ScriptRecoverySettingsWindow : EditorWindow
    {
        private string owner;
        private string repo;
        private string branch;
        private string indexPath;
        private string destination;

        public static void Open()
        {
            GetWindow<ScriptRecoverySettingsWindow>("Script Recovery");
        }

        private void OnEnable()
        {
            owner = Owner;
            repo = Repo;
            branch = Branch;
            indexPath = IndexPath;
            destination = Destination;
        }

        private void OnGUI()
        {
            owner = EditorGUILayout.TextField("GitHub Owner", owner);
            repo = EditorGUILayout.TextField("Repository", repo);
            branch = EditorGUILayout.TextField("LTS Branch", branch);
            indexPath = EditorGUILayout.TextField("Index Path", indexPath);
            destination = EditorGUILayout.TextField("Download Folder", destination);

            EditorGUILayout.Space();

            if (GUILayout.Button("Save"))
            {
                EditorPrefs.SetString(OwnerKey, owner);
                EditorPrefs.SetString(RepoKey, repo);
                EditorPrefs.SetString(BranchKey, branch);
                EditorPrefs.SetString(IndexPathKey, indexPath);
                EditorPrefs.SetString(DestinationKey, destination);
                Close();
            }

            if (GUILayout.Button("Reset Defaults"))
            {
                EditorPrefs.DeleteKey(OwnerKey);
                EditorPrefs.DeleteKey(RepoKey);
                EditorPrefs.DeleteKey(BranchKey);
                EditorPrefs.DeleteKey(IndexPathKey);
                EditorPrefs.DeleteKey(DestinationKey);
                OnEnable();
            }
        }
    }
}
#endif
