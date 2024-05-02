public static class Utils
{
    public static int ConvertToPercentage(int current, int maximum)
    {
        return Godot.Mathf.RoundToInt(current * 100 / maximum);
    }
}
