#!/usr/bin/env python3
"""Conservative check of our untrimmed managed assembly, not framework DLLs."""
import pathlib
import sys

assembly = pathlib.Path(sys.argv[1])
data = assembly.read_bytes()
for name in ("WriteProcessMemory", "NtWriteVirtualMemory", "VirtualProtectEx", "CreateRemoteThread"):
    if name.encode() in data:
        raise SystemExit(f"FAIL: forbidden native symbol {name} in {assembly}")
for name in ("ReadProcessMemory", "OpenProcess", "VirtualQueryEx"):
    if name.encode() not in data:
        raise SystemExit(f"FAIL: expected diagnostic symbol {name} missing")
print(f"PASS: diagnostic assembly contains read APIs and no forbidden write/injection imports: {assembly.name}")
