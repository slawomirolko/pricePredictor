from grpc.grpc_client import AsyncGatewayGrpcClient


class GatewayBridge:
    def __init__(self):
        self._client = AsyncGatewayGrpcClient()

    def call_gateway(self, text: str) -> str:
        return self._client.send(text)