#!/usr/bin/env python3
"""Build a Feishu Docx block payload for the Hatch-based farm animal guide."""

from __future__ import annotations

import argparse
import base64
import html
import json
import os
import re
import sys
import time
import uuid
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable
from urllib.parse import parse_qs, urlparse

import requests
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[3]
DEFAULT_HATCH_ROOT = Path(
    os.environ.get(
        "HATCH_ASSETS_ROOT",
        r"C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets",
    )
)
DEFAULT_OUT_DIR = Path(__file__).resolve().parent / "generated"

JSON_FILES: list[tuple[str, str, str]] = [
    ("包展示信息", "info.json", "提供官方本地 Mod 列表、创意工坊预览和启用界面显示的名称、作者、版本、标签与多语言简介。"),
    ("DTMAPI 内容包清单", "Content/DTMAPI/manifest.json", "让 DTMAPI 识别这个文件夹是内容包，并提供 UniqueID、版本、最低 DTMAPI 版本和内容包类型。"),
    ("自定义动物桥", "Content/DTMAPI/custom-animals.json", "把新增物种、复用模板、AI 模板、动画键、PNG 前缀、动物袋和商店列表连接到 DTMAPI 的自定义动物桥。"),
    ("动物声音替换", "Content/DTMAPI/audio-replacements.json", "按物种和成长阶段把原版动物声音事件替换为本包 WAV 文件，避免影响原版动物。"),
    ("动物基础数据", "Content/animal_tbanimal.json", "定义游戏中的动物 ID、显示名、成长阶段、行为日程、动画资源、声音事件、价格和产物掉落入口。"),
    ("动物袋道具", "Content/item_tbitem.json", "定义可放入背包和商店出售的动物袋道具，并指定释放后生成的目标动物。"),
    ("产物掉落库", "Content/item_tbitemspawn.json", "定义动物产物掉落内容和数量范围，由动物基础数据中的产物入口引用。"),
    ("动物商店扩展", "Content/mod_tbmodstoreextension.json", "把动物袋追加到原版动物商店的出售列表，控制库存、刷新数量和季节配置。"),
    ("动物图鉴信息", "Content/animal_tbanimaldocument.json", "提供动物图鉴中的幼体/成年图片、标题、说明条目和显示开关。"),
    ("PNG 帧清单", "Content/Sprites/hatch_frame_manifest.json", "记录 PNG 路线的阶段、动作、帧数和文件名映射，便于作者核对素材完整性。"),
]

EXPECTED_ACTION_COUNTS = {
    ("young", "eat"): 7,
    ("young", "idle"): 4,
    ("young", "jump"): 2,
    ("young", "move"): 8,
    ("young", "sleep"): 1,
    ("adult", "eat"): 7,
    ("adult", "idle"): 4,
    ("adult", "jump"): 2,
    ("adult", "move"): 8,
    ("adult", "sleep"): 1,
}
EXPECTED_STAGE_SIZES = {"young": (28, 24), "adult": (32, 32)}
ATLAS_ACTIONS = ["idle", "move", "eat", "jump", "sleep"]
STAGES = ["young", "adult"]

FONT_BACKGROUND_YELLOW = 3
CODE_LANGUAGE_JSON = 28
CODE_LANGUAGE_PLAIN = 1


@dataclass(frozen=True)
class JsonSpec:
    index: int
    name: str
    rel_path: str
    purpose: str
    data: Any
    jsonc: str


def posix_path(path: str | Path) -> str:
    return str(path).replace("\\", "/")


def read_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def text_obj(text: str, *, highlight_terms: Iterable[str] | None = None, bold: bool = False) -> dict[str, Any]:
    return {"elements": text_elements(text, highlight_terms=highlight_terms, bold=bold), "style": {}}


def text_elements(
    text: str,
    *,
    highlight_terms: Iterable[str] | None = None,
    bold: bool = False,
) -> list[dict[str, Any]]:
    terms = [term for term in sorted(set(highlight_terms or []), key=len, reverse=True) if term]
    if not terms:
        style = {"bold": True} if bold else {}
        return [{"text_run": {"content": text, "text_element_style": style}}]

    pattern = re.compile("|".join(re.escape(term) for term in terms))
    elements: list[dict[str, Any]] = []
    last = 0
    for match in pattern.finditer(text):
        if match.start() > last:
            style = {"bold": True} if bold else {}
            elements.append({"text_run": {"content": text[last : match.start()], "text_element_style": style}})
        style = {"background_color": FONT_BACKGROUND_YELLOW}
        if bold:
            style["bold"] = True
        elements.append({"text_run": {"content": match.group(0), "text_element_style": style}})
        last = match.end()
    if last < len(text):
        style = {"bold": True} if bold else {}
        elements.append({"text_run": {"content": text[last:], "text_element_style": style}})
    return elements


def annotate_jsonc(rel_path: str, data: Any) -> str:
    text = json.dumps(data, ensure_ascii=False, indent=2)
    comments = comments_for(rel_path)
    out: list[str] = []
    for line in text.splitlines():
        annotated = line
        key_match = re.match(r'^(\s*)"([^"]+)":', line)
        if key_match:
            key = key_match.group(2)
            comment = comments.get(key)
            if comment:
                annotated = f"{line}    // {comment}"
        out.append(annotated)
    return "\n".join(out)


