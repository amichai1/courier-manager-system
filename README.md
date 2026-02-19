# Courier Manager System

A delivery management system inspired by platforms like Wolt, featuring an **Admin Dashboard** and a **Courier Interface**, built with WPF (.NET 8) in a clean 3-tier architecture.

[![Platform](https://img.shields.io/badge/platform-Windows%20WPF-blue)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/architecture-3--Tier%20(DAL%2FBL%2FPL)-orange)]()

---

## Features

### Admin Dashboard
- Full CRUD for couriers and orders
- Real-time order tracking with status indicators (On Time / At Risk / Late)
- Courier salary calculation based on deliveries, distance, and vehicle type
- Database initialization and reset controls
- Delivery history viewer with filtering

### Courier Interface
- Login with ID and password
- View and accept available orders sorted by distance
- Pick up and deliver orders with status transitions
- Route distance calculation (Haversine formula + optional geocoding)

### Simulation Engine
- Background clock simulator with configurable time intervals
- Automated order lifecycle: creation, assignment, pickup, delivery
- Realistic delivery timing based on distance, vehicle type, and randomized delays
- Thread-safe observer pattern for real-time UI updates

---

## Architecture

```
┌─────────────────────────────────────────────┐
│  PL (Presentation Layer) — WPF / XAML       │
│  Custom controls, converters, validation    │
├─────────────────────────────────────────────┤
│  BL (Business Logic Layer)                  │
│  Order/Courier/Delivery managers            │
│  Salary calculation, simulation engine      │
│  Observer pattern, async operations         │
├─────────────────────────────────────────────┤
│  DAL (Data Access Layer)                    │
│  DalList (in-memory) │ DalXml (XML files)  │
│  Configurable via DalFacade abstraction     │
└─────────────────────────────────────────────┘
```

| Project | Description |
|---------|-------------|
| `PL` | WPF presentation layer with MVVM-style bindings |
| `BL` | Business logic, managers, helpers, simulation |
| `BO` | Business objects (Order, Courier, Delivery, etc.) |
| `DalFacade` | DAL interfaces and data objects (DO) |
| `DalList` | In-memory DAL implementation (Lazy Singleton) |
| `DalXml` | XML-based DAL implementation |
| `DalTest` | Console app for DAL testing |
| `BlTest` | Console app for BL testing |

---

## Tech Stack

- **Framework**: .NET 8, WPF (Windows Presentation Foundation)
- **Language**: C# 12
- **Data**: XML persistence / In-memory collections
- **UI**: Custom `ControlTemplate`s, `IValueConverter`s, `DataTrigger`s, vector graphics in XAML
- **Concurrency**: `Task.Run`, `Dispatcher.BeginInvoke`, `Lazy<T>`, mutex-based thread safety
- **Pattern**: Observer pattern for real-time UI updates, Factory pattern for DAL abstraction

---

## Getting Started

### Prerequisites
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022+ (recommended) or `dotnet` CLI

### Build & Run

```bash
# Clone the repository
git clone https://github.com/amichai1/CourierManagerSystem.git
cd CourierManagerSystem

# Build
dotnet build CourierManagerSystem.sln

# Run the WPF application
dotnet run --project PL
```

### Default Credentials
- **Manager ID**: `123456789`
- **Manager Password**: `123456789`

> These are demo credentials for development/testing purposes only.

---

## Key Design Patterns

| Pattern | Usage |
|---------|-------|
| **Observer** | Real-time UI updates across windows when data changes (e.g., order status, courier location) |
| **Factory** | `DalFacade` abstraction allows swapping between `DalList` and `DalXml` without changing BL code |
| **Singleton** | Thread-safe `Lazy<T>` instances for DAL and BL layers |
| **Mutex** | Custom `ObserverMutex` prevents concurrent UI updates from causing race conditions |
| **Async/Await** | Non-blocking operations with `Task.Run` and `Dispatcher.BeginInvoke` for responsive UI |

---

## License

This project is provided as-is for educational and portfolio purposes.
