using System;
using Godot;

public static class Utils
{
    public static int ConvertToPercentage(int current, int maximum)
    {
        return Godot.Mathf.RoundToInt(current * 100 / maximum);
    }

    public static class TimerUtils
    {
        public static Timer CreateTimer(Action action, Node parentNode, float waitTime)
        {
            Timer timer = new() { WaitTime = waitTime };
            timer.Timeout += () => OnTimerTimeout(timer, action, parentNode);
            parentNode.AddChild(timer);
            timer.Start();
            return timer;
        }

        public static void OnTimerTimeout(Timer timer, Action action, Node node)
        {
            action();
            timer.Stop();
            node.RemoveChild(timer);
            timer.QueueFree();
        }
    }
}
