# A4 RoleDef 擴充＋debug actions ＋ A5 相容性驗證

## A4 — `OfficerRoleDef` 擴充＋quota＋debug（0.25d）

**Modify:** `Source/Defs/OfficerRoleDef.cs`、`Source/Dev/OfficerDebugActions.cs`、
`Defs/OfficersDefs/Roles.xml`、`Languages/*/Keyed/NamedOfficers.xml`

### RoleDef 新欄位（role 即官職、疊加不另立 Def——調查 P 定案）

```csharp
public enum RoleScope { Settlement, Warband, Faction }   // 朝廷職=Faction+assignedTo null

public class OfficerRoleDef : Def
{
    public int displayPriority;          // 既有
    public bool leaderLike;              // 既有
    public int rank;                     // 品階（新）：升遷排序/俸祿基準（S2 接 P3）；大者高
    public RoleScope scope = RoleScope.Settlement;   // 新：UI 分組/AI 配對域
    public int quotaPerFaction;          // 新：0=不限；>0 每派系同職在任上限
}
```

- 升遷＝`SetRole` 換 rank 更高的 def；朝廷職＝`scope=Faction` 且 `assignedTo=null`
  （status 仍 Serving——A1 不變式表已涵蓋）。
- **quota 執行點**＝`EmployOfficer`/`SetRole`/`CreateOfficer`（A3）：
  `quotaPerFaction>0` 時計數 `GetByFaction(f)` 中 `role==r && status==Serving && !dead`，
  滿 → 回 false/null。線性掃（同 A3 決策）。
- XML：`pas_officers_Generic` 補 `<rank>1</rank><scope>Settlement</scope>`；
  新增測試用 `pas_officers_GenericSenior`（rank 3，dev 升遷測試）。
  P1 `pas_warband_General`、P2 `pas_settlement_Lord` 的 XML **由 P1/P2 自管**——
  新欄位皆有預設值，**不加欄位也照常載入**（A5 零改動重編的根據）。

### debug actions 增量（驗收介面，沿 P0 慣例全走 OfficersApi）

| action | 行為 |
|---|---|
| `Create idle officer at selected` | 選中聚落 → `CreateIdleOfficer(settlement)` |
| `Employ first idle to selected` | 第一個全域 Idle record → `EmployOfficer(r, host.Faction, host, Generic)` |
| `Release first officer (selected)` | 選中物件首官 → `ReleaseOfficer` |
| `Promote first officer (selected)` | `SetRole(r, GenericSenior)` |
| `Orphan-simulate first officer` | 直呼 registry OrphanFlow（繞過 defeated 等心跳） |
| `Dump idle by faction` | 逐派系列 `GetUnaffiliated` 數量＋名單 |
| `Preroll name (first idle)` | `OfficerNamer.EnsureNameCached` → log 名字（UI 前置驗證） |
| 既有 `Dump officer registry` | 增列 `status/home/appointedTick/historyCount` |
| 既有 `Toggle event log listeners` | 加掛 Orphaned/Employed/Released 三事件 |
| 既有 `API null-safety probe` | 補探 8 個新成員（null 參數全不炸） |

**驗證（A4）**：build＋healthcheck（healthcheck 若有 defName 交叉引用檢查，新 def 入掃）；
dev 全表跑一遍無紅字。

## A5 — 相容性驗證：P1/P2/P3 零改動重編（0.25d）

S0 完成宣告前的硬閘。**改動全是增量**（欄位有預設、簽章不變、事件只加不改），
理論上零改動重編＝綠；本任務把「理論」變「驗過」。

### 步驟

1. `named-officers`：`dotnet build -c Release` 0 警告 0 錯誤 → 產出新 DLL。
2. **不改一行**重編三個消費 mod（HintPath 指向新 DLL）：
   ```bash
   dotnet build warband-generals/Source/WarbandGenerals.csproj -c Release
   dotnet build settlement-lords/Source/SettlementLords.csproj -c Release
   dotnet build city-economy/Source/*.csproj -c Release   # P3 若 ref P0 才需要；不 ref 則跳過（記入 log）
   ```
   全部 0 警告 0 錯誤。
3. 各 mod healthcheck（存在者）通過。
4. 行為相容心智檢查（列入 99-E2E 實機項）：
   - P2 `LordAppointer.Scan` 照常 CreateOfficer（quota=0 不擋、status 自動 Serving）。
   - P1/P2 Heal 對 dead/destroyed 路徑行為不變（本階段尚未加 interceptor）。
   - **已知行為變更（預期、非回歸）**：派系敗亡後 record 不再消失而是轉在野——
     P1/P2 的 `GetById` 仍解析、binding heal 各自處置；在 99-E2E 驗證無紅字。
5. 文件回寫：
   - `named-officers/PROJECT.md`：API 契約表補 8 新成員＋三事件＋RoleDef 新欄位＋
     不變式表＋「禁 Remove+Create」警語；E2E checklist 補 A 系驗收項。
   - 本計畫各檔如有 spike 修正 → 回填修正框。

**驗證（A5）**：上述 1–3 命令輸出乾淨即過；4 留待 99-E2E 簽收。
