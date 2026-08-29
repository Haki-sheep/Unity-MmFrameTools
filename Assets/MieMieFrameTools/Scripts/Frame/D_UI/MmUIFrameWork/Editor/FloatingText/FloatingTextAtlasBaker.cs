#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using MieMieUIFrameWork.UI.FloatingText;
using MieMieUITools.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>
/// 跳字图集与 CharMap 烘焙
/// 方案 Font / TMP 分离 字符 数字/英文/中文 严格分槽 不互相回退抢源
/// </summary>
public static class FloatingTextAtlasBaker
{
    public static string RootFolder => PackagePaths.FloatingTextRoot;
    public static string GeneratedFolder => RootFolder + "/Art/Generated";
    public static string SourceFolder => RootFolder + "/Art/Source";
    public static string AtlasPath => GeneratedFolder + "/FloatingTextAtlas.png";
    public static string MaterialPath => GeneratedFolder + "/FloatingText.mat";
    public static string CharMapPath => GeneratedFolder + "/FloatingTextCharMap.asset";
    public static string PrefabPath => GeneratedFolder + "/FloatingTextWorld.prefab";
    public static string ManagerPrefabPath => GeneratedFolder + "/FloatingTextManager.prefab";
    public const string CritGuid = "49dcf50e28fe3dd40a0c3a030f128c66";

    public const string DefaultCharset =
        "0123456789*ABCDEFGHIJKLMNOPQRSTUVWXYZ暴击伤害治疗未命中完美眩晕击杀";

    public enum EBakeSourceMode
    {
        UnityFont = 0,
        TmpFontAsset = 1
    }

    public enum ECharKind
    {
        Digit = 0,
        English = 1,
        Chinese = 2
    }

    public class BakeRequest
    {
        public EBakeSourceMode SourceMode = EBakeSourceMode.TmpFontAsset;

        public Font DigitFont;
        public Font EnglishFont;
        public Font ChineseFont;

        public TMP_FontAsset DigitTmpFont;
        public TMP_FontAsset EnglishTmpFont;
        public TMP_FontAsset ChineseTmpFont;

        public int FontSize = 48;
        public int CellSize = 64;
        public int Columns = 16;
        public string Charset = DefaultCharset;
        public Texture2D CritOverride;
        public bool AlsoRefreshPrefab = true;
    }

    public class BakeResult
    {
        public Texture2D Atlas;
        public Material Material;
        public FloatingTextCharMap CharMap;
        public string Message;
    }

