import argparse
import os
import sys
import json
import csv
import re
import math


def iter_text_files(root_dir: str):
    """遍历 references 下可读的文本文件（md/json/txt）。"""
    for base, _, files in os.walk(root_dir):
        for name in files:
            lower = name.lower()
            if lower.endswith(".md") or lower.endswith(".json") or lower.endswith(".txt"):
                yield os.path.join(base, name)


def search_file(file_path: str, query_lower: str, max_hits: int):
    """在单个文件中按行搜索关键词（大小写不敏感），返回命中的行号与内容。"""
    hits = 0
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            for i, line in enumerate(f, start=1):
                if query_lower in line.lower():
                    yield (i, line.rstrip("\n"))
                    hits += 1
                    if hits >= max_hits:
                        return
    except UnicodeDecodeError:
        return
    except OSError:
        return


def hex_to_rgb01(hex_value: str):
    """将 #RRGGBB 十六进制颜色转换为 0~1 的 RGB 三元组。"""
    value = hex_value.strip().lstrip("#")
    r = int(value[0:2], 16) / 255.0
    g = int(value[2:4], 16) / 255.0
    b = int(value[4:6], 16) / 255.0
    return (r, g, b)


def clamp01(x: float) -> float:
    """把数值夹到 0~1 区间。"""
    return 0.0 if x < 0.0 else (1.0 if x > 1.0 else x)


def rgb01_to_hex(rgb01) -> str:
    """将 0~1 RGB 三元组转换为 #RRGGBB。"""
    r = int(round(clamp01(float(rgb01[0])) * 255.0))
    g = int(round(clamp01(float(rgb01[1])) * 255.0))
    b = int(round(clamp01(float(rgb01[2])) * 255.0))
    return f"#{r:02X}{g:02X}{b:02X}"


def relative_luminance(rgb01) -> float:
    """计算相对亮度（用于判断主题是偏亮还是偏暗）。"""

    def to_linear(c: float) -> float:
        return c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4

    r, g, b = rgb01
    return 0.2126 * to_linear(float(r)) + 0.7152 * to_linear(float(g)) + 0.0722 * to_linear(float(b))


def mix_rgb(a, b, t: float):
    """线性混合两种 RGB 颜色（t=0 返回 a，t=1 返回 b）。"""
    u = 1.0 - t
    return (a[0] * u + b[0] * t, a[1] * u + b[1] * t, a[2] * u + b[2] * t)


def make_color_token(hex_value: str, alpha: float = 1.0):
    """生成 theme-tokens.json 中单个颜色 token 的标准结构。"""
    rgb01 = hex_to_rgb01(hex_value)
    return {
        "hex": hex_value.upper(),
        "rgba": [
            round(float(rgb01[0]), 6),
            round(float(rgb01[1]), 6),
            round(float(rgb01[2]), 6),
            round(float(alpha), 6),
        ],
    }


def sanitize_theme_name(name: str) -> str:
    """将主题名规范化为只包含字母/数字/下划线的标识符。"""
    safe = re.sub(r"[^0-9A-Za-z]+", "_", (name or "").strip())
    safe = re.sub(r"_+", "_", safe).strip("_")
    return safe if safe else "Theme"


