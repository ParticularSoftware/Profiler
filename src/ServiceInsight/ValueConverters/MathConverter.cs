﻿namespace ServiceInsight.ValueConverters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Markup;

    public class MathConverter :
        MarkupExtension,
        IMultiValueConverter,
        IValueConverter
    {
        Dictionary<string, IExpression> _storedExpressions = new Dictionary<string, IExpression>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(new[] { value }, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var result = Parse(parameter.ToString()).Eval(values);
                if (targetType == typeof(decimal)) return result;
                if (targetType == typeof(string)) return result.ToString(CultureInfo.InvariantCulture);
                if (targetType == typeof(int)) return (int) result;
                if (targetType == typeof(double)) return (double) result;
                if (targetType == typeof(long)) return (long) result;
                throw new ArgumentException(string.Format("Unsupported target type {0}", targetType.FullName));
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        protected virtual void ProcessException(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        IExpression Parse(string s)
        {
            IExpression result;
            if (!_storedExpressions.TryGetValue(s, out result))
            {
                result = new Parser().Parse(s);
                _storedExpressions[s] = result;
            }

            return result;
        }

        interface IExpression
        {
            decimal Eval(object[] args);
        }

        class Constant : IExpression
        {
            decimal _value;

            public Constant(string text)
            {
                if (!decimal.TryParse(text, out _value))
                {
                    throw new ArgumentException(String.Format("'{0}' is not a valid number", text));
                }
            }

            public decimal Eval(object[] args)
            {
                return _value;
            }
        }

        class Variable : IExpression
        {
            int _index;

            public Variable(string text)
            {
                if (!int.TryParse(text, out _index) || _index < 0)
                {
                    throw new ArgumentException(String.Format("'{0}' is not a valid parameter index", text));
                }
            }

            public Variable(int n)
            {
                _index = n;
            }

            public decimal Eval(object[] args)
            {
                if (_index >= args.Length)
                {
                    throw new ArgumentException(String.Format("MathConverter: parameter index {0} is out of range. {1} parameter(s) supplied", _index, args.Length));
                }

                return System.Convert.ToDecimal(args[_index]);
            }
        }

        class BinaryOperation : IExpression
        {
            Func<decimal, decimal, decimal> _operation;
            IExpression _left;
            IExpression _right;

            public BinaryOperation(char operation, IExpression left, IExpression right)
            {
                _left = left;
                _right = right;
                switch (operation)
                {
                    case '+':
                        _operation = (a, b) => (a + b);
                        break;
                    case '-':
                        _operation = (a, b) => (a - b);
                        break;
                    case '*':
                        _operation = (a, b) => (a*b);
                        break;
                    case '/':
                        _operation = (a, b) => (a/b);
                        break;
                    default:
                        throw new ArgumentException("Invalid operation " + operation);
                }
            }

            public decimal Eval(object[] args)
            {
                return _operation(_left.Eval(args), _right.Eval(args));
            }
        }

        class Negate : IExpression
        {
            IExpression _param;

            public Negate(IExpression param)
            {
                _param = param;
            }

            public decimal Eval(object[] args)
            {
                return -_param.Eval(args);
            }
        }

        class Parser
        {
            string text;
            int pos;

            public IExpression Parse(string value)
            {
                try
                {
                    pos = 0;
                    text = value;
                    var result = ParseExpression();
                    RequireEndOfText();
                    return result;
                }
                catch (Exception ex)
                {
                    var msg = string.Format("MathConverter: error parsing expression '{0}'. {1} at position {2}", text, ex.Message, pos);
                    throw new ArgumentException(msg, ex);
                }
            }

            IExpression ParseExpression()
            {
                IExpression left = ParseTerm();

                while (true)
                {
                    if (pos >= text.Length) return left;

                    var c = text[pos];

                    if (c == '+' || c == '-')
                    {
                        ++pos;
                        IExpression right = ParseTerm();
                        left = new BinaryOperation(c, left, right);
                    }
                    else
                    {
                        return left;
                    }
                }
            }

            IExpression ParseTerm()
            {
                IExpression left = ParseFactor();

                while (true)
                {
                    if (pos >= text.Length) return left;

                    var c = text[pos];

                    if (c == '*' || c == '/')
                    {
                        ++pos;
                        IExpression right = ParseFactor();
                        left = new BinaryOperation(c, left, right);
                    }
                    else
                    {
                        return left;
                    }
                }
            }

            IExpression ParseFactor()
            {
                SkipWhiteSpace();
                if (pos >= text.Length) throw new ArgumentException("Unexpected end of text");

                var c = text[pos];

                if (c == '+')
                {
                    ++pos;
                    return ParseFactor();
                }

                if (c == '-')
                {
                    ++pos;
                    return new Negate(ParseFactor());
                }

                if (c == 'x' || c == 'a') return CreateVariable(0);
                if (c == 'y' || c == 'b') return CreateVariable(1);
                if (c == 'z' || c == 'c') return CreateVariable(2);
                if (c == 't' || c == 'd') return CreateVariable(3);

                if (c == '(')
                {
                    ++pos;
                    var expression = ParseExpression();
                    SkipWhiteSpace();
                    Require(')');
                    SkipWhiteSpace();
                    return expression;
                }

                if (c == '{')
                {
                    ++pos;
                    var end = text.IndexOf('}', pos);
                    if (end < 0)
                    {
                        --pos;
                        throw new ArgumentException("Unmatched '{'");
                    }
                    if (end == pos)
                    {
                        throw new ArgumentException("Missing parameter index after '{'");
                    }
                    var result = new Variable(text.Substring(pos, end - pos).Trim());
                    pos = end + 1;
                    SkipWhiteSpace();
                    return result;
                }

                const string decimalRegEx = @"(\d+\.?\d*|\d*\.?\d+)";
                var match = Regex.Match(text.Substring(pos), decimalRegEx);
                if (match.Success)
                {
                    pos += match.Length;
                    SkipWhiteSpace();
                    return new Constant(match.Value);
                }
                else
                {
                    throw new ArgumentException(string.Format("Unexpeted character '{0}'", c));
                }
            }

            IExpression CreateVariable(int n)
            {
                ++pos;
                SkipWhiteSpace();
                return new Variable(n);
            }

            void SkipWhiteSpace()
            {
                while (pos < text.Length && Char.IsWhiteSpace((text[pos]))) ++pos;
            }

            void Require(char c)
            {
                if (pos >= text.Length || text[pos] != c)
                {
                    throw new ArgumentException("Expected '" + c + "'");
                }

                ++pos;
            }

            void RequireEndOfText()
            {
                if (pos != text.Length)
                {
                    throw new ArgumentException("Unexpected character '" + text[pos] + "'");
                }
            }
        }
    }
}