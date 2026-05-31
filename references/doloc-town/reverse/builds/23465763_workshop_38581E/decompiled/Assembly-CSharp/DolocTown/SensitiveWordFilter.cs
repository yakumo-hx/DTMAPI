using System.Collections.Generic;

namespace DolocTown;

public class SensitiveWordFilter
{
	public class TrieNode
	{
		private Dictionary<char, TrieNode> subNodes = new Dictionary<char, TrieNode>();

		public bool IsKeywordEnd { get; set; }

		public void AddSubNode(char key, TrieNode value)
		{
			subNodes[key] = value;
		}

		public TrieNode GetSubNode(char key)
		{
			if (!subNodes.ContainsKey(key))
			{
				return null;
			}
			return subNodes[key];
		}
	}

	private static string REPLACEMENT = "***";

	private static TrieNode root = new TrieNode();

	private List<string> sensitiveWords;

	public SensitiveWordFilter(List<string> sensitiveWords)
	{
		this.sensitiveWords = sensitiveWords;
		foreach (string sensitiveWord in sensitiveWords)
		{
			AddKeyword(sensitiveWord);
		}
	}

	private void AddKeyword(string keyword)
	{
		keyword = keyword.ToLower();
		TrieNode trieNode = root;
		for (int i = 0; i < keyword.Length; i++)
		{
			char key = keyword[i];
			TrieNode trieNode2 = trieNode.GetSubNode(key);
			if (trieNode2 == null)
			{
				trieNode2 = new TrieNode();
				trieNode.AddSubNode(key, trieNode2);
			}
			trieNode = trieNode2;
			if (i == keyword.Length - 1)
			{
				trieNode.IsKeywordEnd = true;
			}
		}
	}

	public bool ContainsSensitiveWord(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		text = text.ToLower();
		TrieNode subNode = root;
		int num = 0;
		int num2 = 0;
		while (num2 < text.Length)
		{
			char c = text[num2];
			if (IsSymbol(c))
			{
				if (subNode == root)
				{
					num++;
				}
				num2++;
				continue;
			}
			subNode = subNode.GetSubNode(c);
			if (subNode == null)
			{
				num2 = ++num;
				subNode = root;
				continue;
			}
			if (subNode.IsKeywordEnd)
			{
				return true;
			}
			num2++;
		}
		return false;
	}

	private static bool IsSymbol(char c)
	{
		if (!char.IsLetterOrDigit(c))
		{
			if (c >= '⺀')
			{
				return c > '\u9fff';
			}
			return true;
		}
		return false;
	}
}
