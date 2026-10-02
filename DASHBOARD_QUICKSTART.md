# Factory Monitoring Dashboard - Quick Start Guide

## Dashboard URL
```
http://127.0.0.1:5180/
```

## What You'll See

### 1. **Header**
- Status indicator (colored dot that pulses)
- "FACTORY MONITORING" title
- Green = Normal, Orange = Warning, Red = Critical

### 2. **Production Summary (Top Grid)**
Shows 4 cards with today's production:
- **Tổng Sản Lượng (Total Quantity)** - Total items produced today
- **Sản Phẩm OK** - Products that passed quality check
- **Sản Phẩm NG** - Defective products
- **Tỷ Lệ OK (OK Ratio)** - Percentage of good products

Each card shows a progress bar for visual reference.

### 3. **System Status (Middle Grid)**
Shows 4 status cards:
- **Thiết Bị Hoạt Động** - Number of active devices
- **Cảm Biến Hoạt Động** - Number of active sensors
- **Báo Động Chưa Xử Lý** - Unresolved alarms (red if any)
- **Sản Phẩm Hiện Tại** - Current product being processed

### 4. **Production Line Flow**
Diagram showing:
- Sensors (CB01, CB02, CB03...) with ON/OFF status
- Devices (Băng tải, etc.) with RUNNING/STOP status
- Arrow flow showing production process

### 5. **Thiết Bị (Devices)**
Grid of all equipment:
- Green border = RUNNING
- Red border = STOPPED
- Shows device name and current status

### 6. **Cảm Biến (Sensors)**
Grid of all sensors:
- Current reading value
- Sensor name
- Unit of measurement
- Green border = Active, Red = Inactive

### 7. **Báo Động (Alarms)**
Table showing last 10 alarms:
- **Mức Độ** - Severity (ERROR/WARN/INFO)
- **Thiết Bị** - Which device triggered alarm
- **Nội Dung** - Description
- **Thời Gian** - When it occurred
- **Trạng Thái** - Status (CHƯA XỬ LÝ / ĐÃ XỬ LÝ)

### 8. **Bảo Trì (Maintenance)**
List of maintenance schedules:
- Equipment ID
- Maintenance type
- Last service date
- Next scheduled service
- Status (OK or SẮP ĐẾN = coming soon)

### 9. **Update Time**
Bottom right shows last update time in format: `dd/MM/yyyy HH:mm:ss`

## Auto-Refresh
The dashboard automatically refreshes every 30 seconds to show latest data.
To refresh manually, press F5 or click the refresh button.

## API Endpoints (for programmatic access)

Get production data only:
```
GET /api/dashboard/summary
```
Returns: { TongSanLuong, SanLuongOK, SanLuongNG, TyLeOK, ... }

Get device list:
```
GET /api/dashboard/devices
```
Returns: Array of devices with status

Get sensor readings:
```
GET /api/dashboard/sensors
```
Returns: Array of sensors with latest values

Get recent alarms:
```
GET /api/dashboard/alarms?limit=10
```
Returns: Array of recent alarms (default 10)

Get complete dashboard data:
```
GET /api/dashboard/full
```
Returns: Complete DashboardMonitoringModel object

## Color Meanings

- 🟢 **Green** (#4CAF50) - Running/Active/OK
- 🔴 **Red** (#f44336) - Stopped/Inactive/Error
- 🟠 **Orange** (#ff9800) - Warning/Attention needed
- 🔵 **Blue** (#2196F3) - Information/Informational

## Interpretation Guide

| Card | Green | Orange | Red |
|------|-------|--------|-----|
| Thiết Bị | All running | Some stopped | Many stopped |
| Cảm Biến | All active | Some inactive | Many inactive |
| Báo Động | 0 | 1-3 | 4+ |
| Tỷ Lệ OK | >95% | 85-95% | <85% |

## Tips

1. **Check Tỷ Lệ OK first** - This tells you overall product quality
2. **Look at Báo Động** - Unresolved alarms indicate issues
3. **Monitor Thiết Bị** - Equipment status shows system capacity
4. **Review Sensors** - Values show production line conditions
5. **Plan Maintenance** - Check Bảo Trì for upcoming service needs

## Troubleshooting

- **No data showing?** Make sure database tables have data
- **All devices showing STOPPED?** Check if production hasn't started today
- **No sensors?** Verify SensorData table has recent readings
- **Alarms empty?** No alarms have been triggered
- **Auto-refresh not working?** Check browser console for errors

## Last Update
Documentation last updated: 2026-08-24
