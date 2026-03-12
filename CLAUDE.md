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
- **Services/** — `PortfolioStorageService` 負責 JSON 持久化至 `portfolio.json`；`StockPriceService` 查詢 TWSE/TPEX 股票收盤價；`FuturesPriceService` 查詢 TAIFEX 期貨收盤價；`StockInfoService` 統一管理股票名稱查詢（TSE/TPEX）與期貨合約代號查詢（TAIFEX）
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

**萬元顯示：** ViewModel 提供 `*Wan` 屬性（`MarketValueWan`、`ProfitLossWan`、`ExposureWan`），將數值除以 10,000 並四捨五入至小數一位，供 DataGrid 欄位繫結使用。

## StockInfoService（股票資訊統一查詢服務）

統一管理三份快取，啟動時以 `Task.WhenAll` 並行預熱：

| 快取 | API 來源 | 資料結構 | 用途 |
|------|----------|----------|------|
| TSE 上市名稱表 | `STOCK_DAY_AVG_ALL` | `Dictionary<Code, Name>` | 現股名稱查詢 |
| TPEX 上櫃名稱表 | `tpex_mainboard_daily_close_quotes` | `Dictionary<Code, Name>` | 現股名稱查詢 |
| TAIFEX 股期對應表 | `SingleStockFuturesMargining` | `List<JsonElement>` | 期貨合約代號/名稱查詢 |

- `GetStockNameAsync(stockCode)` — 查 TSE → TPEX fallback，涵蓋全市場上市+上櫃股票
- `GetContractInfoAsync(stockCode, isSmallContract)` — 查 TAIFEX，以股票代號查詢期貨合約代號（如 `"2330"` → `"CDF"`）
- 啟動時透過 `PreloadCacheAsync()` 預載快取（fire-and-forget），使用 `SemaphoreSlim` 確保執行緒安全

## 批次匯入

- **期貨批次匯入**（`BatchImportWindow`）：大型/小型合約分區輸入，每行格式 `代號 口數 成本價`，選擇年月後匯入。透過 `StockInfoService.GetContractInfoAsync` 自動查詢合約代號與名稱。
- **股票批次匯入**（`StockBatchImportWindow`）：現股/融資分區輸入，每行格式 `代號 股數 進場價`，透過 `StockInfoService.GetStockNameAsync` 自動查詢股票名稱（TSE/TPEX 全市場涵蓋）。
- 支援部分成功：驗證失敗的行會標示錯誤，成功的行仍可匯入。

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
