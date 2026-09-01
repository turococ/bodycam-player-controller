using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class TextureToMaterialEditor : EditorWindow
{
    private enum Mode
    {
        CreateNew,
        FillExisting
    }

    private string _textureFolderPath = "Assets/sprites/playerHead";
    private string _materialsFolderPath = "Assets/Materials";
    private Shader _shader;
    private Mode _mode = Mode.CreateNew;
    private Material _templateMaterial;
    private List<TextureGroup> _foundGroups = new();
    private Vector2 _scrollPosition;
    private bool _includeSubfolders = true;
    private bool _autoDetectFromSuffix = true;
    private string _outputFolder = "Assets/Materials/Generated";
    private List<MaterialMatch> _materialMatches = new();

    private class TextureGroup
    {
        public string BaseName;
        public Texture2D Albedo;
        public Texture2D Normal;
        public Texture2D Metallic;
        public Texture2D Smoothness;
        public Texture2D Occlusion;
        public Texture2D Emission;
        public Texture2D DetailMask;
        public Texture2D DetailAlbedo;
        public Texture2D DetailNormal;

        public int TextureCount => new[] { Albedo, Normal, Metallic, Smoothness, Occlusion, Emission, DetailMask, DetailAlbedo, DetailNormal }.Count(t => t != null);
    }

    private class MaterialMatch
    {
        public Material Material;
        public TextureGroup Group;
        public bool HasMatch => Material != null && Group != null;
        public string Status => Material == null ? "MISSING" : Group == null ? "NO TEXTURES" : "OK";
    }

    [MenuItem("Tools/Texture to Material (Auto)")]
    public static void ShowWindow()
    {
        GetWindow<TextureToMaterialEditor>("Texture to Material (Auto)");
    }

    private void OnEnable()
    {
        _shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        RefreshTextures();
        if (_mode == Mode.FillExisting)
            FindMaterialMatches();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Auto Texture to Material", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        _mode = (Mode)EditorGUILayout.EnumPopup("Mode", _mode);

        EditorGUI.BeginChangeCheck();
        _textureFolderPath = EditorGUILayout.TextField("Texture Folder", _textureFolderPath);
        if (EditorGUI.EndChangeCheck())
        {
            RefreshTextures();
            if (_mode == Mode.FillExisting)
                FindMaterialMatches();
        }

        _includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", _includeSubfolders);
        _autoDetectFromSuffix = EditorGUILayout.Toggle("Auto-detect from Suffix", _autoDetectFromSuffix);

        EditorGUILayout.Space();

        if (_mode == Mode.CreateNew)
        {
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            _shader = (Shader)EditorGUILayout.ObjectField("Shader", _shader, typeof(Shader), false);
        }
        else
        {
            _materialsFolderPath = EditorGUILayout.TextField("Existing Materials Folder", _materialsFolderPath);
            if (GUILayout.Button("Refresh Matches"))
                FindMaterialMatches();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical("box");
        string label = _mode == Mode.CreateNew 
            ? $"Found Texture Groups ({_foundGroups.Count})"
            : $"Material Matches ({_materialMatches.Count(m => m.HasMatch)} matched, {_materialMatches.Count(m => m.Material == null)} missing)";
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        if (_mode == Mode.CreateNew)
        {
            if (_foundGroups.Count == 0)
            {
                EditorGUILayout.HelpBox("No texture groups found. Check folder path and naming.", MessageType.Warning);
            }
            else
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
                foreach (var group in _foundGroups)
                {
                    DrawGroup(group);
                }
                EditorGUILayout.EndScrollView();
            }
        }
        else
        {
            if (_materialMatches.Count == 0)
            {
                EditorGUILayout.HelpBox("No materials found in the specified folder.", MessageType.Warning);
            }
            else
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
                foreach (var match in _materialMatches.OrderBy(m => m.Material?.name ?? m.Group?.BaseName ?? ""))
                {
                    DrawMatch(match);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        bool canApply = _mode == Mode.CreateNew 
            ? _foundGroups.Count > 0 && _shader != null
            : _materialMatches.Any(m => m.HasMatch);

        EditorGUI.BeginDisabledGroup(!canApply);
        string btnLabel = _mode == Mode.CreateNew ? "Generate Materials from All Groups" : "Apply Textures to Matched Materials";
        if (GUILayout.Button(btnLabel, GUILayout.Height(40)))
        {
            if (_mode == Mode.CreateNew)
                ApplyAllGroups();
            else
                ApplyToExistingMaterials();
        }
        EditorGUI.EndDisabledGroup();

        if (_mode == Mode.CreateNew && _shader == null)
            EditorGUILayout.HelpBox("Please select a valid shader.", MessageType.Error);
        if (_mode == Mode.FillExisting && !_materialMatches.Any(m => m.HasMatch))
            EditorGUILayout.HelpBox("No matching materials found. Check material names match texture base names.", MessageType.Warning);
    }

    private void DrawGroup(TextureGroup group)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(group.BaseName, EditorStyles.boldLabel);
        DrawTextureField("Albedo", group.Albedo);
        DrawTextureField("Normal", group.Normal);
        DrawTextureField("Metallic", group.Metallic);
        DrawTextureField("Smoothness", group.Smoothness);
        DrawTextureField("Occlusion", group.Occlusion);
        DrawTextureField("Emission", group.Emission);
        DrawTextureField("Detail Mask", group.DetailMask);
        DrawTextureField("Detail Albedo", group.DetailAlbedo);
        DrawTextureField("Detail Normal", group.DetailNormal);
        EditorGUILayout.EndVertical();
    }

    private void DrawMatch(MaterialMatch match)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        var style = match.Material == null ? EditorStyles.boldLabel : EditorStyles.label;
        EditorGUILayout.LabelField(match.Material?.name ?? match.Group.BaseName, style, GUILayout.Width(200));
        var statusColor = match.Material == null ? Color.red : (match.Group == null ? Color.yellow : Color.green);
        var statusStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = statusColor } };
        EditorGUILayout.LabelField($"[{match.Status}]", statusStyle, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        if (match.Material != null)
            EditorGUILayout.ObjectField(match.Material, typeof(Material), false);

        if (match.Group != null)
        {
            DrawTextureField("Albedo", match.Group.Albedo);
            DrawTextureField("Normal", match.Group.Normal);
            DrawTextureField("Metallic", match.Group.Metallic);
            DrawTextureField("Smoothness", match.Group.Smoothness);
            DrawTextureField("Occlusion", match.Group.Occlusion);
            DrawTextureField("Emission", match.Group.Emission);
            DrawTextureField("Detail Mask", match.Group.DetailMask);
            DrawTextureField("Detail Albedo", match.Group.DetailAlbedo);
            DrawTextureField("Detail Normal", match.Group.DetailNormal);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawTextureField(string label, Texture2D tex)
    {
        if (tex == null) return;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ObjectField(tex, typeof(Texture2D), false, GUILayout.Width(50), GUILayout.Height(50));
        EditorGUILayout.LabelField($"{label}: {tex.name}", GUILayout.Width(250));
        EditorGUILayout.EndHorizontal();
    }

    private void RefreshTextures()
    {
        _foundGroups.Clear();

        if (!AssetDatabase.IsValidFolder(_textureFolderPath))
            return;

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { _textureFolderPath });
        var texturesByBaseName = new Dictionary<string, TextureGroup>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!_includeSubfolders && !path.StartsWith(_textureFolderPath + "/"))
                continue;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;

            string baseName = _autoDetectFromSuffix ? GetBaseName(tex.name) : tex.name;
            
            if (!texturesByBaseName.TryGetValue(baseName, out var group))
            {
                group = new TextureGroup { BaseName = baseName };
                texturesByBaseName[baseName] = group;
            }

            AssignTextureToGroup(group, tex);
        }

        _foundGroups = texturesByBaseName.Values.Where(g => g.TextureCount > 0).OrderBy(g => g.BaseName).ToList();
    }

    private void FindMaterialMatches()
    {
        _materialMatches.Clear();

        if (!AssetDatabase.IsValidFolder(_materialsFolderPath))
            return;

        var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { _materialsFolderPath });
        var materialsByName = new Dictionary<string, Material>();

        foreach (var guid in materialGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                materialsByName[mat.name.ToLower()] = mat;
        }

        foreach (var group in _foundGroups)
        {
            string key = group.BaseName.ToLower();
            if (materialsByName.TryGetValue(key, out var mat))
            {
                _materialMatches.Add(new MaterialMatch { Material = mat, Group = group });
                materialsByName.Remove(key);
            }
            else
            {
                _materialMatches.Add(new MaterialMatch { Material = null, Group = group });
            }
        }

        foreach (var kvp in materialsByName)
        {
            _materialMatches.Add(new MaterialMatch { Material = kvp.Value, Group = null });
        }
    }

    private string GetBaseName(string textureName)
    {
        var suffixes = new[] 
        { 
            "_albedo", "_basecolor", "_base", "_color", "_diffuse", "_diff",
            "_normal", "_norm", "_nrm",
            "_metallic", "_metal", "_mtl",
            "_smoothness", "_smooth", "_roughness", "_rough", "_gloss",
            "_occlusion", "_occl", "_ao",
            "_emission", "_emissive", "_emit",
            "_detailmask", "_detail_mask",
            "_detailalbedo", "_detail_albedo",
            "_detailnormal", "_detail_normal"
        };

        string lower = textureName.ToLower();
        foreach (var suffix in suffixes)
        {
            if (lower.EndsWith(suffix))
            {
                return textureName.Substring(0, textureName.Length - suffix.Length).TrimEnd('_', '-', ' ');
            }
        }
        return textureName;
    }

    private void AssignTextureToGroup(TextureGroup group, Texture2D tex)
    {
        string lower = tex.name.ToLower();
        
        if (lower.EndsWith("_albedo") || lower.EndsWith("_basecolor") || lower.EndsWith("_base") || lower.EndsWith("_color") || lower.EndsWith("_diffuse") || lower.EndsWith("_diff"))
            group.Albedo = tex;
        else if (lower.EndsWith("_normal") || lower.EndsWith("_norm") || lower.EndsWith("_nrm"))
            group.Normal = tex;
        else if (lower.EndsWith("_metallic") || lower.EndsWith("_metal") || lower.EndsWith("_mtl"))
            group.Metallic = tex;
        else if (lower.EndsWith("_smoothness") || lower.EndsWith("_smooth") || lower.EndsWith("_roughness") || lower.EndsWith("_rough") || lower.EndsWith("_gloss"))
            group.Smoothness = tex;
        else if (lower.EndsWith("_occlusion") || lower.EndsWith("_occl") || lower.EndsWith("_ao"))
            group.Occlusion = tex;
        else if (lower.EndsWith("_emission") || lower.EndsWith("_emissive") || lower.EndsWith("_emit"))
            group.Emission = tex;
        else if (lower.EndsWith("_detailmask") || lower.EndsWith("_detail_mask"))
            group.DetailMask = tex;
        else if (lower.EndsWith("_detailalbedo") || lower.EndsWith("_detail_albedo"))
            group.DetailAlbedo = tex;
        else if (lower.EndsWith("_detailnormal") || lower.EndsWith("_detail_normal"))
            group.DetailNormal = tex;
        else if (group.Albedo == null)
            group.Albedo = tex;
    }

    private void ApplyAllGroups()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(_outputFolder))
        {
            var parts = _outputFolder.Split('/');
            string currentPath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                currentPath = nextPath;
            }
        }

        int created = 0;
        int updated = 0;

        foreach (var group in _foundGroups)
        {
            string materialPath = $"{_outputFolder}/{group.BaseName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                material = new Material(_shader);
                AssetDatabase.CreateAsset(material, materialPath);
                created++;
            }
            else
            {
                updated++;
            }

            ApplyTexturesToMaterial(material, group);
            EditorUtility.SetDirty(material);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Done! Created: {created}, Updated: {updated} materials in {_outputFolder}");
        EditorUtility.DisplayDialog("Complete", $"Created: {created}\nUpdated: {updated}\nSaved to: {_outputFolder}", "OK");
    }

    private void ApplyToExistingMaterials()
    {
        int updated = 0;

        foreach (var match in _materialMatches.Where(m => m.HasMatch))
        {
            ApplyTexturesToMaterial(match.Material, match.Group);
            EditorUtility.SetDirty(match.Material);
            updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Done! Updated: {updated} existing materials");
        EditorUtility.DisplayDialog("Complete", $"Updated: {updated} existing materials", "OK");
    }

    private void ApplyTexturesToMaterial(Material material, TextureGroup group)
    {
        if (group.Albedo != null)
        {
            material.SetTexture("_BaseColorMap", group.Albedo);
            material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_SurfaceType"))
                material.SetFloat("_SurfaceType", 0);
        }

        if (group.Normal != null)
        {
            material.SetTexture("_NormalMap", group.Normal);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(group.Normal)) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        if (group.Metallic != null)
            material.SetTexture("_MetallicMap", group.Metallic);

        if (group.Smoothness != null)
        {
            material.SetTexture("_SmoothnessMap", group.Smoothness);
            var lower = group.Smoothness.name.ToLower();
            if (lower.Contains("_rough") || lower.Contains("_roughness"))
            {
                if (material.HasProperty("_SmoothnessMapChannel"))
                    material.SetFloat("_SmoothnessMapChannel", 1);
            }
        }

        if (group.Occlusion != null)
            material.SetTexture("_OcclusionMap", group.Occlusion);

        if (group.Emission != null)
        {
            material.SetTexture("_EmissiveColorMap", group.Emission);
            material.EnableKeyword("_EMISSION");
        }

        if (group.DetailMask != null)
            material.SetTexture("_DetailMask", group.DetailMask);

        if (group.DetailAlbedo != null)
            material.SetTexture("_DetailAlbedoMap", group.DetailAlbedo);

        if (group.DetailNormal != null)
            material.SetTexture("_DetailNormalMap", group.DetailNormal);
    }
}

