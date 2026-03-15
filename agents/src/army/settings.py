from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import os


DEFAULT_CLOUD_URL = "https://ollama.com"
DEFAULT_CLOUD_MODEL = "gpt-oss:120b"


def _load_dotenv_if_present() -> None:
    agents_root = Path(__file__).resolve().parents[2]
    dotenv_path = agents_root / ".env"

    if not dotenv_path.exists():
        return

    for raw_line in dotenv_path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue

        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip().strip("\"'")

        os.environ.setdefault(key, value)


@dataclass(frozen=True)
class OllamaSettings:
    ollama_url: str
    ollama_model: str
    ollama_api_key: str

    @property
    def selected_llm(self) -> str:
        if "/" in self.ollama_model:
            return self.ollama_model

        # CrewAI/LiteLLM agent flows are chat-oriented, so route through ollama_chat.
        return f"ollama_chat/{self.ollama_model}"


def load_ollama_settings() -> OllamaSettings:
    _load_dotenv_if_present()

    ollama_url = os.getenv("CLOUD_OLLAMA_URL") or os.getenv("API_BASE") or DEFAULT_CLOUD_URL
    ollama_model = os.getenv("CLOUD_OLLAMA_MODEL") or os.getenv("MODEL") or DEFAULT_CLOUD_MODEL
    ollama_api_key = os.getenv("OLLAMA_API_KEY") or os.getenv("CLOUD_OLLAMA_API_KEY") or ""

    if not ollama_api_key or ollama_api_key.startswith("__SET_"):
        raise ValueError("OLLAMA_API_KEY must be configured for cloud runs.")

    return OllamaSettings(
        ollama_url=ollama_url,
        ollama_model=ollama_model,
        ollama_api_key=ollama_api_key,
    )


def apply_ollama_environment(settings: OllamaSettings) -> None:
    os.environ["API_BASE"] = settings.ollama_url
    os.environ["MODEL"] = settings.selected_llm
    os.environ["API_KEY"] = settings.ollama_api_key
    os.environ["OLLAMA_API_KEY"] = settings.ollama_api_key
