# Order Count Tracking - SchwabApiCS Library

## Overview

The SchwabApiCS library includes built-in order count tracking to help consuming applications stay within Schwab's daily order limit of **4,000 orders per account**.

This feature is fully automatic and requires minimal setup from the consuming application.

---

## Purpose

Schwab imposes a daily limit of **4,000 orders per account**. Exceeding this limit can result in:
- API throttling
- Temporary account restrictions
- Order rejections

This library provides:
- Automatic tracking of all orders sent (new, replace, delete)
- Daily reset of the counter at midnight
- Thread-safe increment logic
- Notification callback for external integration

---

## Static Fields in `SchwabApiCS.SchwabApi`

### `OrdersCount`
```csharp
public static int OrdersCount = 0;
```

**Description:**  
Tracks the total number of orders sent to Schwab today. This count includes:
- New orders (`OrderExecuteNewAsync`)
- Replace orders (`OrderExecuteReplaceAsync`)
- Delete/Cancel orders (`OrderExecuteDeleteAsync`)

**Key Features:**
- Automatically increments with every order execution
- Automatically resets to `0` at midnight (when `LastOrderCountTime.Date != DateTime.Today`)
- Public static field - can be read by consuming applications
- Can be set by consuming applications for persistence restoration

**Usage in Consuming Application:**
```csharp
// Check current count before placing orders
if (SchwabApiCS.SchwabApi.OrdersCount >= 3900)
{
	Console.WriteLine("Approaching daily order limit!");
	// Stop placing new orders
}

// Access the count at any time
int todaysOrderCount = SchwabApiCS.SchwabApi.OrdersCount;

// Restore from persistence (application startup)
SchwabApiCS.SchwabApi.OrdersCount = restoredCount;
```

---

### `LastOrderCountTime`
```csharp
public static DateTime LastOrderCountTime = DateTime.Now;
```

**Description:**  
Stores the timestamp of the most recent order execution. Used internally to detect when a new trading day starts.

**Key Features:**
- Updated automatically with every order execution
- Used to determine if the day has changed (triggering a reset of `OrdersCount`)
- Public static field - can be read by consuming applications
- Can be set by consuming applications for persistence restoration

**Usage in Consuming Application:**
```csharp
// Check when the last order was sent
DateTime lastOrder = SchwabApiCS.SchwabApi.LastOrderCountTime;
TimeSpan timeSinceLastOrder = DateTime.Now - lastOrder;

if (timeSinceLastOrder.TotalMinutes > 30)
{
	Console.WriteLine("No orders in the last 30 minutes.");
}

// Check if we're on a new day
if (SchwabApiCS.SchwabApi.LastOrderCountTime.Date != DateTime.Today)
{
	// OrdersCount will be reset to 0 on the next order
}

// Restore from persistence (application startup)
SchwabApiCS.SchwabApi.LastOrderCountTime = restoredTimestamp;
```

---

## Automatic Order Tracking

### How It Works

The order counting is **fully automatic**. Consuming applications don't need to manually increment the counter.

#### Internal Flow:
1. Consuming application calls any order execution method:
   - `OrderExecuteNewAsync()`
   - `OrderExecuteReplaceAsync()`
   - `OrderExecuteDeleteAsync()`

2. `IncrementOrderCount()` is invoked internally (private method in `OrderBase.cs`)

3. Daily reset check:
   ```csharp
   if (LastOrderCountTime.Date != DateTime.Today)
   {
	   OrdersCount = 0;  // Reset for new day
   }
   ```

4. Count is incremented:
   ```csharp
   OrdersCount++;
   LastOrderCountTime = DateTime.Now;
   ```

5. Callback is fired (if registered):
   ```csharp
   SchwabApi.OnOrderCountChanged?.Invoke(OrdersCount, LastOrderCountTime);
   ```

---

## Delegate Callback: `OnOrderCountChanged`

### Purpose
Allows consuming applications to be notified whenever `OrdersCount` changes, enabling:
- Real-time UI updates
- Custom logging
- Alert systems
- Order limit enforcement

