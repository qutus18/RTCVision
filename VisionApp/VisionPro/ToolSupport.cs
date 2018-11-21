namespace VisionApp
{
    public static class ToolSupport
    {
        /// <summary>
        /// Sắp xếp và trả về mảng theo chiều giảm dần x,y
        /// </summary>
        /// <param name="inputPatternsList"></param>
        /// <returns></returns>
        public static patternObject[] SortPatterns(patternObject[] inputPatternsList)
        {
            for (int i = 0; i < inputPatternsList.Length - 1; i++)
            {
                for (int j = i; j < inputPatternsList.Length; j++)
                {
                    var sumI = inputPatternsList[i].X + inputPatternsList[i].Y;
                    var sumJ = inputPatternsList[j].X + inputPatternsList[j].Y;
                    if (sumJ > sumI)
                    {
                        patternObject temp = inputPatternsList[j];
                        inputPatternsList[j] = inputPatternsList[i];
                        inputPatternsList[i] = temp;
                    }
                }
            }
            return inputPatternsList;
        }
    }
}
