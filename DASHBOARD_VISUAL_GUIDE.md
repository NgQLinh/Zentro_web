# Factory Monitoring Dashboard - Visual Reference Guide

## Dashboard Layout Map

```
┌─────────────────────────────────────────────────────────────────┐
│  🟢 FACTORY MONITORING (Status Indicator + Title)               │
│                                                                  │
├─────────────────────────────────────────────────────────────── ──┤
│  PRODUCTION SUMMARY (4 Cards - Grid Layout)                     │
├───────────────────┬───────────────────┬───────────────────┬──────┤
│ Tổng Sản Lượng    │ Sản Phẩm OK        │ Sản Phẩm NG       │ TỶ LỆ│
│ ┌─────────────┐   │ ┌─────────────┐    │ ┌─────────────┐   │ ┌───┤
│ │    1250     │   │ │     620     │    │ │     50      │   │ │98 │
│ └─────────────┘   │ └─────────────┘    │ └─────────────┘   │ │ % │
│ [========    ]    │ [===========]      │ [==            ]  │ └───┤
│ Hôm nay           │ Đạt chất lượng     │ Không đạt        │ Chất │
├───────────────────┴───────────────────┴───────────────────┴──────┤
│  SYSTEM STATUS (4 Cards)                                         │
├───────────────────┬───────────────────┬───────────────────┬──────┤
│ Thiết Bị          │ Cảm Biến          │ Báo Động Chưa     │Sản Phẩm
│ Hoạt Động         │ Hoạt Động         │ Xử Lý             │Hiện Tại
│ ┌─────────────┐   │ ┌─────────────┐    │ ┌─────────────┐   │ ┌───┤
│ │    2 / 2    │   │ │    6 / 6    │    │ │      0      │   │ │SP │
│ │ Devices     │   │ │ Sensors     │    │ │ Alarms      │   │ │ L │
│ └─────────────┘   │ └─────────────┘    │ └─────────────┘   │ │ot │
├───────────────────┴───────────────────┴───────────────────┴──────┤
│  TRẠNG THÁI DÂY CHUYỀN (Production Flow Diagram)                 │
│                                                                  │
│  [ CB01 | ● ON ] → [ BĂNG TẢI 01 | RUNNING ] → [ CB02 | ● ON]    │
│                                                                  │
├───────────────────────────────────────────────────────────────── ┤
│  THIẾT BỊ (Devices Grid)                                         │
├─────────────────┬─────────────────┬──────────────┬───────────────┤
│     ⚙️          │       ⚙️        │      ⚙️     │      ⚙️      │
│  Băng Tải 01    │  Băng Tải 02    │   Máy Phân  │  Máy Phân      │
│    RUNNING      │    STOPPED      │   Loại 1   │  Loại 2         │
├─────────────────┴─────────────────┴──────────────┴───────────────┤
│  CẢM BIẾN (Sensors Grid)                                         │
├──────────┬──────────┬──────────┬──────────┬──────────┬───────────┤
│  CB01    │  CB02    │  CB03    │  CB04    │  CB05    │   CB06    │
│   0.5    │   2.1    │  123.4   │   0.0    │   45.6   │   12.3    │
│  Quang   │  Quang   │  Phân    │  Quang   │  Nhiệt   │   Cân     │
│          │          │  Loại    │          │          │           │
├──────────┴──────────┴──────────┴──────────┴──────────┴───────────┤
│  BÁO ĐỘNG (Alarms Table)                                         │
├─────────┬──────────┬──────────────────────┬──────────┬───────────┤
│ Mức Độ  │ Thiết Bị │ Nội Dung             │ Thời Gian│ Trạng Thái│
├─────────┼──────────┼──────────────────────┼──────────┼───────────┤
│ ERROR   │ BT01     │ Quá tải              │ 08:30:15 │ CHƯA XỬ LÝ
│ WARN    │ CB02     │ Mất tín hiệu          │ 08:25:00 │ ĐÃ CONFIRMED
│ INFO    │ M01      │ Hoàn tất phân loại   │ 08:20:30 │ ĐÃ XỬ LÝ
├─────────┴──────────┴──────────────────────┴──────────┴───────────┤
│  BẢO TRÌ (Maintenance Schedule)                                  │
├─────────────────────────────────────────────────────────────────┤
│ BT01 - Định kỳ                             OK                    │
│   Ngày BT: 15/08/2026  |  Lần Tiếp Theo: 15/09/2026              │
│                                                                  │
│ CB02 - Định kỳ                             OK                    │
│   Ngày BT: 10/08/2026  |  Lần Tiếp Theo: 10/09/2026              │
│                                                                  │
│ M01 - Sửa chữa                        SẮP ĐẾN                    │
│   Ngày BT: 01/08/2026  |  Lần Tiếp Theo: 01/11/2026              │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│ Cập nhật lúc: 24/08/2026 14:35:22                                │
└───────────────────────────────────────────────────────────────── ┘
```

---

## Color Legend

### Status Indicators
```
🟢 GREEN (#4CAF50)     - Active, Running, OK, Healthy
🟠 ORANGE (#ff9800)    - Warning, Attention Needed, Pending
🔴 RED (#f44336)       - Error, Stopped, Failed, Critical
🔵 BLUE (#2196F3)      - Information, Neutral, Alert
⚫ YELLOW (#ffeb3b)    - Data Value, Highlighted Info
```