def comments_for(rel_path: str) -> dict[str, str]:
    rel = posix_path(rel_path)
    common = {
        "id": "唯一 ID，复制成自己的包时通常要替换",
        "key": "本地化文本 key，复制成自己的包时同步替换",
        "text": "显示文本，复制成自己的包时按新动物改写",
        "url": "资源键或资源路径，复制成自己的包时检查是否同步替换",
    }
    if rel == "info.json":
        return {
            "name": "模组名；没有翻译文本时会显示这个默认名",
            "author": "作者名",
            "version": "版本号",
            "description": "模组描述；没有翻译文本时会显示这个默认描述",
            "tags": "模组标签",
            "localized_name": "模组名翻译",
            "localized_description": "模组简介翻译",
            "schinese": "简体中文文本",
            "tchinese": "繁体中文文本",
            "english": "英文文本",
        }
    if rel.endswith("DTMAPI/manifest.json"):
        return {
            "Name": "DTMAPI 内容包名称",
            "Author": "作者名",
            "Version": "版本号",
            "Description": "内容包描述",
            "UniqueID": "DTMAPI 唯一包 ID，必须唯一",
            "EntryDll": "纯 JSON/PNG/WAV 内容包保持空字符串",
            "MinimumDTMApiVersion": "最低 DTMAPI 版本",
            "Type": "内容包类型；这里固定为 ContentPack",
            "Dependencies": "依赖列表；本例为空",
        }
    if rel.endswith("custom-animals.json"):
        return {
            "speciesId": "新增动物 ID，必须和 animal_tbanimal.json 的 id 一致",
            "templateSpeciesId": "复用的原版动物模板；哈奇路线沿用 chicken",
            "aiTemplate": "复用的 AI 模板；哈奇路线沿用 chicken",
            "animatorMode": "动画模式；PNG 路线使用 pngSpriteOverride",
            "adultAnimatorKey": "成年动画键，需与 animal_tbanimal levels[*].animator.url 对齐",
            "childAnimatorKey": "幼体动画键，需与 animal_tbanimal levels[*].animator.url 对齐",
            "frameManifest": "PNG 帧清单路径",
            "templateSpritePrefix": "原版模板帧名前缀；哈奇路线可保持 chicken",
            "customSpritePrefix": "自定义 PNG 帧名前缀，需和所有 PNG 文件名对齐",
            "movementMultiplier": "移动倍率；哈奇模板使用 1.0",
            "metabolismMultiplier": "代谢倍率；哈奇模板使用 1.0",
            "packageItemId": "动物袋道具 ID，需与 item_tbitem.json 对齐",
            "shopItemListId": "追加的商店列表 ID；哈奇路线先用 animal_shop",
        }
    if rel.endswith("audio-replacements.json"):
        return {
            "id": "声音替换规则 ID，使用动物和阶段命名",
            "category": "替换类型；动物叫声使用 AnimalVoice",
            "speciesId": "新增动物 ID，不能写模板 chicken，避免污染原版鸡",
            "stage": "声音阶段；音频使用 child/adult，PNG 阶段使用 young/adult",
            "nativeSoundEvent": "原版声音事件，必须和 animal_tbanimal.json 的 sound_event 一致",
            "file": "WAV 路径，从包根目录起算",
            "suppressNativeWhenReady": "自定义 WAV 就绪后是否抑制原版声音",
            "cooldownMilliseconds": "短时间重复触发的冷却",
        }
    if rel.endswith("animal_tbanimal.json"):
        return {
            **common,
            "title": "动物显示名",
            "defaul_input_name": "默认输入名；原字段名保持游戏原拼写",
            "schedule_id": "原版日程/行为路线；哈奇路线沿用 chicken",
            "levels": "幼体和成年阶段数据",
            "sound_event": "该阶段原版声音事件，需与 audio-replacements.json 对齐",
            "animator": "该阶段动画键",
            "sprite_size": "交互/显示手感字段，不是 PNG 画布校验字段",
            "icon": "阶段图标资源键",
            "description_in_sack": "动物袋里的阶段描述",
            "price": "该阶段价格",
            "produce_spawn_entry": "产物掉落配置",
            "spawn_lut": "产物掉落库 ID，需与 item_tbitemspawn.json 对齐",
            "count_range": "本次产出数量范围",
            "produce_require_mood": "产物所需心情",
            "manual_metabolism": "是否手动代谢；哈奇沿用鸡路线",
            "jump_height": "跳跃高度",
        }
    if rel.endswith("item_tbitem.json"):
        return {
            **common,
            "sub_type": "道具子类型；动物袋使用 husbandry_animal",
            "ui_sprite_asset": "背包/商店显示图标",
            "title": "动物袋标题",
            "description_basic": "动物袋描述",
            "function": "道具功能配置",
            "$type": "功能类型；动物袋使用 ItemFunctionAnimalPackage",
            "ui_sprite_catch": "袋子装满时的图标",
            "preset_animal": "释放出的动物 ID，必须等于 speciesId",
            "reusable": "是否可重复使用",
            "type_when_full": "装有动物时的类型",
        }
    if rel.endswith("item_tbitemspawn.json"):
        return {
            **common,
            "spawn_datas": "掉落条目列表",
            "spawn_weight": "权重；1000 可视为本例固定产出",
            "min_count": "最小数量",
            "max_count": "最大数量",
            "item_name": "产物道具 ID；哈奇模板使用原版 meat",
        }
    if rel.endswith("mod_tbmodstoreextension.json"):
        return {
            **common,
            "extra_items": "追加到商店的道具列表",
            "item_name": "追加的动物袋道具 ID",
            "storage": "库存模式",
            "default_unlock": "是否默认解锁",
            "season_spawn_data": "四季刷新数据",
            "count_range": "商店刷出数量范围",
            "spawn_weight": "刷新权重；本例沿用当前实包配置",
        }
    if rel.endswith("animal_tbanimaldocument.json"):
        return {
            **common,
            "ui_sprite_asset": "图鉴幼体展示图",
            "ui_adult_sprite_asset": "图鉴成年展示图",
            "infancy_asset": "幼体资源",
            "adult_asset": "成年资源",
            "document_infos": "图鉴描述条目",
            "document_type": "图鉴条目类型",
            "value": "图鉴条目数值",
            "description_append": "图鉴追加说明",
            "display": "是否显示到图鉴",
        }
    if rel.endswith("hatch_frame_manifest.json"):
        return {
            "animal_id": "动物 ID",
            "display_name": "显示名",
            "animator_mode": "动画模式",
            "template_sprite_prefix": "模板帧名前缀",
            "custom_sprite_prefix": "自定义帧名前缀",
            "canvas_by_stage": "不同阶段的 PNG 画布尺寸",
            "states": "动作状态和帧文件",
            "stage": "阶段；PNG 使用 young/adult",
            "state": "动作名",
            "frame_count": "该动作帧数",
            "files": "对应 PNG 文件",
            "alias_of": "别名动作；jump_ready 复用 jump_0",
            "notes": "维护说明",
        }
    return common


def collect_highlight_terms(data_by_rel: dict[str, Any]) -> list[str]:
    terms = {
        "哈奇",
        "Yuuka",
        "1.0.0",
        "新增可养殖动物哈奇，包含动物数据、动物袋、商店、图鉴、PNG 动画帧和 WAV 声音配置。",
        "DTMAPI.HatchAssets",
        "DolocTownMeta/prototypes/hatch",
        "hatch",
        "Hatch",
        "sack_hatch",
        "hatch_produce",
        "hatch-pet-child",
        "hatch-pet-adult",
        "dtmapi_anim_animal_hatch_child",
        "dtmapi_anim_animal_hatch",
        "anim_animal_hatch",
        "hatch_frame_manifest.json",
        "hatch_pet_young.wav",
        "hatch_pet_adult.wav",
        "Content/Audio/hatch_pet_young.wav",
        "Content/Audio/hatch_pet_adult.wav",
    }

    def visit(value: Any) -> None:
        if isinstance(value, dict):
            for key, item in value.items():
                if key in {"key"} and isinstance(item, str) and "_hatch" in item:
                    terms.add(item)
                visit(item)
        elif isinstance(value, list):
            for item in value:
                visit(item)
        elif isinstance(value, str):
            if "hatch" in value or "Hatch" in value or "哈奇" in value:
                terms.add(value)

    for data in data_by_rel.values():
        visit(data)

    # Keep native template values unhighlighted.
    terms.discard("chicken")
    terms.discard("animal_shop")
    terms.discard("meat")
    return sorted(terms, key=len, reverse=True)


