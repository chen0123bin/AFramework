using System;

namespace LWStep
{
    /// <summary>
    /// 步骤连线（有向边）
    /// </summary>
    public class StepEdge
    {
        public string FromId { get; private set; }
        public string ToId { get; private set; }
        public int Priority { get; private set; }
        public string Condition { get; private set; }

        /// <summary>
        /// 创建连线
        /// </summary>
        public StepEdge(string fromId, string toId, int priority, string condition)
        {
            FromId = fromId;
            ToId = toId;
            Priority = priority;
            Condition = condition;
        }

        public bool IsConditionMatched(StepContext context)
        {
            if (string.IsNullOrEmpty(Condition))
            {
                return true;
            }
            if (context == null)
            {
                return false;
            }
            string trimmed = Condition.Trim();

            int notEqualIndex = trimmed.IndexOf("!=", StringComparison.Ordinal);
            if (notEqualIndex >= 0)
            {
                string key = trimmed.Substring(0, notEqualIndex).Trim();
                string expected = trimmed.Substring(notEqualIndex + 2).Trim();
                object actual;
                if (!context.TryGetRawValue(key, out actual))
                {
                    return false;
                }
                return !IsValueMatched(actual, expected);
            }

            int equalIndex = trimmed.IndexOf("==", StringComparison.Ordinal);
            if (equalIndex >= 0)
            {
                string key = trimmed.Substring(0, equalIndex).Trim();
                string expected = trimmed.Substring(equalIndex + 2).Trim();
                object actual;
                if (!context.TryGetRawValue(key, out actual))
                {
                    return false;
                }
                return IsValueMatched(actual, expected);
            }

            object rawValue;
            if (!context.TryGetRawValue(trimmed, out rawValue))
            {
                return false;
            }
            return IsTruthy(rawValue);
        }

        /// <summary>
        /// 检查值是否匹配
        /// </summary>
        /// <param name="actual">实际值</param>
        /// <param name="expected">预期值连线中设置的值</param>
        /// <returns></returns>
        private bool IsValueMatched(object actual, string expected)
        {
            if (actual == null)
            {
                return string.IsNullOrEmpty(expected) || string.Equals(expected, "null", StringComparison.OrdinalIgnoreCase);
            }

            bool expectedBool;
            if (bool.TryParse(expected, out expectedBool))
            {
                return string.Equals(actual.ToString(), expected, StringComparison.OrdinalIgnoreCase);
            }

            double actualNumber;
            double expectedNumber;
            if (TryGetNumber(actual, out actualNumber) && double.TryParse(expected, out expectedNumber))
            {
                return Math.Abs(actualNumber - expectedNumber) < 0.0001d;
            }

            return string.Equals(actual.ToString(), expected, StringComparison.Ordinal);
        }

        private bool TryGetNumber(object value, out double number)
        {
            number = 0d;
            if (value is int)
            {
                number = (int)value;
                return true;
            }
            if (value is long)
            {
                number = (long)value;
                return true;
            }
            if (value is float)
            {
                number = (float)value;
                return true;
            }
            if (value is double)
            {
                number = (double)value;
                return true;
            }
            if (value is decimal)
            {
                number = (double)(decimal)value;
                return true;
            }
            return false;
        }

        private bool IsTruthy(object value)
        {
            if (value == null)
            {
                return false;
            }
            if (value is bool)
            {
                return (bool)value;
            }
            double number;
            if (TryGetNumber(value, out number))
            {
                return Math.Abs(number) > 0.0001d;
            }
            string text = value.ToString();
            return !string.IsNullOrEmpty(text);
        }

    }
}