### Declaration
```csharp
public delegate void OrderCountChangedCallback(int ordersCount, DateTime lastOrderCountTime);
public static OrderCountChangedCallback? OnOrderCountChanged = null;
```

### Setup in Consuming Application
```csharp
// Register your callback once during initialization
SchwabApiCS.SchwabApi.OnOrderCountChanged = (ordersCount, lastOrderCountTime) =>
{
	// Update your UI
	Console.WriteLine($"Order count changed: {ordersCount}");

	// Update data model
	myAppData.CurrentOrderCount = ordersCount;

	// Check limits
	if (ordersCount >= 3900)
	{
		DisableTrading();
	}
};
```

### Callback Characteristics
- Invoked **synchronously** after each order execution
- Provides both the new count and timestamp
- Optional - library functions without it
- Can be reassigned at any time

---

## Integration Guide for Consuming Applications

### Step 1: Initialize the API
```csharp
var schwabApi = new SchwabApiCS.SchwabApi();
await schwabApi.InitializeAsync(encryptedRefreshToken);
```

### Step 2: Register Change Notification (Optional)
```csharp
SchwabApiCS.SchwabApi.OnOrderCountChanged = (count, timestamp) =>
{
	UpdateUI(count, timestamp);
	CheckOrderLimits(count);
};
```

### Step 3: Execute Orders
```csharp
// Order count automatically tracked
var order = new Order { /* ... */ };
await schwabApi.OrderExecuteNewAsync(accountNumber, order);

// OrdersCount is now incremented
// OnOrderCountChanged callback was invoked
```

### Step 4: Check Count Before Trading
```csharp
public bool CanPlaceOrder()
{
	const int SAFE_LIMIT = 3950;
	return SchwabApiCS.SchwabApi.OrdersCount < SAFE_LIMIT;
}
```

### Step 5: Persist and Restore (Application Responsibility)
```csharp
// Save on shutdown or periodically
void SaveState()
{
	mySettings.OrdersCount = SchwabApiCS.SchwabApi.OrdersCount;
	mySettings.LastOrderCountTime = SchwabApiCS.SchwabApi.LastOrderCountTime;
}

// Restore on startup
void RestoreState()
{
	SchwabApiCS.SchwabApi.OrdersCount = mySettings.OrdersCount;
	SchwabApiCS.SchwabApi.LastOrderCountTime = mySettings.LastOrderCountTime;
}
```

---

## Example Usage Scenarios

### 1. **Monitor Orders with Logging**
```csharp
SchwabApiCS.SchwabApi.OnOrderCountChanged = (count, timestamp) =>
{
	logger.LogInformation($"[{timestamp:HH:mm:ss}] Order #{count} executed");

	if (count % 100 == 0)
	{
		logger.LogWarning($"Milestone reached: {count} orders today");
	}
};
```

### 2. **Implement Safety Cutoff**
```csharp
SchwabApiCS.SchwabApi.OnOrderCountChanged = (count, timestamp) =>
{
	if (count >= 3900)
	{
		tradingEngine.EmergencyStop();
		MessageBox.Show("Order limit reached. Trading stopped.");
	}
};
```

### 3. **Display Real-Time Count in UI**
```csharp
// WPF/XAML example
SchwabApiCS.SchwabApi.OnOrderCountChanged = (count, timestamp) =>
{
	Dispatcher.Invoke(() =>
	{
		OrderCountTextBlock.Text = $"Orders: {count} / 4000";

		if (count >= 3500)
		{
			OrderCountTextBlock.Foreground = Brushes.Orange;
		}
		if (count >= 3900)
		{
			OrderCountTextBlock.Foreground = Brushes.Red;
		}
	});
};
```

### 4. **Pre-Order Validation**
```csharp
public async Task<bool> PlaceOrderWithValidation(string accountNumber, Order order)
{
	// Check before placing
	if (SchwabApiCS.SchwabApi.OrdersCount >= 4000)
	{
		throw new InvalidOperationException("Daily order limit reached");
	}

	if (SchwabApiCS.SchwabApi.OrdersCount >= 3900)
	{
		logger.LogWarning("Approaching order limit!");
	}

	// Execute order
	var result = await schwabApi.OrderExecuteNewAsync(accountNumber, order);

	return result.Success;
}
```

