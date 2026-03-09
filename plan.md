# 計畫書：將「合約代號自動查詢 + 批次匯入」功能移植到 main 分支

## 目標

在 main 分支加入：
1. **ContractMappingService** — 使用者新增期貨時只需輸入**股票代號 + 合約月份 + 大小型**，系統自動從 TAIFEX API 查出對應的期貨合約代號
2. **BatchImportWindow（期貨批次匯入）** — 從 feature 分支移植（已完成雙區塊改善）
3. **股票現貨批次匯入** — main 分支新功能

## 現況比較

### main 分支（目前）
- 使用者手動輸入完整期貨代號（如 `"DIF202603"`）和名稱
- `FutureItem` 欄位：`StockCode`（手動輸入）、`Name`（手動輸入）、`CostPrice`、`CurrentPrice`、`Lots`、`Position`、`IsSmallContract`
- `ExecuteAddFuture()` 是同步方法，直接建立 `FutureItem` 放入集合
- UI 提供代號 + 名稱兩個 TextBox，讓使用者自行填入

### feature/2317forever-futures 分支（來源）
- 使用者輸入**股票代號**（如 `"2330"`）+ 選擇**年/月** + 勾選**大小型**
- 系統呼叫 `ContractMappingService.GetContractInfoAsync()` 自動查出合約代號和名稱
- `FutureItem` 新增欄位：`UnderlyingStockCode`、`ContractMonth`、`GroupName`
- `FutureItem` 移除欄位：`CostPrice`
- `ExecuteAddFuture` 改為 async（`ExecuteAddFutureFireAndForget`）
- `BatchImportWindow` 已改善為雙區塊設計（大型/小型合約分開輸入、年月 ComboBox）

---

## 已完成（feature 分支）

### ~~Part B-feature：改善 BatchImportWindow~~  ✅ 已完成並推送

commit `a7c3b3e` — `refactor: 批次匯入改為雙區塊設計`

已完成的改動：
- 單一 TextBox → 大型/小型合約兩個並排 GroupBox，使用者不再需要手打「大」「小」
- 年月從文字解析 → 上方 ComboBox 下拉選擇
- 格式說明更新，加上「股票代號（空白鍵或 Tab）口數」提示
- 修正：錯誤訊息不清除、Placeholder 對齊、Focus 時機

---

## 待執行（main 分支）

### Part A：合約代號自動查詢

#### 步驟 A1：新增 `ContractMappingService`
- **新增檔案**：`Services/ContractMappingService.cs`（含 `ContractInfo` 類別）
- 直接從 feature 分支複製，無需修改
- 功能：呼叫 TAIFEX `SingleStockFuturesMargining` API，查詢股票代號對應的期貨合約代號
- 有記憶體快取機制，程式啟動後只查一次 API

#### 步驟 A2：修改 `FutureItem` 模型
- **修改檔案**：`Models/FutureItem.cs`
- 新增 `UnderlyingStockCode`（標的股票代號）欄位
- 新增 `ContractMonth`（合約月份）欄位
- **保留** `CostPrice` 欄位（main 有損益計算需要）
- `StockCode` 改為由系統自動填入（不再讓使用者手動輸入）
- `Name` 改為由 API 自動填入

#### 步驟 A3：修改 `MainViewModel` 輸入屬性
- **修改檔案**：`ViewModels/MainViewModel.cs`
- 注入 `ContractMappingService` 欄位
- 將 `NewFutureStockCode` 替換為 `NewUnderlyingStockCode`（股票代號輸入）
- 新增 `NewContractYear`（年份 ComboBox）與 `NewContractMonthNum`（月份 ComboBox）屬性
- 新增 `AvailableContractYears` 集合（自動產生近 5 年）
- 移除 `NewFutureName`（名稱改為 API 自動填入，不需手動輸入）
- 新增 `ValidateNewFutureInputs()` 驗證方法
- 新增 `NormalizeContractMonth()` 輔助方法（4 碼 → 6 碼月份）

#### 步驟 A4：修改 `ExecuteAddFuture` 為 async
- **修改檔案**：`ViewModels/MainViewModel.cs`
- `ExecuteAddFuture` → `ExecuteAddFutureFireAndForget`（async void，含 try-catch）
- 內部呼叫新的 `AddFutureAsync()` 方法
- 流程：驗證輸入 → 呼叫 `ContractMappingService` → 組合代號 → 建立 VM → 加入集合
- 查詢失敗時顯示使用者友善的錯誤訊息（透過 `PriceUpdateStatus`）

