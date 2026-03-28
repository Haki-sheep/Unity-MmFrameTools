using UnityEngine;
using MieMieFrameTools.Archive;

public class ColorToolsDemo : MonoBehaviour
{
    private void Start()
    {
        // 1. Color 转十六进制字符串
        TestToHex();

        // 2. 十六进制字符串解析为 Color
        TestParseHex();

        // 3. Color 与 归一化数组 互转
        TestArrayConvert();

        // 4. 颜色线性渐变混合
        TestLerp();

        // 5. 判断颜色明暗/获取灰度值
        TestDarkAndGrayscale();

        // 6. 随机生成颜色
        TestRandomColor();

        // 7. Color 与 RGBA 整数 互转
        TestIntConvert();
    }

    #region 1. Color 转十六进制字符串
    private void TestToHex()
    {
        Color red = Color.red;
        // 带 # 号（默认）
        string hexWithHash = ColorTools.ToHex(red);
        // 不带 # 号
        string hexWithoutHash = ColorTools.ToHex(red, false);

        Debug.Log($"【ToHex】红色转十六进制：\n带#：{hexWithHash} | 不带#：{hexWithoutHash}");
        // 输出：【ToHex】红色转十六进制：带#：#FF0000 | 不带#：FF0000
    }
    #endregion

    #region 2. 十六进制字符串解析为 Color
    private void TestParseHex()
    {
        // 支持带 # 或不带 # 的格式
        Color color1 = ColorTools.ParseHex("#00FF00");
        Color color2 = ColorTools.ParseHex("0000FF");
        // 非法格式会返回白色
        Color color3 = ColorTools.ParseHex("123");

        Debug.Log($"【ParseHex】解析结果：\n#00FF00 → {color1} | 0000FF → {color2} | 非法值 → {color3}");
        // 输出：【ParseHex】解析结果：#00FF00 → RGBA(0.000, 1.000, 0.000, 1.000) | 0000FF → RGBA(0.000, 0.000, 1.000, 1.000) | 非法值 → RGBA(1.000, 1.000, 1.000, 1.000)
    }
    #endregion

    #region 3. Color 与 归一化数组 互转
    private void TestArrayConvert()
    {
        Color cyan = Color.cyan;
        // Color 转数组（RGBA 归一化值 0~1）
        float[] colorArray = ColorTools.ToArray(cyan);
        // 数组转回 Color（数组长度不足时自动补默认值）
        Color fromArray = ColorTools.FromArray(colorArray);
        // 测试短数组（仅传 RGB）
        Color fromShortArray = ColorTools.FromArray(new float[] { 0.5f, 0.5f, 0.5f });

        Debug.Log($"【ArrayConvert】青色转数组：[{string.Join(", ", colorArray)}] | 数组转回：{fromArray}");
        Debug.Log($"【ArrayConvert】短数组还原：{fromShortArray}（Alpha 自动补 1）");
        // 输出：【ArrayConvert】青色转数组：[0, 1, 1, 1] | 数组转回：RGBA(0.000, 1.000, 1.000, 1.000)
        //       【ArrayConvert】短数组还原：RGBA(0.500, 0.500, 0.500, 1.000)（Alpha 自动补 1）
    }
    #endregion

    #region 4. 颜色线性渐变混合
    private void TestLerp()
    {
        Color from = Color.yellow;
        Color to = Color.magenta;
        // t=0 → 完全取 from 色；t=1 → 完全取 to 色；t=0.5 → 混合色
        Color lerp0 = ColorTools.Lerp(from, to, 0f);
        Color lerp05 = ColorTools.Lerp(from, to, 0.5f);
        Color lerp1 = ColorTools.Lerp(from, to, 1f);
        // t 超出 0~1 会被自动钳制
        Color lerp15 = ColorTools.Lerp(from, to, 1.5f);

        Debug.Log($"【Lerp】黄色→品红混合：\nt=0 → {lerp0} | t=0.5 → {lerp05} | t=1 → {lerp1} | t=1.5（钳制）→ {lerp15}");
        // 输出：【Lerp】黄色→品红混合：t=0 → RGBA(1.000, 1.000, 0.000, 1.000) | t=0.5 → RGBA(1.000, 0.500, 0.500, 1.000) | t=1 → RGBA(1.000, 0.000, 1.000, 1.000) | t=1.5（钳制）→ RGBA(1.000, 0.000, 1.000, 1.000)
    }
    #endregion

    #region 5. 判断颜色明暗/获取灰度值
    private void TestDarkAndGrayscale()
    {
        Color black = Color.black;
        Color white = Color.white;
        Color gray = Color.gray;

        bool isBlackDark = ColorTools.IsDark(black);
        bool isWhiteDark = ColorTools.IsDark(white);
        float grayValue = ColorTools.Grayscale(gray);

        Debug.Log($"【Dark&Grayscale】黑色是否偏暗：{isBlackDark} | 白色是否偏暗：{isWhiteDark} | 灰色灰度值：{grayValue:F2}");
        // 输出：【Dark&Grayscale】黑色是否偏暗：True | 白色是否偏暗：False | 灰色灰度值：0.50
    }
    #endregion

    #region 6. 随机生成颜色
    private void TestRandomColor()
    {
        // 不透明随机色
        Color randomOpaque = ColorTools.Random();
        // 带 Alpha 通道的随机色
        Color randomWithAlpha = ColorTools.RandomWithAlpha();

        Debug.Log($"【RandomColor】不透明随机色：{randomOpaque} | 带Alpha随机色：{randomWithAlpha}");
        // 输出示例：【RandomColor】不透明随机色：RGBA(0.234, 0.876, 0.123, 1.000) | 带Alpha随机色：RGBA(0.456, 0.789, 0.345, 0.678)
    }
    #endregion

    #region 7. Color 与 RGBA 整数 互转
    private void TestIntConvert()
    {
        Color green = new Color(0, 1, 0, 1); // 纯绿色（不透明）
        // Color 转 RGBA 整数（格式：0xRRGGBBAA）
        int colorInt = ColorTools.ToInt(green);
        // 整数转回 Color
        Color fromInt = ColorTools.FromInt(colorInt);

        Debug.Log($"【IntConvert】绿色转整数：0x{colorInt:X8} | 整数转回：{fromInt}");
        // 输出：【IntConvert】绿色转整数：0x00FF00FF | 整数转回：RGBA(0.000, 1.000, 0.000, 1.000)
    }
    #endregion
}