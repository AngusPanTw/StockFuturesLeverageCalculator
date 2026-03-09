# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案概述

股票及期貨庫存槓桿比例計算機 — 基於 WPF (.NET 8.0) 的桌面應用程式，專為同時持有現股、融資與期貨部位的投資人設計。計算總曝險、淨資產、槓桿倍數。無任何外部 NuGet 套件依賴。

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
- **Services/** — `PortfolioStorageService` 負責 JSON 持久化至 `portfolio.json`；`StockPriceService` 查詢 TWSE/TPEX 股票收盤價；`FuturesPriceService` 查詢 TAIFEX 期貨收盤價；`ContractMappingService` 查詢 TAIFEX 合約代號與股票名稱對應
- **Converters/** — `EnumToBooleanConverter` 用於 RadioButton 的資料繫結（支援 `StockType` 和 `PositionType`）
- **View** — `MainWindow.xaml` 為唯一視窗，2×3 Grid 佈局（Row 0: 總資產+公式說明 | Row 1: 股票庫存+期貨庫存 | 結算報告跨兩行）；`BatchImportWindow` 期貨批次匯入對話框；`StockBatchImportWindow` 股票批次匯入對話框

## 集合分割模式

股票和期貨各自拆分為兩個 `ObservableCollection`，載入/儲存時透過 `AllStocks`/`AllFutures` 聚合：

| 集合 | 用途 |
|------|------|
| `CashStocks` | 現股（全額交割）|
| `MarginStocks` | 融資股票 |
| `LargeFutures` | 大型合約（2000股/口）|
| `SmallFutures` | 小型合約（100股/口）|

`Portfolio` Model 儲存扁平的 `List<StockItem>` 和 `List<FutureItem>`，載入時依 `StockType`/`IsSmallContract` 分流至對應集合。

## 核心計算邏輯

**股票（StockItemViewModel）：**
- `TotalCost` = EntryPrice × Shares
- `MarketValue` = CurrentPrice × Shares
- `ProfitLoss` = (CurrentPrice - EntryPrice) × Shares
- 現股：`SelfFunded` = TotalCost，報酬率基底 = TotalCost
- 融資：`SelfFunded` = TotalCost × (1 - MarginRatio)，報酬率基底 = SelfFunded

**槓桿公式（MainViewModel.RecalculateAll）：**
```
總曝險 = 股票總市值 + 期貨曝險
淨資產 = 現股市值 + (融資自備款 + 融資損益) + 可用資金 + 交割款 + 期貨權益金
槓桿倍數 = 總曝險 / 淨資產
```

## ContractMappingService（合約代號查詢服務）

透過 TAIFEX `SingleStockFuturesMargining` API 查詢股票代號與合約代號的對應關係：
- `GetContractInfoAsync(stockCode, isSmallContract)` — 以股票代號（如 `"2330"`）查詢合約代號（如 `"CDF"`）
- `GetStockNameAsync(stockCode)` — 從合約名稱中擷取股票名稱（移除「小型」和「期貨」字樣）
- 啟動時透過 `PreloadCacheAsync()` 預載快取（fire-and-forget），使用 `SemaphoreSlim` 確保執行緒安全

## 批次匯入

- **期貨批次匯入**（`BatchImportWindow`）：大型/小型合約分區輸入，每行格式 `代號 口數 成本價`，選擇年月後匯入。透過 `ContractMappingService` 自動查詢合約代號與名稱。
- **股票批次匯入**（`StockBatchImportWindow`）：現股/融資分區輸入，每行格式 `代號 股數 進場價`，自動查詢股票名稱。
- 支援部分成功：驗證失敗的行會標示錯誤，成功的行仍可匯入。

## 收盤價查詢服務

### StockCode 欄位

`StockItem.StockCode` 和 `FutureItem.StockCode` 用於自動查價時比對 API 資料：
- **股票**：台股代號，如 `"2330"`、`"2317"`。先查 TWSE（上市），無資料再查 TPEX（上櫃）。
- **期貨**：`Contract` + `ContractMonth(Week)` 組合，如 `"DIF202603"`。也支援短格式 `"DIF2603"`（`NormalizeFuturesCode` 會補為 6 碼）。
- 欄位為空時，查價邏輯會透過 `ContractMappingService` 自動補齊合約代號。舊版 `portfolio.json` 不含此欄位，反序列化自動為 `""`，向後相容。

### FutureItem 新增欄位

- `UnderlyingStockCode` — 標的股票代號（如 `"2330"`），用於查詢合約代號
- `ContractMonth` — 合約年月（如 `"2603"`），與查詢到的合約代號組合產生 `StockCode`
- `Name` — 合約名稱（如 `"台積電期貨"`），自動從 TAIFEX API 填入

### API 資料來源

| 服務 | API 端點 | 說明 |
|------|----------|------|
| `StockPriceService` | TWSE `exchangeReport/STOCK_DAY` | 上市股票每日收盤價（`data` 陣列最後一筆 index 6） |
| `StockPriceService` | TPEX `afterTrading/tradingStock` | 上櫃股票每日收盤價（`tables[0].data` 最後一筆 index 6） |
| `FuturesPriceService` | TAIFEX `DailyMarketReportFut` | 一次回傳所有期貨行情，取 `Last` 欄位。過濾 `TradingSession="一般"` 且排除 `ContractMonth(Week)` 含 `/` 的轉倉資料 |
| `ContractMappingService` | TAIFEX `SingleStockFuturesMargining` | 查詢股票代號與合約代號對應，支援大型/小型合約區分 |

### 查詢流程（MainViewModel.ExecuteFetchAllPricesAsync）

1. 逐檔連續查詢股票（TWSE → TPEX fallback），無預設延遲；單筆查詢失敗時自動重試（漸進式延遲 2 秒、4 秒，最多 2 次）
2. 同代號股票使用快取避免重複查詢
3. 期貨若缺少 `StockCode`，自動透過 `ContractMappingService` 查詢合約代號並補齊
4. 一次呼叫 TAIFEX API 取得全部期貨行情，建立 `Dictionary<string, FuturesPriceResult>` 後批次比對
5. 更新 `CurrentPrice` 後自動觸發 `RecalculateAll()`

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
- **期貨合約類型：** 大型合約 = 2000 股/口，小型合約 = 100 股/口
