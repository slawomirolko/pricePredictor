# 🚀 Getting Started with Price Prediction Agent

## Quick Start (3 Steps)

### Step 1: Test the Setup
```powershell
cd C:\Users\sawek\Documents\Git\DotNet\PricePredictor\agents
uv run python test_price_prediction.py
```

**Expected Output:**
- ✅ Data availability check passes
- ✅ Sample volatility data shown
- ✅ Price prediction generated
- ✅ Report file created

### Step 2: Review the Prediction
```powershell
# Check the console output for the prediction
# Or read the generated report
notepad report.md
```

**What to Look For:**
- Direction (UP/DOWN/SIDEWAYS)
- Confidence level (aim for >60%)
- Reasoning (which patterns detected)
- Risk factors
- Recommended actions

### Step 3: Run the Full Crew
```powershell
uv run python -m army.main
```

**This runs all 3 agents:**
1. Researcher (context)
2. Price Prediction Specialist (predictions)
3. Reporting Analyst (final report)

---

## What Just Got Added

### New Agent: Price Prediction Specialist

**What it does:**
- Reads volatility data from your gRPC endpoint
- Analyzes 5 historical market patterns
- Predicts probable price movements
- Provides confidence levels (probabilities)
- Recommends entry/exit/stop-loss levels

**Built-in knowledge:**
- 15 years of commodity market experience (simulated)
- Pattern success rates from historical data
- Gold-specific behavior patterns
- Volatility interpretation framework
- Panic factor psychology

---

## Sample Prediction

When you run the agent, you'll get predictions like this:

```markdown
## PRIMARY PREDICTION

Direction: ⬆️ UPWARD
Magnitude: 2-3% increase
Price Target: $2,195 - $2,205
Timeframe: 4-6 hours
Confidence: 65%

## Reasoning
- Panic spike detected (short_panic: 2.3)
- Volatility expansion (+58% in vol60)
- Price at session low (good risk/reward)

## Risk Factors
⚠️ News events could extend panic
⚠️ Break below $2,148 invalidates prediction

## Recommended Action
Entry: $2,156
Stop: $2,147
Target: $2,200
Risk/Reward: 1:5
```

---

## Configuration

### Change Commodity

Edit `agents/src/army/main.py`:

```python
inputs = {
    'commodity': 'SILVER',  # GOLD, SILVER, NATURAL_GAS, OIL
    # ...
}
```

### Change Timeframe

```python
inputs = {
    'minutes': 240,  # 4 hours instead of 2
    # ...
}
```

**Recommended timeframes:**
- 60 minutes = Scalping/very short term
- 120 minutes = Intraday (recommended)
- 240 minutes = Swing trading
- 480 minutes = Position analysis

---

## Understanding Predictions

### Confidence Levels

- **70%+** = High confidence (strong signal)
- **60-70%** = Moderate confidence (good signal)
- **50-60%** = Low confidence (wait for confirmation)
- **<50%** = Uncertain (don't trade)

### Pattern Types

1. **Volatility Expansion** - Big move coming
2. **Panic Spike** - Reversal likely (oversold)
3. **Panic Buying** - Pullback likely (overbought)
4. **Volatility Compression** - Breakout imminent
5. **Window Alignment** - Strong trend forming

### Key Metrics

**Volatility (vol60):**
- < 0.015 = Low (calm before storm)
- 0.015-0.025 = Normal
- 0.025-0.035 = Elevated (opportunity)
- \> 0.035 = Extreme (high risk)

**Panic Factors:**
- < 1.0 = Normal
- 1.0-1.5 = Moderate stress
- 1.5-2.0 = High stress
- \> 2.0 = Panic (reversal signal)

---

## Files You Should Read

### Essential
1. **PRICE_PREDICTION_QUICK_REF.md** - Quick reference (start here!)
2. **This file** - Getting started guide

### Detailed
3. **PRICE_PREDICTION_AGENT_GUIDE.md** - Complete documentation
4. **BEFORE_AFTER_COMPARISON.md** - See what changed

### Background
5. **GRPC_ARMY_INTEGRATION_GUIDE.md** - How gRPC integration works
6. **agents/EXAMPLE_TASK_CONFIGS.md** - More examples

---

## Troubleshooting

### "No data available"
**Solution:** 
- Ensure your .NET app is running
- Check database has volatility data
- Try a different date/commodity

### "gRPC connection failed"
**Solution:**
- Start your .NET application
- Verify it's running on port 50051
- Check docker-compose is up

### "Agent makes no prediction"
**Solution:**
- Check if enough data points (need 60+ minutes)
- Verify patterns are present in data
- Review agent logs for details

### "Low confidence predictions"
**Solution:**
- Normal! Not all market states have clear signals
- Try a different timeframe
- Wait for better setup

---

## Best Practices

### ✅ DO:
- Test with `test_price_prediction.py` first
- Wait for >60% confidence before acting
- Set stop-losses based on agent recommendations
- Track prediction accuracy over time
- Use confirmation (price action) before entering

### ❌ DON'T:
- Trade on <50% confidence predictions
- Ignore risk factors listed
- Risk more than you can afford
- Rely solely on predictions (use other analysis too)
- Expect 100% accuracy (it's probabilistic)

---

## Workflow

```
1. Run test script
   ↓
2. Verify data is available
   ↓
3. Check sample prediction quality
   ↓
4. Run full crew (main.py)
   ↓
5. Read generated report
   ↓
6. Evaluate prediction confidence
   ↓
7. Wait for price confirmation
   ↓
8. Execute trade if conditions met
   ↓
9. Log result (for accuracy tracking)
   ↓
10. Refine patterns based on results
```

---

## Next Steps

### Today
1. ✅ Run `test_price_prediction.py`
2. ✅ Review sample prediction
3. ✅ Read `PRICE_PREDICTION_QUICK_REF.md`

### This Week
4. Run predictions for different commodities
5. Track prediction accuracy
6. Compare predictions vs actual price moves
7. Identify which patterns work best

### Ongoing
8. Refine historical success rates
9. Add new patterns as you discover them
10. Improve agent knowledge based on results
11. Integrate with your trading strategy

---

## Support Documentation

| Question | Read This |
|----------|-----------|
| How do I run it? | This file |
| Quick reference? | PRICE_PREDICTION_QUICK_REF.md |
| How does it work? | PRICE_PREDICTION_AGENT_GUIDE.md |
| What changed? | BEFORE_AFTER_COMPARISON.md |
| Task examples? | agents/EXAMPLE_TASK_CONFIGS.md |

---

## Remember

🎯 **This is a tool to assist decisions, not make them for you**

The agent provides:
- ✅ Data-driven insights
- ✅ Historical pattern analysis
- ✅ Probabilistic forecasts
- ✅ Risk assessment

You provide:
- ✅ Final decision
- ✅ Risk management
- ✅ Position sizing
- ✅ Trading discipline

---

## Ready?

Run this now:

```powershell
cd C:\Users\sawek\Documents\Git\DotNet\PricePredictor\agents
uv run python test_price_prediction.py
```

Then read your first price prediction! 🔮📈

---

**Questions?** Check the documentation files listed above.

**Issues?** Review the Troubleshooting section.

**Ready to trade?** Remember to use stops and manage risk!

