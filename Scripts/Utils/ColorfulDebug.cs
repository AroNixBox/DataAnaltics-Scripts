using UnityEngine;
using System.Collections.Generic;

namespace Utils {
    public static class ColorfulDebug
    {
        public static void Log(object message, string color)
        {
            Debug.Log("<color=" + color + ">" + message + "</color>");
        }

        public static void LogWithRandomColor(List<string> messages)
        {
            var colors = new List<string> { "red", "green", "blue", "yellow", "magenta", "cyan" };

            foreach (var message in messages) {
                if (colors.Count < 1) {
                    colors = new List<string> { "red", "green", "blue", "yellow", "magenta", "cyan" };
                }

                int index = Random.Range(0, colors.Count);
                string selectedColor = colors[index];

                colors.RemoveAt(index);

                Log(message, selectedColor);
            }
        }
    }
}