    public static ECharKind ClassifyChar(char c)
    {
        if (c >= '0' && c <= '9') return ECharKind.Digit;
        if (c == '+' || c == '-' || c == '.' || c == '%' || c == '*') return ECharKind.Digit;
        if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) return ECharKind.English;
        return ECharKind.Chinese;
    }

    public static BakeResult Bake(BakeRequest request)
    {
        EnsureFolder(GeneratedFolder);
        EnsureFolder(SourceFolder);

        string charset = BuildUniqueCharset(request.Charset);
        if (string.IsNullOrEmpty(charset))
        {
            return new BakeResult { Message = "字符表为空" };
        }

        int count = charset.Length;
        int columns = Mathf.Max(1, request.Columns);
        int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
        int cell = Mathf.Max(16, request.CellSize);
        int critIndex = charset.IndexOf('*');
        if (critIndex < 0) critIndex = 0;

        Texture2D critTex = request.CritOverride != null ? request.CritOverride : LoadCritTexture();
        Texture2D atlas = new Texture2D(columns * cell, rows * cell, TextureFormat.RGBA32, false);
        Clear(atlas, new Color(0f, 0f, 0f, 0f));

        bool useTmp = request.SourceMode == EBakeSourceMode.TmpFontAsset;
        Texture2D digitFontTex = null;
        Texture2D englishFontTex = null;
        Texture2D chineseFontTex = null;

        if (!useTmp)
        {
            digitFontTex = PrepareUnityFontTex(request.DigitFont, charset, request.FontSize);
            englishFontTex = PrepareUnityFontTex(request.EnglishFont, charset, request.FontSize);
            chineseFontTex = PrepareUnityFontTex(request.ChineseFont, charset, request.FontSize);
        }
        else
        {
            PrepareTmpFont(request.DigitTmpFont, FilterCharset(charset, ECharKind.Digit));
            PrepareTmpFont(request.EnglishTmpFont, FilterCharset(charset, ECharKind.English));
            PrepareTmpFont(request.ChineseTmpFont, FilterCharset(charset, ECharKind.Chinese));
        }

        int digitHit = 0, englishHit = 0, chineseHit = 0, fallbackHit = 0, missSlot = 0;

        for (int i = 0; i < count; i++)
        {
            char c = charset[i];
            GetCellOrigin(i, columns, rows, cell, out int ox, out int oy);

            if (c == '*' && critTex != null)
            {
                EnsureTextureReadableAsset(critTex);
                BlitScaled(critTex, atlas, ox, oy, cell, cell);
                continue;
            }

            ECharKind kind = ClassifyChar(c);
            bool drawn = false;

            if (useTmp)
            {
                TMP_FontAsset tmp = GetTmpSlot(request, kind);
                if (tmp != null)
                {
                    drawn = TryBlitFromTmpFont(tmp, c, atlas, ox, oy, cell);
                    if (drawn) CountHit(kind, ref digitHit, ref englishHit, ref chineseHit);
                }
                else
                {
                    missSlot++;
                }
            }
            else
            {
                Font font = GetFontSlot(request, kind);
                Texture2D fontTex = GetFontTexSlot(kind, digitFontTex, englishFontTex, chineseFontTex);
                if (font != null && fontTex != null)
                {
                    drawn = TryBlitFromFont(font, fontTex, c, request.FontSize, atlas, ox, oy, cell);
                    if (drawn) CountHit(kind, ref digitHit, ref englishHit, ref chineseHit);
                }
                else
                {
                    missSlot++;
                }
            }

            // 严格分槽 槽位没画上只回退点阵/占位 绝不拿别的语言槽顶替
            if (!drawn)
            {
                fallbackHit++;
                if (c >= '0' && c <= '9') DrawDigit(atlas, ox, oy, cell, c - '0');
                else if (c >= 'A' && c <= 'Z') DrawLetter(atlas, ox, oy, cell, c - 'A');
                else if (c >= 'a' && c <= 'z') DrawLetter(atlas, ox, oy, cell, c - 'a');
                else if (c == '*') DrawStar(atlas, ox, oy, cell);
                else DrawFallbackGlyph(atlas, ox, oy, cell, c);
            }
        }

        DestroyTemp(digitFontTex);
        DestroyTemp(englishFontTex);
        DestroyTemp(chineseFontTex);

        atlas.Apply();
        WritePng(AtlasPath, atlas);
        Object.DestroyImmediate(atlas);
        ConfigureAtlasImporter(AtlasPath);

        Texture2D atlasAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        FloatingTextCharMap charMap = AssetDatabase.LoadAssetAtPath<FloatingTextCharMap>(CharMapPath);
        if (charMap == null)
        {
            charMap = ScriptableObject.CreateInstance<FloatingTextCharMap>();
            AssetDatabase.CreateAsset(charMap, CharMapPath);
        }

        charMap.EditorSetBakedData(columns, rows, cell, charset, critIndex);
        EditorUtility.SetDirty(charMap);

        Material mat = CreateOrUpdateMaterial(atlasAsset, columns, rows);
        if (request.AlsoRefreshPrefab)
        {
            RefreshPrefab(atlasAsset, mat, charMap);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string modeName = useTmp ? "TMP" : "Font";
        return new BakeResult
        {
            Atlas = atlasAsset,
            Material = mat,
            CharMap = charMap,
            Message =
                $"[{modeName}] 字符={count} {columns}x{rows} cell={cell} | " +
                $"数字命中={digitHit} 英文命中={englishHit} 中文命中={chineseHit} 回退={fallbackHit} 缺槽={missSlot}"
        };
    }

    private static void CountHit(ECharKind kind, ref int digit, ref int english, ref int chinese)
    {
        if (kind == ECharKind.Digit) digit++;
        else if (kind == ECharKind.English) english++;
        else chinese++;
    }

    private static Font GetFontSlot(BakeRequest request, ECharKind kind)
    {
        if (kind == ECharKind.Digit) return request.DigitFont;
        if (kind == ECharKind.English) return request.EnglishFont;
        return request.ChineseFont;
    }

    private static TMP_FontAsset GetTmpSlot(BakeRequest request, ECharKind kind)
    {
        if (kind == ECharKind.Digit) return request.DigitTmpFont;
        if (kind == ECharKind.English) return request.EnglishTmpFont;
        return request.ChineseTmpFont;
    }

    private static Texture2D GetFontTexSlot(
        ECharKind kind,
        Texture2D digit,
        Texture2D english,
        Texture2D chinese)
    {
        if (kind == ECharKind.Digit) return digit;
        if (kind == ECharKind.English) return english;
        return chinese;
    }

    private static string FilterCharset(string charset, ECharKind kind)
    {
        var sb = new StringBuilder(charset.Length);
        for (int i = 0; i < charset.Length; i++)
        {
            if (ClassifyChar(charset[i]) == kind) sb.Append(charset[i]);
        }

        return sb.ToString();
    }

    private static Texture2D PrepareUnityFontTex(Font font, string charset, int fontSize)
    {
        if (font == null) return null;
        font.RequestCharactersInTexture(charset, fontSize, FontStyle.Normal);
        return MakeReadable(font.material.mainTexture);
    }

    private static void DestroyTemp(Texture2D tex)
    {
        if (tex != null) Object.DestroyImmediate(tex);
    }

    private static string BuildUniqueCharset(string raw)
    {
        if (string.IsNullOrEmpty(raw)) raw = DefaultCharset;
        var sb = new StringBuilder(raw.Length);
        var seen = new HashSet<char>();
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (char.IsWhiteSpace(c)) continue;
            if (seen.Add(c)) sb.Append(c);
        }

        return sb.ToString();
    }

    private static void PrepareTmpFont(TMP_FontAsset tmpFont, string charset)
    {
        if (tmpFont == null || string.IsNullOrEmpty(charset)) return;
        tmpFont.TryAddCharacters(charset, out _);
    }

    private static bool TryBlitFromTmpFont(
        TMP_FontAsset tmpFont,
        char c,
        Texture2D atlas,
        int ox,
        int oy,
        int cell)
    {
        if (tmpFont == null) return false;
        if (!tmpFont.characterLookupTable.TryGetValue(c, out TMP_Character character) || character == null)
        {
            return false;
        }

        Glyph glyph = character.glyph;
        if (glyph == null) return false;
        GlyphRect rect = glyph.glyphRect;
        if (rect.width <= 0 || rect.height <= 0) return false;

        Texture[] atlases = tmpFont.atlasTextures;
        if (atlases == null || atlases.Length == 0) return false;
        int atlasIndex = Mathf.Clamp(glyph.atlasIndex, 0, atlases.Length - 1);
        Texture srcTex = atlases[atlasIndex];
        Texture2D srcAtlas = MakeReadable(srcTex);
        if (srcAtlas == null) return false;

        int x0 = Mathf.Clamp(rect.x, 0, srcAtlas.width - 1);
        int y0 = Mathf.Clamp(rect.y, 0, srcAtlas.height - 1);
        int gw = Mathf.Clamp(rect.width, 1, srcAtlas.width - x0);
        int gh = Mathf.Clamp(rect.height, 1, srcAtlas.height - y0);
        Color[] src = srcAtlas.GetPixels(x0, y0, gw, gh);
        if (!ReferenceEquals(srcAtlas, srcTex)) Object.DestroyImmediate(srcAtlas);

        float pad = cell * 0.12f;
        float fit = Mathf.Min((cell - pad * 2f) / gw, (cell - pad * 2f) / gh);
        int dw = Mathf.Max(1, Mathf.RoundToInt(gw * fit));
        int dh = Mathf.Max(1, Mathf.RoundToInt(gh * fit));
        int dx = ox + (cell - dw) / 2;
        int dy = oy + (cell - dh) / 2;

        for (int y = 0; y < dh; y++)
        {
            int sy = Mathf.Clamp((y * gh) / dh, 0, gh - 1);
            for (int x = 0; x < dw; x++)
            {
                int sx = Mathf.Clamp((x * gw) / dw, 0, gw - 1);
                Color cPix = src[sy * gw + sx];
                float sdf = Mathf.Max(cPix.a, Mathf.Max(cPix.r, Mathf.Max(cPix.g, cPix.b)));
                float alpha = Mathf.Clamp01((sdf - 0.45f) / 0.2f);
                if (alpha < 0.05f) continue;
                atlas.SetPixel(dx + x, dy + y, new Color(1f, 1f, 1f, alpha));
            }
        }

        return true;
    }

    private static bool TryBlitFromFont(
        Font font,
        Texture2D fontTex,
        char c,
        int fontSize,
        Texture2D atlas,
        int ox,
        int oy,
        int cell)
    {
        if (!font.GetCharacterInfo(c, out CharacterInfo info, fontSize, FontStyle.Normal)) return false;

        float u0 = info.uvBottomLeft.x;
        float v0 = info.uvBottomLeft.y;
        float u1 = info.uvTopRight.x;
        float v1 = info.uvTopRight.y;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(u0 * fontTex.width), 0, fontTex.width - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(v0 * fontTex.height), 0, fontTex.height - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(u1 * fontTex.width), x0 + 1, fontTex.width);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(v1 * fontTex.height), y0 + 1, fontTex.height);
        int gw = x1 - x0;
        int gh = y1 - y0;
        if (gw <= 0 || gh <= 0) return false;

        Color[] src = fontTex.GetPixels(x0, y0, gw, gh);
        float pad = cell * 0.12f;
        float fit = Mathf.Min((cell - pad * 2f) / gw, (cell - pad * 2f) / gh);
        int dw = Mathf.Max(1, Mathf.RoundToInt(gw * fit));
        int dh = Mathf.Max(1, Mathf.RoundToInt(gh * fit));
        int dx = ox + (cell - dw) / 2;
        int dy = oy + (cell - dh) / 2;

        for (int y = 0; y < dh; y++)
        {
            int sy = Mathf.Clamp((y * gh) / dh, 0, gh - 1);
            for (int x = 0; x < dw; x++)
            {
                int sx = Mathf.Clamp((x * gw) / dw, 0, gw - 1);
                Color cPix = src[sy * gw + sx];
                if (cPix.a < 0.05f) continue;
                cPix.r = 1f;
                cPix.g = 1f;
                cPix.b = 1f;
                atlas.SetPixel(dx + x, dy + y, cPix);
            }
        }

        return true;
    }

    private static Texture2D MakeReadable(Texture src)
    {
        if (src == null) return null;
        if (src is Texture2D t2d && t2d.isReadable) return Object.Instantiate(t2d);

        RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        var readable = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        readable.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return readable;
    }

    private static Texture2D LoadCritTexture()
    {
        string path = AssetDatabase.GUIDToAssetPath(CritGuid);
        if (string.IsNullOrEmpty(path))
        {
            string[] found = AssetDatabase.FindAssets("LOL t:Texture2D", new[] { RootFolder, SourceFolder });
            if (found != null && found.Length > 0) path = AssetDatabase.GUIDToAssetPath(found[0]);
        }

        if (string.IsNullOrEmpty(path)) return null;
        EnsureTextureReadablePath(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void EnsureTextureReadableAsset(Texture2D tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        if (!string.IsNullOrEmpty(path)) EnsureTextureReadablePath(path);
    }

    private static void EnsureTextureReadablePath(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        if (importer.isReadable && importer.textureType == TextureImporterType.Default) return;
        importer.isReadable = true;
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    private static void GetCellOrigin(int cellIndex, int columns, int rows, int cell, out int ox, out int oy)
    {
        int col = cellIndex % columns;
        int row = cellIndex / columns;
        ox = col * cell;
        oy = (rows - 1 - row) * cell;
    }

    private static void Clear(Texture2D tex, Color color)
    {
        Color[] pixels = new Color[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
    }

    private static void BlitScaled(Texture2D src, Texture2D dst, int dx, int dy, int dw, int dh)
    {
        for (int y = 0; y < dh; y++)
        {
            for (int x = 0; x < dw; x++)
            {
                float u = (x + 0.5f) / dw;
                float v = (y + 0.5f) / dh;
                Color c = src.GetPixelBilinear(u, v);
                if (c.a < 0.05f) continue;
                dst.SetPixel(dx + x, dy + y, c);
            }
        }
    }

    private static void DrawDigit(Texture2D tex, int ox, int oy, int cell, int digit)
    {
        int[][] patterns =
        {
            new[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E },
            new[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E },
            new[] { 0x0E, 0x11, 0x01, 0x06, 0x08, 0x10, 0x1F },
            new[] { 0x0E, 0x11, 0x01, 0x06, 0x01, 0x11, 0x0E },
            new[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 },
            new[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E },
            new[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E },
            new[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 },
            new[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E },
            new[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C }
        };
        DrawPattern(tex, ox, oy, cell, patterns[Mathf.Clamp(digit, 0, 9)]);
    }

    private static void DrawLetter(Texture2D tex, int ox, int oy, int cell, int letterIndex)
    {
        int[][] patterns =
        {
            new[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
            new[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E },
            new[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E },
            new[] { 0x1E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1E },
            new[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F },
            new[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 },
            new[] { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0F },
            new[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
            new[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E },
            new[] { 0x01, 0x01, 0x01, 0x01, 0x11, 0x11, 0x0E },
            new[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
            new[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F },
            new[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 },
            new[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 },
            new[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
            new[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 },
            new[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D },
            new[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 },
            new[] { 0x0F, 0x10, 0x10, 0x0E, 0x01, 0x01, 0x1E },
            new[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
            new[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
            new[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 },
            new[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x1B, 0x11 },
            new[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 },
            new[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 },
            new[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F }
        };
        DrawPattern(tex, ox, oy, cell, patterns[Mathf.Clamp(letterIndex, 0, 25)]);
    }

    private static void DrawPattern(Texture2D tex, int ox, int oy, int cell, int[] pattern)
    {
        int scale = Mathf.Max(2, cell / 12);
        int digitW = 5 * scale;
        int digitH = 7 * scale;
        int startX = ox + (cell - digitW) / 2;
        int startY = oy + (cell - digitH) / 2;
        for (int row = 0; row < 7; row++)
        {
            int bits = pattern[row];
            for (int col = 0; col < 5; col++)
            {
                if ((bits & (1 << (4 - col))) == 0) continue;
                for (int py = 0; py < scale; py++)
                for (int px = 0; px < scale; px++)
                {
                    tex.SetPixel(startX + col * scale + px, startY + (6 - row) * scale + py, Color.white);
                }
            }
        }
    }

    private static void DrawStar(Texture2D tex, int ox, int oy, int cell)
    {
        for (int y = 0; y < cell; y++)
        for (int x = 0; x < cell; x++)
        {
            int dx = x - cell / 2;
            int dy = y - cell / 2;
            float ang = Mathf.Atan2(dy, dx);
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float star = cell * 0.28f + cell * 0.16f * Mathf.Cos(ang * 5f);
            if (r < star) tex.SetPixel(ox + x, oy + y, Color.white);
        }
    }

    private static void DrawFallbackGlyph(Texture2D tex, int ox, int oy, int cell, char c)
    {
        int pad = cell / 8;
        for (int x = pad; x < cell - pad; x++)
        {
            tex.SetPixel(ox + x, oy + pad, Color.white);
            tex.SetPixel(ox + x, oy + cell - pad - 1, Color.white);
        }

        for (int y = pad; y < cell - pad; y++)
        {
            tex.SetPixel(ox + pad, oy + y, Color.white);
            tex.SetPixel(ox + cell - pad - 1, oy + y, Color.white);
        }
    }

    private static void WritePng(string assetPath, Texture2D tex)
    {
        string abs = ToAbsolute(assetPath);
        File.WriteAllBytes(abs, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void ConfigureAtlasImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial(Texture2D atlas, int columns, int rows)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Shader shader = Shader.Find("MieMieUIFrameWork/FloatingTextInstanced");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        else
        {
            mat.shader = shader;
        }

        mat.enableInstancing = true;
        mat.mainTexture = atlas;
        mat.SetFloat("_AtlasColumns", columns);
        mat.SetFloat("_AtlasRows", rows);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void RefreshPrefab(Texture2D atlas, Material mat, FloatingTextCharMap charMap)
    {
        var go = new GameObject("FloatingTextWorld");
        var world = go.AddComponent<FloatingTextWorld>();
        world.EditorBindResources(atlas, mat, charMap, null);
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        var worldPrefab = AssetDatabase.LoadAssetAtPath<FloatingTextWorld>(PrefabPath);
        var managerGo = new GameObject("FloatingTextManager");
        var manager = managerGo.AddComponent<FloatingTextManager>();
        manager.EditorBindWorldPrefab(worldPrefab);
        PrefabUtility.SaveAsPrefabAsset(managerGo, ManagerPrefabPath);
        Object.DestroyImmediate(managerGo);
    }

    public static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string ToAbsolute(string assetPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
#endif