#### 步驟 A5：修改 UI（MainWindow.xaml）
- **修改檔案**：`MainWindow.xaml`
- 期貨新增表單區域：
  - 「代號」TextBox → 改綁定 `NewUnderlyingStockCode`（輸入股票代號如 2330）
  - 移除「名稱」TextBox（改為自動填入）
  - 新增「年」ComboBox（綁定 `AvailableContractYears` / `NewContractYear`）
  - 新增「月」ComboBox（綁定 `NewContractMonthNum`，01~12）
  - 保留口數、多空、成本價、現價、小型合約等欄位不變

#### 步驟 A6：更新收盤價流程補上合約代號查詢
- **修改檔案**：`ViewModels/MainViewModel.cs`
- 在 `UpdateAllPricesAsync` 中，對 `StockCode` 為空的期貨項目，先用 `ContractMappingService` 補查代號

---

### Part B-main：期貨批次匯入移植到 main

#### 步驟 B1：複製 `BatchImportWindow` 到 main
- **新增檔案**：`BatchImportWindow.xaml` + `BatchImportWindow.xaml.cs`
- 從 feature 分支複製已改善的雙區塊版本
- **main 適配**：批次匯入格式需多一欄**進場價格**（main 有損益計算）
  - feature 格式：`股票代號  口數`
  - main 格式：`股票代號  口數  進場價格`
  - Placeholder 範例同步更新

#### 步驟 B2：MainViewModel 加入批次匯入邏輯
- **修改檔案**：`ViewModels/MainViewModel.cs`
- 從 feature 分支移植 `BatchImportAsync()` 方法
- 新增 `BatchImportCommand` 與 `ExecuteBatchImport()` 方法
- 適配 main 分支的資料結構（保留 `CostPrice` 等欄位）

#### 步驟 B3：UI 加入批次匯入按鈕
- **修改檔案**：`MainWindow.xaml` + `MainWindow.xaml.cs`
- 期貨區域新增「批次匯入」按鈕
- 按鈕觸發開啟 `BatchImportWindow` 模態對話框

---

### Part C：股票現貨批次匯入（main 新功能）

#### 步驟 C1：新增 `StockBatchImportWindow`
- **新增檔案**：`StockBatchImportWindow.xaml` + `StockBatchImportWindow.xaml.cs`
- 同樣採用雙區塊設計（現股/融資分開輸入）
- 每行：`股票代號  股數  進場均價`（空白或 Tab 分隔）

#### 步驟 C2：MainViewModel 加入股票批次匯入邏輯
- **修改檔案**：`ViewModels/MainViewModel.cs`
- 新增 `StockBatchImportCommand` 與對應方法

#### 步驟 C3：UI 加入股票批次匯入按鈕
- **修改檔案**：`MainWindow.xaml` + `MainWindow.xaml.cs`
- 股票區域新增「批次匯入」按鈕

---

## 不移植的功能

| 功能 | 原因 |
|------|------|
| 移除股票模組（StockItem 等） | main 仍需股票功能 |
| 移除 CostPrice 欄位 | main 有損益計算需要 |
| FutureGroupViewModel 族群分群 | 獨立功能，不在此次範圍 |
| BooleanRadioConverter | 搭配族群功能使用 |
| 移除 StockPriceService | main 仍需股票收盤價查詢 |
| UI 兩欄重構 | 版面大改動，不在此次範圍 |

## 資料相容性

- **不做舊資料相容** — 使用者更新後重新透過批次匯入輸入所有庫存
- 舊的 `portfolio.json` 可直接刪除重來

## 預估影響

- **新增** 5 個檔案：
  - `Services/ContractMappingService.cs`
  - `BatchImportWindow.xaml` + `.xaml.cs`
  - `StockBatchImportWindow.xaml` + `.xaml.cs`
- **修改** 3 個檔案：
  - `Models/FutureItem.cs`
  - `ViewModels/MainViewModel.cs`
  - `MainWindow.xaml` + `.xaml.cs`
- 不影響現有股票損益計算功能
- 不影響期貨收盤價查詢功能（`FuturesPriceService` 不需修改）
- 需要網路連線才能查詢合約代號（查詢失敗有錯誤提示）

## 執行順序

1. ~~Part B-feature（改善 BatchImportWindow）~~ ✅ 已完成
2. Part A（合約代號自動查詢）→ 在 main 分支實作核心功能
3. Part B-main（期貨批次匯入）→ 移植到 main，加上進場價格欄位
4. Part C（股票批次匯入）→ 獨立功能