def import_themes_from_colors_csv(colors_csv_path: str, theme_tokens_path: str) -> tuple[int, int, int]:
    """从 ui-ux-pro-max 的 colors.csv 批量生成主题并合并到 theme-tokens.json。"""
    if not os.path.isfile(colors_csv_path):
        raise FileNotFoundError(f"colors.csv 不存在：{colors_csv_path}")
    if not os.path.isfile(theme_tokens_path):
        raise FileNotFoundError(f"theme-tokens.json 不存在：{theme_tokens_path}")

    with open(colors_csv_path, "r", encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f))

    unique_palettes = {}
    for row in rows:
        key = (
            (row.get("Primary (Hex)") or "").strip().upper(),
            (row.get("Secondary (Hex)") or "").strip().upper(),
            (row.get("CTA (Hex)") or "").strip().upper(),
            (row.get("Background (Hex)") or "").strip().upper(),
            (row.get("Text (Hex)") or "").strip().upper(),
            (row.get("Border (Hex)") or "").strip().upper(),
        )
        if key not in unique_palettes:
            unique_palettes[key] = row

    with open(theme_tokens_path, "r", encoding="utf-8-sig") as f:
        theme_data = json.load(f)

    themes = theme_data.get("themes", {})
    added = 0

    for key, row in unique_palettes.items():
        product = (row.get("Product Type") or "").strip()
        primary, secondary, cta, bg, text, border = key

        bg_rgb = hex_to_rgb01(bg)
        bg_lum = relative_luminance(bg_rgb)
        is_dark = bg_lum < 0.22

        base_name = sanitize_theme_name(product)
        theme_name = ("UXPM_Dark_" if is_dark else "UXPM_Light_") + base_name

        if theme_name in themes:
            continue

        border_rgb = hex_to_rgb01(border)
        text_rgb = hex_to_rgb01(text)

        if is_dark:
            white = (1.0, 1.0, 1.0)
            surface_rgb = mix_rgb(bg_rgb, white, 0.06)
            surface_alt_rgb = mix_rgb(bg_rgb, white, 0.10)
            text_secondary_rgb = mix_rgb(text_rgb, bg_rgb, 0.22)
            tokens = {
                "Bg": make_color_token(bg, 1.0),
                "Surface": {"hex": rgb01_to_hex(surface_rgb), "rgba": [round(surface_rgb[0], 6), round(surface_rgb[1], 6), round(surface_rgb[2], 6), 1.0]},
                "SurfaceAlt": {
                    "hex": rgb01_to_hex(surface_alt_rgb),
                    "rgba": [round(surface_alt_rgb[0], 6), round(surface_alt_rgb[1], 6), round(surface_alt_rgb[2], 6), 1.0],
                },
                "SurfaceGlass": make_color_token("#FFFFFF", 0.12),
                "SurfaceGlassStrong": make_color_token("#FFFFFF", 0.2),
                "Primary": make_color_token(primary, 1.0),
                "Secondary": make_color_token(secondary, 1.0),
                "CTA": make_color_token(cta, 1.0),
                "TextPrimary": make_color_token(text, 1.0),
                "TextSecondary": {
                    "hex": rgb01_to_hex(text_secondary_rgb),
                    "rgba": [round(text_secondary_rgb[0], 6), round(text_secondary_rgb[1], 6), round(text_secondary_rgb[2], 6), 1.0],
                },
                "Border": make_color_token(border, 1.0),
                "BorderGlass": make_color_token("#FFFFFF", 0.2),
                "Overlay": make_color_token("#000000", 0.65),
            }
        else:
            surface_alt_rgb = mix_rgb(bg_rgb, border_rgb, 0.55)
            text_secondary_rgb = mix_rgb(text_rgb, bg_rgb, 0.22)
            tokens = {
                "Bg": make_color_token(bg, 1.0),
                "Surface": make_color_token("#FFFFFF", 1.0),
                "SurfaceAlt": {
                    "hex": rgb01_to_hex(surface_alt_rgb),
                    "rgba": [round(surface_alt_rgb[0], 6), round(surface_alt_rgb[1], 6), round(surface_alt_rgb[2], 6), 1.0],
                },
                "Primary": make_color_token(primary, 1.0),
                "Secondary": make_color_token(secondary, 1.0),
                "CTA": make_color_token(cta, 1.0),
                "TextPrimary": make_color_token(text, 1.0),
                "TextSecondary": {
                    "hex": rgb01_to_hex(text_secondary_rgb),
                    "rgba": [round(text_secondary_rgb[0], 6), round(text_secondary_rgb[1], 6), round(text_secondary_rgb[2], 6), 1.0],
                },
                "Border": make_color_token(border, 1.0),
                "Overlay": make_color_token("#0F172A", 0.55),
            }

        desc = f"ui-ux-pro-max/colors.csv: {product} | Primary {primary} Secondary {secondary} CTA {cta}"
        themes[theme_name] = {"description": desc, "tokens": tokens}
        added += 1

    theme_data["themes"] = themes

    with open(theme_tokens_path, "w", encoding="utf-8", newline="") as f:
        json.dump(theme_data, f, ensure_ascii=False, indent=2)
        f.write("\n")

    return (len(unique_palettes), added, len(themes))


def iter_component_spec_files(spec_dir: str):
    """遍历 references/spec 下的旧版组件描述文件（component-*.json）。"""
    if not os.path.isdir(spec_dir):
        return

    for name in os.listdir(spec_dir):
        lower = name.lower()
        if lower.startswith("component-") and lower.endswith(".json"):
            yield os.path.join(spec_dir, name)


def iter_component_files(components_dir: str, spec_dir: str):
    """遍历自定义组件描述文件：优先 components/*.md/.json，其次兼容 spec/component-*.json。"""
    if os.path.isdir(components_dir):
        for name in os.listdir(components_dir):
            lower = name.lower()
            if lower.endswith(".md") or lower.endswith(".json"):
                yield os.path.join(components_dir, name)

    for file_path in iter_component_spec_files(spec_dir):
        yield file_path


