using System;
using System.Linq;

namespace EverythingToolbar.Helpers
{
    public static class FilterOrderValidator
    {
        public static int[] GetValidFilterOrder(string? order, int count)
        {
            if (count <= 0)
                return Array.Empty<int>();

            var defaultOrder = Enumerable.Range(0, count);

            if (string.IsNullOrWhiteSpace(order))
                return defaultOrder.ToArray();

            var parts = order.Split(',');
            if (parts.Length != count)
                return defaultOrder.ToArray();

            var indices = new int[count];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out var idx))
                    return defaultOrder.ToArray();

                indices[i] = idx;
            }

            if (!indices.OrderBy(i => i).SequenceEqual(defaultOrder))
                return defaultOrder.ToArray();

            return indices;
        }
    }
}