def validate_hatch_package(hatch_root: Path) -> tuple[list[JsonSpec], dict[str, Any]]:
    if not hatch_root.exists():
        raise FileNotFoundError(f"Hatch package root does not exist: {hatch_root}")

    specs: list[JsonSpec] = []
    data_by_rel: dict[str, Any] = {}
    errors: list[str] = []
    for index, (name, rel_path, purpose) in enumerate(JSON_FILES, start=1):
        path = hatch_root / Path(rel_path)
        if not path.exists():
            errors.append(f"missing JSON: {rel_path}")
            continue
        try:
            data = read_json(path)
        except Exception as exc:  # pragma: no cover - emitted in CLI validation.
            errors.append(f"invalid JSON {rel_path}: {exc}")
            continue
        data_by_rel[rel_path] = data
        specs.append(
            JsonSpec(
                index=index,
                name=name,
                rel_path=rel_path,
                purpose=purpose,
                data=data,
                jsonc=annotate_jsonc(rel_path, data),
            )
        )

    sprite_dir = hatch_root / "Content" / "Sprites"
    audio_dir = hatch_root / "Content" / "Audio"
    png_files = sorted(sprite_dir.glob("anim_animal_hatch_*.png"))
    wav_files = sorted(audio_dir.glob("*.wav"))
    if len(png_files) != 44:
        errors.append(f"expected 44 Hatch PNG files, found {len(png_files)}")
    if {path.name for path in wav_files} != {"hatch_pet_adult.wav", "hatch_pet_young.wav"}:
        errors.append("expected WAV files hatch_pet_adult.wav and hatch_pet_young.wav")

    stage_counts: dict[str, int] = {stage: 0 for stage in STAGES}
    stage_sizes: dict[str, set[tuple[int, int]]] = {stage: set() for stage in STAGES}
    action_counts: dict[tuple[str, str], int] = {}
    for png in png_files:
        match = re.match(r"anim_animal_hatch_(young|adult)_([a-z]+)_\d+\.png$", png.name)
        if not match:
            errors.append(f"unexpected PNG filename: {png.name}")
            continue
        stage, action = match.group(1), match.group(2)
        stage_counts[stage] += 1
        action_counts[(stage, action)] = action_counts.get((stage, action), 0) + 1
        with Image.open(png) as image:
            stage_sizes[stage].add(image.size)

    for stage, expected_size in EXPECTED_STAGE_SIZES.items():
        if stage_sizes[stage] != {expected_size}:
            errors.append(f"expected {stage} PNG size {expected_size}, found {sorted(stage_sizes[stage])}")
    for key, expected_count in EXPECTED_ACTION_COUNTS.items():
        actual = action_counts.get(key, 0)
        if actual != expected_count:
            errors.append(f"expected {key[0]} {key[1]} frame count {expected_count}, found {actual}")

    report = {
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "hatchRoot": str(hatch_root),
        "jsonCount": len(specs),
        "pngCount": len(png_files),
        "wavFiles": [{"name": path.name, "bytes": path.stat().st_size} for path in wav_files],
        "stageCounts": stage_counts,
        "stageSizes": {stage: sorted([list(size) for size in sizes]) for stage, sizes in stage_sizes.items()},
        "actionCounts": {f"{stage}/{action}": count for (stage, action), count in sorted(action_counts.items())},
        "errors": errors,
    }
    if errors:
        raise ValueError("Hatch package validation failed:\n" + "\n".join(f"- {error}" for error in errors))
    return specs, report


def load_font(size: int, *, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        r"C:\Windows\Fonts\consolab.ttf" if bold else r"C:\Windows\Fonts\consola.ttf",
        r"C:\Windows\Fonts\msyhbd.ttc" if bold else r"C:\Windows\Fonts\msyh.ttc",
    ]
    for candidate in candidates:
        if candidate and Path(candidate).exists():
            return ImageFont.truetype(candidate, size)
    return ImageFont.load_default()


def draw_wrapped_label(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    font: ImageFont.ImageFont,
    fill: tuple[int, int, int],
    max_width: int,
) -> int:
    parts: list[str] = []
    current = ""
    for char in text:
        trial = current + char
        if draw.textlength(trial, font=font) <= max_width or not current:
            current = trial
        else:
            parts.append(current)
            current = char
    if current:
        parts.append(current)
    x, y = xy
    line_height = font.getbbox("Ag")[3] - font.getbbox("Ag")[1] + 2
    for part in parts:
        width = draw.textlength(part, font=font)
        draw.text((x + (max_width - width) / 2, y), part, font=font, fill=fill)
        y += line_height
    return y


def generate_frame_atlases(hatch_root: Path, out_dir: Path) -> list[dict[str, Any]]:
    sprite_dir = hatch_root / "Content" / "Sprites"
    atlas_dir = out_dir / "frames"
    atlas_dir.mkdir(parents=True, exist_ok=True)
    label_font = load_font(11)
    title_font = load_font(16, bold=True)
    stage_font = load_font(14, bold=True)
    scale = 4
    margin = 14
    stage_label_width = 76
    atlas_entries: list[dict[str, Any]] = []

    for action in ATLAS_ACTIONS:
        stage_files: dict[str, list[Path]] = {}
        for stage in STAGES:
            stage_files[stage] = sorted(sprite_dir.glob(f"anim_animal_hatch_{stage}_{action}_*.png"))
        max_count = max(len(files) for files in stage_files.values())
        cell_width = 230
        sprite_area_height = max(EXPECTED_STAGE_SIZES[stage][1] for stage in STAGES) * scale + 18
        label_height = 42
        row_height = sprite_area_height + label_height + margin
        width = stage_label_width + max_count * cell_width + margin * 2
        height = 52 + len(STAGES) * row_height + margin
        atlas = Image.new("RGBA", (width, height), (248, 250, 252, 255))
        draw = ImageDraw.Draw(atlas)
        draw.text((margin, margin), f"Hatch {action} frames", font=title_font, fill=(15, 23, 42))
        y = 48
        for stage in STAGES:
            draw.rounded_rectangle(
                [margin, y, width - margin, y + row_height - 4],
                radius=8,
                fill=(255, 255, 255, 255),
                outline=(203, 213, 225, 255),
                width=1,
            )
            draw.text((margin + 10, y + 18), stage, font=stage_font, fill=(30, 41, 59))
            for index, png in enumerate(stage_files[stage]):
                image = Image.open(png).convert("RGBA")
                scaled = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
                x = margin + stage_label_width + index * cell_width
                sprite_x = x + (cell_width - scaled.width) // 2
                sprite_y = y + 12 + (sprite_area_height - scaled.height) // 2
                draw.rectangle(
                    [x + 8, y + 10, x + cell_width - 8, y + sprite_area_height - 2],
                    fill=(241, 245, 249, 255),
                    outline=(226, 232, 240, 255),
                )
                atlas.alpha_composite(scaled, (sprite_x, sprite_y))
                draw_wrapped_label(
                    draw,
                    (x + 8, y + sprite_area_height + 2),
                    png.name,
                    label_font,
                    (51, 65, 85),
                    cell_width - 16,
                )
                image.close()
            y += row_height
        out_path = atlas_dir / f"hatch_{action}_frames.png"
        atlas.save(out_path)
        atlas_entries.append(
            {
                "action": action,
                "path": str(out_path),
                "relativePath": posix_path(out_path.relative_to(out_dir)),
                "stages": {stage: [path.name for path in files] for stage, files in stage_files.items()},
            }
        )
    return atlas_entries


