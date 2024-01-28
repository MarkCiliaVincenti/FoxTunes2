﻿using FoxDb;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public static partial class Extensions
    {
        public static bool HasCustomAttribute<T>(this Type type, out T attribute) where T : Attribute
        {
            return type.HasCustomAttribute<T>(false, out attribute);
        }

        public static bool HasCustomAttribute<T>(this Type type, bool inherit, out T attribute) where T : Attribute
        {
            if (!type.Assembly.ReflectionOnly)
            {
                return (attribute = type.GetCustomAttribute<T>(inherit)) != null;
            }
            var sequence = type.GetCustomAttributesData();
            foreach (var element in sequence)
            {
                if (!string.Equals(element.Constructor.DeclaringType.FullName, typeof(T).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                attribute = AssemblyRegistry.Instance.GetExecutableType(type).GetCustomAttribute<T>();
                return true;
            }
            attribute = default(T);
            return false;
        }

        public static bool HasCustomAttributes<T>(this Type type, out IEnumerable<T> attributes) where T : Attribute
        {
            return type.HasCustomAttributes<T>(false, out attributes);
        }

        public static bool HasCustomAttributes<T>(this Type type, bool inherit, out IEnumerable<T> attributes) where T : Attribute
        {
            if (!type.Assembly.ReflectionOnly)
            {
                return (attributes = type.GetCustomAttributes<T>(inherit)).Any();
            }
            var sequence = type.GetCustomAttributesData();
            var result = new List<T>();
            foreach (var element in sequence)
            {
                if (!string.Equals(element.Constructor.DeclaringType.FullName, typeof(T).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                result.Add(AssemblyRegistry.Instance.GetExecutableType(type).GetCustomAttribute<T>());
            }
            if (result.Any())
            {
                attributes = result;
                return true;
            }
            else
            {
                attributes = Enumerable.Empty<T>();
                return false;
            }
        }

        public static T GetCustomAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return type.GetCustomAttributes<T>(inherit).FirstOrDefault();
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), inherit).OfType<T>();
        }

        public static T Do<T>(this T value, Action<T> action)
        {
            action(value);
            return value;
        }

        public static IEnumerable<T> Do<T>(this IEnumerable<T> sequence, Action action)
        {
            foreach (var element in sequence)
            {
                action();
                yield return element;
            }
        }

        public static IEnumerable<T> Do<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (var element in sequence)
            {
                action(element);
                yield return element;
            }
        }

        public static IEnumerable<T> Enumerate<T>(this IEnumerable<T> sequence)
        {
            foreach (var element in sequence) ;
            return sequence;
        }

        public static bool Contains(this IEnumerable<string> sequence, string value, bool ignoreCase)
        {
            if (!ignoreCase)
            {
                return sequence.Contains(value);
            }
            return sequence.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        public static bool Contains(this string subject, string value, bool ignoreCase)
        {
            if (!ignoreCase)
            {
                return subject.Contains(value);
            }
            return subject.IndexOf(value, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static string IfNullOrEmpty(this string value, string alternative)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
            return alternative;
        }

        public static int IndexOf<T>(this IEnumerable<T> sequence, T element, IEqualityComparer<T> comparer = null)
        {
            var index = 0;
            var enumerator = sequence.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (comparer != null)
                {
                    if (comparer.Equals(enumerator.Current, element))
                    {
                        return index;
                    }
                }
                else if (enumerator.Current != null && enumerator.Current.Equals(element))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        public static string GetName(this string fileName)
        {
            if (string.IsNullOrEmpty(Path.GetPathRoot(fileName)))
            {
                return fileName;
            }
            var name = Path.GetFileName(fileName);
            return name;
        }

        public static string GetExtension(this string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension) || extension.Length <= 1)
            {
                return string.Empty;
            }
            return extension.Substring(1).ToLower(CultureInfo.InvariantCulture);
        }

        public static IEnumerable<string> GetLines(this string sequence)
        {
            if (string.IsNullOrEmpty(sequence))
            {
                yield break;
            }
            foreach (var element in sequence.Split('\n'))
            {
                yield return element.TrimEnd('\r');
            }
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            return sequence.Do(action).Enumerate();
        }

        public static IEnumerable<T> Try<T>(this IEnumerable<T> sequence, Action<T> action, Action<T, Exception> errorHandler = null)
        {
            return sequence.ForEach(element => element.Try(action, errorHandler));
        }

        public static T Try<T>(this T value, Action action, Action<Exception> errorHandler = null)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                if (errorHandler != null)
                {
                    errorHandler(e);
                }
            }
            return value;
        }

        public static T Try<T>(this T value, Action<T> action, Action<T, Exception> errorHandler = null)
        {
            try
            {
                action(value);
            }
            catch (Exception e)
            {
                if (errorHandler != null)
                {
                    errorHandler(value, e);
                }
            }
            return value;
        }

        public static IEnumerable<T> AddRange<T>(this IList<T> list, IEnumerable<T> sequence)
        {
            foreach (var element in sequence)
            {
                list.Add(element);
            }
            return sequence;
        }

        public static IEnumerable<T> RemoveRange<T>(this IList<T> list, IEnumerable<T> sequence)
        {
            foreach (var element in sequence)
            {
                list.Remove(element);
            }
            return sequence;
        }

        public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (EqualityComparer<TKey>.Default.Equals(key, default(TKey)))
            {
                return false;
            }
            var value = default(TValue);
            return dictionary.TryRemove(key, out value);
        }

        public static int ToNearestPower(this int value)
        {
            var result = value;
            var power = 10;
            var a = 0;

            while ((result /= power) >= power)
            {
                a++;
            }

            if (value % (int)(Math.Pow(power, a + 1) + 0.5) != 0)
            {
                result++;
            }

            for (; a >= 0; a--)
            {
                result *= power;
            }

            return result;
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> sequence, TKey key, Func<TKey, TValue> factory)
        {
            var value = default(TValue);
            if (sequence.TryGetValue(key, out value))
            {
                return value;
            }
            value = factory(key);
            sequence.Add(key, value);
            return value;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> sequence, TKey key)
        {
            var value = default(TValue);
            if (!sequence.TryGetValue(key, out value))
            {
                return default(TValue);
            }
            return value;
        }

        public static void Shuffle<T>(this IList<T> sequence)
        {
            var random = new Random(unchecked((int)DateTime.Now.Ticks));
            for (var a = 0; a < sequence.Count; a++)
            {
                var b = sequence[a];
                var c = random.Next(sequence.Count);
                sequence[a] = sequence[c];
                sequence[c] = b;
            }
        }

        public static void Shuffle<TKey, TValue>(this IDictionary<TKey, TValue> sequence)
        {
            var random = new Random(unchecked((int)DateTime.Now.Ticks));
            var keys = sequence.Keys.ToArray();
            for (var a = 0; a < keys.Length; a++)
            {
                var key1 = keys[a];
                var key2 = keys[random.Next(sequence.Count)];
                var value1 = sequence[key1];
                var value2 = sequence[key2];
                sequence[key1] = value2;
                sequence[key2] = value1;
            }
        }

        public static string Replace(this string value, IEnumerable<string> oldValues, string newValue, bool ignoreCase, bool once)
        {
            foreach (var oldValue in oldValues)
            {
                var success = default(bool);
                value = value.Replace(oldValue, newValue, ignoreCase, out success);
                if (success && once)
                {
                    break;
                }
            }
            return value;
        }

        public static string Replace(this string value, string oldValue, string newValue, bool ignoreCase, out bool success)
        {
            var index = default(int);
            if (ignoreCase)
            {
                index = value.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                index = value.IndexOf(oldValue);
            }
            if (success = (index != -1))
            {
                var offset = index + oldValue.Length;
                return
                    value.Substring(0, index) +
                    newValue +
                    value.Substring(offset, value.Length - offset);
            }
            return value;
        }

        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> sequence)
        {
            return sequence.OrderBy(element => element);
        }

        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> sequence, IComparer<T> comparer)
        {
            return sequence.OrderBy(element => element, comparer);
        }

        public static string GetQueryParameter(this Uri uri, string name)
        {
            var parameters = uri.Query.TrimStart('?').Split('&');
            foreach (var parameter in parameters)
            {
                var parts = parameter.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    continue;
                }
                if (!string.Equals(parts[0], name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                return parts[1];
            }
            return null;
        }
    }
}
