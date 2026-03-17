#!/usr/bin/env python
import asyncio
import json
import logging
import sys
import warnings
from datetime import datetime
from pathlib import Path
from typing import Any

from apscheduler.schedulers.asyncio import AsyncIOScheduler

from gateway_client.grpc_client import AsyncGatewayGrpcClient
from army.crew import Army
from army.settings import apply_ollama_environment, load_ollama_settings

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

warnings.filterwarnings("ignore", category=SyntaxWarning, module="pysbd")

_SETTINGS = load_ollama_settings()
apply_ollama_environment(_SETTINGS)
_STATE_FILE_PATH = Path(__file__).resolve().parents[2] / "state" / "army_state.json"


def run():
    asyncio.run(run_async())


async def run_async():
    """
    Run the crew.
    """
    inputs = _build_default_inputs()
    inputs = await _attach_newest_articles_async(inputs)
    logger.info("Army run starting with last_read_at_utc=%s", inputs["last_read_at_utc"] or "<empty>")
    _log_ollama_context()
    _log_crew_input_context(inputs)

    try:
        result = await Army().crew().kickoff_async(inputs=inputs)
        _log_articles_reader_output(result)
        _save_last_read_at(datetime.utcnow())
    except Exception as exc:
        _log_crew_failure_context(inputs, exc)
        raise


def run_every_minute():
    """Run the army on a fixed one-minute interval."""
    asyncio.run(_run_every_minute_async())


async def _run_every_minute_async():
    """Start a scheduler that triggers the Army crew every minute without overlapping runs."""
    scheduler = AsyncIOScheduler()
    scheduler.add_job(
        _run_scheduled_crew,
        trigger="interval",
        minutes=1,
        next_run_time=datetime.now(),
        id="army-minute-runner",
        max_instances=1,
        coalesce=True,
        replace_existing=True,
    )

    logger.info("Army minute runner started. First execution scheduled for %s.", datetime.now())
    scheduler.start()

    try:
        await asyncio.Event().wait()
    except (KeyboardInterrupt, asyncio.CancelledError):
        logger.info("Army minute runner stopping.")
    finally:
        scheduler.shutdown(wait=False)


async def _run_scheduled_crew():
    inputs = _build_default_inputs()
    inputs = await _attach_newest_articles_async(inputs)
    started_at = datetime.now()
    logger.info(
        "Army scheduled run started at %s with last_read_at_utc=%s.",
        started_at.isoformat(),
        inputs["last_read_at_utc"] or "<empty>",
    )
    _log_ollama_context()
    _log_crew_input_context(inputs)

    try:
        result = await Army().crew().kickoff_async(inputs=inputs)
        _log_articles_reader_output(result)
        _save_last_read_at(datetime.utcnow())
        logger.info("Army scheduled run finished at %s.", datetime.now().isoformat())
    except Exception as exc:
        _log_crew_failure_context(inputs, exc)
        logger.exception("Army scheduled run failed: %s", exc)


def _build_default_inputs():
    state = _load_state()
    last_read_at_utc = state.get("last_read_at_utc", "")

    return {
        "topic": "AI LLMs",
        "current_year": str(datetime.now().year),
        "last_read_at_utc": last_read_at_utc,
    }


async def _attach_newest_articles_async(inputs: dict[str, str]) -> dict[str, str]:
    client = AsyncGatewayGrpcClient("localhost:50051")

    try:
        articles = await client.get_newest_important_articles()
        inputs["newest_important_articles"] = json.dumps(articles)
        logger.info("Fetched %s newest important articles.", len(articles))

        for index, article in enumerate(articles, start=1):
            logger.info(
                "Important article %s: id=%s [%s] %s (%s)",
                index,
                article.get("article_id", "<missing>"),
                article["source"],
                article["url"],
                article["read_at_utc"],
            )
    except Exception as exc:
        inputs["newest_important_articles"] = "[]"
        logger.error("Failed to fetch newest important articles: %s", exc)
    finally:
        await client.close()

    return inputs


