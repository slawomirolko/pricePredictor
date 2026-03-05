import grpc
from datetime import datetime
from typing import List
from google.protobuf.timestamp_pb2 import Timestamp
from . import gateway_pb2
from . import gateway_pb2_grpc


class AsyncGatewayGrpcClient:
    def __init__(self, address: str = "localhost:50051"):
        self._channel = grpc.aio.insecure_channel(address)
        self._stub = gateway_pb2_grpc.GatewayStub(self._channel)

    async def send(self, payload: str) -> str:
        request = gateway_pb2.GatewayRequest(payload=payload)
        response = await self._stub.Send(request)
        return response.result

    async def get_volatility(
        self, 
        commodity: int,  # 1=GOLD, 2=SILVER, 3=NATURAL_GAS, 4=OIL
        date: datetime,
        minutes: int
    ) -> List[dict]:
        """
        Query volatility data for a commodity.
        
        Args:
            commodity: Commodity enum (1=GOLD, 2=SILVER, 3=NATURAL_GAS, 4=OIL)
            date: The starting date/time
            minutes: Number of minutes from the start date (1-1440)
        
        Returns:
            List of volatility points with OHLCV data and panic factors
        """
        # Convert datetime to protobuf Timestamp
        timestamp = Timestamp()
        timestamp.FromDatetime(date)
        
        request = gateway_pb2.VolatilityQueryRequest(
            commodity=commodity,
            date=timestamp,
            minutes=minutes
        )
        
        response = await self._stub.GetVolatility(request)
        
        # Convert response to dict for easier use
        points = []
        for point in response.points:
            points.append({
                'timestamp': point.timestamp.ToDatetime(),
                'open': point.open,
                'high': point.high,
                'low': point.low,
                'close': point.close,
                'volume': point.volume,
                'log_return': point.log_return,
                'vol5': point.vol5,
                'vol15': point.vol15,
                'vol60': point.vol60,
                'short_panic': point.short_panic,
                'long_panic': point.long_panic,
            })
        
        return points

    async def close(self):
        await self._channel.close()