def block(kind: str, **kwargs: Any) -> dict[str, Any]:
    return {"kind": kind, **kwargs}


def table_block(
    caption: str,
    headers: list[str],
    rows: list[list[str]],
    *,
    column_widths: list[int] | None = None,
) -> dict[str, Any]:
    return block("table", caption=caption, headers=headers, rows=rows, columnWidths=column_widths or [])


def code_block(caption: str, content: str, *, language: int = CODE_LANGUAGE_JSON, highlight_terms: list[str] | None = None) -> dict[str, Any]:
    return block("code", caption=caption, language=language, content=content, highlightTerms=highlight_terms or [])


def build_document_model(
    specs: list[JsonSpec],
    validation: dict[str, Any],
    atlas_entries: list[dict[str, Any]],
    highlight_terms: list[str],
) -> dict[str, Any]:
    json_catalog_rows = [
        [f"{spec.name}：{spec.rel_path}", spec.purpose]
        for spec in specs
    ]
    key_rows = [
        ["包 UniqueID", "DTMAPI.HatchAssets", "复制成自己的包时必须唯一"],
        ["动物 speciesId / animal id", "hatch", "custom-animals.json、animal_tbanimal.json、动物袋 preset_animal 要一致"],
        ["复用模板", "chicken", "哈奇路线使用鸡模板"],
        ["AI 模板", "chicken", "哈奇路线使用鸡 AI"],
        ["模板帧名前缀", "anim_animal_chicken", "哈奇路线复用鸡模板帧名"],
        ["自定义帧名前缀", "anim_animal_hatch", "必须和所有 PNG 文件名一致"],
        ["幼体 animator key", "dtmapi_anim_animal_hatch_child", "需与幼体 levels[*].animator.url 一致"],
        ["成年 animator key", "dtmapi_anim_animal_hatch", "需与成年 levels[*].animator.url 一致"],
        ["动物袋道具", "sack_hatch", "custom-animals.packageItemId 和商店 extra_items 要引用它"],
        ["产物掉落库", "hatch_produce", "animal_tbanimal.produce_spawn_entry.spawn_lut 要引用它"],
        ["商店列表", "animal_shop", "哈奇路线把动物袋追加到原版动物商店"],
        ["幼体声音事件", "PLAY_ANIMAL_PET_CHICKEN_CHILD", "需与幼体 sound_event 一致"],
        ["成年声音事件", "PLAY_ANIMAL_PET_CHICKEN", "需与成年 sound_event 一致"],
    ]
    frame_rows = [
        ["young", "28 x 24", "4", "8", "7", "2", "1", "22"],
        ["adult", "32 x 32", "4", "8", "7", "2", "1", "22"],
    ]
    requirement_rows = [
        [
            "要求",
            "每一帧都是独立 PNG",
            "anim_animal_hatch_阶段_动作_编号.png",
            "PNG 使用 young/adult；音频使用 child/adult",
            "idle、move、eat、jump、sleep；jump_ready 复用 jump_0",
            "不准备 left/right 两套图，左右朝向由原版渲染器翻转",
            "用于交互/显示手感，不参与 PNG 画布校验",
        ],
    ]

    blocks: list[dict[str, Any]] = [
        block("heading", level=2, text="一、整体说明"),
        block("bullet", text="本说明用于制作基于 DTMAPI 的 JSON + PNG + WAV 新增养殖动物内容包。"),
        block("bullet", text=f"通过以下 {len(specs)} 个 JSON 文件的配合，实现新增动物、获取动物袋、购买动物袋、释放动物、显示图鉴、播放 WAV 声音和读取 PNG 动画帧的完整链路。"),
        table_block("JSON 文件配合关系", ["配置文件", "在新增动物链路中的作用"], json_catalog_rows, column_widths=[255, 648]),
        block(
            "paragraph",
            text="其中，Content/DTMAPI/manifest.json、Content/DTMAPI/custom-animals.json、Content/DTMAPI/audio-replacements.json 由 DTMAPI 直接读取；其余 JSON 进入 Doloc Town 官方内容数据表，共同组成可购买、可释放、可显示、可产出的动物内容包。",
        ),
        block("bullet", text="配置示例中，标黄的字段为【需要修改的字段】。"),
        block(
            "paragraph",
            text="说明：标黄内容表示从哈奇实包复制到自己的动物包时必须替换的包名、作者、物种 ID、动物袋 ID、动画键、PNG 前缀、WAV 路径或显示文本。未标黄的 chicken、原版声音事件、animal_shop、meat 是哈奇模板沿用的原版配置。",
        ),
        block("heading", level=2, text="二、配置示例及数据结构说明"),
        block("paragraph", text="以下 JSON 均读取自当前本地哈奇实包。代码块使用 JSON 语言显示，但为了说明字段，保留官方示例风格的 // 中文注释。"),
    ]

    for spec in specs:
        blocks.extend(
            [
                block("heading", level=3, text=f"{spec.index}. {spec.rel_path}"),
                block("bullet", text=f"名称：{spec.name}。"),
                block("bullet", text=f"作用：{spec.purpose}"),
                code_block(spec.rel_path, spec.jsonc, highlight_terms=highlight_terms),
            ]
        )

    blocks.extend(
        [
            block("heading", level=2, text="三、动画帧图片格式要求"),
            block("paragraph", text="哈奇当前使用 chicken 模板路线，运行时通过 pngSpriteOverride 将鸡模板帧映射到哈奇 PNG。"),
            table_block(
                "动作帧数量与画布尺寸",
                ["阶段", "画布尺寸", "idle", "move", "eat", "jump", "sleep", "PNG 合计"],
                frame_rows,
                column_widths=[95, 125, 80, 80, 80, 80, 80, 100],
            ),
            block("paragraph", text="文件名规则：anim_animal_hatch_阶段_动作_编号.png。复制为新动物时，将 hatch 替换为自己的动物 ID，并保持 customSpritePrefix 与 PNG 文件名前缀一致。"),
            table_block(
                "图片要求",
                ["项目", "PNG 文件", "命名", "阶段", "动作", "方向", "sprite_size"],
                requirement_rows,
                column_widths=[80, 170, 260, 180, 205, 205, 160],
            ),
        ]
    )
    for atlas in atlas_entries:
        blocks.append(block("image", caption=f"· 哈奇 {atlas['action']} 动作帧图", path=atlas["path"], relativePath=atlas["relativePath"]))

    blocks.extend(
        [
            block("heading", level=2, text="四、参考示例"),
            block("heading", level=3, text="1. 哈奇实包文件结构"),
            table_block(
                "哈奇实包文件结构",
                ["层级", "路径/文件", "说明"],
                [
                    ["根目录", "info.json", "官方本地/创意工坊展示信息"],
                    ["Content", "animal_tbanimal.json", "动物本体数据"],
                    ["Content", "item_tbitem.json", "动物袋道具"],
                    ["Content", "item_tbitemspawn.json", "产物掉落库"],
                    ["Content", "mod_tbmodstoreextension.json", "动物商店扩展"],
                    ["Content/DTMAPI", "manifest.json", "DTMAPI 内容包清单"],
                    ["Content/DTMAPI", "custom-animals.json", "自定义动物桥"],
                    ["Content/DTMAPI", "audio-replacements.json", "WAV 声音替换"],
                    ["Content/Sprites", "anim_animal_hatch_*.png", "44 张独立帧 PNG"],
                    ["Content/Audio", "hatch_pet_young.wav / hatch_pet_adult.wav", "幼体/成年 WAV"],
                ],
                column_widths=[120, 360, 420],
            ),
            block("heading", level=3, text="2. 哈奇关键 ID 对照"),
            table_block("哈奇关键 ID 对照", ["用途", "哈奇实包值", "说明"], key_rows, column_widths=[170, 290, 440]),
            block("heading", level=3, text="3. 从哈奇复制一只新动物的顺序"),
        ]
    )
    for item in [
        "复制 DTMAPI_HatchAssets 到新文件夹，不要直接修改哈奇本体。",
        "先改包 ID：DTMAPI.HatchAssets 改成自己的 UniqueID。",
        "再改动物 ID：把 hatch 统一改成自己的 speciesId。",
        "改动物袋：sack_hatch 改成自己的动物袋 ID，并同步 packageItemId 和 preset_animal。",
        "改 PNG 前缀：anim_animal_hatch 改成自己的 customSpritePrefix，并按同样前缀重命名所有 PNG。",
        "替换 WAV：保持 audio-replacements.json 的阶段和声音事件一致，只改 file 路径和文件。",
        "产物默认沿用原版 meat；如果新增自定义产物，需要同步扩展产物道具和掉落库。",
        "同步调整价格、商店数量、图鉴文本、产物和数值。",
    ]:
        blocks.append(block("ordered", text=item))

    blocks.append(block("heading", level=3, text="4. 启动前检查清单"))
    for item in [
        "Content/DTMAPI/manifest.json 存在，Type 为 ContentPack，EntryDll 为空。",
        "speciesId、animal_tbanimal.id、preset_animal 三者一致。",
        "adultAnimatorKey / childAnimatorKey 和 levels[*].animator.url 一致。",
        "templateSpeciesId、aiTemplate、schedule_id 同步指向同一个模板。",
        "nativeSoundEvent 和 levels[*].sound_event 完全一致。",
        "PNG 数量和编号完整；同阶段画布尺寸一致。",
        "WAV 路径从包根目录可找到，例如 Content/Audio/hatch_pet_young.wav。",
        "商店扩展追加的是动物袋 ID，不要覆盖整个原版商店列表。",
    ]:
        blocks.append(block("bullet", text=item))

    blocks.append(block("heading", level=3, text="5. 游戏内手测顺序"))
    for item in [
        "启动游戏后确认日志里注册了内容包、custom-animals.json 和 audio-replacements.json。",
        "拿到动物袋，确认袋子图标和名称不是原版鸡。",
        "释放幼体，确认显示为新 PNG，不是原版鸡。",
        "抚摸幼体，确认播放幼体 WAV。",
        "等待成长或用测试流程进入成年，确认成年 PNG 正常。",
        "抚摸成年，确认播放成年 WAV。",
        "观察待机、移动、吃饭、睡觉、跳跃，不应缺帧或闪回原版图。",
        "收取产物，确认产物进入对应设备或掉落逻辑。",
        "同时放一只原版鸡，确认原版鸡贴图和声音没有被污染。",
        "保存、退出、重进，再看新动物仍能正常加载。",
    ]:
        blocks.append(block("ordered", text=item))

    return {
        "title": "新增养殖动物",
        "source": {
            "hatchRoot": validation["hatchRoot"],
            "generatedAt": validation["generatedAt"],
            "jsonCount": validation["jsonCount"],
            "pngCount": validation["pngCount"],
            "wavFiles": validation["wavFiles"],
        },
        "validation": validation,
        "highlight": {"backgroundColor": FONT_BACKGROUND_YELLOW, "terms": highlight_terms},
        "blocks": blocks,
    }


