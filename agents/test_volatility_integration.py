"""
Quick test script for the GetVolatility gRPC endpoint integration.
Run this to verify the client and tool work correctly.
"""
import asyncio
import sys
from datetime import datetime
from pathlib import Path

# Add src to path
sys.path.insert(0, str(Path(__file__).parent / "src"))

from gateway_client.grpc_client import AsyncGatewayGrpcClient
from army.tools.volatility_tool import VolatilityQueryTool


async def test_client():
    """Test the gRPC client directly."""
    print("=" * 60)
    print("Testing AsyncGatewayGrpcClient.get_volatility()...")
    print("=" * 60)
    
    client = AsyncGatewayGrpcClient("localhost:50051")
    try:
        # Test with GOLD commodity
        data = await client.get_volatility(
            commodity=1,  # GOLD
            date=datetime(2026, 3, 5, 0, 0, 0),
            minutes=60
        )
        
        print(f"✓ Successfully retrieved {len(data)} volatility points")
        
        if data:
            print("\nFirst data point:")
            first = data[0]
            for key, value in first.items():
                print(f"  {key}: {value}")
        else:
            print("⚠ No data returned (database might be empty)")
            
    except Exception as e:
        print(f"✗ Error: {e}")
        return False
    finally:
        await client.close()
    
    return True


def test_tool():
    """Test the VolatilityQueryTool."""
    print("\n" + "=" * 60)
    print("Testing VolatilityQueryTool...")
    print("=" * 60)
    
    try:
        tool = VolatilityQueryTool()
        result = tool._run(
            commodity="GOLD",
            date="2026-03-05",
            minutes=60
        )
        
        print("✓ Tool executed successfully")
        print("\nTool output:")
        print(result)
        
    except Exception as e:
        print(f"✗ Error: {e}")
        import traceback
        traceback.print_exc()
        return False
    
    return True


async def main():
    print("\n🧪 Testing GetVolatility gRPC Integration\n")
    
    # Test 1: Direct client
    client_ok = await test_client()
    
    # Test 2: CrewAI tool
    tool_ok = test_tool()
    
    # Summary
    print("\n" + "=" * 60)
    print("Test Summary")
    print("=" * 60)
    print(f"gRPC Client: {'✓ PASS' if client_ok else '✗ FAIL'}")
    print(f"CrewAI Tool: {'✓ PASS' if tool_ok else '✗ FAIL'}")
    
    if client_ok and tool_ok:
        print("\n✅ All tests passed! Ready to use in army flow.")
    else:
        print("\n❌ Some tests failed. Check the errors above.")


if __name__ == "__main__":
    asyncio.run(main())

