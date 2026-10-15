using System;
using System.Collections.Generic;
using System.Linq;

using Notary.Contract;

namespace Notary;

/// <summary>
/// Orders certificate authorities as a hierarchy so each CA is followed by the CAs it issued.
/// </summary>
public static class CaHierarchy
{
    /// <summary>
    /// Flatten a list of certificate authorities into depth-first rows.
    /// </summary>
    /// <param name="authorities">The certificate authorities, in any order</param>
    /// <returns>Rows with roots first and each CA followed by its descendants; roots and siblings are sorted by name.
    /// A CA whose parent is not in the list is treated as a root, and a parent cycle cannot hide a CA.</returns>
    public static List<CaHierarchyRow> Flatten(IEnumerable<CertificateAuthority> authorities)
    {
        var all = authorities.ToList();
        var slugs = all.Select(ca => ca.Slug).ToHashSet(StringComparer.Ordinal);

        var childrenByParent = all
            .Where(ca => !string.IsNullOrEmpty(ca.ParentCaSlug) && slugs.Contains(ca.ParentCaSlug) && ca.ParentCaSlug != ca.Slug)
            .GroupBy(ca => ca.ParentCaSlug, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.OrderBy(ca => ca.Name, StringComparer.OrdinalIgnoreCase).ToList(), StringComparer.Ordinal);

        var roots = all
            .Where(ca => string.IsNullOrEmpty(ca.ParentCaSlug) || !slugs.Contains(ca.ParentCaSlug) || ca.ParentCaSlug == ca.Slug)
            .OrderBy(ca => ca.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<CaHierarchyRow>(all.Count);
        var visited = new HashSet<CertificateAuthority>(ReferenceEqualityComparer.Instance);

        Walk(roots, 0);

        // Anything still unvisited sits in a parent cycle with no root above it; show each as a root
        foreach (var ca in all.Where(ca => !visited.Contains(ca)).OrderBy(ca => ca.Name, StringComparer.OrdinalIgnoreCase).ToList())
        {
            if (!visited.Contains(ca))
            {
                Walk([ca], 0);
            }
        }

        return rows;

        void Walk(IEnumerable<CertificateAuthority> start, int startDepth)
        {
            var stack = new Stack<(CertificateAuthority Ca, int Depth)>(start.Reverse().Select(ca => (ca, startDepth)));
            while (stack.Count > 0)
            {
                var (ca, depth) = stack.Pop();
                if (!visited.Add(ca))
                {
                    continue;
                }

                rows.Add(new CaHierarchyRow(ca, depth));

                if (childrenByParent.TryGetValue(ca.Slug, out var children))
                {
                    foreach (var child in Enumerable.Reverse(children))
                    {
                        stack.Push((child, depth + 1));
                    }
                }
            }
        }
    }
}
