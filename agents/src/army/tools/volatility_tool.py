import asyncio
from datetime import datetime
from threading import Thread
from typing import Any, Type, List

from crewai.tools import BaseTool
from pydantic import BaseModel, Field

from gateway_client.grpc_client import AsyncGatewayGrpcClient


class VolatilityQueryInput(BaseModel):
    commodity: str = Field(
        ..., 
        description="Commodity name: GOLD, SILVER, NATURAL_GAS, or OIL"
    )
    date: str = Field(
        ..., 
        description="ISO format date (YYYY-MM-DD or YYYY-MM-DDTHH:MM:SS)"
    )
    minutes: int = Field(
        default=60,
        description="Number of minutes of data to retrieve (1-1440). Default: 60"
    )


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
        except BaseException as exc:
            error["exc"] = exc

    thread = Thread(target=runner, daemon=True)
    thread.start()
    thread.join()

    if "exc" in error:
        raise error["exc"]
    return result.get("value")


class VolatilityQueryTool(BaseTool):
    name: str = "volatility_query"
    description: str = (
        "Query volatility and panic factor data for commodities (GOLD, SILVER, NATURAL_GAS, OIL). "
        "Returns OHLCV data with rolling volatilities (5m, 15m, 60m) and panic factors. "
        "Use this to analyze price movements, volatility patterns, and market panic indicators."
    )
    args_schema: Type[BaseModel] = VolatilityQueryInput
    gateway_address: str = "localhost:50051"

    _COMMODITY_MAP = {
        "GOLD": 1,
        "SILVER": 2,
        "NATURAL_GAS": 3,
        "OIL": 4,
    }

    async def _query_volatility(
        self, 
        commodity: str, 
        date: str, 
        minutes: int
    ) -> List[dict]:
        # Map commodity name to enum
        commodity_enum = self._COMMODITY_MAP.get(commodity.upper())
        if commodity_enum is None:
            raise ValueError(
                f"Invalid commodity: {commodity}. "
                f"Must be one of: {', '.join(self._COMMODITY_MAP.keys())}"
            )
        
        # Parse date
        try:
            if 'T' in date:
                dt = datetime.fromisoformat(date)
            else:
                dt = datetime.fromisoformat(f"{date}T00:00:00")
        except ValueError as e:
            raise ValueError(f"Invalid date format: {date}. Use YYYY-MM-DD or ISO format.") from e
        
        # Query via gRPC
        client = AsyncGatewayGrpcClient(self.gateway_address)
        try:
            return await client.get_volatility(commodity_enum, dt, minutes)
        finally:
            await client.close()

    def _run(self, commodity: str, date: str, minutes: int = 60) -> str:
        points = _run_coro_sync(self._query_volatility(commodity, date, minutes))
        
        # Format as readable string for LLM
        if not points:
            return f"No volatility data found for {commodity} on {date}"
        
        summary = f"Volatility data for {commodity} starting {date} ({len(points)} data points):\n\n"
        
        # Show first few and last few points
        display_points = points[:3] + points[-2:] if len(points) > 5 else points
        
        for point in display_points:
            summary += (
                f"Time: {point['timestamp']}\n"
                f"  Price: Open={point['open']:.2f}, High={point['high']:.2f}, "
                f"Low={point['low']:.2f}, Close={point['close']:.2f}\n"
                f"  Volume: {point['volume']}\n"
                f"  Volatility: 5m={point['vol5']:.4f}, 15m={point['vol15']:.4f}, 60m={point['vol60']:.4f}\n"
                f"  Panic: Short={point['short_panic']:.4f}, Long={point['long_panic']:.4f}\n\n"
            )
        
        if len(points) > 5:
            summary += f"... ({len(points) - 5} more points) ...\n\n"
        
        # Add summary statistics
        avg_close = sum(p['close'] for p in points) / len(points)
        max_vol60 = max(p['vol60'] for p in points)
        max_panic = max(max(p['short_panic'], p['long_panic']) for p in points)
        
        summary += (
            f"Summary:\n"
            f"  Average Close: {avg_close:.2f}\n"
            f"  Max 60m Volatility: {max_vol60:.4f}\n"
            f"  Max Panic Factor: {max_panic:.4f}\n"
        )
        
        return summary

