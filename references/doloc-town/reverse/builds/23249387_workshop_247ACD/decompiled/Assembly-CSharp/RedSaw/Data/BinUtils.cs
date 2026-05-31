using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace RedSaw.Data;

public static class BinUtils
{
	private static IEnumerable<FieldInfo> GetFingerPrintFields(this object obj)
	{
		if (obj == null)
		{
			yield break;
		}
		Type type = obj.GetType();
		FieldInfo[] fields = type.GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.GetCustomAttribute<FingerPrintAttribute>() != null)
			{
				yield return fieldInfo;
			}
		}
	}

	public static byte[] GetBinDatas(object obj)
	{
		IEnumerable<FieldInfo> fingerPrintFields = obj.GetFingerPrintFields();
		List<byte> list = new List<byte>();
		foreach (FieldInfo item2 in fingerPrintFields)
		{
			object value = item2.GetValue(obj);
			if (value == null)
			{
				byte[] collection = ((item2.FieldType == typeof(string)) ? BitConverter.GetBytes(0) : new byte[Marshal.SizeOf(item2.FieldType)]);
				list.AddRange(collection);
			}
			else if (!(value is byte item))
			{
				if (!(value is sbyte b))
				{
					if (!(value is short value2))
					{
						if (!(value is ushort value3))
						{
							if (!(value is int value4))
							{
								if (!(value is uint value5))
								{
									if (!(value is long value6))
									{
										if (!(value is ulong value7))
										{
											if (!(value is float value8))
											{
												if (!(value is double value9))
												{
													if (!(value is string text))
													{
														if (!(value is byte[] collection2))
														{
															if (!(value is char value10))
															{
																if (!(value is bool flag))
																{
																	if (value is Enum @enum)
																	{
																		list.AddRange(BitConverter.GetBytes(@enum.GetHashCode()));
																		continue;
																	}
																	if (value.GetType().GetCustomAttribute<FingerPrintObjectAttribute>() == null)
																	{
																		throw new Exception("Unsupport type");
																	}
																	list.AddRange(GetBinDatas(value));
																}
																else
																{
																	list.Add((byte)(flag ? 1 : 0));
																}
															}
															else
															{
																list.AddRange(BitConverter.GetBytes(value10));
															}
														}
														else
														{
															list.AddRange(collection2);
														}
													}
													else
													{
														list.AddRange(BitConverter.GetBytes(text.Length));
														list.AddRange(Encoding.UTF8.GetBytes(text));
													}
												}
												else
												{
													list.AddRange(BitConverter.GetBytes(value9));
												}
											}
											else
											{
												list.AddRange(BitConverter.GetBytes(value8));
											}
										}
										else
										{
											list.AddRange(BitConverter.GetBytes(value7));
										}
									}
									else
									{
										list.AddRange(BitConverter.GetBytes(value6));
									}
								}
								else
								{
									list.AddRange(BitConverter.GetBytes(value5));
								}
							}
							else
							{
								list.AddRange(BitConverter.GetBytes(value4));
							}
						}
						else
						{
							list.AddRange(BitConverter.GetBytes(value3));
						}
					}
					else
					{
						list.AddRange(BitConverter.GetBytes(value2));
					}
				}
				else
				{
					list.Add((byte)b);
				}
			}
			else
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	public static string Encode_Sha256(byte[] bin)
	{
		using SHA256 sHA = SHA256.Create();
		return BitConverter.ToString(sHA.ComputeHash(bin)).Replace("-", "").ToLower();
	}

	public static string FingerPrint(this object obj)
	{
		try
		{
			byte[] binDatas = GetBinDatas(obj);
			return (binDatas.Length == 0) ? null : Encode_Sha256(binDatas);
		}
		catch (Exception)
		{
			return null;
		}
	}
}