public class TextureToMaterialBatchProcessor
{
    [MenuItem("Assets/Create Materials from Textures (Auto)", true)]
    private static bool ValidateCreateMaterials()
    {
        var selection = Selection.objects;
        return selection.Length > 0 && selection.All(o => o is Texture2D || (o is DefaultAsset && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(o))));
    }

    [MenuItem("Assets/Create Materials from Textures (Auto)")]
    private static void CreateMaterialsFromSelection()
    {
        var selection = Selection.objects;
        var textures = new List<Texture2D>();

        foreach (var obj in selection)
        {
            if (obj is Texture2D tex)
                textures.Add(tex);
            else if (obj is DefaultAsset folder && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder)))
            {
                var path = AssetDatabase.GetAssetPath(folder);
                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                foreach (var guid in guids)
                {
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                    if (t != null) textures.Add(t);
                }
            }
        }

        if (textures.Count == 0) return;

        var groups = new Dictionary<string, TextureGroup>();
        foreach (var tex in textures)
        {
            string baseName = GetBaseNameStatic(tex.name);
            if (!groups.TryGetValue(baseName, out var group))
            {
                group = new TextureGroup { BaseName = baseName };
                groups[baseName] = group;
            }
            AssignTextureToGroupStatic(group, tex);
        }

        var shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return;

        string outputFolder = "Assets/Materials/Generated";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(outputFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Generated");

        foreach (var group in groups.Values.Where(g => g.TextureCount > 0))
        {
            var mat = new Material(shader) { name = group.BaseName };
            ApplyTexturesToMaterialStatic(mat, group);
            var path = $"{outputFolder}/{group.BaseName}.mat";
            AssetDatabase.CreateAsset(mat, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created {groups.Count} materials in {outputFolder}");
    }

    private static string GetBaseNameStatic(string textureName)
    {
        var suffixes = new[] { "_albedo", "_basecolor", "_base", "_color", "_diffuse", "_diff", "_normal", "_norm", "_nrm", "_metallic", "_metal", "_mtl", "_smoothness", "_smooth", "_roughness", "_rough", "_gloss", "_occlusion", "_occl", "_ao", "_emission", "_emissive", "_emit", "_detailmask", "_detail_mask", "_detailalbedo", "_detail_albedo", "_detailnormal", "_detail_normal" };
        string lower = textureName.ToLower();
        foreach (var suffix in suffixes)
        {
            if (lower.EndsWith(suffix))
                return textureName.Substring(0, textureName.Length - suffix.Length).TrimEnd('_', '-', ' ');
        }
        return textureName;
    }

    private static void AssignTextureToGroupStatic(TextureGroup group, Texture2D tex)
    {
        string lower = tex.name.ToLower();
        if (lower.EndsWith("_albedo") || lower.EndsWith("_basecolor") || lower.EndsWith("_base") || lower.EndsWith("_color") || lower.EndsWith("_diffuse") || lower.EndsWith("_diff"))
            group.Albedo = tex;
        else if (lower.EndsWith("_normal") || lower.EndsWith("_norm") || lower.EndsWith("_nrm"))
            group.Normal = tex;
        else if (lower.EndsWith("_metallic") || lower.EndsWith("_metal") || lower.EndsWith("_mtl"))
            group.Metallic = tex;
        else if (lower.EndsWith("_smoothness") || lower.EndsWith("_smooth") || lower.EndsWith("_roughness") || lower.EndsWith("_rough") || lower.EndsWith("_gloss"))
            group.Smoothness = tex;
        else if (lower.EndsWith("_occlusion") || lower.EndsWith("_occl") || lower.EndsWith("_ao"))
            group.Occlusion = tex;
        else if (lower.EndsWith("_emission") || lower.EndsWith("_emissive") || lower.EndsWith("_emit"))
            group.Emission = tex;
        else if (lower.EndsWith("_detailmask") || lower.EndsWith("_detail_mask"))
            group.DetailMask = tex;
        else if (lower.EndsWith("_detailalbedo") || lower.EndsWith("_detail_albedo"))
            group.DetailAlbedo = tex;
        else if (lower.EndsWith("_detailnormal") || lower.EndsWith("_detail_normal"))
            group.DetailNormal = tex;
        else if (group.Albedo == null)
            group.Albedo = tex;
    }

    private static void ApplyTexturesToMaterialStatic(Material material, TextureGroup group)
    {
        if (group.Albedo != null) { material.SetTexture("_BaseColorMap", group.Albedo); material.SetColor("_BaseColor", Color.white); if (material.HasProperty("_SurfaceType")) material.SetFloat("_SurfaceType", 0); }
        if (group.Normal != null) { material.SetTexture("_NormalMap", group.Normal); var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(group.Normal)) as TextureImporter; if (importer != null && importer.textureType != TextureImporterType.NormalMap) { importer.textureType = TextureImporterType.NormalMap; importer.SaveAndReimport(); } }
        if (group.Metallic != null) material.SetTexture("_MetallicMap", group.Metallic);
        if (group.Smoothness != null) { material.SetTexture("_SmoothnessMap", group.Smoothness); var lower = group.Smoothness.name.ToLower(); if (lower.Contains("_rough") || lower.Contains("_roughness")) if (material.HasProperty("_SmoothnessMapChannel")) material.SetFloat("_SmoothnessMapChannel", 1); }
        if (group.Occlusion != null) material.SetTexture("_OcclusionMap", group.Occlusion);
        if (group.Emission != null) { material.SetTexture("_EmissiveColorMap", group.Emission); material.EnableKeyword("_EMISSION"); }
        if (group.DetailMask != null) material.SetTexture("_DetailMask", group.DetailMask);
        if (group.DetailAlbedo != null) material.SetTexture("_DetailAlbedoMap", group.DetailAlbedo);
        if (group.DetailNormal != null) material.SetTexture("_DetailNormalMap", group.DetailNormal);
    }

    private class TextureGroup
    {
        public string BaseName;
        public Texture2D Albedo;
        public Texture2D Normal;
        public Texture2D Metallic;
        public Texture2D Smoothness;
        public Texture2D Occlusion;
        public Texture2D Emission;
        public Texture2D DetailMask;
        public Texture2D DetailAlbedo;
        public Texture2D DetailNormal;
        public int TextureCount => new[] { Albedo, Normal, Metallic, Smoothness, Occlusion, Emission, DetailMask, DetailAlbedo, DetailNormal }.Count(t => t != null);
    }
}