def feishu_block_for(model_block: dict[str, Any]) -> dict[str, Any] | None:
    kind = model_block["kind"]
    if kind == "heading":
        level = int(model_block["level"])
        block_type = level + 2
        return {"block_type": block_type, f"heading{level}": text_obj(model_block["text"])}
    if kind == "paragraph":
        return {"block_type": 2, "text": text_obj(model_block["text"])}
    if kind == "bullet":
        return {"block_type": 12, "bullet": text_obj(model_block["text"])}
    if kind == "ordered":
        return {"block_type": 13, "ordered": text_obj(model_block["text"])}
    if kind == "code":
        return {
            "block_type": 14,
            "code": {
                "style": {"language": model_block.get("language", CODE_LANGUAGE_JSON), "wrap": True},
                "elements": text_elements(model_block["content"], highlight_terms=model_block.get("highlightTerms")),
            },
        }
    if kind == "image":
        return {"block_type": 27, "image": {}}
    if kind == "table":
        return {
            "block_type": 31,
            "table": {
                "property": {
                    "row_size": len(model_block["rows"]) + 1,
                    "column_size": len(model_block["headers"]),
                }
            },
        }
    return None


def write_outputs(out_dir: Path, model: dict[str, Any]) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    payload_blocks = []
    for model_block in model["blocks"]:
        converted = feishu_block_for(model_block)
        if converted is not None:
            payload_blocks.append({"kind": model_block["kind"], "caption": model_block.get("caption"), "block": converted})
    payload = {
        "title": model["title"],
        "source": model["source"],
        "validation": model["validation"],
        "blocks": payload_blocks,
        "documentModel": model["blocks"],
    }
    (out_dir / "feishu-blocks.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    (out_dir / "validation-report.json").write_text(json.dumps(model["validation"], ensure_ascii=False, indent=2), encoding="utf-8")
    (out_dir / "hatch-feishu-doc.preview.md").write_text(render_markdown_preview(model), encoding="utf-8")
    (out_dir / "hatch-feishu-rich-copy.html").write_text(render_rich_copy_html(model), encoding="utf-8")


def render_markdown_preview(model: dict[str, Any]) -> str:
    lines = [f"# {model['title']}", ""]
    for item in model["blocks"]:
        kind = item["kind"]
        if kind == "heading":
            lines.extend([f"{'#' * item['level']} {item['text']}", ""])
        elif kind == "paragraph":
            lines.extend([item["text"], ""])
        elif kind == "bullet":
            lines.append(f"- {item['text']}")
        elif kind == "ordered":
            lines.append(f"1. {item['text']}")
        elif kind == "table":
            headers = item["headers"]
            lines.extend([f"**{item['caption']}**", ""])
            lines.append("| " + " | ".join(headers) + " |")
            lines.append("| " + " | ".join(["---"] * len(headers)) + " |")
            for row in item["rows"]:
                lines.append("| " + " | ".join(row) + " |")
            lines.append("")
        elif kind == "code":
            lines.extend([f"**{item['caption']}**", "", "```json", item["content"], "```", ""])
        elif kind == "image":
            lines.extend([f"**{item['caption']}**", "", f"![{item['caption']}]({posix_path(item['relativePath'])})", ""])
    return "\n".join(lines).replace("\n\n\n", "\n\n")


def highlighted_lark_code_html(text: str, highlight_terms: Iterable[str] | None = None) -> str:
    terms = [term for term in sorted(set(highlight_terms or []), key=len, reverse=True) if term]
    if not terms:
        return html.escape(text)
    pattern = re.compile("|".join(re.escape(term) for term in terms))
    parts: list[str] = []
    last = 0
    for match in pattern.finditer(text):
        if match.start() > last:
            parts.append(html.escape(text[last : match.start()]))
        parts.append(
            '<span style="background-color:rgba(255,246,122,0.8)">'
            f"{html.escape(match.group(0))}"
            "</span>"
        )
        last = match.end()
    if last < len(text):
        parts.append(html.escape(text[last:]))
    return "".join(parts)


def image_data_uri(image_path: Path) -> str:
    encoded = base64.b64encode(image_path.read_bytes()).decode("ascii")
    return f"data:image/png;base64,{encoded}"


def render_code_html(item: dict[str, Any]) -> str:
    code_html = highlighted_lark_code_html(item["content"], item.get("highlightTerms"))
    return (
        '<pre style="white-space:pre;" '
        f'class="ace-line feishu-code-block" data-code-caption="{html.escape(item["caption"])}">'
        '<code class="language-JSON" data-lark-language="JSON" data-wrap="false">'
        f"<div>{code_html}</div>"
        "</code></pre>"
    )


def render_table_html(item: dict[str, Any]) -> str:
    column_count = len(item["headers"])
    widths = item.get("columnWidths") or [max(120, 900 // max(1, column_count))] * column_count
    if len(widths) != column_count:
        widths = (widths + [max(120, 900 // max(1, column_count))] * column_count)[:column_count]
    colgroup = "".join(f'<col width="{int(width)}">' for width in widths)

    def cell_html(cell: str, *, header: bool = False) -> str:
        style = [
            "color:rgb(0, 0, 0)",
            "word-wrap:break-word",
            "word-break:break-word",
            "white-space:pre-wrap",
            "vertical-align:middle",
        ]
        if header:
            style.extend(["text-align:center", "font-weight:bold", "background-color:#f5f6f7"])
        return f'<td style="{";".join(style)};">{html.escape(str(cell))}</td>'

    header_html = "".join(cell_html(header, header=True) for header in item["headers"])
    rows_html = [f'<tr height="30">{header_html}</tr>']
    for row in item["rows"]:
        rows_html.append(
            '<tr height="34">'
            + "".join(cell_html(str(row[index]) if index < len(row) else "") for index in range(column_count))
            + "</tr>"
        )
    table_id = uuid.uuid5(uuid.NAMESPACE_URL, item["caption"]).int % 10_000_000_000_000
    return (
        f'<div class="table-caption">{html.escape(item["caption"])}</div>'
        '<byte-sheet-html-origin '
        f'data-id="{table_id}" data-version="4" data-is-embed="true" '
        'data-grid-line-hidden="false" data-lark-html-role="root">'
        '<table class="doc-table feishu-sheet-table" style="border-collapse: collapse;">'
        f"<colgroup>{colgroup}</colgroup>"
        f"<tbody>{''.join(rows_html)}</tbody>"
        "</table>"
        "</byte-sheet-html-origin>"
    )


def render_image_html(item: dict[str, Any]) -> str:
    image_path = Path(item["path"])
    return (
        f'<div class="image-title">{html.escape(item["caption"])}</div>'
        '<figure class="frame-figure">'
        f'<img src="{image_data_uri(image_path)}" alt="{html.escape(item["caption"])}">'
        "</figure>"
    )


def render_html_blocks(model: dict[str, Any]) -> str:
    parts: list[str] = []
    list_kind: str | None = None

    def close_list() -> None:
        nonlocal list_kind
        if list_kind == "bullet":
            parts.append("</ul>")
        elif list_kind == "ordered":
            parts.append("</ol>")
        list_kind = None

    for item in model["blocks"]:
        kind = item["kind"]
        if kind in {"bullet", "ordered"}:
            if list_kind != kind:
                close_list()
                parts.append('<ul class="doc-list">' if kind == "bullet" else '<ol class="doc-list">')
                list_kind = kind
            parts.append(f"<li>{html.escape(item['text'])}</li>")
            continue

        close_list()
        if kind == "heading":
            level = max(1, min(int(item["level"]), 4))
            parts.append(f'<h{level}>{html.escape(item["text"])}</h{level}>')
        elif kind == "paragraph":
            parts.append(f'<p>{html.escape(item["text"])}</p>')
        elif kind == "table":
            parts.append(render_table_html(item))
        elif kind == "code":
            parts.append(render_code_html(item))
        elif kind == "image":
            parts.append(render_image_html(item))

    close_list()
    return "\n".join(parts)


def render_rich_copy_html(model: dict[str, Any]) -> str:
    body_html = render_html_blocks(model)
    plain_text = html.escape(render_markdown_preview(model))
    generated_at = html.escape(model["source"]["generatedAt"])
    return f"""<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{html.escape(model["title"])} - 飞书富文本复制页</title>
  <style>
    :root {{
      color-scheme: light;
      --text: #1f2329;
      --muted: #646a73;
      --line: #dee0e3;
      --soft: #f5f6f7;
      --code: #f7f8fa;
      --blue: #245bdb;
      --mark: #fff36d;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      background: #ffffff;
      color: var(--text);
      font: 15px/1.75 -apple-system, BlinkMacSystemFont, "Segoe UI", "Microsoft YaHei", sans-serif;
    }}
    .copy-bar {{
      position: sticky;
      top: 0;
      z-index: 10;
      display: flex;
      gap: 12px;
      align-items: center;
      justify-content: center;
      padding: 12px 24px;
      border-bottom: 1px solid var(--line);
      background: rgba(255, 255, 255, 0.96);
      backdrop-filter: blur(8px);
    }}
    .copy-bar button {{
      border: 0;
      border-radius: 6px;
      padding: 8px 16px;
      background: var(--blue);
      color: #fff;
      font-weight: 600;
      cursor: pointer;
    }}
    .copy-bar button.secondary {{
      border: 1px solid var(--line);
      background: #fff;
      color: var(--text);
    }}
    .copy-bar span {{ color: var(--muted); font-size: 13px; }}
    #doc-fragment {{
      width: min(920px, calc(100vw - 64px));
      margin: 52px auto 80px;
    }}
    h1 {{
      margin: 0 0 28px;
      font-size: 34px;
      line-height: 1.25;
      font-weight: 800;
      letter-spacing: 0;
    }}
    h2 {{
      margin: 42px 0 14px;
      font-size: 24px;
      line-height: 1.35;
      font-weight: 800;
      letter-spacing: 0;
    }}
    h3 {{
      margin: 30px 0 12px;
      font-size: 19px;
      line-height: 1.4;
      font-weight: 800;
      letter-spacing: 0;
    }}
    p {{ margin: 10px 0 14px; }}
    .doc-list {{
      margin: 8px 0 16px 22px;
      padding: 0;
    }}
    .doc-list li {{
      margin: 5px 0;
      padding-left: 3px;
    }}
    .table-caption {{
      margin: 18px 0 8px;
      color: var(--muted);
      font-size: 13px;
      font-weight: 600;
    }}
    .doc-table {{
      width: auto;
      min-width: max-content;
      margin: 0 0 18px;
      border-collapse: collapse;
      table-layout: fixed;
      font-size: 14px;
    }}
    .doc-table th,
    .doc-table td {{
      border: 1px solid var(--line);
      padding: 8px 10px;
      vertical-align: top;
      word-break: break-word;
    }}
    .doc-table th {{
      background: var(--soft);
      font-weight: 700;
    }}
    byte-sheet-html-origin {{
      display: block;
      max-width: 100%;
      margin: 0 0 18px;
      overflow-x: auto;
    }}
    .feishu-sheet-table {{
      margin-bottom: 0;
    }}
    .feishu-code-block {{
      position: relative;
      margin: 16px 0 26px;
      padding: 44px 16px 14px;
      overflow-x: auto;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: var(--code);
      font: 13px/1.75 Consolas, "SFMono-Regular", "Cascadia Mono", "Microsoft YaHei Mono", monospace;
      white-space: pre;
    }}
    .feishu-code-block::before {{
      content: attr(data-code-caption);
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 32px;
      padding: 6px 12px;
      border-bottom: 1px solid var(--line);
      color: var(--muted);
      background: #f1f3f5;
      font: 13px/20px -apple-system, BlinkMacSystemFont, "Segoe UI", "Microsoft YaHei", sans-serif;
    }}
    .feishu-code-block code,
    .feishu-code-block div {{
      display: block;
      margin: 0;
      padding: 0;
      white-space: pre;
    }}
    .feishu-code-block span[style*="background-color"] {{
      padding: 0 1px;
    }}
    .image-title {{
      margin: 22px 0 8px;
      color: var(--text);
      font-size: 14px;
      font-weight: 700;
    }}
    .frame-figure {{
      margin: 0 0 28px;
      padding: 0;
    }}
    .frame-figure img {{
      display: block;
      max-width: 100%;
      height: auto;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: #fff;
    }}
    @media (max-width: 720px) {{
      #doc-fragment {{ width: calc(100vw - 28px); margin-top: 28px; }}
      .copy-bar {{ align-items: flex-start; flex-direction: column; }}
      .doc-table {{ font-size: 13px; }}
    }}
  </style>
</head>
<body>
  <div class="copy-bar">
    <button type="button" onclick="copyRich()">复制富文本</button>
    <button type="button" class="secondary" onclick="selectDocument()">选择正文</button>
    <span id="copy-status">本页由本地哈奇实包生成，生成时间：{generated_at}</span>
  </div>
  <main id="doc-fragment">
{body_html}
  </main>
  <textarea id="plain-text" hidden>{plain_text}</textarea>
  <script>
    function htmlPayload() {{
      const fragment = document.getElementById("doc-fragment").innerHTML;
      return '<meta charset="utf-8"><div data-lark-html-role="root" data-docx-has-block-data="false"><!--StartFragment-->' + fragment + '<!--EndFragment--></div>';
    }}

    function selectDocument() {{
      const range = document.createRange();
      range.selectNodeContents(document.getElementById("doc-fragment"));
      const selection = window.getSelection();
      selection.removeAllRanges();
      selection.addRange(range);
      document.getElementById("copy-status").textContent = "正文已选中，可以按 Ctrl+C 后粘贴到飞书。";
    }}

    async function copyRich() {{
      const status = document.getElementById("copy-status");
      const html = htmlPayload();
      const text = document.getElementById("plain-text").value;
      try {{
        if (navigator.clipboard && window.ClipboardItem) {{
          await navigator.clipboard.write([
            new ClipboardItem({{
              "text/html": new Blob([html], {{ type: "text/html" }}),
              "text/plain": new Blob([text], {{ type: "text/plain" }})
            }})
          ]);
        }} else {{
          selectDocument();
          if (!document.execCommand("copy")) {{
            throw new Error("document.execCommand(copy) returned false");
          }}
        }}
        status.textContent = "已复制富文本。切到飞书文档后按 Ctrl+V。";
      }} catch (error) {{
        selectDocument();
        status.textContent = "浏览器拒绝直接写剪贴板，已选中正文；请按 Ctrl+C 后粘贴到飞书。";
      }}
    }}
  </script>
</body>
</html>
"""


class FeishuClient:
    def __init__(self, token: str) -> None:
        self.token = token
        self.base = "https://open.feishu.cn/open-apis"

    def _headers(self) -> dict[str, str]:
        return {"Authorization": f"Bearer {self.token}"}

    def request_json(self, method: str, path: str, *, params: dict[str, Any] | None = None, body: Any = None) -> dict[str, Any]:
        url = f"{self.base}{path}"
        headers = self._headers()
        if body is not None:
            headers["Content-Type"] = "application/json"
        response = requests.request(method, url, params=params, json=body, headers=headers, timeout=45)
        try:
            data = response.json()
        except Exception:
            data = {"raw": response.text}
        if response.status_code >= 400 or data.get("code", 0) != 0:
            raise RuntimeError(f"Feishu API failed {method} {path}: HTTP {response.status_code} {data}")
        return data

    def create_children(self, document_id: str, parent_block_id: str, children: list[dict[str, Any]], *, index: int | None = None) -> dict[str, Any]:
        body: dict[str, Any] = {"children": children}
        if index is not None:
            body["index"] = index
        return self.request_json(
            "POST",
            f"/docx/v1/documents/{document_id}/blocks/{parent_block_id}/children",
            params={"client_token": str(uuid.uuid4())},
            body=body,
        )

    def get_children(self, document_id: str, block_id: str) -> dict[str, Any]:
        return self.request_json("GET", f"/docx/v1/documents/{document_id}/blocks/{block_id}/children")

    def upload_docx_image(self, image_block_id: str, image_path: Path) -> dict[str, Any]:
        url = f"{self.base}/drive/v1/medias/upload_all"
        with image_path.open("rb") as handle:
            files = {"file": (image_path.name, handle, "image/png")}
            data = {
                "file_name": image_path.name,
                "parent_type": "docx_image",
                "parent_node": image_block_id,
                "size": str(image_path.stat().st_size),
            }
            response = requests.post(url, headers=self._headers(), data=data, files=files, timeout=60)
        payload = response.json()
        if response.status_code >= 400 or payload.get("code", 0) != 0:
            raise RuntimeError(f"Feishu image upload failed: HTTP {response.status_code} {payload}")
        return payload

    def resolve_wiki_url(self, document_url: str) -> str:
        parsed = urlparse(document_url)
        path_parts = [part for part in parsed.path.split("/") if part]
        if "docx" in path_parts:
            return path_parts[path_parts.index("docx") + 1]
        if "wiki" in path_parts:
            token = path_parts[path_parts.index("wiki") + 1]
            data = self.request_json("GET", "/wiki/v2/spaces/get_node", params={"token": token})
            node = data.get("data", {}).get("node", {})
            obj_token = node.get("obj_token") or node.get("objToken")
            if obj_token:
                return obj_token
            raise RuntimeError(f"Could not resolve wiki token {token}: {data}")
        query = parse_qs(parsed.query)
        for key in ("document_id", "doc_id", "token"):
            if query.get(key):
                return query[key][0]
        raise ValueError("Could not parse a docx document_id from the provided URL")


def upload_document(model: dict[str, Any], out_dir: Path, document_id: str, parent_block_id: str, token: str) -> dict[str, Any]:
    client = FeishuClient(token)
    log: list[dict[str, Any]] = []
    current_index = 0
    for item in model["blocks"]:
        converted = feishu_block_for(item)
        if converted is None:
            continue
        response = client.create_children(document_id, parent_block_id, [converted], index=current_index)
        created = response.get("data", {}).get("children", [])
        block_id = created[0].get("block_id") if created else None
        log.append({"kind": item["kind"], "caption": item.get("caption"), "blockId": block_id})
        if item["kind"] == "image" and block_id:
            upload_response = client.upload_docx_image(block_id, Path(item["path"]))
            log[-1]["upload"] = upload_response.get("data", {})
        elif item["kind"] == "table" and block_id:
            fill_table_cells(client, document_id, block_id, item, log[-1])
        current_index += 1
        time.sleep(0.42)
    upload_log = {"documentId": document_id, "parentBlockId": parent_block_id, "operations": log}
    (out_dir / "feishu-upload-log.json").write_text(json.dumps(upload_log, ensure_ascii=False, indent=2), encoding="utf-8")
    return upload_log


def fill_table_cells(
    client: FeishuClient,
    document_id: str,
    table_block_id: str,
    table: dict[str, Any],
    log_entry: dict[str, Any],
) -> None:
    children_response = client.get_children(document_id, table_block_id)
    cells = children_response.get("data", {}).get("items") or children_response.get("data", {}).get("children") or []
    cell_ids = [cell.get("block_id") for cell in cells if cell.get("block_id")]
    values = [table["headers"], *table["rows"]]
    flat_values = [value for row in values for value in row]
    if len(cell_ids) < len(flat_values):
        log_entry["tableFill"] = {"status": "skipped", "reason": f"expected {len(flat_values)} cells, got {len(cell_ids)}"}
        return
    for cell_id, value in zip(cell_ids, flat_values):
        client.create_children(document_id, cell_id, [{"block_type": 2, "text": text_obj(str(value))}])
        time.sleep(0.42)
    log_entry["tableFill"] = {"status": "filled", "cells": len(flat_values)}


def build(out_dir: Path, hatch_root: Path) -> dict[str, Any]:
    specs, validation = validate_hatch_package(hatch_root)
    atlas_entries = generate_frame_atlases(hatch_root, out_dir)
    data_by_rel = {spec.rel_path: spec.data for spec in specs}
    highlight_terms = collect_highlight_terms(data_by_rel)
    model = build_document_model(specs, validation, atlas_entries, highlight_terms)
    write_outputs(out_dir, model)
    return model


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--hatch-root", type=Path, default=DEFAULT_HATCH_ROOT, help="Path to DTMAPI_HatchAssets.")
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR, help="Directory for generated payloads and frame atlases.")
    parser.add_argument("--upload", action="store_true", help="Write the generated model to Feishu Docx OpenAPI.")
    parser.add_argument("--document-id", default=os.environ.get("FEISHU_DOC_ID"), help="Feishu docx document_id.")
    parser.add_argument("--document-url", default=os.environ.get("FEISHU_DOC_URL"), help="Feishu docx/wiki URL to resolve.")
    parser.add_argument("--parent-block-id", default=os.environ.get("FEISHU_PARENT_BLOCK_ID"), help="Parent block id. Defaults to document_id.")
    parser.add_argument("--access-token", default=os.environ.get("FEISHU_ACCESS_TOKEN"), help="Feishu bearer token.")
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    model = build(args.out_dir, args.hatch_root)
    print(f"Generated {args.out_dir / 'feishu-blocks.json'}")
    print(f"Generated {args.out_dir / 'hatch-feishu-rich-copy.html'}")
    print(f"Generated {len([b for b in model['blocks'] if b['kind'] == 'code'])} code blocks and 5 frame atlases")

    if not args.upload:
        return 0
    if not args.access_token:
        print("FEISHU_ACCESS_TOKEN is required for --upload", file=sys.stderr)
        return 2
    client = FeishuClient(args.access_token)
    document_id = args.document_id
    if not document_id and args.document_url:
        document_id = client.resolve_wiki_url(args.document_url)
    if not document_id:
        print("FEISHU_DOC_ID or --document-id/--document-url is required for --upload", file=sys.stderr)
        return 2
    parent_block_id = args.parent_block_id or document_id
    upload_log = upload_document(model, args.out_dir, document_id, parent_block_id, args.access_token)
    print(f"Uploaded {len(upload_log['operations'])} top-level blocks to Feishu document {document_id}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
