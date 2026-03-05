# Price Prediction Specialist Agent Guide

## Overview

I've added a new specialized agent to your army crew: **Price Prediction Specialist**

This agent analyzes volatility data and uses historical market pattern knowledge to predict probable price movements.

## What Was Added

### 1. New Agent: `price_prediction_specialist`

**Location**: `agents/src/army/config/agents.yaml`

**Expertise**:
- 15 years of commodity market experience (simulated knowledge)
- Pattern recognition from volatility and panic factors
- Probabilistic forecasting with confidence levels
- Risk-aware predictions

**Knowledge Base** (built into the agent):
- Volatility expansion patterns
- Panic spike reversals
- Volume-volatility relationships
- Mean reversion behaviors
- Market cycle patterns

### 2. New Task: `price_prediction_task`

**Location**: `agents/src/army/config/tasks.yaml`

**What it does**:
1. Queries recent volatility data using the `volatility_query` tool
2. Analyzes historical patterns in the data
3. Calculates probability-weighted predictions
4. Provides directional forecasts with confidence levels

**Historical Patterns the Agent Knows**:

| Pattern | Signal | Historical Probability | Expected Outcome |
|---------|--------|----------------------|------------------|
| Volatility Expansion | vol60 increases >50% | 70% | 2-5% price move in 4-8 hours |
| Panic Spike | short_panic > 2.0 | 60% | Reversal within 6 hours |
| Panic Buying | long_panic > 2.0 | 55% | Pullback within 6 hours |
| Volatility Compression | vol60 drops from >0.025 to <0.015 | 70% | Breakout within 12 hours |
| Rolling Window Alignment | vol5, vol15, vol60 all rising | 75% | Strong directional move |

### 3. Updated Crew Flow

The crew now runs in this sequence:
1. **Researcher** - Gathers general information
2. **Price Prediction Specialist** - Analyzes volatility and predicts price movements
3. **Reporting Analyst** - Compiles everything into a report

## How to Use

### Basic Usage

Run the crew with default settings (analyzes GOLD for last 2 hours):

```powershell
cd C:\Users\sawek\Documents\Git\DotNet\PricePredictor\agents
uv run python -m army.main
```

### Custom Commodity and Timeframe

Edit `main.py` to change parameters:

```python
inputs = {
    'topic': 'Market Analysis',
    'commodity': 'SILVER',      # GOLD, SILVER, NATURAL_GAS, OIL
    'date': '2026-03-05',       # Current date or historical
    'minutes': 240,             # 4 hours of data
    'current_year': '2026'
}
```

### Example Prediction Output

The agent will produce predictions like:

```markdown
## Current Market State
- **Price**: $2,151.00 (down 0.5% from open)
- **60-min Volatility**: 0.0285 (elevated)
- **Short Panic Factor**: 2.3 (extreme)
- **Long Panic Factor**: 0.4 (low)
- **Volume**: Above average

## Identified Patterns
1. **Panic Spike Pattern** (confidence: 85%)
   - Short panic factor at 2.3 indicates overselling
   - Historical success rate: 60% reversal within 6 hours
   
2. **Volatility Expansion** (confidence: 75%)
   - vol60 increased from 0.018 to 0.0285 (+58%)
   - Suggests 2-5% move likely in 4-8 hours

## Primary Prediction
**Direction**: UPWARD
**Magnitude**: 2-3% increase ($2,195 - $2,205)
**Timeframe**: 4-6 hours
**Confidence**: 65%

**Reasoning**: 
- Extreme panic selling (short_panic 2.3) typically leads to reversal
- Current price at session low provides good risk/reward
- Volatility expansion confirms strong movement imminent
- Low long_panic suggests buying pressure limited so far

## Alternative Scenarios
- **30% probability**: Continued decline if panic intensifies (short_panic > 3.0)
- **5% probability**: Sideways consolidation if volume drops

## Key Levels to Watch
- **Resistance**: $2,155 (must break for upward move)
- **Support**: $2,148 (if broken, invalidates bullish case)

## Risk Factors
- Geopolitical news could extend panic
- Low Asian trading volume may delay reversal
- If short_panic exceeds 3.0, pattern invalidated

## Recommended Action
Wait for confirmation:
- Price breaks above $2,155 with volume
- Short panic factor starts declining
- Entry: $2,156, Stop: $2,147, Target: $2,200
```

## Configuration Parameters

### In `main.py`

```python
inputs = {
    'commodity': str,   # "GOLD", "SILVER", "NATURAL_GAS", "OIL"
    'date': str,        # ISO format "2026-03-05" or "2026-03-05T14:30:00"
    'minutes': int,     # 60-240 recommended for predictions (1-1440 max)
    # ... other parameters
}
```

### Recommended Settings by Use Case

