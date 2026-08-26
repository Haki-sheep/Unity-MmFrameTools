namespace MieMieUIFrameWork.UI.FloatingText
{
    /// <summary>
    /// 跳字图集格子索引约定
    /// </summary>
    public static class FloatingTextAtlasIds
    {
        public const int Digit0 = 0;
        public const int Crit = 10;
        public const int LetterA = 11;
        public const int CellCount = 37;
        public const int Columns = 16;
        public const int Rows = 3;
        public const int MaxGlyphsPerText = 9;

        /// <summary>
        /// 字符转图集索引 失败返回 -1
        /// </summary>
        public static int CharToIndex(char c)
        {
            if (c >= '0' && c <= '9') return Digit0 + (c - '0');
            if (c >= 'A' && c <= 'Z') return LetterA + (c - 'A');
            if (c >= 'a' && c <= 'z') return LetterA + (c - 'a');
            if (c == '*') return Crit;
            return -1;
        }
    }
}
