import grpc
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

    async def close(self):
        await self._channel.close()
