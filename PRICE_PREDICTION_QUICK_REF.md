# Price Prediction Agent - Quick Reference

## ⚡ Quick Start

```powershell
# Test the agent
cd C:\Users\sawek\Documents\Git\DotNet\PricePredictor\agents
uv run python test_price_prediction.py

# Run full crew with predictions
uv run python -m army.main
```

## 🤖 What the Agent Does

Analyzes volatility data and predicts price movements using historical patterns:

| Input | Output |
|-------|--------|
| Volatility data (OHLCV + panic factors) | Price prediction with confidence % |
| Historical patterns | Direction (UP/DOWN/SIDEWAYS) |
| Market context | Magnitude (% change) |
| Risk factors | Timeframe (hours) |

## 📊 Historical Patterns (Built-in Knowledge)

| Pattern | Signal | Success Rate | Outcome |
|---------|--------|--------------|---------|
| **Volatility Expansion** | vol60 up >50% | 70% | 2-5% move in 4-8h |
| **Panic Spike** | short_panic > 2.0 | 60% | Reversal in 6h |
| **Panic Buying** | long_panic > 2.0 | 55% | Pullback in 6h |
| **Vol Compression** | vol60: 0.025→0.015 | 70% | Breakout in 12h |
| **Window Alignment** | vol5↑ vol15↑ vol60↑ | 75% | Strong move soon |

## 🎯 Example Prediction Format

```
PRIMARY PREDICTION
Direction: UPWARD
Magnitude: 2-3% ($2,195-$2,205)
Timeframe: 4-6 hours
Confidence: 65%

REASONING
• Short panic at 2.3 (oversold → reversal likely)
• Vol60 expanded 58% (big move coming)
• Price at session low (good entry)

RISK FACTORS
• News events could extend panic
• Break below $2,148 invalidates prediction

RECOMMENDED ACTION
• Wait for break above $2,155
• Entry: $2,156, Stop: $2,147, Target: $2,200
```

## ⚙️ Configuration

Edit `agents/src/army/main.py`:

```python
inputs = {
    'commodity': 'GOLD',        # GOLD, SILVER, NATURAL_GAS, OIL
    'date': '2026-03-05',       # ISO format date
    'minutes': 120,             # 60-240 recommended
    'current_year': '2026'
}
```

## 📈 Timeframe Guide

| Use Case | Minutes | Best For |
|----------|---------|----------|
| Scalping | 30-60 | Very short term |
| Intraday | 60-120 | Day trading |
| Swing | 120-240 | Multi-hour trades |
| Position | 240-480 | Longer analysis |

## 🔍 Interpreting Metrics

### Volatility (vol60)
- `< 0.015` = Low (calm before storm)
- `0.015-0.025` = Normal
- `0.025-0.035` = Elevated (opportunity)
- `> 0.035` = Extreme (high risk)

### Panic Factors
- `< 1.0` = Normal market
- `1.0-1.5` = Moderate stress
- `1.5-2.0` = High stress
- `> 2.0` = Panic (reversal signal)

### Confidence Levels
- `70%+` = High confidence
- `60-70%` = Moderate confidence
- `50-60%` = Low confidence
- `< 50%` = Uncertain (don't trade)

## 🎓 Agent's Knowledge

The agent "knows" that:

✅ **Gold Behavior**
- Mean-reverts after panic events
- Spikes on geopolitical news
- Consolidates in Asian hours
- Safe-haven buying patterns

✅ **Volatility Patterns**
- Rising vol = directional move coming
- Falling vol = consolidation/reversal
- Vol clustering = sustained trend

✅ **Volume-Volatility**
- High both = strong conviction
- High vol + low volume = fake move
- Low vol + high volume = accumulation

## 🛠️ Customization

### Add New Pattern

Edit `agents/src/army/config/tasks.yaml`:

```yaml
- **Your Pattern**: When [condition], historically [probability]% chance of [outcome] within [timeframe]
```

### Change Agent Personality

Edit `agents/src/army/config/agents.yaml`:

```yaml
price_prediction_specialist:
  backstory: >
    Your custom backstory with different experience/approach...
```

### Add More Data Sources

Add tools to the agent in `agents/src/army/crew.py`:

```python
@agent
def price_prediction_specialist(self) -> Agent:
    return Agent(
        # ...
        tools=[VolatilityQueryTool(), YourOtherTool()]
    )
```

## 📝 Output Files

After running:
- `report.md` - Full analysis report
- Console logs - Detailed agent reasoning

## ⚠️ Important Notes

1. **Probabilistic, not guaranteed** - These are predictions based on historical patterns
2. **Use stop losses** - Always set risk limits
3. **Combine with other analysis** - Don't rely solely on volatility
4. **Track accuracy** - Log predictions vs actual outcomes
5. **Update patterns** - Refine success rates based on results

## 🔧 Troubleshooting

| Problem | Solution |
|---------|----------|
| No predictions | Check data availability for date/commodity |
| Low confidence | Need more data or clearer patterns |
| Agent errors | Run `test_price_prediction.py` |
| gRPC errors | Ensure .NET app running on port 50051 |

## 📚 Full Documentation

- `PRICE_PREDICTION_AGENT_GUIDE.md` - Complete guide
- `GRPC_ARMY_INTEGRATION_GUIDE.md` - Integration details
- `agents/EXAMPLE_TASK_CONFIGS.md` - More task examples

## 🚀 Workflow

```
1. Run test → uv run python test_price_prediction.py
2. Check output → Review predictions
3. Adjust params → Edit main.py if needed
4. Run crew → uv run python -m army.main
5. Read report → Check report.md
6. Execute trade → Based on prediction + your judgment
7. Track results → Log accuracy for refinement
```

## 💡 Pro Tips

1. **Best time windows**: 60-120 minutes for most accurate predictions
2. **Multiple commodities**: Run for GOLD, SILVER, OIL and compare
3. **Combine patterns**: Higher confidence when multiple patterns align
4. **Watch panic factors**: Most reliable signal for reversals
5. **Use confirmation**: Wait for price to confirm before entering
6. **Risk management**: Never bet on <60% confidence predictions

---

**Ready to predict?** Run `uv run python test_price_prediction.py` 🔮

