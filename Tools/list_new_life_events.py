#!/usr/bin/env python3
"""
Lists every entity prototype that appears in the New Life config window
(`newlifeconfig`) — i.e. non-abstract entity prototypes whose *resolved*
component set (own + inherited via `parent`) contains StationEvent and/or
AntagSelection. These are the rules you can tick to block respawning.

Usage:
    python Tools/list_new_life_events.py [--ids] [path-to-Resources/Prototypes]

    --ids   print only prototype IDs (one per line), handy for piping.
"""

import os
import sys
import yaml

# Component registration names (the `- type:` value in YAML) we care about.
EVENT_COMP = "StationEvent"
ANTAG_COMP = "AntagSelection"


# SS14 YAML uses custom tags like `!type:Foo`; make the loader tolerate them.
class Loader(yaml.SafeLoader):
    pass


def _ignore_unknown(loader, tag_suffix, node):
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node, deep=True)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node, deep=True)
    return loader.construct_scalar(node)


Loader.add_multi_constructor("!", _ignore_unknown)


def find_proto_root():
    if len(sys.argv) > 1 and not sys.argv[-1].startswith("--"):
        return sys.argv[-1]
    here = os.path.dirname(os.path.abspath(__file__))
    return os.path.normpath(os.path.join(here, "..", "Resources", "Prototypes"))


def load_all(root):
    """Returns id -> prototype dict for every `type: entity` prototype."""
    protos = {}
    for dirpath, _, files in os.walk(root):
        for fn in files:
            if not fn.endswith((".yml", ".yaml")):
                continue
            path = os.path.join(dirpath, fn)
            try:
                with open(path, "r", encoding="utf-8") as f:
                    docs = list(yaml.load_all(f, Loader=Loader))
            except Exception as e:
                print(f"  ! skipped {path}: {e}", file=sys.stderr)
                continue
            for doc in docs:
                if not isinstance(doc, list):
                    continue
                for entry in doc:
                    if not isinstance(entry, dict):
                        continue
                    if entry.get("type") != "entity":
                        continue
                    pid = entry.get("id")
                    if pid:
                        protos[pid] = entry
    return protos


def parents_of(entry):
    p = entry.get("parent")
    if p is None:
        return []
    return p if isinstance(p, list) else [p]


def comp_types(entry):
    out = set()
    for c in entry.get("components", []) or []:
        if isinstance(c, dict) and "type" in c:
            out.add(c["type"])
    return out


def resolved_comps(pid, protos, cache, seen=None):
    if pid in cache:
        return cache[pid]
    seen = seen or set()
    if pid in seen or pid not in protos:
        return set()
    seen.add(pid)
    entry = protos[pid]
    comps = set(comp_types(entry))
    for parent in parents_of(entry):
        comps |= resolved_comps(parent, protos, cache, seen)
    cache[pid] = comps
    return comps


def display_name(pid, protos, seen=None):
    seen = seen or set()
    if pid in seen or pid not in protos:
        return pid
    seen.add(pid)
    entry = protos[pid]
    if entry.get("name"):
        return entry["name"]
    for parent in parents_of(entry):
        n = display_name(parent, protos, seen)
        if n != parent:
            return n
    return pid


def main():
    ids_only = "--ids" in sys.argv
    root = find_proto_root()
    if not os.path.isdir(root):
        print(f"Prototype dir not found: {root}", file=sys.stderr)
        sys.exit(1)

    protos = load_all(root)
    cache = {}

    events, antags, both = [], [], []
    for pid, entry in protos.items():
        if entry.get("abstract"):
            continue
        comps = resolved_comps(pid, protos, cache)
        is_event = EVENT_COMP in comps
        is_antag = ANTAG_COMP in comps
        if not (is_event or is_antag):
            continue
        row = (pid, display_name(pid, protos))
        if is_event and is_antag:
            both.append(row)
        elif is_event:
            events.append(row)
        else:
            antags.append(row)

    if ids_only:
        for pid, _ in sorted(events + antags + both):
            print(pid)
        return

    def dump(title, rows):
        print(f"\n=== {title} ({len(rows)}) ===")
        for pid, name in sorted(rows, key=lambda r: r[0].lower()):
            print(f"  {pid:<40} {name}")

    total = len(events) + len(antags) + len(both)
    print(f"New Life blockable rules (scanned {len(protos)} entity prototypes): {total}")
    dump("Station events (StationEvent)", events)
    dump("Antag rules (AntagSelection)", antags)
    dump("Both event + antag", both)


if __name__ == "__main__":
    main()