def load_json_file(file_path: str):
    """读取 JSON 文件并返回对象，兼容 utf-8/utf-8-sig。"""
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except UnicodeDecodeError:
        with open(file_path, "r", encoding="utf-8-sig") as f:
            return json.load(f)


def read_component_specs(components_dir: str, spec_dir: str) -> list[dict]:
    """读取所有组件描述（components 优先），并补充来源路径字段。

    - .json：解析为结构化信息
    - .md：仅记录文件名与显示标题（从第一行 '# ' 提取）
    """
    specs_by_key: dict[str, dict] = {}

    for file_path in iter_component_files(components_dir, spec_dir):
        file_name = os.path.basename(file_path)
        file_stem = os.path.splitext(file_name)[0]
        ext = os.path.splitext(file_name)[1].lower()

        if ext == ".md":
            try:
                with open(file_path, "r", encoding="utf-8") as f:
                    first_line = (f.readline() or "").strip()
            except Exception:
                first_line = ""

            display_name = ""
            if first_line.startswith("#"):
                display_name = first_line.lstrip("#").strip()

            key = file_stem
            if key in specs_by_key:
                continue

            specs_by_key[key] = {
                "componentName": file_stem,
                "displayName": display_name,
                "__file": file_path,
                "__format": "markdown",
            }
            continue

        try:
            data = load_json_file(file_path)
        except Exception:
            continue
        if not isinstance(data, dict):
            continue

        component_name = str(data.get("componentName", "")).strip()
        key = component_name if component_name else file_stem
        if not key:
            continue
        if key in specs_by_key:
            continue

        data["__file"] = file_path
        data["__format"] = "json"
        specs_by_key[key] = data

    return list(specs_by_key.values())