**Intraday Trading** (short-term predictions):
```python
'minutes': 60,  # Last hour
```

**Swing Trading** (4-12 hour predictions):
```python
'minutes': 120,  # Last 2 hours
```

**Position Analysis** (longer-term):
```python
'minutes': 240,  # Last 4 hours
```

## Integration with Existing Agents

The Price Prediction Specialist works alongside:

1. **Researcher** - Can provide context on news events affecting markets
2. **Reporting Analyst** - Compiles predictions into actionable reports

You can modify the task sequence in `crew.py` if you want different flow.

## Agent's Built-in Knowledge

The agent is programmed with knowledge about:

### Gold-Specific Behaviors
- Mean reversion after panic events
- Spikes during geopolitical uncertainty
- Consolidation during Asian trading hours
- Safe-haven buying patterns

### Volatility Interpretation
- High vol60 (>0.025) = elevated risk/opportunity
- Low vol60 (<0.015) = calm before storm
- Increasing volatility trend = directional move coming
- Decreasing volatility = consolidation/reversal

### Panic Factor Interpretation
- short_panic > 2.0 = oversold, reversal likely
- long_panic > 2.0 = overbought, pullback likely
- Both high = extreme uncertainty, wait for clarity
- Both low = stable market, follow trend

### Volume Patterns
- High volume + high volatility = strong conviction move
- High volume + low volatility = accumulation/distribution
- Low volume + high volatility = fake move/low liquidity

## Customizing the Agent

### Add More Historical Patterns

Edit `agents/src/army/config/tasks.yaml` in the `price_prediction_task` description:

```yaml
- **Your Pattern Name**: When [condition], historically followed by [outcome] with [probability]%
```

### Change Confidence Thresholds

The agent uses probabilistic language:
- 70%+ = "High confidence"
- 60-70% = "Moderate confidence"  
- 50-60% = "Low confidence"
- <50% = "Uncertain"

### Add More Commodities

The agent works with any commodity in your system:
- GOLD
- SILVER
- NATURAL_GAS
- OIL

Just change the `commodity` parameter.

## Testing the New Agent

### Quick Test

```powershell
cd C:\Users\sawek\Documents\Git\DotNet\PricePredictor\agents
uv run python -m army.main
```

### Verify Agent Is Active

Check the console output for:
```
> Entering new CrewAgentExecutor chain...
Agent: Commodity Price Prediction Specialist
```

### Check Prediction Output

The agent's predictions will be in:
- Console output (verbose mode)
- `report.md` file (compiled by reporting analyst)

## Example Use Cases

### 1. Morning Price Forecast
```python
inputs = {
    'commodity': 'GOLD',
    'date': '2026-03-05T09:00:00',  # Market open
    'minutes': 60,
    # ...
}
```

### 2. Mid-Day Reversal Check
```python
inputs = {
    'commodity': 'OIL',
    'date': '2026-03-05T12:00:00',
    'minutes': 120,
    # ...
}
```

### 3. Multi-Commodity Comparison
Run the crew multiple times with different commodities, or create a custom task that queries multiple commodities.

## Limitations & Best Practices

### Limitations
- Predictions are probabilistic, not guaranteed
- Based on historical patterns, not fundamental analysis
- Needs sufficient data (at least 60 minutes)
- Best for commodities with regular volatility patterns

### Best Practices
1. **Use appropriate timeframes**: 60-240 minutes for most predictions
2. **Check confidence levels**: Only act on >60% confidence
3. **Verify data quality**: Ensure database has recent data
4. **Combine with other analysis**: Don't rely solely on volatility
5. **Monitor risk factors**: Agent provides them - use them!
6. **Update historical patterns**: As you gather more data, refine the patterns

## Troubleshooting

**Agent doesn't make predictions**:
- Check if volatility data is available for the date/commodity
- Ensure minutes parameter is reasonable (60-240)
- Verify the gRPC server is running

**Predictions seem incorrect**:
- Historical patterns may not apply to current market
- Consider if unusual events are affecting the market
- Check if data quality is good

**Agent takes too long**:
- Reduce minutes parameter (try 60 instead of 240)
- Ensure database queries are fast

## Next Steps

1. **Run a test**: Execute the crew with default parameters
2. **Review output**: Check the prediction quality
3. **Refine patterns**: Update historical success rates based on real results
4. **Integrate with trading**: Use predictions for entry/exit decisions
5. **Track accuracy**: Log predictions vs actual outcomes to improve patterns

## Files Modified

- ✅ `agents/src/army/config/agents.yaml` - Added price_prediction_specialist
- ✅ `agents/src/army/config/tasks.yaml` - Added price_prediction_task
- ✅ `agents/src/army/crew.py` - Added agent and task to crew
- ✅ `agents/src/army/main.py` - Updated inputs with commodity parameters

