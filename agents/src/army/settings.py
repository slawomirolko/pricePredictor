from __future__ import annotations

from pathlib import Path
import os


DEFAULT_CLOUD_URL = "https://ollama.com"
DEFAULT_CLOUD_MODEL = "gpt-oss:120b-cloud"
LEGACY_MODEL_ALIASES = {
    "nemotron-3-super:cloud": "gpt-oss:20b-cloud",
}
AGENTS_ROOT = Path(__file__).resolve().parents[2]
DOTENV_PATH = AGENTS_ROOT / ".env"


def _load_dotenv_if_present() -> dict[str, str]:
    if not DOTENV_PATH.exists():
        raise ValueError(f"Army requires {DOTENV_PATH} to exist.")

    values: dict[str, str] = {}

    for raw_line in DOTENV_PATH.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue

        key, value = line.split("=", 1)
        values[key.strip()] = value.strip().strip("\"'")

    return values


def resolve_llm(model: str) -> str:
    resolved_model = LEGACY_MODEL_ALIASES.get(model, model)
    if "/" in resolved_model:
        return resolved_model

    return f"ollama_chat/{resolved_model}"


_DOTENV_VALUES = _load_dotenv_if_present()
OLLAMA_URL = (
    _DOTENV_VALUES.get("CLOUD_OLLAMA_URL")
    or _DOTENV_VALUES.get("API_BASE")
    or DEFAULT_CLOUD_URL
)
if _DOTENV_VALUES.get("OLLAMA_API_KEY"):
    OLLAMA_API_KEY = _DOTENV_VALUES["OLLAMA_API_KEY"]
    OLLAMA_API_KEY_SOURCE = f"{DOTENV_PATH}:OLLAMA_API_KEY"
elif _DOTENV_VALUES.get("CLOUD_OLLAMA_API_KEY"):
    OLLAMA_API_KEY = _DOTENV_VALUES["CLOUD_OLLAMA_API_KEY"]
    OLLAMA_API_KEY_SOURCE = f"{DOTENV_PATH}:CLOUD_OLLAMA_API_KEY"
else:
    OLLAMA_API_KEY = ""
    OLLAMA_API_KEY_SOURCE = "<missing>"

if not OLLAMA_API_KEY or OLLAMA_API_KEY.startswith("__SET_"):
    raise ValueError("OLLAMA_API_KEY must be configured for cloud runs.")


def apply_ollama_environment() -> None:
    os.environ["API_BASE"] = OLLAMA_URL
    os.environ["OLLAMA_API_BASE"] = OLLAMA_URL
    os.environ["API_KEY"] = OLLAMA_API_KEY
    os.environ["OLLAMA_API_KEY"] = OLLAMA_API_KEY


def get_ollama_api_key_fingerprint() -> str:
    if len(OLLAMA_API_KEY) < 4:
        return "***"

    return f"***{OLLAMA_API_KEY[-4:]}"
