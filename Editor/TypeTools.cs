using System;
using UnityEditor.MemoryProfiler;
using System.Collections.Generic;
using System.Linq;

namespace MemoryProfilerWindow
{
	static class TypeTools
	{
		public enum FieldFindOptions
		{
			OnlyInstance,
			OnlyStatic
		}

		static public IEnumerable<FieldDescription> AllFieldsOf (TypeDescription typeDescription, TypeDescription[] typeDescriptions, FieldFindOptions findOptions)
		{
			if (typeDescription.isArray)
				yield break;
			
			if (findOptions != FieldFindOptions.OnlyStatic && typeDescription.baseOrElementTypeIndex != -1)
			{
				var baseTypeDescription = typeDescriptions [typeDescription.baseOrElementTypeIndex];
				foreach(var field in AllFieldsOf(baseTypeDescription, typeDescriptions, findOptions))
					yield return field;
			}

			foreach (var field in typeDescription.fields.Where(f => FieldMatchesOptions(f, findOptions)))
				yield return field;
		}

		static bool FieldMatchesOptions(FieldDescription field, FieldFindOptions options)
		{
			if (field.isStatic && options == FieldFindOptions.OnlyStatic)
				return true;
			if (!field.isStatic && options == FieldFindOptions.OnlyInstance)
				return true;

			return false;

		}
	}
}