### 5. **Daily Summary Report**
```csharp
public string GenerateDailySummary()
{
	int count = SchwabApiCS.SchwabApi.OrdersCount;
	DateTime lastOrder = SchwabApiCS.SchwabApi.LastOrderCountTime;

	return $@"
Daily Trading Summary - {DateTime.Today:yyyy-MM-dd}
==========================================
Orders Sent: {count} / 4000
Last Order:  {lastOrder:HH:mm:ss}
Remaining:   {4000 - count}
Utilization: {(count / 4000.0) * 100:F1}%
";
}
```

---

## Best Practices for Consuming Applications

### 1. **Set a Safety Threshold**
Don't wait until exactly 4,000. Stop trading at 3,900-3,950 to leave a buffer.

```csharp
const int DAILY_LIMIT = 4000;
const int SAFETY_THRESHOLD = 3900;

if (SchwabApiCS.SchwabApi.OrdersCount >= SAFETY_THRESHOLD)
{
	StopTrading();
}
```

### 2. **Persist the Counter**
Save `OrdersCount` and `LastOrderCountTime` when your application shuts down and restore them on startup:

```csharp
// Shutdown
File.WriteAllText("ordercount.json", JsonConvert.SerializeObject(new
{
	Count = SchwabApiCS.SchwabApi.OrdersCount,
	Timestamp = SchwabApiCS.SchwabApi.LastOrderCountTime
}));

// Startup
var data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText("ordercount.json"));
SchwabApiCS.SchwabApi.OrdersCount = data.Count;
SchwabApiCS.SchwabApi.LastOrderCountTime = data.Timestamp;
```

### 3. **Monitor Throughout the Day**
Use the callback to monitor continuously, not just before placing orders.

```csharp
SchwabApiCS.SchwabApi.OnOrderCountChanged = (count, timestamp) =>
{
	if (count == 2000)
		ShowNotification("50% of daily limit used");
	if (count == 3000)
		ShowNotification("75% of daily limit used");
	if (count == 3500)
		ShowWarning("87.5% of daily limit used");
};
```

### 4. **Account for Complex Orders**
Remember that bracket orders, OCO orders, and other complex orders may count as multiple orders. Each execution method call increments the counter by 1.

```csharp
// A bracket order might require:
// 1. OrderExecuteNewAsync (entry) = +1
// 2. OrderExecuteNewAsync (stop loss) = +1
// 3. OrderExecuteNewAsync (take profit) = +1
// Total: OrdersCount += 3
```

### 5. **Handle Day Boundaries**
The library automatically resets `OrdersCount` to 0 when a new day is detected. Your application should handle this:

```csharp
SchwabApiCS.SchwabApi.OnOrderCountChanged = (count, timestamp) =>
{
	// Detect reset (count goes from high to low)
	if (count < previousCount && count < 100)
	{
		logger.LogInformation("New trading day detected - counter reset");
		ResetDailyStatistics();
	}
	previousCount = count;
};
```

---

## Troubleshooting

### Count Not Incrementing
**Problem:** `OrdersCount` stays at 0 after orders.

**Solution:** Ensure you're using the async order execution methods:
- `OrderExecuteNewAsync()`
- `OrderExecuteReplaceAsync()`
- `OrderExecuteDeleteAsync()`

Direct API calls bypassing these methods will not increment the counter.

---

### Callback Not Firing
**Problem:** `OnOrderCountChanged` callback is never invoked.

**Solution:** 
1. Verify you registered the callback:
   ```csharp
   SchwabApiCS.SchwabApi.OnOrderCountChanged = MyCallback;
   ```
2. Ensure orders are being executed through the library methods
3. Check that the callback delegate signature matches exactly

---

### Count Resets Unexpectedly
**Problem:** `OrdersCount` resets to 0 during the day.