def main():
    """命令行入口：在 uguitoolkit/references 下搜索关键词并输出命中位置。"""
    parser = argparse.ArgumentParser()
    parser.add_argument("query", nargs="?", default="", help="要搜索的关键词（大小写不敏感）")
    parser.add_argument("--root", default=None, help="references 根目录（默认使用当前脚本所在技能目录）")
    parser.add_argument("--max-hits", type=int, default=30, help="每个文件最多输出命中行数")
    parser.add_argument("--max-files", type=int, default=80, help="最多输出命中的文件数量")
    parser.add_argument("--list-themes", action="store_true", help="列出 design/theme-tokens.json 内的主题")
    parser.add_argument("--show-theme", default=None, help="打印指定主题的 tokens（例如 Light_Default_SaaS）")
    parser.add_argument("--token", default=None, help="配合 --show-theme，仅输出某个 token（例如 Primary）")
    parser.add_argument("--list-components", action="store_true", help="列出 references/components 下可用的组件描述（*.md/*.json）")
    parser.add_argument("--show-component", default=None, help="打印指定组件描述（例如 RoundedImage）")
    parser.add_argument("--import-colors", action="store_true", help="从 ui-ux-pro-max/colors.csv 导入主题到 theme-tokens.json")
    parser.add_argument("--colors-csv", default=None, help="colors.csv 路径（默认自动定位到 ui-ux-pro-max 技能）")
    args = parser.parse_args()

    script_dir = os.path.dirname(os.path.abspath(__file__))
    skill_dir = os.path.dirname(script_dir)
    default_root = os.path.join(skill_dir, "references")
    root_dir = os.path.abspath(args.root) if args.root else default_root

    theme_tokens_path = os.path.join(root_dir, "design", "theme-tokens.json")
    spec_dir = os.path.join(root_dir, "spec")
    components_dir = os.path.join(root_dir, "components")

    if args.import_colors:
        skills_root = os.path.dirname(skill_dir)
        default_colors_csv = os.path.join(skills_root, "ui-ux-pro-max", "data", "colors.csv")
        colors_csv_path = os.path.abspath(args.colors_csv) if args.colors_csv else default_colors_csv

        try:
            unique_count, added, total = import_themes_from_colors_csv(colors_csv_path, theme_tokens_path)
        except Exception as e:
            print(f"导入主题失败：{e}", file=sys.stderr)
            return 2

        print(f"已导入主题：unique_palettes={unique_count} added={added} total_themes={total}")
        return 0

    if args.list_themes or args.show_theme:
        if not os.path.isfile(theme_tokens_path):
            print(f"theme-tokens.json 不存在：{theme_tokens_path}", file=sys.stderr)
            return 2

        try:
            with open(theme_tokens_path, "r", encoding="utf-8") as f:
                theme_data = json.load(f)
        except Exception as e:
            print(f"读取 theme-tokens.json 失败：{e}", file=sys.stderr)
            return 2

        themes = theme_data.get("themes", {})
        if args.list_themes:
            if not themes:
                print("未定义任何主题。")
                return 0
            print("Themes:")
            for theme_name in sorted(themes.keys()):
                desc = themes.get(theme_name, {}).get("description", "")
                desc_suffix = f" - {desc}" if desc else ""
                print(f"- {theme_name}{desc_suffix}")
            return 0

        theme_name = (args.show_theme or "").strip()
        if not theme_name:
            print("--show-theme 不能为空", file=sys.stderr)
            return 2

        theme = themes.get(theme_name)
        if not theme:
            print(f"未找到主题：{theme_name}", file=sys.stderr)
            return 2

        tokens = theme.get("tokens", {})
        if args.token:
            token_name = args.token.strip()
            token_value = tokens.get(token_name)
            if token_value is None:
                print(f"主题 {theme_name} 中未找到 token：{token_name}", file=sys.stderr)
                return 2
            print(json.dumps({token_name: token_value}, ensure_ascii=False, indent=2))
            return 0

        print(json.dumps({"theme": theme_name, "description": theme.get("description", ""), "tokens": tokens}, ensure_ascii=False, indent=2))
        return 0

    if args.list_components or args.show_component:
        specs = read_component_specs(components_dir, spec_dir)

        if args.list_components:
            if not specs:
                print("未找到任何组件描述文件（component-*.json）。")
                return 0
            print("Components:")
            def sort_key(item: dict):
                return (str(item.get("componentName", "")), str(item.get("displayName", "")))

            for item in sorted(specs, key=sort_key):
                component_name = str(item.get("componentName", "")).strip()
                display_name = str(item.get("displayName", "")).strip()
                file_name = os.path.basename(str(item.get("__file", "")))
                label = component_name if component_name else file_name
                display_suffix = f" - {display_name}" if display_name else ""
                print(f"- {label}{display_suffix} ({file_name})")
            return 0

        component_name = (args.show_component or "").strip()
        if not component_name:
            print("--show-component 不能为空", file=sys.stderr)
            return 2

        matched = None
        lowered = component_name.lower()
        for item in specs:
            name_value = str(item.get("componentName", "")).strip()
            file_name = os.path.basename(str(item.get("__file", "")))
            if name_value.lower() == lowered:
                matched = item
                break
            if file_name.lower() in (f"{lowered}.md", f"{lowered}.json", f"component-{lowered}.json"):
                matched = item
                break

        if matched is None:
            for item in specs:
                name_value = str(item.get("componentName", "")).strip().lower()
                display_value = str(item.get("displayName", "")).strip().lower()
                if lowered in name_value or lowered in display_value:
                    matched = item
                    break

        if matched is None:
            print(f"未找到组件描述：{component_name}", file=sys.stderr)
            return 2

        file_path = str(matched.get("__file", ""))
        fmt = str(matched.get("__format", "json"))

        if fmt == "markdown" and file_path:
            try:
                with open(file_path, "r", encoding="utf-8") as f:
                    print(f.read())
            except Exception as e:
                print(f"读取组件 Markdown 失败：{e}", file=sys.stderr)
                return 2
            return 0

        output = dict(matched)
        output.pop("__file", None)
        output.pop("__format", None)
        print(json.dumps(output, ensure_ascii=False, indent=2))
        return 0

    if not os.path.isdir(root_dir):
        print(f"references 目录不存在：{root_dir}", file=sys.stderr)
        return 2

    query = (args.query or "").strip()
    if not query:
        print("query 不能为空", file=sys.stderr)
        return 2

    query_lower = query.lower()
    matched_files = 0

    for file_path in iter_text_files(root_dir):
        rel = os.path.relpath(file_path, skill_dir)
        filename_hit = query_lower in rel.lower()
        per_file_hits = list(search_file(file_path, query_lower, args.max_hits))
        if (not filename_hit) and (not per_file_hits):
            continue

        matched_files += 1
        suffix = "（文件名匹配）" if filename_hit and (not per_file_hits) else ""
        print(f"\n== {rel} =={suffix}")
        if filename_hit and (not per_file_hits):
            print("0: <文件名匹配>")
        for (line_no, content) in per_file_hits:
            print(f"{line_no}: {content}")

        if matched_files >= args.max_files:
            print(f"\n已达到最大文件数限制（{args.max_files}）。")
            break

    if matched_files == 0:
        print("未找到匹配内容。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