def _load_state() -> dict[str, Any]:
    if not _STATE_FILE_PATH.exists():
        return {}

    try:
        return json.loads(_STATE_FILE_PATH.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        logger.warning("Failed to load army state from %s: %s", _STATE_FILE_PATH, exc)
        return {}


def _save_last_read_at(last_read_at_utc: datetime) -> None:
    _STATE_FILE_PATH.parent.mkdir(parents=True, exist_ok=True)

    state = _load_state()
    state["last_read_at_utc"] = _format_utc(last_read_at_utc)

    _STATE_FILE_PATH.write_text(
        json.dumps(state, indent=2),
        encoding="utf-8",
    )

    logger.info("Army state updated: last_read_at_utc=%s", state["last_read_at_utc"])


def _format_utc(value: datetime) -> str:
    return value.replace(microsecond=0).isoformat() + "Z"


def _log_articles_reader_output(result: Any) -> None:
    task_outputs = getattr(result, "tasks_output", None)
    if not task_outputs:
        logger.info("Articles-reader output: %s", _serialize_crew_output(result))
        return

    articles_reader_output = task_outputs[0]
    logger.info("Articles-reader output: %s", _serialize_crew_output(articles_reader_output))


def _log_ollama_context() -> None:
    logger.info(
        "Ollama context: model=%s selected_llm=%s api_base=%s.",
        _SETTINGS.ollama_model,
        _SETTINGS.selected_llm,
        _SETTINGS.ollama_url,
    )


def _log_crew_input_context(inputs: dict[str, str]) -> None:
    articles_json = inputs.get("newest_important_articles", "[]")
    article_count = 0

    try:
        parsed_articles = json.loads(articles_json)
        if isinstance(parsed_articles, list):
            article_count = len(parsed_articles)
    except json.JSONDecodeError:
        pass

    logger.info(
        "Crew input context: topic=%s article_count=%s newest_important_articles_chars=%s.",
        inputs.get("topic", ""),
        article_count,
        len(articles_json),
    )


def _log_crew_failure_context(inputs: dict[str, str], exc: Exception) -> None:
    articles_json = inputs.get("newest_important_articles", "[]")
    preview = articles_json[:1000]

    logger.error(
        "Crew kickoff failed. model=%s api_base=%s article_payload_chars=%s article_payload_preview=%s error=%s",
        _SETTINGS.selected_llm,
        _SETTINGS.ollama_url,
        len(articles_json),
        preview,
        exc,
    )


def _serialize_crew_output(value: Any) -> str:
    for attribute_name in ("raw", "result", "output"):
        attribute_value = getattr(value, attribute_name, None)
        if attribute_value:
            return str(attribute_value)

    json_dict = getattr(value, "json_dict", None)
    if json_dict:
        return json.dumps(json_dict, ensure_ascii=True)

    pydantic_value = getattr(value, "pydantic", None)
    if pydantic_value is not None:
        if hasattr(pydantic_value, "model_dump_json"):
            return pydantic_value.model_dump_json()

        return str(pydantic_value)

    return str(value)


if __name__ == "__main__":
    run()


def train():
    """
    Train the crew for a given number of iterations.
    """
    inputs = _build_default_inputs()
    try:
        Army().crew().train(n_iterations=int(sys.argv[1]), filename=sys.argv[2], inputs=inputs)

    except Exception:
        raise


def replay():
    """
    Replay the crew execution from a specific task.
    """
    try:
        Army().crew().replay(task_id=sys.argv[1])

    except Exception:
        raise


def test():
    """
    Test the crew execution and returns the results.
    """
    inputs = _build_default_inputs()

    try:
        Army().crew().test(n_iterations=int(sys.argv[1]), eval_llm=sys.argv[2], inputs=inputs)

    except Exception:
        raise


def run_with_trigger():
    """
    Run the crew with trigger payload.
    """
    if len(sys.argv) < 2:
        raise Exception("No trigger payload provided. Please provide JSON payload as argument.")

    try:
        trigger_payload = json.loads(sys.argv[1])
    except json.JSONDecodeError:
        raise Exception("Invalid JSON payload provided as argument")

    inputs = {
        "crewai_trigger_payload": trigger_payload,
        "topic": "",
        "current_year": ""
    }

    try:
        result = Army().crew().kickoff(inputs=inputs)
        return result
    except Exception:
        raise