### Card Styling
```
┌─────────────────────────────┐
│ LABEL                       │ ← Label (gray text, uppercase)
│                             │
│      Value                  │ ← Large bold value
│                             │
│   [========    ]            │ ← Progress bar
│ Sub Label                   │ ← Secondary info (blue)
└─────────────────────────────┘
```

### Device Status Cards
```
GREEN BORDER (Active)          RED BORDER (Inactive)
┌──────────────────┐           ┌──────────────────┐
│ ⚙️               │           |⚙️               │
│ Device Name      │           │ Device Name      │
│ RUNNING          │           │ STOPPED          │
│ 80%              │           │                  │
└──────────────────┘           └──────────────────┘
```

### Sensor Cards
```
ACTIVE (Green)                INACTIVE (Red)
┌──────────────────┐           ┌──────────────────┐
│ CB01             │           │ CB04             │
│ 45.6             │           │ N/A              │
│ Nhiệt Độ         │           │ Cảm Biến         │
│ °C               │           │ (opacity: 0.6)   │
└──────────────────┘           └──────────────────┘
```
---

## Data Display Examples

### Production Summary
```
Today's Statistics:
├─ Total Produced: 1,250 items
├─ Good Quality: 620 (49.6%)
├─ Defects: 50 (4%)
└─ OK Ratio: 92.5%
```

### System Status
```
Current System State:
├─ Devices Online: 2 of 2
├─ Sensors Active: 6 of 6
├─ Unresolved Alarms: 0
└─ Current Product: SP-001 / Lot-A
```

### Alarm Severity Coding
```
ERROR  (Red)    - Critical issue, requires immediate action
WARN   (Orange) - Warning state, needs attention
INFO   (Blue)   - Informational, no action needed
```

### Maintenance Status
```
OK         - All good, no action needed
SẮP ĐẾN    - Coming soon, plan ahead
DUE        - Maintenance due, schedule now
OVERDUE    - Past due, take action immediately
```

---

## Interactive Features

### Hover Effects
```
Normal State              Hover State
┌─────────────────┐     ┌─────────────────┐
│                 │     │ ↨ (moved up)    │
│  Stat Card      │ → │  Stat Card      │
│                 │     │  (border glow)  │
└─────────────────┘     └─────────────────┘
```

### Status Indicator Animation
```
Frame 1        Frame 2        Frame 3        Frame 4
🟢            🟢            🟢            🟢
(opacity:1)   (opacity:0.5) (opacity:0.5) (opacity:1)
```
Pulses continuously to draw attention to system status.

---

## Responsive Design Breakpoints

### Mobile (< 768px)
```
┌──────────────────────┐
│ Card 1               │
├──────────────────────┤
│ Card 2               │
├──────────────────────┤
│ Card 3               │
├──────────────────────┤
│ Card 4               │
└──────────────────────┘
```
(Single column stack)

### Tablet (768px - 1024px)
```
┌─────────────────┬─────────────────┐
│ Card 1          │ Card 2          │
├─────────────────┼─────────────────┤
│ Card 3          │ Card 4          │
└─────────────────┴─────────────────┘
```
(2 column grid)

### Desktop (> 1024px)
```
┌──────────┬──────────┬──────────┬──────────┐
│ Card 1   │ Card 2   │ Card 3   │ Card 4   │
└──────────┴──────────┴──────────┴──────────┘
```
(4 column grid)

---

## Update Cycle

```
00:00                             23:59
│◄───────── Every 30 seconds ────────►│

Page Loads
    ↓
Fetch Data from Database
    ↓
Render Dashboard
    ↓
[30 second wait]
    ↓
Auto-Refresh (location.reload())
    ↓
[cycle repeats]
```

---

## Data Refresh Flow

```
Browser                  Server                    Database
│                         │                          │
├─ Page Load ────────────→│                          │
│                         ├─ Query ProductionRecords→│
│                         │                          │
│                         │←─ Data Returned ────────┤
│                         │                          │
│                         ├─ Query Devices ────────→│
│                         │←─ Data Returned ────────┤
│                         │                          │
│                         ├─ Query Sensors ────────→│
│                         │←─ Data Returned ────────┤
│                         │                          │
│                         ├─ Query Alarms ────────→│
│                         │←─ Data Returned ────────┤
│                         │                          │
│←─ HTML Response ───────┤                          │
│                         │                          │
[Display Dashboard]       │                          │
│                         │                          │
[Wait 30 seconds]         │                          │
│                         │                          │
├─ Auto Refresh ────────→│                          │
│  (cycle repeats)        │                          │
```

---

## Example Data Display

### Sample Production Data
```
┌─────────────────────────────────────────┐
│ Production Summary for 24/08/2026       │
├─────────────────────────────────────────┤
│ Total Produced:        1250             │
│ OK Products:            620             │
│ NG Products:             50             │
│ OK Ratio:            92.5%              │
│ Current Product:      SP-001            │
│ Current Lot:          Lot-A             │
└─────────────────────────────────────────┘
```

### Sample Device Status
```
┌──────────────────────────────────────────────┐
│ Device Status Summary                        │
├──────────────────────────────────────────────┤
│ Device      │ Type         │ Status   │ Speed│
├─────────────┼──────────────┼──────────┼─────┤
│ BT01        │ Conveyor     │ RUNNING  │ 80% │
│ BT02        │ Conveyor     │ STOPPED  │ 0%  │
│ SORT1       │ Sorting      │ RUNNING  │ 90% │
│ SORT2       │ Sorting      │ RUNNING  │ 85% │
└──────────────────────────────────────────────┘
```

---

**Last Updated**: 2026-08-24
**Version**: 1.0 Production
