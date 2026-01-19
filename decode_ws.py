import re
import sys


def decode_hex_line(line: str) -> str:
    line = line.strip()
    if not line:
        return ""
    try:
        data = bytes.fromhex(line)
    except ValueError:
        return ""

    if len(data) >= 2 and sum(1 for b in data[1::2] if b == 0) > len(data) / 4:
        for encoding in ("utf-16le", "utf-8", "latin1"):
            try:
                return data.decode(encoding, errors="ignore")
            except Exception:
                continue
    else:
        for encoding in ("utf-8", "latin1", "utf-16le"):
            try:
                return data.decode(encoding, errors="ignore")
            except Exception:
                continue

    return ""


def clean_text(text: str) -> str:
    text = text.replace("\r\n", "\n").replace("\r", "\n")
    cleaned = []
    for ch in text:
        if ch == "\n":
            cleaned.append(ch)
            continue
        codepoint = ord(ch)
        if 32 <= codepoint <= 126:
            cleaned.append(ch)
    return "".join(cleaned)


def main() -> int:
    input_path = sys.argv[1] if len(sys.argv) > 1 else "wireshark_output_7.txt"
    try:
        with open(input_path, "r", encoding="ascii", errors="ignore") as handle:
            raw_lines = handle.readlines()
    except OSError as exc:
        print(f"Failed to read {input_path}: {exc}", file=sys.stderr)
        return 1

    decoded_chunks = []
    for raw_line in raw_lines:
        decoded = decode_hex_line(raw_line)
        cleaned = clean_text(decoded)
        if cleaned:
            decoded_chunks.append(cleaned)

    full_text = "\n".join(decoded_chunks)
    raw_lines = [line.strip() for line in full_text.splitlines() if line.strip()]

    prefixes = ("Input", "Driver", "System Manager", "Macro")
    normalized_lines = []
    for line in raw_lines:
        match = re.search(r"(Input|Driver|System Manager|Macro|hello)", line)
        if match and match.start() > 0:
            line = line[match.start():]
        normalized_lines.append(line.strip())

    date_pattern = re.compile(r"\d{2}/\d{2}/\d{4}")
    logical_lines = []
    for line in normalized_lines:
        if not line or not any(ch.isalnum() for ch in line):
            continue
        if line.strip() == "hello":
            continue
        if line.strip().isdigit():
            continue
        if (
            not date_pattern.search(line)
            and logical_lines
            and (
                line.startswith("Driver event")
                or line.startswith("Driver - Command")
                or line.startswith("System Manager")
                or (not line.startswith(prefixes) and not line[0].isdigit())
            )
        ):
            logical_lines[-1] = f"{logical_lines[-1]} {line}".strip()
            continue
        if line.startswith(prefixes) or line[0].isdigit():
            logical_lines.append(line)
        else:
            if logical_lines:
                logical_lines[-1] = f"{logical_lines[-1]} {line}".strip()
            else:
                logical_lines.append(line)

    cleaned_lines = []
    for line in logical_lines:
        line = line.replace("Driver e vent", "Driver event")
        line = re.sub(r"\[Schedule\s+Driver event", "[ScheduledTasks] Driver event", line)
        line = re.sub(r"(Sustain:NO)\s+[A-Za-z]{1,3}$", r"\1", line)
        line = re.sub(r"([)'\"])\s+[A-Za-z]{1,3}$", r"\1", line)
        cleaned_lines.append(line)

    for idx, line in enumerate(cleaned_lines, 1):
        print(f"{idx}: {line}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
