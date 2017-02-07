using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MemoryProfilerWindow;
using UnityEditor;

public class SnapshotUtil
{
    public static Dictionary<int, MemCategory> PopulateCategories(CrawledMemorySnapshot snapshot)
    {
        var categories = new Dictionary<int, MemCategory>();
        foreach (ThingInMemory thingInMemory in snapshot.allObjects)
        {
            int category = MemUtil.GetCategory(thingInMemory);

            MemCategory theCategory;
            if (!categories.TryGetValue(category, out theCategory))
            {
                theCategory = new MemCategory();
                theCategory.Category = category;
                categories.Add(category, theCategory);
            }
            theCategory.Size += thingInMemory.size;
            theCategory.Count++;
        }

        return categories;
    }

    public static string[] FormulateCategoryLiterals(Dictionary<int, MemCategory> categories)
    {
        int[] sizes = new int[MemConst.MemTypeCategories.Length];
        int[] counts = new int[MemConst.MemTypeCategories.Length];
        foreach (var item in categories)
        {
            sizes[0] += item.Value.Size;
            counts[0] += item.Value.Count;

            if (item.Key == 1)
            {
                sizes[1] += item.Value.Size;
                counts[1] += item.Value.Count;
            }
            else if (item.Key == 2)
            {
                sizes[2] += item.Value.Size;
                counts[2] += item.Value.Count;
            }
            else
            {
                sizes[3] += item.Value.Size;
                counts[3] += item.Value.Count;
            }
        }

        string[] categoryLiterals = new string[MemConst.MemTypeCategories.Length];
        for (int i = 0; i < categoryLiterals.Length; i++)
        {
            categoryLiterals[i] = string.Format("{0} ({1}, {2})", MemConst.MemTypeCategories[i], counts[i], EditorUtility.FormatBytes(sizes[i]));
        }
        return categoryLiterals;
    }

    public static string[] FormulateCategoryLiteralsDiffed(Dictionary<int, MemCategory> categories1st, Dictionary<int, MemCategory> categories2nd)
    {
        int[] sizes = new int[MemConst.MemTypeCategories.Length];
        int[] counts = new int[MemConst.MemTypeCategories.Length];

        foreach (var item in categories1st)
        {
            var c1 = item.Value;
            var c2 = categories2nd[item.Key];

            sizes[0] += c2.Size - c1.Size;
            counts[0] += c2.Count - c1.Count;

            if (item.Key == 1)
            {
                sizes[1] += c2.Size - c1.Size;
                counts[1] += c2.Count - c1.Count;
            }
            else if (item.Key == 2)
            {
                sizes[2] += c2.Size - c1.Size;
                counts[2] += c2.Count - c1.Count;
            }
            else
            {
                sizes[3] += c2.Size - c1.Size;
                counts[3] += c2.Count - c1.Count;
            }
        }

        string[] categoryLiterals = new string[MemConst.MemTypeCategories.Length];
        for (int i = 0; i < categoryLiterals.Length; i++)
        {
            categoryLiterals[i] = string.Format("{0} ({1}, {2}{3})", MemConst.MemTypeCategories[i],
                MemUtil.IntStrWithSign(counts[i]),
                MemUtil.GetSign(sizes[i]),
                EditorUtility.FormatBytes(sizes[i]));
        }
        return categoryLiterals;
    }
}
