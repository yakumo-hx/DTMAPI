import sys
from pathlib import Path
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).parent))
import check_links as links


class LinkTests(unittest.TestCase):
    def test_links_ignore_commands_and_preserve_unicode_spaces_anchor_and_parentheses(self):
        text = '[one](<中文 空格.md#范围> "caption")\n[two](a(b).md)\n[ref]: <中文 空格.md> "title"\n`[literal](missing.md)`\n```ps1\n[command](missing.md)\n```\n'
        self.assertEqual([(1, "中文 空格.md#范围"), (2, "a(b).md"), (3, "中文 空格.md")], list(links.links(text)))

    def test_current_anchor_loss_is_detected(self):
        source = '[body](target.md#原生-owner)\n[external](https://example.invalid)\n'
        before = {"docs/source.md": source, "docs/target.md": "## 原生 `Owner`\n"}
        with tempfile.TemporaryDirectory() as temp:
            self.assertEqual([], list(links.defects(before, set(before), Path(temp))))
            after = dict(before, **{"docs/target.md": "## Shortened\n"})
            self.assertEqual("missing-anchor", list(links.defects(after, set(after), Path(temp)))[0]["reason"])

    def test_duplicate_headings_and_explicit_anchors(self):
        text = '# A & B!\n## A & B!\n<a id="explicit"></a>\n## 中文 _保留_\n'
        self.assertEqual({"a--b", "a--b-1", "explicit", "中文-_保留_"}, links.anchors(text))


if __name__ == "__main__":
    unittest.main()
