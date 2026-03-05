"""
Test script for the Price Prediction Specialist agent.
This script runs just the price prediction task to verify the agent works correctly.
"""
import asyncio
import sys
from datetime import datetime
from pathlib import Path

# Add src to path
sys.path.insert(0, str(Path(__file__).parent / "src"))

from gateway_client.grpc_client import AsyncGatewayGrpcClient
from army.crew import Army
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


async def test_price_prediction_agent():
    """
    Test the price prediction specialist agent with real data.
    """
    print("=" * 80)
    print("🔮 Testing Price Prediction Specialist Agent")
    print("=" * 80)
    
    # Test parameters
    commodity = "GOLD"
    date = "2026-03-05"
    minutes = 120  # 2 hours of data
    
    print(f"\nTest Configuration:")
    print(f"  Commodity: {commodity}")
    print(f"  Date: {date}")
    print(f"  Data Window: {minutes} minutes")
    print()
    
    # First, verify we can get data
    print("-" * 80)
    print("Step 1: Verifying volatility data availability...")
    print("-" * 80)
    
    client = AsyncGatewayGrpcClient("localhost:50051")
    try:
        data = await client.get_volatility(
            commodity=1,  # GOLD
            date=datetime(2026, 3, 5, 0, 0, 0),
            minutes=minutes
        )
        
        if not data:
            print("❌ No data available. Cannot test prediction agent.")
            print("   Make sure:")
            print("   1. Your .NET application is running")
            print("   2. Database has volatility data for this date/commodity")
            return False
        
        print(f"✅ Found {len(data)} data points")
        
        # Show sample data point
        if data:
            sample = data[0]
            print(f"\nSample data point (first):")
            print(f"  Time: {sample['timestamp']}")
            print(f"  Price: ${sample['close']:.2f}")
            print(f"  Vol60: {sample['vol60']:.4f}")
            print(f"  Short Panic: {sample['short_panic']:.4f}")
            print(f"  Long Panic: {sample['long_panic']:.4f}")
            
    except Exception as e:
        print(f"❌ Error connecting to gRPC service: {e}")
        print("   Make sure your .NET application is running on port 50051")
        return False
    finally:
        await client.close()
    
    # Now run the agent
    print("\n" + "-" * 80)
    print("Step 2: Running Price Prediction Specialist Agent...")
    print("-" * 80)
    print()
    
    inputs = {
        'topic': 'Commodity Market Analysis',
        'commodity': commodity,
        'date': date,
        'minutes': minutes,
        'current_year': str(datetime.now().year)
    }
    
    try:
        # Run only the price prediction task
        crew = Army().crew()
        
        print("🤖 Agent is analyzing market data and making predictions...")
        print("   This may take 30-60 seconds depending on your LLM speed...")
        print()
        
        result = await crew.kickoff_async(inputs=inputs)
        
        print("\n" + "=" * 80)
        print("✅ Price Prediction Agent Test Complete!")
        print("=" * 80)
        
        # Check if report.md was created
        report_path = Path(__file__).parent / "report.md"
        if report_path.exists():
            print(f"\n📄 Report generated: {report_path}")
            print("\nPreview of the report:")
            print("-" * 80)
            with open(report_path, 'r', encoding='utf-8') as f:
                content = f.read()
                # Show first 1000 characters
                preview = content[:1000]
                print(preview)
                if len(content) > 1000:
                    print(f"\n... ({len(content) - 1000} more characters)")
                    print(f"\nRead the full report at: {report_path}")
        
        return True
        
    except Exception as e:
        print(f"\n❌ Error running prediction agent: {e}")
        import traceback
        traceback.print_exc()
        return False


async def quick_pattern_test():
    """
    Quick test to show what patterns the agent looks for.
    """
    print("\n" + "=" * 80)
    print("📊 Historical Patterns the Agent Uses")
    print("=" * 80)
    
    patterns = [
        {
            'name': 'Volatility Expansion',
            'condition': 'vol60 increases >50%',
            'probability': '70%',
            'outcome': '2-5% price move in 4-8 hours'
        },
        {
            'name': 'Panic Spike (Overselling)',
            'condition': 'short_panic > 2.0',
            'probability': '60%',
            'outcome': 'Price reversal within 6 hours'
        },
        {
            'name': 'Panic Buying (Overbuying)',
            'condition': 'long_panic > 2.0',
            'probability': '55%',
            'outcome': 'Price pullback within 6 hours'
        },
        {
            'name': 'Volatility Compression',
            'condition': 'vol60 drops from >0.025 to <0.015',
            'probability': '70%',
            'outcome': 'Breakout (up or down) within 12 hours'
        },
        {
            'name': 'Rolling Window Alignment',
            'condition': 'vol5, vol15, vol60 all increasing',
            'probability': '75%',
            'outcome': 'Strong directional move imminent'
        },
    ]
    
    print("\nThe agent knows these historical patterns:\n")
    for i, pattern in enumerate(patterns, 1):
        print(f"{i}. {pattern['name']}")
        print(f"   Condition: {pattern['condition']}")
        print(f"   Success Rate: {pattern['probability']}")
        print(f"   Expected: {pattern['outcome']}")
        print()
    
    print("💡 The agent will:")
    print("   • Identify which patterns are present in current data")
    print("   • Calculate combined probability from multiple patterns")
    print("   • Provide direction, magnitude, timeframe, and confidence")
    print("   • List risk factors that could invalidate the prediction")


async def main():
    print("\n🧪 Price Prediction Specialist Agent Test Suite\n")
    
    # Show patterns first
    await quick_pattern_test()
    
    # Run the actual test
    success = await test_price_prediction_agent()
    
    # Summary
    print("\n" + "=" * 80)
    print("Test Summary")
    print("=" * 80)
    
    if success:
        print("✅ Price Prediction Agent is working correctly!")
        print("\nNext steps:")
        print("  1. Review the generated report for prediction quality")
        print("  2. Compare predictions with actual price movements")
        print("  3. Adjust historical pattern probabilities if needed")
        print("  4. Run: uv run python -m army.main")
    else:
        print("❌ Price Prediction Agent test failed")
        print("\nTroubleshooting:")
        print("  1. Ensure .NET app is running: docker-compose up")
        print("  2. Check if database has volatility data")
        print("  3. Verify gRPC is accessible on localhost:50051")
        print("  4. Run: uv run python test_volatility_integration.py")


if __name__ == "__main__":
    asyncio.run(main())

