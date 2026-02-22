import grpc
import gateway_pb2
import gateway_pb2_grpc


class AsyncGatewayGrpcClient:
    def __init__(self, address: str = "localhost:50051"):
        self._channel = grpc.aio.insecure_channel(address)
        self._stub = gateway_pb2_grpc.GatewayStub(self._channel)

    async def send(self, payload: str) -> str:
        request = gateway_pb2.GatewayRequest(payload=payload)
        response = await self._stub.Send(request)
        return response.result

    async def close(self):
        await self._channel.close()