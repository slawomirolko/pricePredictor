# Example Task Configurations Using Volatility Tool

## Example 1: Basic Volatility Analysis

```yaml
volatility_analysis_task:
  description: >
    Analyze market volatility for {commodity} on {date}:
    
    1. Use the volatility_query tool to fetch {minutes} minutes of data
    2. Examine the volatility metrics (vol5, vol15, vol60)
    3. Identify periods where volatility exceeded normal levels
    4. Check panic factor indicators for unusual market behavior
    5. Summarize key findings
    
    Tool usage: volatility_query(commodity="{commodity}", date="{date}", minutes={minutes})
  expected_output: >
    A detailed volatility analysis report including:
    - Summary statistics (avg price, max volatility, max panic)
    - Periods of high volatility with timestamps
    - Panic factor interpretation
    - Overall market sentiment assessment
  agent: researcher
```

## Example 2: Trading Signal Detection

```yaml
trading_signals_task:
  description: >
    Detect potential trading signals for {commodity}:
    
    1. Query volatility data using: volatility_query(commodity="{commodity}", date="{date}", minutes=240)
    2. Look for these patterns:
       - Volatility spike: vol60 > 0.03
       - Panic selling: short_panic > 2.0
       - Panic buying: long_panic > 2.0
       - Price reversals: large gap between high and low
    3. For each signal found, note the timestamp and metrics
    4. Assess signal strength and reliability
    
    Provide actionable trading recommendations based on the data.
  expected_output: >
    Trading signals report with:
    - List of identified signals with timestamps
    - Signal type (buy/sell/hold)
    - Confidence level for each signal
    - Risk assessment
    - Recommended actions
  agent: researcher
```

## Example 3: Multi-Commodity Comparison

```yaml
commodity_comparison_task:
  description: >
    Compare volatility across multiple commodities:
    
    1. Query volatility for GOLD: volatility_query(commodity="GOLD", date="{date}", minutes=120)
    2. Query volatility for SILVER: volatility_query(commodity="SILVER", date="{date}", minutes=120)
    3. Query volatility for OIL: volatility_query(commodity="OIL", date="{date}", minutes=120)
    4. Compare:
       - Average volatility levels
       - Panic factor correlations
       - Price movement patterns
    5. Identify which commodity shows the most trading opportunity
  expected_output: >
    Comparative analysis showing:
    - Volatility rankings
    - Correlation insights
    - Best trading opportunities
    - Risk-adjusted recommendations
  agent: researcher
```

## Example 4: Risk Assessment

```yaml
risk_assessment_task:
  description: >
    Assess current market risk for {commodity}:
    
    1. Get recent volatility data: volatility_query(commodity="{commodity}", date="{date}", minutes=60)
    2. Analyze risk indicators:
       - Current volatility vs normal range
       - Panic factor levels
       - Volume patterns
       - Price stability (high-low spread)
    3. Calculate risk score based on:
       - High vol60 = higher risk
       - High panic factors = higher risk
       - Large price swings = higher risk
    4. Provide risk rating: LOW, MEDIUM, HIGH, EXTREME
  expected_output: >
    Risk assessment report with:
    - Overall risk rating
    - Key risk factors identified
    - Risk score calculation breakdown
    - Hedging recommendations if high risk
  agent: reporting_analyst
```

## Example 5: Volatility Pattern Detection

```yaml
pattern_detection_task:
  description: >
    Detect volatility patterns in {commodity} market:
    
    1. Fetch extended data: volatility_query(commodity="{commodity}", date="{date}", minutes=360)
    2. Look for patterns:
       - Increasing volatility trend
       - Decreasing volatility trend
       - Volatility clustering (high vol periods together)
       - Calm before storm (low vol before spike)
       - Mean reversion patterns
    3. Correlate patterns with panic factors
    4. Predict likely next movements based on patterns
  expected_output: >
    Pattern analysis with:
    - Identified patterns with timestamps
    - Pattern significance assessment
    - Correlation with panic factors
    - Predictions for next 1-2 hours
    - Confidence levels
  agent: researcher
```

## Example 6: Hourly Market Report

```yaml
hourly_report_task:
  description: >
    Generate hourly market report for {commodity}:
    
    1. Query last hour: volatility_query(commodity="{commodity}", date="{date}", minutes=60)
    2. Summarize:
       - Price change (opening vs closing)
       - Highest and lowest points
       - Average volatility across timeframes
       - Any panic events (factors > 1.5)
       - Volume trends
    3. Compare to previous hour if available
    4. Highlight any unusual activity
  expected_output: >
    Concise hourly report including:
    - Price summary (open, high, low, close)
    - Volatility summary
    - Notable events
    - Comparison to previous period
    - Outlook for next hour
  agent: reporting_analyst
  output_file: 'hourly_report_{commodity}_{date}.md'
```

## Main Configuration Example

Complete example of `tasks.yaml`:

```yaml
research_task:
  description: >
    Research {topic} and gather relevant market data.
    Use volatility_query tool when analyzing commodity markets.
  expected_output: >
    A comprehensive research report with data-backed insights
  agent: researcher

volatility_task:
  description: >
    Analyze volatility for {commodity} on {date}:
    Use volatility_query(commodity="{commodity}", date="{date}", minutes=120)
    to fetch 2 hours of volatility data and analyze patterns.
  expected_output: >
    Volatility analysis with key metrics and trading signals
  agent: researcher

reporting_task:
  description: >
    Create a detailed report combining research findings and volatility analysis.
    Provide actionable insights and recommendations.
  expected_output: >
    Final comprehensive report with all findings
  agent: reporting_analyst
  output_file: 'report.md'
```

## Input Variables Example

When running your crew, provide:

```python
inputs = {
    'topic': 'Commodity Market Volatility',
    'commodity': 'GOLD',
    'date': '2026-03-05',
    'minutes': 120,
    'current_year': '2026'
}
```

## Tips

1. **Start with small time windows** (60-120 minutes) to avoid overwhelming the LLM
2. **Be specific in task descriptions** about what metrics to look for
3. **Combine volatility data with other tools** for richer analysis
4. **Use panic factors as key indicators** - they're unique to your system
5. **Chain tasks** - use volatility analysis as input to trading decision tasks

