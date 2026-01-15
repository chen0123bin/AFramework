import argparse
import os
import sys


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


def main():
    """命令行入口：在 uguitoolkit/references 下搜索关键词并输出命中位置。"""
    parser = argparse.ArgumentParser()
    parser.add_argument("query", help="要搜索的关键词（大小写不敏感）")
    parser.add_argument("--root", default=None, help="references 根目录（默认使用当前脚本所在技能目录）")
    parser.add_argument("--max-hits", type=int, default=30, help="每个文件最多输出命中行数")
    parser.add_argument("--max-files", type=int, default=80, help="最多输出命中的文件数量")
    args = parser.parse_args()

    script_dir = os.path.dirname(os.path.abspath(__file__))
    skill_dir = os.path.dirname(script_dir)
    default_root = os.path.join(skill_dir, "references")
    root_dir = os.path.abspath(args.root) if args.root else default_root

    if not os.path.isdir(root_dir):
        print(f"references 目录不存在：{root_dir}", file=sys.stderr)
        return 2

    query = args.query.strip()
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
