using System;

using BestMQTT.Packets;

using UnityEngine.UI;

namespace BestMQTT.Examples
{
    public static class UIExtensions
    {
        public static string GetValue(this InputField field)
        {
            var value = field.text;
            return value;
        }

        public static string GetValue(this InputField field, string defaultValue)
        {
            var value = field.text;

            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        public static int GetIntValue(this InputField field, int defaultValue)
        {
            return int.TryParse(field.text, out var value) ? value : defaultValue;
        }

        public static bool GetBoolValue(this Toggle toggle)
        {
            return toggle.isOn;
        }

        public static QoSLevels GetQoS(this Dropdown dropdown)
        {
            return ((QoSLevels[])Enum.GetValues(typeof(QoSLevels)))[dropdown.value];
        }
    }
}
