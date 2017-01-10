using System.Collections;
using System.Collections.Generic;

public class UniqueString
{
    // 'removable = false' means the string would be added to the global string pool
    //  which would stay in memory in the rest of the whole execution period.
    public static string Intern(string str, bool removable = true)  
    {
        if (str == null)
            return null;

        string ret = IsInterned(str);
        if (ret != null)
            return ret;

        if (removable)
        {
            // the app-level interning (which could be cleared regularly)
            m_strings.Add(str, str);
            return str;
        }
        else
        {
            return string.Intern(str);
        }
    }

    // Why return a ref rather than a bool?
    //  return-val is the ref to the unique interned one, which should be tested against `null`
    public static string IsInterned(string str)      
    {
        if (str == null)
            return null;

        string ret = string.IsInterned(str);
        if (ret != null)
            return ret;

        if (m_strings.TryGetValue(str, out ret))
            return ret;

        return null;
    }

    // should be called on a regular basis
    public static void Clear()
    {
        m_strings.Clear();
    }

    // Why use Dictionary? 
    //  http://stackoverflow.com/questions/7760364/how-to-retrieve-actual-item-from-hashsett
    private static Dictionary<string, string> m_strings = new Dictionary<string, string>();    
}
