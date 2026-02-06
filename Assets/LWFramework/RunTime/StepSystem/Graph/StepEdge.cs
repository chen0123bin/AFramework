using System;
using System.Globalization;

namespace LWStep
{
    /// <summary>
    /// 比较类型（支持多种阈值判断）
    /// </summary>
    public enum ComparisonType
    {
        EqualTo,
        NotEqualTo,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }

    /// <summary>
    /// 步骤连线（有向边）
    /// </summary>
    public class StepEdge
    {
        public string FromId { get; private set; }
        public string ToId { get; private set; }
        public int Priority { get; private set; }

        public string ConditionKey { get; private set; }
        public ComparisonType ConditionComparisonType { get; private set; }
        public string ConditionValue { get; private set; }

        /// <summary>
        /// 创建连线（结构化条件：Key + 比较类型 + Value）
        /// </summary>
        public StepEdge(string fromId, string toId, int priority, string conditionKey, ComparisonType comparisonType, string conditionValue)
        {
            FromId = fromId;
            ToId = toId;
            Priority = priority;
            ConditionKey = conditionKey ?? string.Empty;
            ConditionComparisonType = comparisonType;
            ConditionValue = conditionValue ?? string.Empty;
        }

        /// <summary>
        /// 判断连线条件是否满足
        /// </summary>
        public bool IsConditionMatched(StepContext context)
        {
            if (string.IsNullOrEmpty(ConditionKey))
            {
                return true;
            }
            if (context == null)
            {
                return false;
            }
            //获取原始数据
            object rawValue;
            if (!context.TryGetRawValue(ConditionKey, out rawValue))
            {
                return false;
            }


            if (ConditionComparisonType == ComparisonType.EqualTo)
            {
                return IsValueMatched(rawValue, ConditionValue);
            }
            if (ConditionComparisonType == ComparisonType.NotEqualTo)
            {
                return !IsValueMatched(rawValue, ConditionValue);
            }

            double actualNumber;
            if (!TryGetNumber(rawValue, out actualNumber))
            {
                return false;
            }
            double expectedNumber;
            if (!double.TryParse(ConditionValue, NumberStyles.Float, CultureInfo.InvariantCulture, out expectedNumber))
            {
                return false;
            }

            if (ConditionComparisonType == ComparisonType.GreaterThan)
            {
                return actualNumber > expectedNumber;
            }
            if (ConditionComparisonType == ComparisonType.GreaterThanOrEqual)
            {
                return actualNumber >= expectedNumber;
            }
            if (ConditionComparisonType == ComparisonType.LessThan)
            {
                return actualNumber < expectedNumber;
            }
            if (ConditionComparisonType == ComparisonType.LessThanOrEqual)
            {
                return actualNumber <= expectedNumber;
            }
            return false;
        }

        /// <summary>
        /// 检查值是否匹配
        /// </summary>
        /// <param name="actual">上下文中实际值</param>
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
            if (TryGetNumber(actual, out actualNumber) && double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out expectedNumber))
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


    }
}
