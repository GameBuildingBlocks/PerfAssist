using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MemoryProfilerWindow;
using UnityEditor;

public class SnapshotUtil
{
    public static Dictionary<string, MemType> PopulateTypes(CrawledMemorySnapshot snapshot)
    {
        var types = new Dictionary<string, MemType>();
        foreach (ThingInMemory thingInMemory in snapshot.allObjects)
        {
            string typeName = MemUtil.GetGroupName(thingInMemory);
            if (typeName.Length == 0)
                continue;

            MemType theType;
            if (!types.TryGetValue(typeName, out theType))
            {
                theType = new MemType();
                theType.TypeName = MemUtil.GetCategoryLiteral(thingInMemory) + typeName;
                theType.Category = MemUtil.GetCategory(thingInMemory);
                theType.Objects = new List<object>();
                types.Add(typeName, theType);
            }

            MemObject item = new MemObject(thingInMemory, snapshot);
            theType.Size += item.Size;
            theType.Count++;
            theType.Objects.Add(item);
        }
        return types;
    }

    public static Dictionary<string, MemType> DiffTypes(Dictionary<string, MemType> types1st, Dictionary<string, MemType> types2nd)
    {
        Dictionary<string, int> unifiedKeys = new Dictionary<string, int>();

        foreach (var p in types1st)
            if (!unifiedKeys.ContainsKey(p.Key))
                unifiedKeys.Add(p.Key, p.Value.Category);

        foreach (var p in types2nd)
            if (!unifiedKeys.ContainsKey(p.Key))
                unifiedKeys.Add(p.Key, p.Value.Category);

        var retTypes = new Dictionary<string, MemType>();
        foreach (var p in unifiedKeys)
        {
            var dummyType = new MemType();
            dummyType.TypeName = p.Key;
            dummyType.Category = p.Value;
            dummyType.Objects = new List<object>();
            dummyType.Size = 0;
            dummyType.Count = 0;

            // add the dummy one if not exists in either 1st or 2nd
            if (!types1st.ContainsKey(p.Key))
            {
                types1st.Add(p.Key, dummyType);   
            }
            if (!types2nd.ContainsKey(p.Key))
            {
                types2nd.Add(p.Key, dummyType);
            }

            var t1 = types1st[p.Key];
            var t2 = types2nd[p.Key];

            // here we reuse the dummy type for the combined output type
            var diffedType = dummyType;
            diffedType.Size = t2.Size - t1.Size;
            diffedType.Count = t2.Count - t1.Count;
            if (diffedType.Size == 0 && diffedType.Count == 0)
                continue;

            diffedType.Objects = MemObjectInfoSet.Diff(t1.Objects, t2.Objects);

            retTypes[p.Key] = diffedType;
        }
        return retTypes;
    }

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
