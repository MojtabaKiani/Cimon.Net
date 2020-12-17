using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrainsNotNullAttribute = JetBrains.Annotations.NotNullAttribute;

namespace Ardalis.GuardClauses
{
    public static class GuardClauseExtensions
    {
        public static string BadFormat([JetBrainsNotNull] this IGuardClause guardClause, [JetBrainsNotNull] string input, [JetBrainsNotNull] string parameterName, [JetBrainsNotNull] string regexPattern)
        {
            if (input != Regex.Match(input, regexPattern).Value)
                throw new ArgumentException($"Input {parameterName} was not in required format", parameterName);

            return input;
        }

        public static IEnumerable<T> OutOfRange<T>([JetBrainsNotNull] this IGuardClause guardClause, [JetBrainsNotNull] IEnumerable<T> input, [JetBrainsNotNull] string parameterName, T rangeFrom, T rangeTo) where T : IComparable<T>
        {
            Comparer<T> comparer = Comparer<T>.Default;

            if (comparer.Compare(rangeFrom, rangeTo) >= 0)
            {
                throw new ArgumentException($"{nameof(rangeFrom)} should be less or equal than {nameof(rangeTo)}");
            }

            if (input.Any(x => comparer.Compare(x, rangeFrom) < 0 || comparer.Compare(x, rangeTo) > 0))
            {
                throw new ArgumentOutOfRangeException(parameterName, $"Input {parameterName} was out of range, it must be between {rangeFrom} and {rangeTo}.");
            }

            return input;

        }

        public static T InvalidData<T>([JetBrainsNotNull] this IGuardClause guardClause, [JetBrainsNotNull] T input, [JetBrainsNotNull] string parameterName, Func<T, bool> predicate)
        {
            if (!predicate(input))
                throw new ArgumentException($"Input {parameterName} did not satisfy the options", parameterName);

            return input;
        }
    }
}
