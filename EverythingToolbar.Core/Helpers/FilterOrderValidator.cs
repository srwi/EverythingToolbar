using System;
using System.Linq;

namespace EverythingToolbar.Core.Helpers
{
    public static class FilterOrderValidator
    {
        public static int[] GetValidFilterOrder(string? order, int count)
        {
            if (count <= 0)
                return Array.Empty<int>();

            var defaultOrder = Enumerable.Range(0, count).ToArray();

            if (string.IsNullOrWhiteSpace(order))
                return defaultOrder;

            var parts = order.Split(',');
            if (parts.Length != count)
                return defaultOrder;

            var indices = new int[count];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out var idx))
                    return defaultOrder;

                indices[i] = idx;
            }

            if (!indices.OrderBy(i => i).SequenceEqual(defaultOrder))
                return defaultOrder;

            return indices;
        }
    }
}
