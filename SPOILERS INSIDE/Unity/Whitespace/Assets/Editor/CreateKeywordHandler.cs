using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

public class CreateKeywordHandler : EditorWindow {
    private const string MenuPath = "Assets/Scripting/Create Keyword Handler";
    private const string TemplateRelativePath = "Assets/Editor/KeywordHandlerTemplate.cs.txt";

    string className = "NewKeywordHandler";
    string targetFolder = "Assets/Scripts/KeywordHandlers";

    [MenuItem(MenuPath)]
    public static void OpenWindow() {
        var w = GetWindow<CreateKeywordHandler>(true, "Create Keyword Handler", true);
        w.minSize = new Vector2(420, 80);
        w.InitFromSelection();
        w.Show();
    }

    void InitFromSelection() {
        targetFolder = GetSelectedPathOrFallback();
        // try to default class name from selection (folder or asset name)
        var sel = Selection.activeObject;
        if (sel != null) {
            var p = AssetDatabase.GetAssetPath(sel);
            if (!string.IsNullOrEmpty(p)) {
                if (Directory.Exists(p))
                    className = Path.GetFileName(p);
                else
                    className = Path.GetFileNameWithoutExtension(p);
            }
        }
        className = MakeValidClassName(className);
    }

    void OnGUI() {
        GUILayout.Label("Create Keyword Handler", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        className = EditorGUILayout.TextField("Class name", className);
        if (GUILayout.Button("Create", GUILayout.Width(80)))
            DoCreate();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Folder:", targetFolder);
        if (GUILayout.Button("Change", GUILayout.Width(80))) {
            string newFolder = EditorUtility.OpenFolderPanel("Select target folder (inside project)", Application.dataPath, "");
            if (!string.IsNullOrEmpty(newFolder)) {
                // convert absolute path to Assets/... path if possible
                if (newFolder.StartsWith(Application.dataPath)) {
                    targetFolder = "Assets" + newFolder.Substring(Application.dataPath.Length);
                } else {
                    EditorUtility.DisplayDialog("Invalid folder", "Please choose a folder inside this project's Assets folder.", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    void DoCreate() {
        className = MakeValidClassName(className);
        if (string.IsNullOrEmpty(className)) {
            EditorUtility.DisplayDialog("Invalid name", "Please enter a valid class name.", "OK");
            return;
        }

        string relativePath = Path.Combine(targetFolder, className + ".cs").Replace("\\", "/");

        if (File.Exists(GetAbsolutePath(relativePath))) {
            if (!EditorUtility.DisplayDialog("Overwrite file?", $"File already exists at {relativePath}. Overwrite?", "Yes", "No"))
                return;
        }

        string templateAbs = GetAbsolutePath(TemplateRelativePath);
        if (!File.Exists(templateAbs)) {
            EditorUtility.DisplayDialog("Template missing",
                $"Template not found at:\n{TemplateRelativePath}\n\nPlace the template at that path or update the script.", "OK");
            return;
        }

        string content = File.ReadAllText(templateAbs).Replace("#CLASSNAME#", className);
        File.WriteAllText(GetAbsolutePath(relativePath), content);
        AssetDatabase.Refresh();

        var asset = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);

        Close();
    }

    static string GetSelectedPathOrFallback() {
        var obj = Selection.activeObject;
        if (obj == null) return "Assets";
        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return "Assets";
        if (Directory.Exists(path)) return path;
        return Path.GetDirectoryName(path).Replace("\\", "/");
    }

    static string GetAbsolutePath(string assetPath) {
        // assetPath like "Assets/..."
        string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        return Path.Combine(projectRoot, assetPath).Replace("\\", "/");
    }

    static string MakeValidClassName(string name) {
        if (string.IsNullOrEmpty(name)) return "KeywordHandler";
        var valid = Regex.Replace(name, "[^a-zA-Z0-9_]", "");
        if (string.IsNullOrEmpty(valid)) valid = "KeywordHandler";
        if (!char.IsLetter(valid[0]) && valid[0] != '_') valid = "_" + valid;
        return valid;
    }
}