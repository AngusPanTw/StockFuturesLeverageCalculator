# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案概述

股票及期貨庫存槓桿比例計算機 — 基於 WPF (.NET 8.0) 的桌面應用程式，專為同時持有現股、融資與期貨部位的投資人設計。計算總曝險、淨資產、槓桿倍數，並提供風險狀態警示。無任何外部 NuGet 套件依賴。

## 建置與執行指令

```bash
dotnet build                # 建置
dotnet run                  # 執行應用程式
```

**發布單一執行檔 (win-x64)：**
```bash
dotnet publish -r win-x64 -c Release --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o ./PublishOutput
```

目前未設定測試或靜態分析指令。

## 架構 (MVVM)

- **Models/** — 資料模型：`StockItem`、`FutureItem`、`Portfolio`、`StockType` 列舉（Cash/Margin）、`PositionType` 列舉（Long/Short）
- **ViewModels/** — `MainViewModel` 統籌所有邏輯；`StockItemViewModel` 與 `FutureItemViewModel` 包裝 Model 並提供計算屬性與變更通知；`BaseViewModel` 提供 `INotifyPropertyChanged`；`RelayCommand` 實作 `ICommand`
- **Services/** — `PortfolioStorageService` 負責 JSON 持久化至 `portfolio.json`；`StockPriceService` 查詢 TWSE/TPEX 股票收盤價；`FuturesPriceService` 查詢 TAIFEX 期貨收盤價
- **Converters/** — `EnumToBooleanConverter` 用於 RadioButton 的資料繫結（支援 `StockType` 和 `PositionType`）
- **View** — `MainWindow.xaml` 為唯一視窗，三欄式佈局（股票 | 期貨 | 結算報告）

## 集合分割模式

股票和期貨各自拆分為兩個 `ObservableCollection`，載入/儲存時透過 `AllStocks`/`AllFutures` 聚合：

| 集合 | 用途 |
|------|------|
| `CashStocks` | 現股（全額交割）|
| `MarginStocks` | 融資股票 |
| `LargeFutures` | 大台（2000股/口）|
| `SmallFutures` | 小台（100股/口）|

`Portfolio` Model 儲存扁平的 `List<StockItem>` 和 `List<FutureItem>`，載入時依 `StockType`/`IsSmallContract` 分流至對應集合。

## 核心計算邏輯

**股票（StockItemViewModel）：**
- `TotalCost` = EntryPrice × Shares
- `MarketValue` = CurrentPrice × Shares
- `ProfitLoss` = (CurrentPrice - EntryPrice) × Shares
- 現股：`LoanAmount` = 0，`SelfFunded` = TotalCost，報酬率基底 = TotalCost
- 融資：`LoanAmount` = TotalCost × MarginRatio（預設 0.6），`SelfFunded` = TotalCost × (1 - MarginRatio)，報酬率基底 = SelfFunded

**槓桿公式（MainViewModel.RecalculateAll）：**
```
總曝險 = 股票總市值 + 期貨曝險
淨資產 = 現股市值 + (融資自備款 + 融資損益) + 可用資金 + 交割款 + 期貨權益金
槓桿倍數 = 總曝險 / 淨資產
```

**風險等級：** 穩定（≤1.0 倍）、適中（1.0–2.0 倍）、高風險（>2.0 倍）

## 收盤價查詢服務

### StockCode 欄位

`StockItem.StockCode` 和 `FutureItem.StockCode` 用於自動查價時比對 API 資料：
- **股票**：台股代號，如 `"2330"`、`"2317"`。先查 TWSE（上市），無資料再查 TPEX（上櫃）。
- **期貨**：`Contract` + `ContractMonth(Week)` 組合，如 `"DIF202603"`。也支援短格式 `"DIF2603"`（`NormalizeFuturesCode` 會補為 6 碼）。
- 欄位為空時，查價邏輯會自動跳過該筆。舊版 `portfolio.json` 不含此欄位，反序列化自動為 `""`，向後相容。

### API 資料來源

| 服務 | API 端點 | 說明 |
|------|----------|------|
| `StockPriceService` | TWSE `exchangeReport/STOCK_DAY` | 上市股票每日收盤價（`data` 陣列最後一筆 index 6） |
| `StockPriceService` | TPEX `afterTrading/tradingStock` | 上櫃股票每日收盤價（`tables[0].data` 最後一筆 index 6） |
| `FuturesPriceService` | TAIFEX `DailyMarketReportFut` | 一次回傳所有期貨行情，取 `Last` 欄位。過濾 `TradingSession="一般"` 且排除 `ContractMonth(Week)` 含 `/` 的轉倉資料 |

### 查詢流程（MainViewModel.ExecuteFetchAllPricesAsync）

1. 逐檔查詢股票（TWSE → TPEX fallback），每次查詢後延遲 2 秒（TWSE 頻率限制）
2. 同代號股票使用快取避免重複查詢
3. 一次呼叫 TAIFEX API 取得全部期貨行情，建立 `Dictionary<string, FuturesPriceResult>` 後批次比對
4. 更新 `CurrentPrice` 後自動觸發 `RecalculateAll()`

## 核心資料流

ViewModel 上的任何屬性變更皆會觸發 `MainViewModel.RecalculateAll()`，自動重新計算所有衍生欄位。關閉視窗時自動儲存投資組合，啟動時自動載入。

## 編碼規範

- 使用**明確的型別宣告**（非顯而易見的型別不使用 `var`）
- 避免巢狀三元運算子 — 改用 `switch` 或 `if/else`
- 清晰度優先於簡潔度
- 遵循 .NET 命名慣例
- 使用可為 Null 的參考型別（專案已啟用 `Nullable`）
- 適當使用 `async/await` 模式
- 保持 LINQ 查詢的可讀性

## 領域知識

- **台股慣例：** 獲利顯示紅色，虧損顯示綠色（與歐美市場相反）
- **融資：** 預設融資六成（券商借 60%）、自備四成（投資人出 40%）
- **期貨合約類型：** 大台 = 2000 股/口，小台 = 100 股/口
