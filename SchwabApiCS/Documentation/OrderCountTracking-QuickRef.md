# Order Count Tracking - Quick Reference

## TL;DR
The SchwabApiCS library automatically tracks daily order count to help stay within Schwab's 4,000 order limit.

---

## The Two Static Fields

```csharp
// In SchwabApiCS.SchwabApi class:
public static int OrdersCount = 0;              // Count of orders sent today
public static DateTime LastOrderCountTime = DateTime.Now;  // Timestamp of last order
```

---

## How It Works

1. **Automatic Tracking**: Every call to `OrderExecuteNewAsync()`, `OrderExecuteReplaceAsync()`, or `OrderExecuteDeleteAsync()` increments `OrdersCount`
2. **Daily Reset**: Counter resets to 0 when `LastOrderCountTime.Date != DateTime.Today`
3. **Notification**: Optional callback `OnOrderCountChanged` fires after each increment

---

## Quick Setup (Consuming Application)

```csharp
// 1. Initialize API
var schwabApi = new SchwabApiCS.SchwabApi();
await schwabApi.InitializeAsync(refreshToken);

// 2. Register callback (optional)
SchwabApiCS.SchwabApi.OnOrderCountChanged = (count, timestamp) =>
{
	Console.WriteLine($"Order count: {count}");
	if (count >= 3900) StopTrading();
};

// 3. Restore from persistence (if you saved it)
SchwabApiCS.SchwabApi.OrdersCount = savedCount;
SchwabApiCS.SchwabApi.LastOrderCountTime = savedTimestamp;

// 4. Place orders (count auto-increments)
await schwabApi.OrderExecuteNewAsync(accountNumber, order);

// 5. Check count anytime
if (SchwabApiCS.SchwabApi.OrdersCount >= 3900)
{
	Console.WriteLine("Approaching limit!");
}
```

---

## Common Tasks

### Check Current Count
```csharp
int count = SchwabApiCS.SchwabApi.OrdersCount;
Console.WriteLine($"Orders today: {count} / 4000");
```

### Pre-Order Validation
```csharp
if (SchwabApiCS.SchwabApi.OrdersCount >= 3900)
{
	throw new Exception("Daily order limit reached");
}
await schwabApi.OrderExecuteNewAsync(accountNumber, order);
```

### Monitor Changes
```csharp
SchwabApiCS.SchwabApi.OnOrderCountChanged = (count, timestamp) =>
{
	UpdateUI(count);
	if (count >= 3900) DisableTrading();
};
```

### Persist State
```csharp
// Save on shutdown
SaveToFile(SchwabApiCS.SchwabApi.OrdersCount, 
		   SchwabApiCS.SchwabApi.LastOrderCountTime);

// Restore on startup
var (count, timestamp) = LoadFromFile();
SchwabApiCS.SchwabApi.OrdersCount = count;
SchwabApiCS.SchwabApi.LastOrderCountTime = timestamp;
```

---

## Important Notes

✅ **Library Provides:**
- Automatic counting
- Daily reset at midnight
- Change notification callback
- Public read/write access to fields

❌ **Library Does NOT Provide:**
- Persistence (your app must save/restore)
- Order limit enforcement (your app must check and stop)
- UI updates (your app must implement)
- Alerts (your app must implement)

---

## Safety Recommendations

| Threshold | Action |
|-----------|--------|
| **3,000** | Log warning (75% used) |
| **3,500** | Display alert (87.5% used) |
| **3,900** | Stop trading (97.5% used) |
| **4,000** | Hard limit - reject all orders |

**Best Practice:** Stop at 3,900 to leave a 100-order safety buffer.

---

## Callback Signature

```csharp
public delegate void OrderCountChangedCallback(int ordersCount, DateTime lastOrderCountTime);
public static OrderCountChangedCallback? OnOrderCountChanged = null;
```

**Characteristics:**
- Invoked synchronously after each order
- Optional (null by default)
- Can be reassigned anytime
- Parameters: new count and timestamp

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Count not incrementing | Use `OrderExecuteNewAsync/Replace/DeleteAsync` methods |
| Callback not firing | Verify `OnOrderCountChanged` is assigned |
| Count lost after restart | Implement persistence in your application |
| Count resets during day | Check system time; verify `LastOrderCountTime` persistence |

---

## Key Files

| File | Purpose |
|------|---------|
| `SchwabApiCS/Orders/OrderBase.cs` | Contains `IncrementOrderCount()` logic |
| `SchwabApiCS/SchwabApi.cs` | Declares callback delegate |
| `SchwabApiCS/Documentation/OrderCountTracking.md` | Full documentation |

---

## Minimal Example

```csharp
// Initialize
var api = new SchwabApiCS.SchwabApi();
await api.InitializeAsync(token);

// Optional: monitor changes
SchwabApiCS.SchwabApi.OnOrderCountChanged = (c, t) => 
	Console.WriteLine($"Order #{c}");

// Place orders (count auto-updates)
await api.OrderExecuteNewAsync(accountNumber, order);

// Check count
Console.WriteLine($"Total: {SchwabApiCS.SchwabApi.OrdersCount}");
```

---

## Full Documentation
See: `SchwabApiCS/Documentation/OrderCountTracking.md`
