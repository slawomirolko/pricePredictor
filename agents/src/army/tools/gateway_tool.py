import asyncio
from threading import Thread
from typing import Any, Type

from crewai.tools import BaseTool
from pydantic import BaseModel, Field

from gateway_client.grpc_client import AsyncGatewayGrpcClient


class GatewayMessageToolInput(BaseModel):
    payload: str = Field(..., description="Message payload to send to the gateway server.")


def _run_coro_sync(coro: Any) -> Any:
    """Run an async coroutine from sync code, even if an event loop is already running."""
    try:
        asyncio.get_running_loop()
    except RuntimeError:
        return asyncio.run(coro)

    result: dict[str, Any] = {}
    error: dict[str, BaseException] = {}

    def runner() -> None:
        try:
            result["value"] = asyncio.run(coro)
        except BaseException as exc:  # pragma: no cover - passthrough
            error["exc"] = exc

    thread = Thread(target=runner, daemon=True)
    thread.start()
    thread.join()

    if "exc" in error:
        raise error["exc"]
    return result.get("value")


class GatewayMessageTool(BaseTool):
    name: str = "gateway_message"
    description: str = (
        "Send a message to the gRPC gateway server and return the server response. "
        "Use this when you need fresh data or a server-generated message."
    )
    args_schema: Type[BaseModel] = GatewayMessageToolInput
    gateway_address: str = "localhost:50051"

    async def _send_to_gateway(self, payload: str) -> str:
        client = AsyncGatewayGrpcClient(self.gateway_address)
        try:
            return await client.send(payload)
        finally:
            await client.close()

    def _run(self, payload: str) -> str:
        return _run_coro_sync(self._send_to_gateway(payload))