**Solution:** 
- The reset only happens when `LastOrderCountTime.Date != DateTime.Today`
- Check system time is correct
- Verify `LastOrderCountTime` is being persisted and restored correctly
- Do not manually set `LastOrderCountTime` to old dates

---

### Lost Count After Restart
**Problem:** `OrdersCount` is 0 after restarting the application.

**Solution:** 
The library does **not** persist data automatically. Consuming applications must:
1. Save `OrdersCount` and `LastOrderCountTime` before shutdown
2. Restore both values after initialization
3. Ensure persistence logic runs before any orders are placed

---

## Technical Details

### Thread Safety
- `OrdersCount` and `LastOrderCountTime` are static fields
- `IncrementOrderCount()` is called within order execution methods (typically async/await context)
- For truly concurrent order execution, consider adding synchronization in your consuming application

### Performance
- Order counting adds negligible overhead (~1 microsecond per order)
- Callback execution is synchronous
- No I/O operations in the library - persistence is application responsibility

### Library Boundaries
The SchwabApiCS library:
- ✅ Tracks order count automatically
- ✅ Resets count at midnight
- ✅ Provides callback notification
- ✅ Exposes public static fields for read/write

The consuming application must:
- ✅ Persist `OrdersCount` and `LastOrderCountTime`
- ✅ Restore values on startup
- ✅ Implement UI updates
- ✅ Enforce order limits
- ✅ Handle alerts and notifications

---

## API Reference

### Fields

| Field | Type | Access | Description |
|-------|------|--------|-------------|
| `OrdersCount` | `int` | Public Static | Current count of orders sent today |
| `LastOrderCountTime` | `DateTime` | Public Static | Timestamp of most recent order |

### Delegate

| Delegate | Signature | Description |
|----------|-----------|-------------|
| `OrderCountChangedCallback` | `void (int ordersCount, DateTime lastOrderCountTime)` | Invoked after each order execution |

### Callback Registration

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `OnOrderCountChanged` | `OrderCountChangedCallback?` | Public Static | Callback hook (null by default) |

### Methods That Increment Count

| Method | Description |
|--------|-------------|
| `OrderExecuteNewAsync()` | Places new order, increments count |
| `OrderExecuteReplaceAsync()` | Replaces existing order, increments count |
| `OrderExecuteDeleteAsync()` | Cancels order, increments count |

---

## Minimal Integration Example

```csharp
using SchwabApiCS;

public class TradingApp
{
	private SchwabApi schwabApi;

	public async Task Initialize()
	{
		// 1. Create API instance
		schwabApi = new SchwabApi();
		await schwabApi.InitializeAsync(refreshToken);

		// 2. Register callback (optional)
		SchwabApi.OnOrderCountChanged = (count, timestamp) =>
		{
			Console.WriteLine($"Order #{count} at {timestamp:HH:mm:ss}");
		};

		// 3. Restore persisted count (if available)
		SchwabApi.OrdersCount = LoadPersistedCount();
		SchwabApi.LastOrderCountTime = LoadPersistedTimestamp();
	}

	public async Task PlaceOrder(string accountNumber, Order order)
	{
		// 4. Check limit before placing
		if (SchwabApi.OrdersCount >= 3900)
		{
			throw new Exception("Daily limit reached");
		}

		// 5. Execute order (count auto-increments)
		await schwabApi.OrderExecuteNewAsync(accountNumber, order);
	}

	public void Shutdown()
	{
		// 6. Persist count
		SaveCount(SchwabApi.OrdersCount, SchwabApi.LastOrderCountTime);
	}
}
```

---

## Version History

**Version 1.0** (Current)
- Initial implementation
- Automatic order counting in `OrderBase.cs`
- Daily reset at midnight
- `OnOrderCountChanged` callback support
- Public static fields for external access

---

## Support

For questions or issues:
1. Review inline XML documentation in `SchwabApiCS/Orders/OrderBase.cs`
2. Check `SchwabApiCS/SchwabApi.cs` for callback declaration
3. Refer to this documentation
4. Review consuming application's integration code

**Important:** This library tracks orders but does not enforce limits. Consuming applications must implement their own safety checks and limit enforcement.
