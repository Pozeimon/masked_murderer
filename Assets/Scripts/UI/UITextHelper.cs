using System;
using System.Reflection;
using UnityEngine;

namespace TheTear.UI
{
    public static class UITextHelper
    {
        public static void SetText(Component target, string text)
        {
            if (target == null)
            {
                return;
            }
            Type type = target.GetType();
            PropertyInfo prop = type.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(string))
            {
                prop.SetValue(target, text, null);
            }
        }

        public static string GetText(Component target)
        {
            if (target == null)
            {
                return string.Empty;
            }
            Type type = target.GetType();
            PropertyInfo prop = type.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(string))
            {
                return prop.GetValue(target, null) as string;
            }
            return string.Empty;
        }
    }
}
