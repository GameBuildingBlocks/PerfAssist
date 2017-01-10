/*!lic_info

The MIT License (MIT)

Copyright (c) 2015 SeaSunOpenSource

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

﻿using System.Collections;
using System.Collections.Generic;

using System;
using System.Text;

public interface IBytesProvider<T>
{
	byte[] GetBytes(T value);
}

public class BytesProvider<T> : IBytesProvider<T>
{
	public static BytesProvider<T> Default
	{
		get { return DefaultBytesProviders.GetDefaultProvider<T>(); }
	}
	
	Func<T, byte[]> _conversion;
	
	internal BytesProvider(Func<T, byte[]> conversion)
	{
		_conversion = conversion;
	}
	
	public byte[] GetBytes(T value)
	{
		return _conversion(value);
	}
}

static class DefaultBytesProviders
{
	static Dictionary<Type, object> _providers;
	
	static DefaultBytesProviders()
	{
		// Here are a couple for illustration. Yes, I am suggesting that
		// in reality you would add a BytesProvider<T> for each T
		// supported by the BitConverter class.
		_providers = new Dictionary<Type, object>
		{
			{ typeof(int), new BytesProvider<int>(BitConverter.GetBytes) },
			{ typeof(long), new BytesProvider<long>(BitConverter.GetBytes) },
			{ typeof(short), new BytesProvider<short>(BitConverter.GetBytes) },
			{ typeof(float), new BytesProvider<float>(BitConverter.GetBytes) },
		};
	}
	
	public static BytesProvider<T> GetDefaultProvider<T>()
	{
		return (BytesProvider<T>)_providers[typeof(T)];
	}
}

public class UsGeneric 
{
	public static IEnumerable<List<T>> Slice<T>(List<T> objList, int slice) 
	{
		for (int i = 0; i < objList.Count; i += slice) 
		{
			yield return objList.GetRange(i, Math.Min(objList.Count - i, slice));
		}
	}

	public static byte[] Convert<T>(T value)
	{
		return BytesProvider<T>.Default.GetBytes(value);
	}

	public static object Convert<T>(byte[] buffer, int startIndex) 
	{
		if (typeof(T) == typeof(int)) 
		{
			return BitConverter.ToInt32(buffer, startIndex);
		}
		else if (typeof(T) == typeof(short)) 
		{
			return BitConverter.ToInt16(buffer, startIndex);
		}
		else if (typeof(T) == typeof(float)) 
		{
			return BitConverter.ToSingle(buffer, startIndex);
		}
		return null;
	}
}
