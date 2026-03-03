# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案概述

股票期貨庫存追蹤器 — 基於 WPF (.NET 8.0) 的桌面應用程式，追蹤台灣股票期貨庫存的市值與族群分佈。此為 `feature/2317forever-futures` 分支，功能獨立於 `main` 分支。僅追蹤市值，不計算損益。無任何外部 NuGet 套件依賴。

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

- **Models/** — `FutureItem`（期貨項目）、`Portfolio`（含 `Futures` + `GroupNames`）、`PositionType` 列舉（Long/Short）
- **ViewModels/** — `MainViewModel` 統籌所有邏輯；`FutureItemViewModel` 包裝 FutureItem 提供市值計算；`FutureGroupViewModel` 管理群組與族群市值；`BaseViewModel` 提供 `INotifyPropertyChanged` 與 `FormatWan()`；`RelayCommand` 實作 `ICommand`
- **Services/** — `PortfolioStorageService` 持久化至 `portfolio.json`；`ContractMappingService` 查詢 TAIFEX 合約代號對照；`FuturesPriceService` 查詢 TAIFEX 期貨收盤價
- **Converters/** — `EnumToBooleanConverter`（PositionType RadioButton）；`BooleanRadioConverter`（bool RadioButton，用於 IsSmallContract）
- **View** — `MainWindow.xaml`（主視窗）+ `BatchImportWindow.xaml`（批次匯入對話框）

## 資料模型

### FutureItem
| 欄位 | 型別 | 說明 |
|------|------|------|
| `UnderlyingStockCode` | string | 標的股票代號（如 `"2330"`） |
| `ContractMonth` | string | 合約月份（如 `"2603"`） |
| `StockCode` | string | 完整期貨代號（如 `"CDF202603"`，自動組合） |
| `Name` | string | 合約名稱（自動從 API 填入） |
| `Lots` | int | 持倉口數 |
| `Position` | PositionType | 多空方向 |
| `CurrentPrice` | decimal | 目前市價 |
| `IsSmallContract` | bool | 是否為小型合約 |
| `GroupName` | string | 所屬群組（空字串 = 未分群） |

### Portfolio
- `Futures: List<FutureItem>` — 所有期貨項目
- `GroupNames: List<string>` — 群組名稱列表
- 分群透過 `FutureItem.GroupName` 直接關聯

### BatchImportItem（BatchImportWindow 內部類別）
| 欄位 | 型別 | 說明 |
|------|------|------|
| `StockCode` | string | 股票代號 |
| `Lots` | int | 口數 |
| `IsSmallContract` | bool | 是否為小型合約 |
| `Year` | string | 西元年後兩碼（如 `"26"`） |
| `Month` | string | 月份兩碼（如 `"03"`） |
| `ContractMonth` | string (計算) | `Year + Month`（如 `"2603"`） |

## 核心計算

- `SharesPerLot` = IsSmallContract ? 100 : 2000
- `Exposure` = CurrentPrice × SharesPerLot × Lots
- `FormatWan()`: 整數萬 → `1,234萬`，有餘數 → `345.5萬`
- `RecalculateAll()`: 加總所有 Exposure/Lots + `RefreshGroups()`

## 分群機制

- `FutureGroups` + `UngroupedFutures` 兩個 ObservableCollection
- 互斥：每檔期貨只屬於一個群組
- `RefreshGroups()` 在每次 RecalculateAll 時重建
- 指派方式：拖曳（DataGrid → 群組卡片）或未分群區域的 ComboBox 下拉選群組

## API 流程

### ContractMappingService
- 端點: `openapi.taifex.com.tw/v1/SingleStockFuturesMargining`
- 從股票代號查期貨合約代號，以 `ContractName` 含「小型」區分大小型
- 回傳 `ContractInfo`（`ContractCode` + `ContractName`）或 null

### FuturesPriceService
- 端點: TAIFEX `DailyMarketReportFut`
- 一次取全部行情，取 `Last` 欄位，過濾 `TradingSession="一般"`（排除含 `/` 的週契約）
- 回傳 `FuturesBatchResult`（`Success` + `Prices` 字典 + `ErrorMessage`）
- 價格字典 key 格式: `"DIF202603"`（Contract + ContractMonth）

### 新增期貨流程
輸入 → `ValidateNewFutureInputs()` → `ContractMappingService` 查詢 → 組合 StockCode → 失敗顯示錯誤訊息

### 批次匯入流程
BatchImportWindow 解析文字 → `BatchImportItem` 列表 → `BatchImportAsync()` 逐筆呼叫 ContractMappingService → 回報成功/失敗筆數

### 更新收盤價流程
補齊缺少的 StockCode → `GetAllFuturesPricesAsync` → `NormalizeFuturesCode()` 正規化代號 → 批次比對更新 → 顯示結果與資料日期

### 輔助方法
- `NormalizeFuturesCode(code)`: 將 4 碼月份展開為 6 碼（如 `"KUF2603"` → `"KUF202603"`）
- `FormatTaifexDate(rawDate)`: 將 `"20260224"` 格式化為 `"2026/02/24"`

## UI 佈局

### MainWindow（兩欄佈局）
- **Row 0（橫跨兩欄）**: 總市值橫幅（金色背景）
- **Row 1 左欄**:
  - 新增表單 GroupBox（代號、年/月 ComboBox、口數、大小型/多空 RadioButton）
  - 「新增股票期貨」+ 「批次匯入」按鈕
  - DataGrid（名稱、代號、類型、口數、方向、市價、市值、刪除）
  - 底部摘要區（總市值、總口數、更新收盤價按鈕、狀態訊息）
- **Row 1 右欄**:
  - 群組管理 GroupBox（新增群組名稱）
  - 底部總市值橫幅（金色）
  - ScrollViewer 內含群組卡片（藍底）+ 未分群區域（灰底）

### BatchImportWindow（模態對話框）
- 格式說明區（藍框）
- 多行文字輸入區（含浮水印範例）
- 錯誤訊息區（紅字，預設隱藏）
- 匯入 / 取消按鈕

## UI 互動

- **Enter 鍵新增期貨**：`AddFutureArea_PreviewKeyDown` 隧道事件，新增後聚焦回代號欄位
- **Enter 鍵新增群組**：`GroupNameArea_PreviewKeyDown` 同樣支援 Enter 鍵
- **年/月選擇**：ComboBox 下拉，年份自動產生從當前年起 5 年範圍
- **輸入驗證**：`ValidateNewFutureInputs()` 回傳 null 或錯誤訊息
- **拖曳分群**：WPF 原生 DragDrop API（DataGrid → 群組卡片 / 未分群區域）
- **ComboBox 分群**：未分群項目旁的下拉選單，`GroupComboBox_SelectionChanged` 觸發 `MoveItemToGroup()`
- **TextBox 自動全選**：全域 `GotFocus` 事件處理，聚焦時自動選取文字
- **狀態顯示**：`PriceUpdateStatus` 屬性顯示 API 操作結果；`IsUpdatingPrices` 控制按鈕停用

### 批次匯入格式
```
大                    ← 切換為大型合約（預設）
小                    ← 切換為小型合約
26年03月              ← 設定合約年月
2330 5                ← 股票代號 口數（空白分隔）
2454 3
```
- 方向全部預設為多單（Long）
- 年份為西元年後兩碼

## 持久化

- 檔案：`portfolio.json`（位於 `AppContext.BaseDirectory`）
- 關閉視窗自動儲存（`OnClosing` → `SaveData()`），啟動自動載入（建構函式 → `LoadData()`）

## 編碼規範

- 明確型別宣告（非顯而易見的型別不使用 `var`）
- 避免巢狀三元運算子
- 清晰度優先於簡潔度
- .NET 命名慣例 + Nullable 參考型別
- 空字串一律使用 `string.Empty`

## 領域知識

- **台股慣例：** 獲利紅色，虧損綠色
- **期貨合約：** 大型 = 2000 股/口，小型 = 100 股/口
- **市值單位：** 以「萬」為最小單位顯示
