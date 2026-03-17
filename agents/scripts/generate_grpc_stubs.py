#!/usr/bin/env python
"""
Regenerates gateway_pb2.py and gateway_pb2_grpc.py from Protos/gateway.proto.

Run from the repository root:
    uv run --directory agents python scripts/generate_grpc_stubs.py
"""
from pathlib import Path
import sys

import grpc_tools
from grpc_tools import protoc


def rewrite_grpc_imports(grpc_file_path: Path) -> None:
    grpc_content = grpc_file_path.read_text(encoding="utf-8")
    absolute_import = "import gateway_pb2 as gateway__pb2\n"

    if absolute_import not in grpc_content:
        return

    grpc_file_path.write_text(
        grpc_content.replace(
            absolute_import,
            "try:\n"
            "    from . import gateway_pb2 as gateway__pb2  # relative import when used as package\n"
            "except ImportError:\n"
            "    import gateway_pb2 as gateway__pb2  # noqa: E402 (kept for grpc_tools compatibility)\n",
            1,
        ),
        encoding="utf-8",
    )


def main() -> int:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parents[2]
    proto_root = repo_root / "Protos"
    output_root = repo_root / "agents" / "src" / "gateway_client"
    grpc_include_root = Path(grpc_tools.__file__).resolve().parent / "_proto"

    result = protoc.main([
        "grpc_tools.protoc",
        f"-I{proto_root}",
        f"-I{grpc_include_root}",
        f"--python_out={output_root}",
        f"--grpc_python_out={output_root}",
        str(proto_root / "gateway.proto"),
    ])

    if result == 0:
        rewrite_grpc_imports(output_root / "gateway_pb2_grpc.py")

    return result


if __name__ == "__main__":
    result = main()
    if result != 0:
        print(f"protoc failed with exit code {result}", file=sys.stderr)
        sys.exit(result)

    print("Stubs generated successfully.")
