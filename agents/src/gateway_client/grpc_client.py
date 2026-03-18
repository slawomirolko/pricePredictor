import grpc
from google.protobuf.empty_pb2 import Empty
from google.protobuf.timestamp_pb2 import Timestamp
from . import gateway_pb2
from .gateway_pb2_grpc import GatewayStub


class AsyncGatewayGrpcClient:
    def __init__(self, address: str = "localhost:50051"):
        self._channel = grpc.aio.insecure_channel(address)
        self._stub = GatewayStub(self._channel)

    async def get_newest_important_articles(self) -> list[dict[str, str]]:
        request = gateway_pb2.NewestImportantArticlesRequest()
        response = await self._stub.GetNewestImportantArticles(request)
        return [
            {
                "article_id": article.article_id,
                "url": article.url,
                "source": article.source,
                "read_at_utc": article.read_at_utc.ToJsonString(),
                "summary": article.summary,
            }
            for article in response.articles
        ]

    async def get_volatility_period_json(self, start_utc: str, end_utc: str) -> str:
        start_timestamp = Timestamp()
        start_timestamp.FromJsonString(start_utc)

        end_timestamp = Timestamp()
        end_timestamp.FromJsonString(end_utc)

        request = gateway_pb2.VolatilityPeriodRequest(
            start_utc=start_timestamp,
            end_utc=end_timestamp,
        )
        response = await self._stub.ExportVolatilityPeriodJson(request)
        return response.json

    async def get_latest_volatility_json(self) -> str:
        response = await self._stub.ExportLatestVolatilityJson(Empty())
        return response.json

    async def close(self):
        await self._channel.close()
