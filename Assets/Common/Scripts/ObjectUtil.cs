using System.Runtime.CompilerServices;

public class ObjectUtil
{
    public static string GetHashCodeString(object obj)
    {
        return RuntimeHelpers.GetHashCode(obj).ToString("X8");
    }
}